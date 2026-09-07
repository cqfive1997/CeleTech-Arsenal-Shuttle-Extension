using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal enum ShuttleHolderIncomingRestoreFailureStatus
    {
        None,
        MedicalBayRestoreFailedQuarantined,
        MedicalBayRestoreFatalUnresolved,
        HabitatRestoreFailedQuarantined,
        HabitatRestoreFatalUnresolved,
        MechChargerRestoreFailedQuarantined,
        MechChargerRestoreFatalUnresolved,
        PrisonCellRestoreFailedQuarantined,
        PrisonCellRestoreFatalUnresolved
    }

    internal enum HabitatIncomingRestoreStatus
    {
        None,
        AllRestored,
        AllSafelyEjected,
        AllQuarantined,
        MixedResolved,
        FatalUnresolved
    }

    internal sealed class HabitatIncomingRestoreResult
    {
        internal HabitatIncomingRestoreResult(
            HabitatIncomingRestoreStatus status,
            string failureReason,
            string debugDump,
            int restoredCount,
            int ejectedCount,
            int quarantinedCount,
            int fatalCount)
        {
            this.Status = status;
            this.FailureReason = failureReason;
            this.DebugDump = debugDump;
            this.RestoredCount = restoredCount;
            this.EjectedCount = ejectedCount;
            this.QuarantinedCount = quarantinedCount;
            this.FatalCount = fatalCount;
        }

        internal HabitatIncomingRestoreStatus Status { get; private set; }

        internal string FailureReason { get; private set; }

        internal string DebugDump { get; private set; }

        internal int RestoredCount { get; private set; }

        internal int EjectedCount { get; private set; }

        internal int QuarantinedCount { get; private set; }

        internal int FatalCount { get; private set; }

        internal bool AllowsImpact
        {
            get
            {
                return this.Status != HabitatIncomingRestoreStatus.FatalUnresolved;
            }
        }
    }

    /// <summary>
    /// Coordinates non-cargo shuttle holder transfer across launch and incoming impact.
    /// Cargo remains owned by the cargo backend; this service only exports/restores
    /// shuttle-owned holders such as Habitat sleep/dining/joy occupants.
    /// </summary>
    internal static partial class ShuttleHolderLaunchTransferService
    {
        private const string IncomingSkyfallerDefName = "CT_ModularShuttleIncoming";

        private const string MedicalBayUnsupportedArrivalFailureKey = "CT_Shuttle_Launch_Failed_MedicalBayTransferUnsupportedArrival";

        private const string MechChargerUnsupportedArrivalFailureKey = "CT_Shuttle_Launch_Failed_MechChargerTransferUnsupportedArrival";

        private const string PrisonCellUnsupportedArrivalFailureKey = "CT_Shuttle_Launch_Failed_PrisonCellTransferUnsupportedArrival";

        // Internal for holder transfer handlers; preserves the static service façade while handlers own implementation.
        internal static CompShuttleHolderLaunchTransferState GetState(ThingWithComps shuttleHost)
        {
            return shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }

        private static bool IsFatalIncomingRestoreStatus(ShuttleHolderIncomingRestoreFailureStatus status)
        {
            return status == ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFatalUnresolved ||
                status == ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFatalUnresolved ||
                status == ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFatalUnresolved ||
                status == ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFatalUnresolved;
        }

        private static void KeepMostSevereIncomingRestoreFailure(
            string candidateFailureReason,
            ShuttleHolderIncomingRestoreFailureStatus candidateFailureStatus,
            ref string failureReason,
            ref ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            if (candidateFailureStatus == ShuttleHolderIncomingRestoreFailureStatus.None)
            {
                return;
            }

            if (IsFatalIncomingRestoreStatus(candidateFailureStatus))
            {
                if (!IsFatalIncomingRestoreStatus(failureStatus))
                {
                    failureReason = candidateFailureReason;
                    failureStatus = candidateFailureStatus;
                }

                return;
            }

            if (!IsFatalIncomingRestoreStatus(failureStatus))
            {
                failureReason = candidateFailureReason;
                failureStatus = candidateFailureStatus;
            }
        }
    }
}
