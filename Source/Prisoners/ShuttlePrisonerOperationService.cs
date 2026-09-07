using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Command/job boundary for Prison Cell operations. It coordinates validation,
    /// guest status, and holder atomics without owning durable prison state.
    /// </summary>
    internal sealed class ShuttlePrisonerOperationService
    {
        private readonly PrisonCellAdmissionValidator validator =
            new PrisonCellAdmissionValidator();
        private readonly ShuttlePrisonerGuestStatusService guestStatusService =
            new ShuttlePrisonerGuestStatusService();

        internal bool TryAssignCarryPrisonerJob(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                PrisonCellAdmissionValidator.CarryPrisonerToShuttleCellJobDefName);
            if (jobDef == null || carrier == null || carrier.jobs == null)
            {
                failReason = "CT_Shuttle_PrisonCell_CarryJobFailed".Translate().ToString();
                return false;
            }

            if (this.HasActiveCarryOrder(shuttleHost, prisoner, jobDef))
            {
                failReason = "CT_Shuttle_PrisonCell_CarryAlreadyAssigned".Translate().ToString();
                return false;
            }

            if (!this.validator.CanAssignCarryToPrisonCell(
                shuttleHost,
                carrier,
                prisoner,
                out failReason,
                carrier))
            {
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, prisoner, shuttleHost);
            job.count = 1;
            if (!carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                failReason = "CT_Shuttle_PrisonCell_CarryJobFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool HasActiveCarryOrder(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            JobDef carryJobDef)
        {
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null ||
                prisoner == null ||
                carryJobDef == null)
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<Pawn> pawns =
                shuttleHost.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; pawns != null && i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Job currentJob = pawn != null ? pawn.CurJob : null;
                if (currentJob != null &&
                    currentJob.def == carryJobDef &&
                    currentJob.GetTarget(TargetIndex.A).Pawn == prisoner &&
                    currentJob.GetTarget(TargetIndex.B).Thing == shuttleHost)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryPreparePrisonerForCarryAdmission(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out bool changedGuestStatus,
            out string failReason)
        {
            changedGuestStatus = false;
            failReason = null;
            if (!this.validator.CanAssignCarryToPrisonCell(
                shuttleHost,
                carrier,
                prisoner,
                out failReason,
                carrier))
            {
                return false;
            }

            bool wasPrisoner = prisoner != null &&
                prisoner.guest != null &&
                prisoner.guest.IsPrisoner;
            Faction previousHostFaction = prisoner != null && prisoner.guest != null
                ? prisoner.guest.HostFaction
                : null;

            ShuttlePrisonerAdmissionContext context;
            if (!this.guestStatusService.EnsurePrisonerStatusForAdmission(
                prisoner,
                Faction.OfPlayer,
                out context,
                out failReason))
            {
                return false;
            }

            changedGuestStatus = !wasPrisoner ||
                previousHostFaction != (prisoner != null && prisoner.guest != null
                    ? prisoner.guest.HostFaction
                    : null);

            return this.validator.CanAssignCarryToPrisonCell(
                shuttleHost,
                carrier,
                prisoner,
                out failReason,
                carrier);
        }

        internal bool TryAdmitCarriedPrisoner(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.validator.CanAdmitCarriedPrisoner(
                shuttleHost,
                carrier,
                prisoner,
                out failReason))
            {
                return false;
            }

            ShuttlePrisonerAdmissionContext context;
            if (!this.guestStatusService.TryBuildPreparedAdmissionContext(
                prisoner,
                Faction.OfPlayer,
                out context,
                out failReason))
            {
                return false;
            }

            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.validator.TryGetPrisonCellOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            return occupancy.TryAdmitCarriedPrisonerForInternalUse(
                carrier,
                prisoner,
                context,
                out failReason);
        }

        internal bool TryAdmitSpawnedPrisonerForDebugOrFallback(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.validator.CanAdmitSpawnedPrisoner(shuttleHost, prisoner, out failReason))
            {
                return false;
            }

            ShuttlePrisonerAdmissionContext context;
            if (!this.guestStatusService.EnsurePrisonerStatusForAdmission(
                prisoner,
                Faction.OfPlayer,
                out context,
                out failReason))
            {
                return false;
            }

            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.validator.TryGetPrisonCellOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            return occupancy.TryAdmitSpawnedPrisonerForInternalUse(
                prisoner,
                context,
                out failReason);
        }

        internal bool TryEjectPrisonerByThingID(
            ThingWithComps shuttleHost,
            int prisonerThingIDNumber,
            out bool hadPrisoner,
            out string failReason)
        {
            hadPrisoner = false;
            failReason = null;
            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.validator.TryGetPrisonCellOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (!occupancy.HasPrisoners)
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState transferState = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            if (transferState != null && transferState.HasAnyActiveOrRecoveryTransfer)
            {
                failReason = "CT_Shuttle_PrisonCell_TransferBusy".Translate().ToString();
                return false;
            }

            if (prisonerThingIDNumber > 0)
            {
                return occupancy.TryEjectPrisonerByThingID(
                    prisonerThingIDNumber,
                    out hadPrisoner,
                    out failReason);
            }

            hadPrisoner = true;
            return occupancy.TryEjectAllPrisoners(out failReason);
        }
    }
}
