using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal sealed class ShuttleProfileTuning
    {
        private ShuttleProfileTuning(
            uint baseCargoRegionCount,
            float baseLaunchEnergyWd,
            float energyPerTileWd,
            float energyPerKgTileWd,
            float payloadLiftEnergyPerKgWd,
            float reserveEnergyFraction,
            int hardRangeCapTiles,
            int launchCooldownTicks,
            int baseHullHitPoints,
            float minLaunchHullIntegrityPct,
            float hullRepairMaterialStagingRadius)
        {
            this.BaseCargoRegionCount = baseCargoRegionCount;
            this.BaseLaunchEnergyWd = baseLaunchEnergyWd;
            this.EnergyPerTileWd = energyPerTileWd;
            this.EnergyPerKgTileWd = energyPerKgTileWd;
            this.PayloadLiftEnergyPerKgWd = payloadLiftEnergyPerKgWd;
            this.ReserveEnergyFraction = reserveEnergyFraction;
            this.HardRangeCapTiles = hardRangeCapTiles;
            this.LaunchCooldownTicks = launchCooldownTicks;
            this.BaseHullHitPoints = baseHullHitPoints;
            this.MinLaunchHullIntegrityPct = minLaunchHullIntegrityPct;
            this.HullRepairMaterialStagingRadius = hullRepairMaterialStagingRadius;
        }

        public uint BaseCargoRegionCount { get; private set; }
        public float BaseLaunchEnergyWd { get; private set; }
        public float EnergyPerTileWd { get; private set; }
        public float EnergyPerKgTileWd { get; private set; }
        public float PayloadLiftEnergyPerKgWd { get; private set; }
        public float ReserveEnergyFraction { get; private set; }
        public int HardRangeCapTiles { get; private set; }
        public int LaunchCooldownTicks { get; private set; }
        public int BaseHullHitPoints { get; private set; }
        public float MinLaunchHullIntegrityPct { get; private set; }
        public float HullRepairMaterialStagingRadius { get; private set; }

        public static ShuttleProfileTuning Fallback()
        {
            return new ShuttleProfileTuning(
                ShuttleProfileTuningDef.FallbackBaseCargoRegionCount,
                ShuttleProfileTuningDef.FallbackBaseLaunchEnergyWd,
                ShuttleProfileTuningDef.FallbackEnergyPerTileWd,
                ShuttleProfileTuningDef.FallbackEnergyPerKgTileWd,
                ShuttleProfileTuningDef.FallbackPayloadLiftEnergyPerKgWd,
                ShuttleProfileTuningDef.FallbackReserveEnergyFraction,
                ShuttleProfileTuningDef.FallbackHardRangeCapTiles,
                ShuttleProfileTuningDef.FallbackLaunchCooldownTicks,
                ShuttleProfileTuningDef.FallbackBaseHullHitPoints,
                ShuttleProfileTuningDef.FallbackMinLaunchHullIntegrityPct,
                ShuttleProfileTuningDef.FallbackHullRepairMaterialStagingRadius);
        }

        public static ShuttleProfileTuning FromDef(ShuttleProfileTuningDef tuningDef)
        {
            if (tuningDef == null)
            {
                return Fallback();
            }

            return new ShuttleProfileTuning(
                tuningDef.baseCargoRegionCount <= ShuttleProfileTuningDef.CorruptionGuardMaxCargoRegionCount
                    ? tuningDef.baseCargoRegionCount
                    : ShuttleProfileTuningDef.CorruptionGuardMaxCargoRegionCount,
                FiniteNonNegative(
                    tuningDef.baseLaunchEnergyWd,
                    ShuttleProfileTuningDef.FallbackBaseLaunchEnergyWd,
                    ShuttleProfileTuningDef.CorruptionGuardMaxEnergyWd),
                FiniteNonNegative(
                    tuningDef.energyPerTileWd,
                    ShuttleProfileTuningDef.FallbackEnergyPerTileWd,
                    ShuttleProfileTuningDef.CorruptionGuardMaxEnergyCostWd),
                FiniteNonNegative(
                    tuningDef.energyPerKgTileWd,
                    ShuttleProfileTuningDef.FallbackEnergyPerKgTileWd,
                    ShuttleProfileTuningDef.CorruptionGuardMaxEnergyCostWd),
                FiniteNonNegative(
                    tuningDef.payloadLiftEnergyPerKgWd,
                    ShuttleProfileTuningDef.FallbackPayloadLiftEnergyPerKgWd,
                    ShuttleProfileTuningDef.CorruptionGuardMaxEnergyCostWd),
                FiniteClamped(tuningDef.reserveEnergyFraction, 0f, 1f, ShuttleProfileTuningDef.FallbackReserveEnergyFraction),
                tuningDef.hardRangeCapTiles >= 0 &&
                    tuningDef.hardRangeCapTiles <= ShuttleProfileTuningDef.CorruptionGuardMaxHardRangeCapTiles
                    ? tuningDef.hardRangeCapTiles
                    : ShuttleProfileTuningDef.FallbackHardRangeCapTiles,
                tuningDef.launchCooldownTicks >= 0 &&
                    tuningDef.launchCooldownTicks <= ShuttleProfileTuningDef.CorruptionGuardMaxLaunchCooldownTicks
                    ? tuningDef.launchCooldownTicks
                    : ShuttleProfileTuningDef.FallbackLaunchCooldownTicks,
                tuningDef.baseHullHitPoints >= 0
                    ? tuningDef.baseHullHitPoints
                    : ShuttleProfileTuningDef.FallbackBaseHullHitPoints,
                FiniteClamped(tuningDef.minLaunchHullIntegrityPct, 0f, 1f, ShuttleProfileTuningDef.FallbackMinLaunchHullIntegrityPct),
                FiniteClamped(tuningDef.hullRepairMaterialStagingRadius, 0.5f, 20f, ShuttleProfileTuningDef.FallbackHullRepairMaterialStagingRadius));
        }

        private static float FiniteNonNegative(float value, float fallback)
        {
            return FiniteNonNegative(value, fallback, float.MaxValue);
        }

        private static float FiniteNonNegative(float value, float fallback, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > max ? max : value;
        }

        private static float FiniteClamped(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            return Clamp(value, min, max);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }

    internal static class ShuttleProfileTuningResolver
    {
        private static bool missingDefaultWarningLogged;

        public static ShuttleProfileTuning ResolveDefault()
        {
            ShuttleProfileTuningDef tuningDef =
                DefDatabase<ShuttleProfileTuningDef>.GetNamedSilentFail(ShuttleProfileTuningDef.DefaultTuningDefName);
            if (tuningDef == null)
            {
                if (!missingDefaultWarningLogged)
                {
                    ShuttleLog.Warn(
                        "ProfileTuning",
                        "Default shuttle profile tuning def " + ShuttleProfileTuningDef.DefaultTuningDefName +
                        " was not found. Using built-in fallback tuning values.");
                    missingDefaultWarningLogged = true;
                }

                return ShuttleProfileTuning.Fallback();
            }

            return ShuttleProfileTuning.FromDef(tuningDef);
        }
    }
}
