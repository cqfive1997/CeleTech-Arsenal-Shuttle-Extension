using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Boundary around the temporary vanilla transporter cargo backend.
    /// Cargo truth remains in the backend; UI and launch code talk through this interface.
    /// </summary>
    public interface IShuttleCargoBackend
    {
        void ApplyProfile(ThingWithComps host, ShuttleProfile profile);

        ShuttleCargoSnapshot BuildSnapshot(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig);

        ShuttleLoadCargoReadModel BuildLoadCargoReadModel(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState);

        List<TransferableOneWay> BuildPassengerTransferables(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig);

        List<TransferableOneWay> BuildCargoTransferables(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig);

        bool HasQueuedLoads(ThingWithComps host);

        float GetAvailableMass(ThingWithComps host);

        int GetSelectedCount(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables);

        float GetSelectedMassKg(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables);

        bool BeginLoading(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            bool cleanExistingQueue,
            float reservedExternalMassKg,
            out string failureReason);

        bool ClearQueuedLoad(ThingWithComps host, out string failureReason);

        bool CancelQueuedLoadEntry(
            ThingWithComps host,
            ShuttleRuntimeState runtimeState,
            int transporterIndex,
            int queueIndex,
            int count,
            out string failureReason);

        bool UnloadLoadedCargoEntry(
            ThingWithComps host,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            out string failureReason);

        bool UnloadLoadedCargoEntry(
            ThingWithComps host,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            out int unloadedCount,
            out string failureReason);

        bool UnloadLoadedCargoBay(
            ThingWithComps host,
            IReadOnlyList<ShuttleLoadedCargoUnloadTarget> targets,
            out int unloadedStackCount,
            out int unloadedThingCount,
            out string failureReason);

        List<CompTransporter> ResolveTransportersForLaunch(ThingWithComps host);

        List<IThingHolder> ResolveTransporterHoldersForLaunch(ThingWithComps host);

        bool HasPendingLoadQueue(ThingWithComps host);

        bool TryGetLoadedMassKg(ShuttleCargoSnapshot cargoSnapshot, out float loadedMassKg);

        bool TryGetQueuedMassKg(ShuttleCargoSnapshot cargoSnapshot, out float queuedMassKg);

        bool AnyLaunchTransporterPositionRoofed(ThingWithComps host, Map map);

        int GetLaunchGroupID(ThingWithComps host);

        void TryRemoveLaunchLords(ThingWithComps host, Map map);

        bool PlanLaunchCargoHandoffs(
            ThingWithComps host,
            Map map,
            ThingDef activeTransporterDef,
            out List<ShuttleLaunchCargoHandoffPlan> plans,
            out string failureReason);

        bool CommitLaunchCargoHandoffs(
            List<ShuttleLaunchCargoHandoffPlan> plans,
            Map rollbackMap,
            out List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason);

        LaunchCargoRollbackResult RollbackLaunchCargoHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, Map map);

        LaunchCargoFinalizeResult FinalizeLaunchCargoHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, Map map);
    }

    public enum LaunchCargoRollbackStatus
    {
        Success,
        RecoveredWithDrop,
        FatalUnresolved
    }

    public sealed class LaunchCargoRollbackResult
    {
        private LaunchCargoRollbackResult(LaunchCargoRollbackStatus status, string failureReason)
        {
            this.Status = status;
            this.FailureReason = failureReason;
        }

        public LaunchCargoRollbackStatus Status { get; private set; }
        public string FailureReason { get; private set; }

        public static LaunchCargoRollbackResult Success()
        {
            return new LaunchCargoRollbackResult(LaunchCargoRollbackStatus.Success, null);
        }

        public static LaunchCargoRollbackResult RecoveredWithDrop(string notice)
        {
            return new LaunchCargoRollbackResult(LaunchCargoRollbackStatus.RecoveredWithDrop, notice);
        }

        public static LaunchCargoRollbackResult Fatal(string failureReason)
        {
            return new LaunchCargoRollbackResult(LaunchCargoRollbackStatus.FatalUnresolved, failureReason);
        }
    }

    public enum LaunchCargoFinalizeStatus
    {
        Success,
        FatalBlocked
    }

    public sealed class LaunchCargoFinalizeResult
    {
        private LaunchCargoFinalizeResult(LaunchCargoFinalizeStatus status, string failureReason)
        {
            this.Status = status;
            this.FailureReason = failureReason;
        }

        public LaunchCargoFinalizeStatus Status { get; private set; }
        public string FailureReason { get; private set; }

        public static LaunchCargoFinalizeResult Success()
        {
            return new LaunchCargoFinalizeResult(LaunchCargoFinalizeStatus.Success, null);
        }

        public static LaunchCargoFinalizeResult Fatal(string failureReason)
        {
            return new LaunchCargoFinalizeResult(LaunchCargoFinalizeStatus.FatalBlocked, failureReason);
        }
    }

    /// <summary>
    /// A side-effect-free launch cargo plan. It records where and how a transporter
    /// would hand off cargo, but does not move anything out of the live backend.
    /// </summary>
    public sealed class ShuttleLaunchCargoHandoffPlan
    {
        public ShuttleLaunchCargoHandoffPlan(
            CompTransporter sourceTransporter,
            IntVec3 spawnCell,
            int groupID,
            ThingDef activeTransporterDef,
            ThingDef sentTransporterDef,
            Rot4 rotation,
            bool sourceIsShuttle)
        {
            this.SourceTransporter = sourceTransporter;
            this.SpawnCell = spawnCell;
            this.GroupID = groupID;
            this.ActiveTransporterDef = activeTransporterDef;
            this.SentTransporterDef = sentTransporterDef;
            this.Rotation = rotation;
            this.SourceIsShuttle = sourceIsShuttle;
        }

        public CompTransporter SourceTransporter { get; private set; }
        public IntVec3 SpawnCell { get; private set; }
        public int GroupID { get; private set; }
        public ThingDef ActiveTransporterDef { get; private set; }
        public ThingDef SentTransporterDef { get; private set; }
        public Rot4 Rotation { get; private set; }
        public bool SourceIsShuttle { get; private set; }
    }

    /// <summary>
    /// One committed vanilla cargo payload plus the map location where its leaving skyfaller should spawn.
    /// </summary>
    public sealed class ShuttleLaunchCargoHandoff
    {
        public ShuttleLaunchCargoHandoff(ShuttleLaunchCargoHandoffPlan plan, ActiveTransporter activeTransporter)
        {
            this.Plan = plan;
            this.ActiveTransporter = activeTransporter;
        }

        public ShuttleLaunchCargoHandoffPlan Plan { get; private set; }
        public ActiveTransporter ActiveTransporter { get; private set; }

        public CompTransporter SourceTransporter
        {
            get
            {
                return this.Plan != null ? this.Plan.SourceTransporter : null;
            }
        }

        public IntVec3 SpawnCell
        {
            get
            {
                return this.Plan != null ? this.Plan.SpawnCell : IntVec3.Invalid;
            }
        }

        public int GroupID
        {
            get
            {
                return this.Plan != null ? this.Plan.GroupID : -1;
            }
        }

        public bool SourceIsShuttle
        {
            get
            {
                return this.Plan != null && this.Plan.SourceIsShuttle;
            }
        }
    }

    public sealed class ShuttleLoadedCargoUnloadTarget
    {
        public ShuttleLoadedCargoUnloadTarget(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count)
        {
            this.TransporterIndex = transporterIndex;
            this.LoadedIndex = loadedIndex;
            this.ThingIDNumber = thingIDNumber;
            this.ExpectedDefName = expectedDefName;
            this.Count = count;
        }

        public int TransporterIndex { get; private set; }
        public int LoadedIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string ExpectedDefName { get; private set; }
        public int Count { get; private set; }
    }
}
