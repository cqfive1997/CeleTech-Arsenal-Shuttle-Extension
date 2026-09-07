namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Derived static cargo-logistics capability. Cargo item truth remains in the cargo backend.
    /// </summary>
    public sealed class CargoLogisticsProfile
    {
        public CargoLogisticsProfile(
            int logisticsModuleCount,
            bool supportsItemTransfer,
            bool supportsItemConsumption,
            bool supportsItemDeposit,
            bool supportsNutritionDistribution,
            int maxStacksMovedPerTick)
        {
            this.LogisticsModuleCount = Max(0, logisticsModuleCount);
            this.SupportsItemTransfer = supportsItemTransfer;
            this.SupportsItemConsumption = supportsItemConsumption;
            this.SupportsItemDeposit = supportsItemDeposit;
            this.SupportsNutritionDistribution = supportsNutritionDistribution;
            this.MaxStacksMovedPerTick = Max(0, maxStacksMovedPerTick);
        }

        public bool HasCargoLogistics
        {
            get
            {
                return this.LogisticsModuleCount > 0;
            }
        }

        public int LogisticsModuleCount { get; private set; }
        public bool SupportsItemTransfer { get; private set; }
        public bool SupportsItemConsumption { get; private set; }
        public bool SupportsItemDeposit { get; private set; }
        public bool SupportsNutritionDistribution { get; private set; }
        public int MaxStacksMovedPerTick { get; private set; }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
