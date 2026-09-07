using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Read-only rules for feeding held shuttle prisoners. This validator never
    /// consumes food, edits needs, touches holders, or assigns jobs.
    /// </summary>
    internal sealed class PrisonCellFeedingValidator
    {
        internal const string FeedPrisonerJobDefName =
            "CT_Shuttle_FeedPrisonerInShuttleCell";
        internal const float FullFoodThreshold = 0.95f;
        internal const float NeedsFeedingThreshold = 0.8f;

        internal bool CanAssignFeedJob(
            ThingWithComps shuttleHost,
            Pawn feeder,
            Pawn prisoner,
            out string failReason)
        {
            if (!this.CanFeedHeldPrisoner(shuttleHost, feeder, prisoner, out failReason))
            {
                return false;
            }

            if (!feeder.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                return false;
            }

            if (!feeder.CanReserve(shuttleHost, 1, 1, null, false))
            {
                failReason = "CT_Shuttle_PrisonCell_Unreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanFeedHeldPrisoner(
            ThingWithComps shuttleHost,
            Pawn feeder,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            if (!this.CanUseShuttle(shuttleHost, out failReason))
            {
                return false;
            }

            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.TryGetPrisonCellOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (!this.CanUsePrisonerForFeeding(prisoner, out failReason))
            {
                return false;
            }

            if (!this.CanUseFeeder(shuttleHost, feeder, out failReason))
            {
                return false;
            }

            ShuttlePrisonerCargoFoodSource foodSource;
            if (!ShuttlePrisonerFoodSourceResolver.TryResolve(shuttleHost, out foodSource) ||
                foodSource == null ||
                !foodSource.CanProvideFood(shuttleHost, prisoner, out failReason))
            {
                if (string.IsNullOrEmpty(failReason))
                {
                    failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                }

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

        internal bool CanUseFeeder(
            ThingWithComps shuttleHost,
            Pawn feeder,
            out string failReason)
        {
            failReason = null;
            if (feeder == null)
            {
                failReason = "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
                return false;
            }

            if (feeder.Destroyed ||
                feeder.Dead ||
                !feeder.Spawned ||
                feeder.Map == null ||
                feeder.Downed ||
                feeder.Drafted ||
                feeder.MentalState != null ||
                feeder.RaceProps == null ||
                !feeder.RaceProps.Humanlike ||
                feeder.Faction != Faction.OfPlayer ||
                !feeder.IsColonist)
            {
                failReason = "CT_Shuttle_PrisonCell_FeederUnavailable".Translate().ToString();
                return false;
            }

            if (!CanUseCapacity(feeder, PawnCapacityDefOf.Moving) ||
                !CanUseCapacity(feeder, PawnCapacityDefOf.Manipulation))
            {
                failReason = "CT_Shuttle_PrisonCell_FeederUnavailable".Translate().ToString();
                return false;
            }

            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                feeder.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanUsePrisonerForFeeding(Pawn prisoner, out string failReason)
        {
            failReason = null;
            if (prisoner == null ||
                prisoner.Destroyed ||
                prisoner.Dead ||
                prisoner.RaceProps == null ||
                !prisoner.RaceProps.Humanlike)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (prisoner.needs == null || prisoner.needs.food == null)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (prisoner.needs.food.CurLevelPercentage >= FullFoodThreshold)
            {
                failReason = "CT_Shuttle_PrisonCell_NotHungry".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool HasFoodAvailable(ThingWithComps shuttleHost, Pawn prisoner)
        {
            ShuttlePrisonerCargoFoodSource foodSource;
            string failReason;
            return ShuttlePrisonerFoodSourceResolver.TryResolve(shuttleHost, out foodSource) &&
                foodSource != null &&
                foodSource.CanProvideFood(shuttleHost, prisoner, out failReason);
        }

        private bool CanUseShuttle(ThingWithComps shuttleHost, out string failReason)
        {
            failReason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            PrisonCellProfile prisonCell;
            if (!this.TryGetPrisonCellProfile(shuttleHost, out prisonCell) ||
                prisonCell == null ||
                !prisonCell.HasPrisonCell ||
                prisonCell.PrisonerSlots <= 0 ||
                !prisonCell.SupportsFeeding)
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

        private static bool CanUseCapacity(Pawn pawn, PawnCapacityDef capacityDef)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                capacityDef != null &&
                pawn.health.capacities.CapableOf(capacityDef);
        }
    }
}
