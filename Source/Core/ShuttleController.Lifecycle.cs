using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal void ConfigureInitialStoredEnergyPercent(float initialStoredEnergyPercent)
        {
            if (initialStoredEnergyPercent < 0f)
            {
                initialStoredEnergyPercent = 0f;
            }
            else if (initialStoredEnergyPercent > 1f)
            {
                initialStoredEnergyPercent = 1f;
            }

            this.initialStoredEnergyPercent = initialStoredEnergyPercent;
        }

        internal void Bind(Building_ModularShuttle shuttleBuildingHost, ThingWithComps shuttleHost)
        {
            // Binding only supplies the current host seam. It does not bootstrap assembly contents.
            bool hostChanged =
                !object.ReferenceEquals(this.shuttleBuildingHost, shuttleBuildingHost) ||
                !object.ReferenceEquals(this.shuttleHost, shuttleHost);

            this.shuttleBuildingHost = shuttleBuildingHost;
            this.shuttleHost = shuttleHost;

            if (this.assemblyState == null)
            {
                this.assemblyState = new ShuttleAssemblyState();
            }

            this.assemblyState.EnsureInitialized();
            this.EnsureCurrentSegmentModuleSlots();
            this.EnsureRuntimeState();
            if (hostChanged)
            {
                this.lastExternalSyncProfileRevision = -1;
                this.InvalidateCachedCommandContext();
                this.InvalidateProfileDependentRuntimeCaches();
                this.MarkProfileDirty(ProfileDirtyReason.ControllerBound);
            }

            this.MarkRefrigeratedCargoRegistryDirty();
            this.ReconcileRefrigeratedCargoRegistryIfNeeded(this.GetTicksGameSafe());
        }

        internal void InitializeAfterCreation(string assemblyLayoutDefName)
        {
            // First creation owns shuttle-level slot materialization. Save loads rebuild from persisted assembly state instead.
            if (this.assemblyState == null)
            {
                this.assemblyState = new ShuttleAssemblyState();
            }

            this.assemblyState.EnsureInitialized();
            this.EnsureCurrentSegmentModuleSlots();
            this.EnsureRuntimeState();

            if (!string.IsNullOrEmpty(assemblyLayoutDefName))
            {
                ShuttleAssemblyLayoutDef layoutDef = DefDatabase<ShuttleAssemblyLayoutDef>.GetNamedSilentFail(assemblyLayoutDefName);
                if (layoutDef != null)
                {
                    this.assemblyBootstrapper.InitializeSegmentSlots(this.assemblyState, layoutDef);
                }
                else
                {
                    ShuttleLog.Warn("ShuttleController", "Default shuttle assembly layout def " + assemblyLayoutDefName + " was not found during initial creation.");
                }
            }

            this.assemblyState.CaptureStarterPresetDecisionAtCreation(
                ShuttleStarterPresetBuildPolicy.IsBasicModeActive);
            this.TryInstallSelectedStarterPreset();

            this.MarkProfileDirty(ProfileDirtyReason.InitialCreation | ProfileDirtyReason.AssemblyTopologyChanged);
            this.MarkRefrigeratedCargoRegistryDirty();
            this.ReconcileRefrigeratedCargoRegistryIfNeeded(this.GetTicksGameSafe());
        }

        internal void NotifyLoadedFromSave()
        {
            // After load, assembly truth already exists. The controller only needs to restore transient state and invalidate the cached profile.
            if (this.assemblyState == null)
            {
                this.assemblyState = new ShuttleAssemblyState();
            }

            this.assemblyState.EnsureInitialized();
            this.EnsureCurrentSegmentModuleSlots();
            this.EnsureRuntimeState();
            this.profile = null;
            this.lastRuntimeSyncProfileRevision = -1;
            this.lastRuntimeSyncDispatchFingerprint = -1;
            this.lastExternalSyncProfileRevision = -1;
            this.MarkProfileDirty(ProfileDirtyReason.LoadCompleted | ProfileDirtyReason.AssemblyTopologyChanged);
            this.InvalidateCachedCommandContext();
            this.InvalidateProfileDependentRuntimeCaches();
            this.MarkRefrigeratedCargoRegistryDirty();
            this.ReconcileRefrigeratedCargoRegistryIfNeeded(this.GetTicksGameSafe());
        }

        private void EnsureRuntimeState()
        {
            if (this.runtimeState == null)
            {
                this.runtimeState = new ShuttleRuntimeState();
            }

            this.runtimeState.EnsureInitialized();
        }

        internal void TryApplyStarterPresetAfterSelection()
        {
            this.assemblyState.ScheduleStarterPresetForInitialScenarioSelection();
            this.TryInstallSelectedStarterPreset();
            this.MarkProfileDirty(
                ProfileDirtyReason.InitialCreation |
                ProfileDirtyReason.AssemblyTopologyChanged);
        }

        private void TryInstallSelectedStarterPreset()
        {
            if (this.assemblyState == null ||
                !this.assemblyState.StarterPresetInstallationPending)
            {
                return;
            }

            string failureReason;
            if (!this.starterPresetAssemblyInstaller.TryInstall(
                this.shuttleHost,
                this.assemblyState,
                this,
                out failureReason))
            {
                ShuttleLog.Error(
                    "StarterPreset",
                    "Could not install the complete basic shuttle package: " +
                    failureReason + ".");
                return;
            }

            this.assemblyState.CompleteStarterPresetInstallation();
        }

        private void EnsureCurrentSegmentModuleSlots()
        {
            if (this.assemblyState == null || this.assemblyBootstrapper == null)
            {
                return;
            }

            if (this.assemblyBootstrapper.EnsureMissingModuleSlotsForCurrentSegmentDefs(this.assemblyState))
            {
                this.profile = null;
                this.profileDirty = true;
                this.pendingDirtyReasons |= ProfileDirtyReason.AssemblyTopologyChanged;
                this.InvalidateCachedCommandContext();
                this.InvalidateProfileDependentRuntimeCaches();
            }
        }

        public void ExposeData()
        {
            // Persist only durable assembly/runtime truth here. Profile is rebuilt after load
            // and host binding is restored by the core comp.
            Scribe_Deep.Look(ref this.assemblyState, "assemblyState");
            Scribe_Deep.Look(ref this.runtimeState, "runtimeState");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.assemblyState == null)
                {
                    this.assemblyState = new ShuttleAssemblyState();
                }

                this.assemblyState.EnsureInitialized();
                this.EnsureCurrentSegmentModuleSlots();
                this.EnsureRuntimeState();
                this.profile = null;
                this.profileDirty = true;
                this.lastRuntimeSyncProfileRevision = -1;
                this.lastRuntimeSyncDispatchFingerprint = -1;
                this.lastExternalSyncProfileRevision = -1;
                this.InvalidateCachedCommandContext();
                this.InvalidateProfileDependentRuntimeCaches();
                this.MarkRefrigeratedCargoRegistryDirty();
                this.pendingDirtyReasons = ProfileDirtyReason.LoadCompleted | ProfileDirtyReason.AssemblyTopologyChanged;
                this.assemblyState.MarkDirty(
                    ShuttleDirtyFlags.Profile |
                    ShuttleDirtyFlags.AssemblyTopology |
                    ShuttleDirtyFlags.RuntimeSync |
                    ShuttleDirtyFlags.ExternalSync |
                    ShuttleDirtyFlags.ReadModel);
            }
        }
    }
}
