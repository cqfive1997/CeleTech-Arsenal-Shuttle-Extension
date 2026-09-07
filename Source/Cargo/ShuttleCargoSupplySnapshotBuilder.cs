using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Pure classifier over copied Cargo stack identities. It never reads a transporter or
    /// ThingOwner and runs only when the source inventory snapshot changes.
    /// </summary>
    internal sealed class ShuttleCargoSupplySnapshotBuilder
    {
        internal ShuttleCargoSupplySnapshot Build(ShuttleCargoInventorySnapshot inventory)
        {
            if (inventory == null)
            {
                return new ShuttleCargoSupplySnapshot(
                    0,
                    -1,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    null,
                    null);
            }

            int regularFoodCount = 0;
            int refrigeratedFoodCount = 0;
            int regularMedicineCount = 0;
            int loadedHumanlikePawnCount = 0;
            Dictionary<FoodPreferability, int> regularFoodByPreferability =
                new Dictionary<FoodPreferability, int>();
            Dictionary<FoodPreferability, int> refrigeratedFoodByPreferability =
                new Dictionary<FoodPreferability, int>();
            Dictionary<string, ThingDef> resolvedDefs = new Dictionary<string, ThingDef>();

            IReadOnlyList<CargoStackRef> stackRefs = inventory.StackRefs;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                if (stackRef == null || stackRef.Count <= 0)
                {
                    continue;
                }

                ThingDef thingDef = this.ResolveThingDef(stackRef.DefName, resolvedDefs);
                if (thingDef == null)
                {
                    continue;
                }

                bool regular =
                    stackRef.SourceKind == ShuttleCargoInventorySourceKind.RegularCargo;
                if (regular && this.IsHumanlikePawnDef(thingDef))
                {
                    loadedHumanlikePawnCount = SafeAdd(
                        loadedHumanlikePawnCount,
                        stackRef.Count);
                }

                if (regular && thingDef.IsMedicine)
                {
                    regularMedicineCount = SafeAdd(regularMedicineCount, stackRef.Count);
                }

                if (!this.IsFood(thingDef))
                {
                    continue;
                }

                FoodPreferability preferability = thingDef.ingestible.preferability;
                if (regular)
                {
                    regularFoodCount = SafeAdd(regularFoodCount, stackRef.Count);
                    AddCount(regularFoodByPreferability, preferability, stackRef.Count);
                }
                else
                {
                    refrigeratedFoodCount = SafeAdd(refrigeratedFoodCount, stackRef.Count);
                    AddCount(refrigeratedFoodByPreferability, preferability, stackRef.Count);
                }
            }

            return new ShuttleCargoSupplySnapshot(
                inventory.Revision,
                inventory.BuiltAtTick,
                inventory.IncludesRegularCargo,
                inventory.IncludesRefrigeratedCargo,
                regularFoodCount,
                refrigeratedFoodCount,
                regularMedicineCount,
                loadedHumanlikePawnCount,
                regularFoodByPreferability,
                refrigeratedFoodByPreferability);
        }

        private ThingDef ResolveThingDef(
            string defName,
            Dictionary<string, ThingDef> resolvedDefs)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            ThingDef thingDef;
            if (resolvedDefs.TryGetValue(defName, out thingDef))
            {
                return thingDef;
            }

            thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            resolvedDefs[defName] = thingDef;
            return thingDef;
        }

        private bool IsFood(ThingDef thingDef)
        {
            return thingDef != null &&
                thingDef.ingestible != null &&
                thingDef.IsNutritionGivingIngestible &&
                !thingDef.IsDrug;
        }

        private bool IsHumanlikePawnDef(ThingDef thingDef)
        {
            return thingDef != null &&
                thingDef.race != null &&
                thingDef.race.Humanlike;
        }

        private static void AddCount(
            Dictionary<FoodPreferability, int> counts,
            FoodPreferability preferability,
            int count)
        {
            if (counts == null || count <= 0)
            {
                return;
            }

            int existing;
            counts.TryGetValue(preferability, out existing);
            counts[preferability] = SafeAdd(existing, count);
        }

        private static int SafeAdd(int left, int right)
        {
            if (left <= 0)
            {
                return right > 0 ? right : 0;
            }

            if (right <= 0)
            {
                return left;
            }

            return left > int.MaxValue - right ? int.MaxValue : left + right;
        }
    }
}
