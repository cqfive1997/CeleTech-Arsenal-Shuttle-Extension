using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Derived static Prison Cell capability. Live prisoners and interactions stay
    /// in future occupancy/runtime services, not in this rebuildable profile.
    /// </summary>
    public sealed class PrisonCellProfile
    {
        private static readonly PrisonCellProfile empty = new PrisonCellProfile(
            false,
            0,
            0f,
            0f,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            FoodPreferability.MealAwful);

        public PrisonCellProfile(
            bool hasPrisonCell,
            int prisonerSlots,
            float maxSecurity,
            float maxComfort,
            bool supportsFeeding,
            bool supportsTending,
            bool supportsCargoFoodSupply,
            bool supportsRefrigeratedCargoFoodSupply,
            bool requiresCargoLogisticsForFoodSupply,
            bool cargoFoodSupplyEnabled,
            bool refrigeratedCargoFoodSupplyEnabled,
            FoodPreferability maximumCargoFoodPreferability)
        {
            this.PrisonerSlots = Max(0, prisonerSlots);
            this.HasPrisonCell = hasPrisonCell;
            this.MaxSecurity = SanitizeNonNegative(maxSecurity);
            this.MaxComfort = SanitizeNonNegative(maxComfort);
            this.SupportsFeeding = this.HasPrisonCell && this.PrisonerSlots > 0 && supportsFeeding;
            this.SupportsTending = this.HasPrisonCell && this.PrisonerSlots > 0 && supportsTending;
            this.SupportsCargoFoodSupply = this.SupportsFeeding && supportsCargoFoodSupply;
            this.SupportsRefrigeratedCargoFoodSupply =
                this.SupportsCargoFoodSupply && supportsRefrigeratedCargoFoodSupply;
            this.RequiresCargoLogisticsForFoodSupply =
                this.SupportsCargoFoodSupply && requiresCargoLogisticsForFoodSupply;
            this.CargoFoodSupplyEnabled =
                this.SupportsCargoFoodSupply && cargoFoodSupplyEnabled;
            this.RefrigeratedCargoFoodSupplyEnabled =
                this.CargoFoodSupplyEnabled &&
                this.SupportsRefrigeratedCargoFoodSupply &&
                refrigeratedCargoFoodSupplyEnabled;
            this.MaximumCargoFoodPreferability = SanitizeFoodPreferability(
                maximumCargoFoodPreferability);
        }

        public static PrisonCellProfile Empty
        {
            get
            {
                return empty;
            }
        }

        public bool HasPrisonCell { get; private set; }

        public int PrisonerSlots { get; private set; }

        public float MaxSecurity { get; private set; }

        public float MaxComfort { get; private set; }

        public bool SupportsFeeding { get; private set; }

        public bool SupportsTending { get; private set; }

        public bool SupportsCargoFoodSupply { get; private set; }

        public bool SupportsRefrigeratedCargoFoodSupply { get; private set; }

        public bool RequiresCargoLogisticsForFoodSupply { get; private set; }

        public bool CargoFoodSupplyEnabled { get; private set; }

        public bool RefrigeratedCargoFoodSupplyEnabled { get; private set; }

        public FoodPreferability MaximumCargoFoodPreferability { get; private set; }

        private static float SanitizeNonNegative(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        private static FoodPreferability SanitizeFoodPreferability(
            FoodPreferability preferability)
        {
            return preferability >= FoodPreferability.MealAwful &&
                preferability <= FoodPreferability.MealLavish
                ? preferability
                : FoodPreferability.MealAwful;
        }
    }
}
