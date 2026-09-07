namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class PowerProfile
    {
        public PowerProfile(
            float energyStorageCapacityWd,
            float reactorGenerationWatts,
            float gridExportCapacityWatts,
            float internalIdleDemandWatts,
            float maxBatteryChargeWatts,
            float maxBatteryDischargeWatts)
        {
            this.EnergyStorageCapacityWd = energyStorageCapacityWd;
            this.ReactorGenerationWatts = reactorGenerationWatts;
            this.GridExportCapacityWatts = gridExportCapacityWatts;
            this.InternalIdleDemandWatts = internalIdleDemandWatts;
            this.MaxBatteryChargeWatts = maxBatteryChargeWatts;
            this.MaxBatteryDischargeWatts = maxBatteryDischargeWatts;
        }

        public float EnergyStorageCapacityWd { get; private set; }
        public float ReactorGenerationWatts { get; private set; }
        public float GridExportCapacityWatts { get; private set; }
        public float InternalIdleDemandWatts { get; private set; }
        public float MaxBatteryChargeWatts { get; private set; }
        public float MaxBatteryDischargeWatts { get; private set; }
    }
}
