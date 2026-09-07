using System.Collections.Generic;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Small, copied Cargo supply projection. It is derived from a Cargo inventory snapshot,
    /// owns no holder truth, and is safe for read-model consumers to reuse.
    /// </summary>
    internal sealed class ShuttleCargoSupplySnapshot
    {
        private readonly Dictionary<FoodPreferability, int> regularFoodByPreferability;
        private readonly Dictionary<FoodPreferability, int> refrigeratedFoodByPreferability;

        internal ShuttleCargoSupplySnapshot(
            int inventoryRevision,
            int builtAtTick,
            bool hasRegularCargo,
            bool hasActiveRefrigeratedCargo,
            int regularFoodCount,
            int activeRefrigeratedFoodCount,
            int regularMedicineCount,
            int loadedHumanlikePawnCount,
            Dictionary<FoodPreferability, int> regularFoodByPreferability,
            Dictionary<FoodPreferability, int> refrigeratedFoodByPreferability)
        {
            this.InventoryRevision = inventoryRevision;
            this.BuiltAtTick = builtAtTick;
            this.HasRegularCargo = hasRegularCargo;
            this.HasActiveRefrigeratedCargo = hasActiveRefrigeratedCargo;
            this.RegularFoodCount = regularFoodCount;
            this.ActiveRefrigeratedFoodCount = activeRefrigeratedFoodCount;
            this.RegularMedicineCount = regularMedicineCount;
            this.LoadedHumanlikePawnCount = loadedHumanlikePawnCount;
            this.regularFoodByPreferability = regularFoodByPreferability ??
                new Dictionary<FoodPreferability, int>();
            this.refrigeratedFoodByPreferability = refrigeratedFoodByPreferability ??
                new Dictionary<FoodPreferability, int>();
        }

        internal int InventoryRevision { get; private set; }
        internal int BuiltAtTick { get; private set; }
        internal bool HasRegularCargo { get; private set; }
        internal bool HasActiveRefrigeratedCargo { get; private set; }
        internal int RegularFoodCount { get; private set; }
        internal int ActiveRefrigeratedFoodCount { get; private set; }
        internal int RegularMedicineCount { get; private set; }
        internal int LoadedHumanlikePawnCount { get; private set; }

        internal int GeneralFoodCount
        {
            get { return SafeAdd(this.RegularFoodCount, this.ActiveRefrigeratedFoodCount); }
        }

        internal bool HasAnyCargoSource
        {
            get { return this.HasRegularCargo || this.HasActiveRefrigeratedCargo; }
        }

        internal int CountPrisonerFood(
            FoodPreferability maximumPreferability,
            bool includeActiveRefrigeratedCargo)
        {
            int count = this.CountFoodByPreferability(
                this.regularFoodByPreferability,
                maximumPreferability);
            if (includeActiveRefrigeratedCargo)
            {
                count = SafeAdd(
                    count,
                    this.CountFoodByPreferability(
                        this.refrigeratedFoodByPreferability,
                        maximumPreferability));
            }

            return count;
        }

        private int CountFoodByPreferability(
            Dictionary<FoodPreferability, int> counts,
            FoodPreferability maximumPreferability)
        {
            if (counts == null || maximumPreferability < FoodPreferability.MealAwful)
            {
                return 0;
            }

            int total = 0;
            foreach (KeyValuePair<FoodPreferability, int> pair in counts)
            {
                if (pair.Key < FoodPreferability.MealAwful ||
                    pair.Key > maximumPreferability ||
                    pair.Value <= 0)
                {
                    continue;
                }

                total = SafeAdd(total, pair.Value);
            }

            return total;
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
