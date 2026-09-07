using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    internal static class HabitatIngestUtility
    {
        internal static bool TryFindBestInventoryFoodForHabitat(
            Pawn pawn,
            out Thing food,
            out ThingDef foodDef,
            out int count)
        {
            food = null;
            foodDef = null;
            count = 0;

            if (pawn == null ||
                pawn.inventory == null ||
                pawn.inventory.innerContainer == null ||
                pawn.needs == null ||
                pawn.needs.food == null)
            {
                return false;
            }

            Thing bestFood = null;
            FoodPreferability bestPreferability = FoodPreferability.NeverForNutrition;
            float bestNutrition = -1f;

            ThingOwner<Thing> inventory = pawn.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                Thing candidate = inventory[i];
                if (!IsUsableHabitatFood(pawn, candidate))
                {
                    continue;
                }

                FoodPreferability preferability = candidate.def.ingestible.preferability;
                float nutrition = FoodUtility.NutritionForEater(pawn, candidate);
                if (bestFood == null ||
                    preferability > bestPreferability ||
                    (preferability == bestPreferability && nutrition > bestNutrition))
                {
                    bestFood = candidate;
                    bestPreferability = preferability;
                    bestNutrition = nutrition;
                }
            }

            if (bestFood == null)
            {
                return false;
            }

            int ingestCount = ComputeIngestCount(pawn, bestFood, bestFood.def);
            if (ingestCount <= 0)
            {
                return false;
            }

            food = bestFood;
            foodDef = bestFood.def;
            count = ingestCount;
            return true;
        }

        internal static bool IsUsableHabitatFood(Pawn pawn, Thing food)
        {
            if (pawn == null ||
                food == null ||
                food.Destroyed ||
                food.def == null ||
                food.stackCount <= 0 ||
                food.def.ingestible == null ||
                !food.def.IsNutritionGivingIngestible ||
                !food.IngestibleNow)
            {
                return false;
            }

            if (food.def.IsDrug)
            {
                return false;
            }

            return pawn.WillEat(food, pawn, true, false);
        }

        internal static int ComputeIngestCount(Pawn pawn, Thing food, ThingDef foodDef)
        {
            if (pawn == null || food == null || foodDef == null || food.stackCount <= 0)
            {
                return 0;
            }

            float nutrition = FoodUtility.NutritionForEater(pawn, food);
            if (nutrition <= 0f)
            {
                nutrition = food.GetStatValue(StatDefOf.Nutrition);
            }

            int count = FoodUtility.WillIngestStackCountOf(pawn, foodDef, nutrition);
            if (count < 1)
            {
                count = 1;
            }

            return count > food.stackCount ? food.stackCount : count;
        }

        internal static bool FinalizeHabitatIngest(Pawn pawn, Thing food, HabitatProfile habitat)
        {
            if (pawn == null || food == null || food.Destroyed)
            {
                return false;
            }

            if (pawn.needs == null || pawn.needs.food == null)
            {
                return false;
            }

            float nutritionWanted = pawn.needs.food.NutritionWanted;
            if (nutritionWanted <= 0f)
            {
                float nutrition = food.GetStatValue(StatDefOf.Nutrition);
                nutritionWanted = nutrition * food.stackCount;
            }

            float gainedNutrition = food.Ingested(pawn, nutritionWanted);
            if (!pawn.Dead && pawn.needs != null && pawn.needs.food != null)
            {
                pawn.needs.food.CurLevel += gainedNutrition;
            }

            if (pawn.records != null)
            {
                pawn.records.AddTo(RecordDefOf.NutritionEaten, gainedNutrition);
            }

            if (!pawn.Dead)
            {
                HabitatDiningThoughtUtility.TryApplyHabitatDiningThought(pawn, habitat);
            }

            return true;
        }
    }
}
