namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class FlightProfile
    {
        public FlightProfile(
            float batteryCapacityWd,
            float baseLaunchEnergyWd,
            float energyPerTileWd,
            float energyPerKgTileWd,
            float payloadLiftEnergyPerKgWd,
            float reserveEnergyWd,
            int hardRangeCapTiles,
            int launchCooldownTicks)
        {
            this.BatteryCapacityWd = batteryCapacityWd;
            this.BaseLaunchEnergyWd = baseLaunchEnergyWd;
            this.EnergyPerTileWd = energyPerTileWd;
            this.EnergyPerKgTileWd = energyPerKgTileWd;
            this.PayloadLiftEnergyPerKgWd = payloadLiftEnergyPerKgWd;
            this.ReserveEnergyWd = reserveEnergyWd;
            this.HardRangeCapTiles = hardRangeCapTiles;
            this.LaunchCooldownTicks = launchCooldownTicks;
        }

        // Static battery capacity projection. Runtime charge lives in
        // ShuttleRuntimeState.Power.StoredEnergyWd, not in profile.
        public float BatteryCapacityWd { get; private set; }

        // Fixed launch overhead paid before range and mass-distance costs are applied.
        public float BaseLaunchEnergyWd { get; private set; }

        // Distance-only energy cost after the planet layer's rangeDistanceFactor is applied.
        public float EnergyPerTileWd { get; private set; }

        // Extra cost for moving each kilogram across one adjusted tile.
        public float EnergyPerKgTileWd { get; private set; }

        // One-time cost for launching each loaded/queued payload kilogram.
        public float PayloadLiftEnergyPerKgWd { get; private set; }

        // Runtime stored energy below this value is treated as reserved, not launch-available.
        public float ReserveEnergyWd { get; private set; }

        // Absolute profile cap after the energy-based distance is calculated.
        public int HardRangeCapTiles { get; private set; }

        // Runtime cooldown applied after a successful launch.
        public int LaunchCooldownTicks { get; private set; }
    }
}
