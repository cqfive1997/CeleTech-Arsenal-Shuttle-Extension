using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Read-only admission rules for shuttle Prison Cells. This class never mutates
    /// guest status, holders, jobs, records, or prisoner interaction modes.
    /// </summary>
    internal sealed class PrisonCellAdmissionValidator
    {
        internal const string CarryPrisonerToShuttleCellJobDefName =
            "CT_Shuttle_CarryPrisonerToShuttleCell";

        internal bool CanAssignCarryToPrisonCell(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out string failReason)
        {
            return this.CanAssignCarryToPrisonCell(
                shuttleHost,
                carrier,
                prisoner,
                out failReason,
                null);
        }

        internal bool CanAssignCarryToPrisonCell(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out string failReason,
            Pawn ignoredJobPawn)
        {
            failReason = null;
            if (!this.CanUseCarrier(shuttleHost, carrier, out failReason))
            {
                return false;
            }

            if (!this.CanUsePrisonerForAdmission(
                shuttleHost,
                prisoner,
                out failReason))
            {
                return false;
            }

            Thing carriedThing = carrier.carryTracker.CarriedThing;
            if (carriedThing != null && carriedThing != prisoner)
            {
                failReason = "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
                return false;
            }

            bool carrierAlreadyCarriesPrisoner = carriedThing == prisoner;
            if (!carrierAlreadyCarriesPrisoner)
            {
                if (!prisoner.Spawned || prisoner.Map == null || prisoner.Map != carrier.Map)
                {
                    failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                    return false;
                }

                if (!carrier.CanReach(prisoner, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                {
                    failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                    return false;
                }

                if (!carrier.CanReserve(prisoner, 1, -1, null, false))
                {
                    failReason = "CT_Shuttle_PrisonCell_PrisonerReserved".Translate().ToString();
                    return false;
                }
            }

            PrisonCellProfile prisonCell;
            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.CanUseShuttleForPrisonCellAdmission(
                shuttleHost,
                out prisonCell,
                out occupancy,
                out failReason))
            {
                return false;
            }

            if (occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_AlreadyHeld".Translate().ToString();
                return false;
            }

            if (this.CountCurrentPrisonCellAdmissionUsers(
                shuttleHost,
                prisoner,
                ignoredJobPawn) >= prisonCell.PrisonerSlots)
            {
                failReason = "CT_Shuttle_PrisonCell_Full".Translate().ToString();
                return false;
            }

            if (!carrier.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                return false;
            }

            if (!carrier.CanReserve(shuttleHost, Max(1, prisonCell.PrisonerSlots), 1, null, false))
            {
                failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanAdmitSpawnedPrisoner(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.CanUsePrisonerForAdmission(
                shuttleHost,
                prisoner,
                out failReason))
            {
                return false;
            }

            if (!prisoner.Spawned || prisoner.Map == null)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            PrisonCellProfile prisonCell;
            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.CanUseShuttleForPrisonCellAdmission(
                shuttleHost,
                out prisonCell,
                out occupancy,
                out failReason))
            {
                return false;
            }

            if (prisoner.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_AlreadyHeld".Translate().ToString();
                return false;
            }

            if (this.CountCurrentPrisonCellAdmissionUsers(shuttleHost, prisoner, null) >= prisonCell.PrisonerSlots)
            {
                failReason = "CT_Shuttle_PrisonCell_Full".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanContinueCarryOrder(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.CanUseCarrier(shuttleHost, carrier, out failReason) ||
                !this.CanUsePrisonerForAdmission(shuttleHost, prisoner, out failReason))
            {
                return false;
            }

            Thing carriedThing = carrier.carryTracker.CarriedThing;
            if (carriedThing != null && carriedThing != prisoner)
            {
                failReason = "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
                return false;
            }

            bool alreadyCarried = carriedThing == prisoner;
            if (!alreadyCarried &&
                (!prisoner.Spawned || prisoner.Map == null || prisoner.Map != carrier.Map))
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            PrisonCellProfile prisonCell;
            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.CanUseShuttleForPrisonCellAdmission(
                shuttleHost,
                out prisonCell,
                out occupancy,
                out failReason))
            {
                return false;
            }

            if (occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_AlreadyHeld".Translate().ToString();
                return false;
            }

            // The running job already owns its reservations. Path following handles
            // route failure, and final admission performs the exact capacity check.
            return carrier.Map == shuttleHost.Map;
        }

        internal bool CanAdmitCarriedPrisoner(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.IsCarrierCarryingPrisoner(carrier, prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_CarriedAdmitFailed".Translate().ToString();
                return false;
            }

            if (!this.CanUsePrisonerForAdmission(
                shuttleHost,
                prisoner,
                out failReason))
            {
                return false;
            }

            if (!this.CanUseCarrier(shuttleHost, carrier, out failReason))
            {
                return false;
            }

            PrisonCellProfile prisonCell;
            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.CanUseShuttleForPrisonCellAdmission(
                shuttleHost,
                out prisonCell,
                out occupancy,
                out failReason))
            {
                return false;
            }

            if (occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_AlreadyHeld".Translate().ToString();
                return false;
            }

            if (this.CountCurrentPrisonCellAdmissionUsers(shuttleHost, prisoner, carrier) >= prisonCell.PrisonerSlots)
            {
                failReason = "CT_Shuttle_PrisonCell_Full".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool TryGetPrisonCellOccupancy(
            ThingWithComps shuttleHost,
            out CompShuttlePrisonCellOccupancy occupancy)
        {
            occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            return occupancy != null;
        }

        internal bool IsCarrierCarryingPrisoner(Pawn carrier, Pawn prisoner)
        {
            return carrier != null &&
                carrier.carryTracker != null &&
                prisoner != null &&
                carrier.carryTracker.CarriedThing == prisoner;
        }

        private bool CanUseCarrier(
            ThingWithComps shuttleHost,
            Pawn carrier,
            out string failReason)
        {
            failReason = null;
            if (carrier == null)
            {
                failReason = "CT_Shuttle_PrisonCell_NoCarrier".Translate().ToString();
                return false;
            }

            if (carrier.Destroyed ||
                carrier.Dead ||
                !carrier.Spawned ||
                carrier.Map == null ||
                carrier.Downed ||
                carrier.Drafted ||
                carrier.MentalState != null ||
                carrier.carryTracker == null)
            {
                failReason = "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
                return false;
            }

            if (carrier.RaceProps == null ||
                !carrier.RaceProps.Humanlike ||
                carrier.Faction != Faction.OfPlayer ||
                !carrier.IsColonist)
            {
                failReason = "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
                return false;
            }

            if (!CanMove(carrier) || !CanManipulate(carrier))
            {
                failReason = "CT_Shuttle_PrisonCell_CarrierUnavailable".Translate().ToString();
                return false;
            }

            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                carrier.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool CanUsePrisonerForAdmission(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (prisoner == null || prisoner.Destroyed || prisoner.Dead)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (prisoner.RaceProps == null || !prisoner.RaceProps.Humanlike)
            {
                failReason = "CT_Shuttle_PrisonCell_NotHumanlike".Translate().ToString();
                return false;
            }

            if (prisoner.RaceProps.IsMechanoid ||
                prisoner.Faction == Faction.OfPlayer ||
                prisoner.IsColonist)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (!ShuttlePassengerFacilityAdmissionPolicy.AllowsEntry(
                prisoner,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (this.IsPrisonerOfPlayer(prisoner))
            {
                return true;
            }

            if (!prisoner.Downed)
            {
                failReason = "CT_Shuttle_PrisonCell_PrisonerNotDownedOrPrisoner".Translate().ToString();
                return false;
            }

            if (!prisoner.HostileTo(Faction.OfPlayer))
            {
                failReason = "CT_Shuttle_PrisonCell_NotHostile".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool CanUseShuttleForPrisonCellAdmission(
            ThingWithComps shuttleHost,
            out PrisonCellProfile prisonCell,
            out CompShuttlePrisonCellOccupancy occupancy,
            out string failReason)
        {
            prisonCell = null;
            occupancy = null;
            failReason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            CompShuttleHolderLaunchTransferState transferState =
                shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (transferState != null && transferState.HasAnyActiveOrRecoveryTransfer)
            {
                failReason = "CT_Shuttle_PrisonCell_TransferBusy".Translate().ToString();
                return false;
            }

            if (!this.TryGetPrisonCellProfile(shuttleHost, out prisonCell) ||
                prisonCell == null ||
                !prisonCell.HasPrisonCell ||
                prisonCell.PrisonerSlots <= 0)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (!this.TryGetPrisonCellOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool TryGetPrisonCellProfile(
            ThingWithComps shuttleHost,
            out PrisonCellProfile prisonCell)
        {
            prisonCell = null;
            CompModularShuttleCore core = shuttleHost != null
                ? shuttleHost.TryGetComp<CompModularShuttleCore>()
                : null;
            ShuttleProfile profile = core != null && core.Controller != null
                ? core.Controller.GetProfileForRead()
                : null;
            prisonCell = profile != null ? profile.PrisonCell : null;
            return prisonCell != null;
        }

        private int CountCurrentPrisonCellAdmissionUsers(
            ThingWithComps shuttleHost,
            Pawn ignoredPrisoner,
            Pawn ignoredJobPawn)
        {
            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null)
            {
                return 0;
            }

            int count = 0;
            CompShuttlePrisonCellOccupancy occupancy;
            if (this.TryGetPrisonCellOccupancy(shuttleHost, out occupancy))
            {
                count += occupancy.PrisonerCount;
                if (ignoredPrisoner != null && occupancy.ContainsPrisoner(ignoredPrisoner))
                {
                    count--;
                }
            }

            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail(CarryPrisonerToShuttleCellJobDefName);
            if (carryJobDef == null || shuttleHost.Map.mapPawns == null)
            {
                return count < 0 ? 0 : count;
            }

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return count < 0 ? 0 : count;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == ignoredJobPawn)
                {
                    continue;
                }

                Job job = pawn.CurJob;
                if (job == null ||
                    job.def != carryJobDef ||
                    !TargetReferences(job.GetTarget(TargetIndex.B), shuttleHost))
                {
                    continue;
                }

                Pawn targetPrisoner = job.GetTarget(TargetIndex.A).Pawn;
                if (ignoredPrisoner != null && targetPrisoner == ignoredPrisoner)
                {
                    continue;
                }

                count++;
            }

            return count < 0 ? 0 : count;
        }

        private bool IsPrisonerOfPlayer(Pawn pawn)
        {
            return pawn != null &&
                pawn.guest != null &&
                pawn.guest.IsPrisoner &&
                (pawn.guest.HostFaction == Faction.OfPlayer ||
                    pawn.guest.HostFaction == null);
        }

        private static bool CanMove(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving);
        }

        private static bool CanManipulate(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        }

        private static bool TargetReferences(LocalTargetInfo target, Thing thing)
        {
            return thing != null && target.IsValid && target.Thing == thing;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
