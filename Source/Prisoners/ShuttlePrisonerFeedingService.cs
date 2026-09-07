using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Command/job operation boundary for feeding prisoners held inside shuttle
    /// Prison Cells. Cargo selection, consumption, and food-need mutation are
    /// delegated to ShuttlePrisonerCargoFoodSource.
    /// </summary>
    internal sealed class ShuttlePrisonerFeedingService
    {
        private readonly PrisonCellFeedingValidator validator =
            new PrisonCellFeedingValidator();
        internal bool TryAssignFeedPrisonerJob(
            ThingWithComps shuttleHost,
            Pawn feeder,
            int prisonerThingIDNumber,
            out string failReason)
        {
            failReason = null;
            if (this.HasActiveCareOrder(
                shuttleHost,
                prisonerThingIDNumber,
                PrisonCellFeedingValidator.FeedPrisonerJobDefName))
            {
                failReason = "CT_Shuttle_PrisonCell_CareAlreadyAssigned".Translate().ToString();
                return false;
            }

            Pawn prisoner = this.FindHeldPrisoner(shuttleHost, prisonerThingIDNumber);
            if (!this.validator.CanAssignFeedJob(
                shuttleHost,
                feeder,
                prisoner,
                out failReason))
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                PrisonCellFeedingValidator.FeedPrisonerJobDefName);
            if (jobDef == null || feeder == null || feeder.jobs == null)
            {
                failReason = "CT_Shuttle_PrisonCell_FeedFailed".Translate().ToString();
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, shuttleHost);
            // job.count intentionally stores the held prisoner thingID because
            // the prisoner is inside a holder and is not a spawned job target.
            job.count = prisonerThingIDNumber;
            if (!feeder.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                failReason = "CT_Shuttle_PrisonCell_FeedFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool HasActiveCareOrder(
            ThingWithComps shuttleHost,
            int prisonerThingIDNumber,
            string jobDefName)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(jobDefName);
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null ||
                prisonerThingIDNumber <= 0 ||
                jobDef == null)
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<Pawn> pawns =
                shuttleHost.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; pawns != null && i < pawns.Count; i++)
            {
                Job currentJob = pawns[i] != null ? pawns[i].CurJob : null;
                if (currentJob != null &&
                    currentJob.def == jobDef &&
                    currentJob.count == prisonerThingIDNumber &&
                    currentJob.GetTarget(TargetIndex.A).Thing == shuttleHost)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryFeedHeldPrisoner(
            ThingWithComps shuttleHost,
            Pawn feeder,
            int prisonerThingIDNumber,
            out string failReason)
        {
            failReason = null;
            Pawn prisoner = this.FindHeldPrisoner(shuttleHost, prisonerThingIDNumber);
            if (!this.validator.CanFeedHeldPrisoner(
                shuttleHost,
                feeder,
                prisoner,
                out failReason))
            {
                return false;
            }

            PrisonCellFoodConsumption consumption;
            ShuttlePrisonerCargoFoodSource foodSource;
            if (!ShuttlePrisonerFoodSourceResolver.TryResolve(shuttleHost, out foodSource) ||
                foodSource == null)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (!foodSource.TryConsumeFoodForPrisoner(
                shuttleHost,
                prisoner,
                out consumption,
                out failReason))
            {
                return false;
            }

            CompShuttlePrisonCellOccupancy occupancy;
            if (this.validator.TryGetPrisonCellOccupancy(shuttleHost, out occupancy) &&
                occupancy != null &&
                consumption != null)
            {
                occupancy.MarkPrisonerFedForInternalUse(
                    prisonerThingIDNumber,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    consumption.Nutrition,
                    consumption.FoodLabel);
            }

            string foodLabel = consumption != null ? consumption.FoodLabel : string.Empty;
            Messages.Message(
                "CT_Shuttle_PrisonCell_FeedSucceeded".Translate(foodLabel).ToString(),
                MessageTypeDefOf.PositiveEvent,
                false);
            return true;
        }

        private Pawn FindHeldPrisoner(ThingWithComps shuttleHost, int prisonerThingIDNumber)
        {
            CompShuttlePrisonCellOccupancy occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            return occupancy != null
                ? occupancy.FindHeldPrisonerByThingID(prisonerThingIDNumber)
                : null;
        }
    }
}
