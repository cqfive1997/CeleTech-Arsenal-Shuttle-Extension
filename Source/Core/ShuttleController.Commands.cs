using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        private const int MaxPendingAssemblyEvents = 16;

        private readonly Queue<ShuttleAssemblyEvent> pendingAssemblyEvents =
            new Queue<ShuttleAssemblyEvent>();
        private int nextAssemblyEventSequence;

        public ShuttleCommandResult Execute(IShuttleCommand command)
        {
            // UI still talks to one authoritative controller command boundary, but subsystem
            // handlers now own the assembly/cargo/launch command details.
            ShuttleCommandResult result = this.commandDispatcher.Execute(command, this.BuildCommandContext());
            if (result != null && result.Success)
            {
                this.InvalidateCargoInventorySnapshotCaches();
                this.MarkRefrigeratedCargoRegistryDirty();
                this.ReconcileRefrigeratedCargoRegistryIfNeeded(this.GetTicksGameSafe());
                this.EnqueueAssemblyEventForSuccessfulCommand(command);
            }

            return result;
        }

        internal bool TryDequeueAssemblyEvent(out ShuttleAssemblyEvent assemblyEvent)
        {
            if (this.pendingAssemblyEvents.Count <= 0)
            {
                assemblyEvent = null;
                return false;
            }

            assemblyEvent = this.pendingAssemblyEvents.Dequeue();
            return true;
        }

        private void EnqueueAssemblyEventForSuccessfulCommand(IShuttleCommand command)
        {
            InstallSegmentCommand installSegment = command as InstallSegmentCommand;
            if (installSegment == null)
            {
                return;
            }

            int ticksGame = this.GetTicksGameSafe();
            this.nextAssemblyEventSequence++;
            string eventID =
                "segment-installed:" +
                (installSegment.SegmentSlotID ?? string.Empty) +
                ":" +
                (installSegment.SegmentDefName ?? string.Empty) +
                ":" +
                ticksGame.ToString() +
                ":" +
                this.nextAssemblyEventSequence.ToString();

            this.pendingAssemblyEvents.Enqueue(new ShuttleAssemblyEvent(
                ShuttleAssemblyEventKind.SegmentInstalled,
                eventID,
                installSegment.SegmentSlotID,
                installSegment.SegmentDefName,
                this.ResolveInstalledSegmentKindKey(installSegment.SegmentDefName),
                ticksGame));

            while (this.pendingAssemblyEvents.Count > MaxPendingAssemblyEvents)
            {
                this.pendingAssemblyEvents.Dequeue();
            }
        }

        private string ResolveInstalledSegmentKindKey(string segmentDefName)
        {
            if (string.IsNullOrEmpty(segmentDefName))
            {
                return null;
            }

            ShuttleSegmentBaseDef segmentDef =
                DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(segmentDefName);
            if (segmentDef == null)
            {
                return null;
            }

            string kindKey = ShuttleSegmentTypeCatalog.ToTypeID(segmentDef.SegmentType);
            return kindKey == ShuttleSegmentTypeCatalog.Unknown ? null : kindKey;
        }

        internal bool CanInstallModuleForUI(
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            failureReason = null;
            if (this.assemblyMutationController == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            return this.assemblyMutationController.CanInstallModuleForUI(
                this.assemblyState,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out failureReason);
        }

        internal ShuttleCommandResult AddAssemblyConstructionWorkFromPawn(Pawn pawn, int workTicks)
        {
            return this.assemblyConstructionSystem.AddConstructionWork(
                this.BuildCommandContext(),
                pawn,
                workTicks);
        }

        internal bool IsReadyForModuleRemovalWork()
        {
            this.EnsureRuntimeState();
            return this.moduleRemovalService.IsReadyForRemovalWork(this.runtimeState);
        }

        internal ShuttleCommandResult AddActiveModuleRemovalWorkFromPawn(Pawn pawn, int workTicks)
        {
            this.EnsureRuntimeState();
            ShuttleModuleRemovalState state = this.runtimeState.ModuleRemoval;
            ShuttleModuleRemovalRecord record = state != null ? state.ActiveRecord : null;
            if (record == null || !record.IsActive)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalNoneActive".Translate().ToString());
            }

            return this.moduleRemovalService.AddRemovalWork(
                this.BuildCommandContext(),
                pawn,
                workTicks);
        }

        private ShuttleCommandContext BuildCommandContext()
        {
            // Command contexts hold stable controller seams and mutable state references.
            // Rebuild only when host binding or the assembly state object changes.
            if (this.cachedCommandContext != null &&
                object.ReferenceEquals(this.cachedCommandContextHost, this.shuttleHost) &&
                object.ReferenceEquals(this.cachedCommandContextBuildingHost, this.shuttleBuildingHost) &&
                object.ReferenceEquals(this.cachedCommandContextAssemblyState, this.assemblyState))
            {
                return this.cachedCommandContext;
            }

            ShuttleControllerUIPort uiPort = new ShuttleControllerUIPort(this);
            this.cachedCommandContext = new ShuttleCommandContext(
                this.shuttleHost,
                this.shuttleBuildingHost,
                this.assemblyState,
                this.assemblyMutationController,
                this.cargoBackend,
                this,
                uiPort,
                this.worldTargetingService,
                this.electricLaunchService,
                this.moduleRuntimeCoordinator,
                this.GetLaunchRuntimeState,
                this.powerSystem,
                this.GetProfileForRead,
                this.ReconcileProfileToHost,
                this.BuildCargoSnapshot,
                this.BuildLaunchTargetingContext,
                this.BuildLaunchExecutionInput,
                this.TrySetActiveSurfaceShieldRechargeSpeed,
                this.TryCancelMedicalBayProcedure,
                this.TryStartMedicalBaySurgeryProcedureFromPawn,
                this.MarkProfileDirty);
            this.cachedCommandContextHost = this.shuttleHost;
            this.cachedCommandContextBuildingHost = this.shuttleBuildingHost;
            this.cachedCommandContextAssemblyState = this.assemblyState;
            return this.cachedCommandContext;
        }

        private void MarkProfileDirty(ProfileDirtyReason reason)
        {
            if (reason == ProfileDirtyReason.None)
            {
                return;
            }

            // Profile dirtying only marks downstream recomputation needs.
            // The controller does not eagerly rebuild during every assembly mutation.
            this.profileDirty = true;
            this.pendingDirtyReasons |= reason;
            this.InvalidateProfileDependentRuntimeCaches();
            this.MarkRefrigeratedCargoRegistryDirty();
            if (this.assemblyState == null)
            {
                this.assemblyState = new ShuttleAssemblyState();
            }

            ShuttleDirtyFlags dirtyFlags =
                ShuttleDirtyFlags.Profile |
                ShuttleDirtyFlags.RuntimeSync |
                ShuttleDirtyFlags.ExternalSync |
                ShuttleDirtyFlags.ReadModel;

            if ((reason & (ProfileDirtyReason.AssemblyChanged | ProfileDirtyReason.AssemblyTopologyChanged)) != 0)
            {
                dirtyFlags |= ShuttleDirtyFlags.AssemblyTopology;
            }

            this.assemblyState.MarkDirty(dirtyFlags);
        }

        internal void MarkProfileDirtyForSettings(ProfileDirtyReason reason)
        {
            this.MarkProfileDirty(reason);
        }

        private void InvalidateCachedCommandContext()
        {
            this.cachedCommandContext = null;
            this.cachedCommandContextHost = null;
            this.cachedCommandContextBuildingHost = null;
            this.cachedCommandContextAssemblyState = null;
        }
    }
}
