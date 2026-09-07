using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    public sealed class JobGiver_GetHabitatFood : ThinkNode_JobGiver
    {
        // Vanilla JobGiver_GetFood is 9.5. RimWorld randomizes equal-priority
        // PrioritySorter children, so Habitat food must sit just above it to be deterministic.
        private const float HabitatFoodPriority = 9.6f;

        public override float GetPriority(Pawn pawn)
        {
            Thing shuttleHost;
            Thing food;
            ThingDef foodDef;
            int count;
            bool hasInventoryFood;
            return this.TryFindFoodHabitat(
                pawn,
                out shuttleHost,
                out food,
                out foodDef,
                out count,
                out hasInventoryFood)
                ? HabitatFoodPriority
                : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing shuttleHost;
            Thing food;
            ThingDef foodDef;
            int count;
            bool hasInventoryFood;
            if (!this.TryFindFoodHabitat(
                pawn,
                out shuttleHost,
                out food,
                out foodDef,
                out count,
                out hasInventoryFood))
            {
                return null;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(HabitatUtility.HabitatIngestJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return null;
            }

            Job job;
            if (hasInventoryFood && food != null)
            {
                job = JobMaker.MakeJob(jobDef, shuttleHost, food);
                job.count = count;
                return job;
            }

            // Cargo-food mode intentionally leaves targetB invalid. The JobDriver treats
            // targetA-only Habitat ingest jobs as "withdraw real food from loaded cargo now".
            job = JobMaker.MakeJob(jobDef, shuttleHost);
            job.count = 0;
            return job;
        }

        private bool TryFindFoodHabitat(
            Pawn pawn,
            out Thing shuttleHost,
            out Thing food,
            out ThingDef foodDef,
            out int count,
            out bool hasInventoryFood)
        {
            shuttleHost = null;
            food = null;
            foodDef = null;
            count = 0;
            hasInventoryFood = false;

            if (!IsEligiblePawn(pawn) || !WantsFood(pawn))
            {
                return false;
            }

            hasInventoryFood = HabitatIngestUtility.TryFindBestInventoryFoodForHabitat(
                pawn,
                out food,
                out foodDef,
                out count);

            if (hasInventoryFood)
            {
                shuttleHost = this.FindBestInventoryFoodHabitat(pawn);
            }

            if (!hasInventoryFood || shuttleHost == null)
            {
                shuttleHost = this.FindBestCargoFoodHabitat(pawn);
            }

            if (shuttleHost == null || shuttleHost.Destroyed)
            {
                shuttleHost = null;
                return false;
            }

            return true;
        }

        private Thing FindBestInventoryFoodHabitat(Pawn pawn)
        {
            return HabitatUtility.FindBestHabitatForDining(
                pawn,
                delegate(Thing shuttleHost, HabitatProfile habitat)
                {
                    return habitat != null && habitat.AllowsInventoryFood;
                });
        }

        private Thing FindBestCargoFoodHabitat(Pawn pawn)
        {
            return HabitatUtility.FindBestHabitatForDining(
                pawn,
                delegate(Thing shuttleHost, HabitatProfile habitat)
                {
                    if (habitat == null || !habitat.AllowsCargoFoodWithdrawal)
                    {
                        return false;
                    }

                    ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
                    if (shuttleWithComps == null)
                    {
                        return false;
                    }

                    IShuttleHabitatFoodSource foodSource;
                    return HabitatUtility.TryGetHabitatFoodSource(shuttleHost, out foodSource) &&
                        foodSource.CanProvideFood(shuttleWithComps, pawn, habitat);
                });
        }

        private static bool IsEligiblePawn(Pawn pawn)
        {
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                !pawn.Spawned ||
                pawn.Map == null ||
                pawn.Faction != Faction.OfPlayer ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.Humanlike ||
                pawn.Drafted ||
                pawn.Downed ||
                pawn.IsPrisoner ||
                pawn.IsSlave ||
                pawn.needs == null ||
                pawn.needs.food == null)
            {
                return false;
            }

            return true;
        }

        private static bool WantsFood(Pawn pawn)
        {
            if (pawn == null || pawn.needs == null || pawn.needs.food == null)
            {
                return false;
            }

            Need_Food foodNeed = pawn.needs.food;
            return foodNeed.CurLevelPercentage < pawn.RaceProps.FoodLevelPercentageWantEat ||
                foodNeed.CurCategory == HungerCategory.Hungry ||
                foodNeed.CurCategory == HungerCategory.Starving;
        }
    }
}
