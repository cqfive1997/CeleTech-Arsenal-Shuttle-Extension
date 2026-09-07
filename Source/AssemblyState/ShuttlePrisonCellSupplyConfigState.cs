using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Durable per-shuttle policy for supplying held prisoners from shuttle cargo.
    /// This is static player configuration; food stacks and feeding cooldowns live elsewhere.
    /// </summary>
    public sealed class ShuttlePrisonCellSupplyConfigState : IExposable
    {
        private bool cargoFoodSupplyEnabled = true;
        private bool refrigeratedFoodSupplyEnabled = true;
        private FoodPreferability maximumFoodPreferability = FoodPreferability.MealAwful;

        public bool CargoFoodSupplyEnabled
        {
            get { return this.cargoFoodSupplyEnabled; }
        }

        public bool RefrigeratedFoodSupplyEnabled
        {
            get { return this.refrigeratedFoodSupplyEnabled; }
        }

        public FoodPreferability MaximumFoodPreferability
        {
            get { return this.maximumFoodPreferability; }
        }

        public void EnsureInitialized()
        {
            this.maximumFoodPreferability = SanitizeMaximumFoodPreferability(
                this.maximumFoodPreferability);
        }

        internal void Set(
            bool cargoFoodSupplyEnabled,
            bool refrigeratedFoodSupplyEnabled,
            FoodPreferability maximumFoodPreferability)
        {
            this.cargoFoodSupplyEnabled = cargoFoodSupplyEnabled;
            this.refrigeratedFoodSupplyEnabled = refrigeratedFoodSupplyEnabled;
            this.maximumFoodPreferability = SanitizeMaximumFoodPreferability(
                maximumFoodPreferability);
        }

        internal static bool IsSupportedMaximumFoodPreferability(
            FoodPreferability preferability)
        {
            return preferability >= FoodPreferability.MealAwful &&
                preferability <= FoodPreferability.MealLavish;
        }

        internal static FoodPreferability SanitizeMaximumFoodPreferability(
            FoodPreferability preferability)
        {
            return IsSupportedMaximumFoodPreferability(preferability)
                ? preferability
                : FoodPreferability.MealAwful;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref this.cargoFoodSupplyEnabled,
                "cargoFoodSupplyEnabled",
                true);
            Scribe_Values.Look(
                ref this.refrigeratedFoodSupplyEnabled,
                "refrigeratedFoodSupplyEnabled",
                true);
            Scribe_Values.Look(
                ref this.maximumFoodPreferability,
                "maximumFoodPreferability",
                FoodPreferability.MealAwful);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                FoodPreferability legacyMinimum = FoodPreferability.Undefined;
                Scribe_Values.Look(
                    ref legacyMinimum,
                    "minimumFoodPreferability",
                    FoodPreferability.Undefined);
                if (legacyMinimum != FoodPreferability.Undefined)
                {
                    this.maximumFoodPreferability = legacyMinimum;
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }
    }
}
