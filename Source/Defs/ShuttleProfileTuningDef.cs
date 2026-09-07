using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Numeric tuning for derived shuttle profile and launch behavior.
    /// These values are static Def data, not save data and not runtime state.
    /// </summary>
    public sealed class ShuttleProfileTuningDef : Def
    {
        public const string DefaultTuningDefName = "CT_DefaultShuttleProfileTuning";

        public const uint FallbackBaseCargoRegionCount = 0;
        public const float FallbackBaseLaunchEnergyWd = 25f;
        public const float FallbackEnergyPerTileWd = 1f;
        public const float FallbackEnergyPerKgTileWd = 0.01f;
        public const float FallbackPayloadLiftEnergyPerKgWd = 0.05f;
        public const float FallbackReserveEnergyFraction = 0.15f;
        public const int FallbackHardRangeCapTiles = 186;
        public const int FallbackLaunchCooldownTicks = 3750;
        public const int FallbackBaseHullHitPoints = 600;
        public const float FallbackMinLaunchHullIntegrityPct = 0.25f;
        public const float FallbackHullRepairMaterialStagingRadius = 3f;
        public const uint CorruptionGuardMaxCargoRegionCount = 1000000u;
        public const float CorruptionGuardMaxEnergyWd = 1E+12f;
        public const float CorruptionGuardMaxEnergyCostWd = 1E+9f;
        public const int CorruptionGuardMaxHardRangeCapTiles = 10000000;
        public const int CorruptionGuardMaxLaunchCooldownTicks = 1000000000;

        // Base visual cargo regions available before dedicated cargo modules add more.
        public uint baseCargoRegionCount = FallbackBaseCargoRegionCount;

        // Fixed launch overhead paid before distance and mass-distance costs.
        public float baseLaunchEnergyWd = FallbackBaseLaunchEnergyWd;

        // Energy cost per adjusted world tile.
        public float energyPerTileWd = FallbackEnergyPerTileWd;

        // Extra energy cost per kilogram per adjusted world tile.
        public float energyPerKgTileWd = FallbackEnergyPerKgTileWd;

        // Extra one-time energy cost per loaded/queued payload kilogram at launch.
        // This applies even for same-tile launch/landing, where distance cost is zero.
        public float payloadLiftEnergyPerKgWd = FallbackPayloadLiftEnergyPerKgWd;

        // Fraction of installed battery capacity reserved from launch spending.
        public float reserveEnergyFraction = FallbackReserveEnergyFraction;

        // Absolute distance cap after energy-based range is calculated.
        public int hardRangeCapTiles = FallbackHardRangeCapTiles;

        // Runtime launch cooldown applied after a successful launch.
        public int launchCooldownTicks = FallbackLaunchCooldownTicks;

        // Whole-shuttle static hull profile defaults. Current integrity is future runtime state.
        public int baseHullHitPoints = FallbackBaseHullHitPoints;
        public float minLaunchHullIntegrityPct = FallbackMinLaunchHullIntegrityPct;
        public float hullRepairMaterialStagingRadius = FallbackHullRepairMaterialStagingRadius;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.baseCargoRegionCount > CorruptionGuardMaxCargoRegionCount)
            {
                yield return this.defName + " baseCargoRegionCount exceeds corruption guard max " +
                    CorruptionGuardMaxCargoRegionCount + ".";
            }

            if (!this.IsFiniteFloat(this.baseLaunchEnergyWd))
            {
                yield return this.defName + " has non-finite baseLaunchEnergyWd.";
            }
            else if (this.baseLaunchEnergyWd < 0f)
            {
                yield return this.defName + " has negative baseLaunchEnergyWd.";
            }
            else if (this.baseLaunchEnergyWd > CorruptionGuardMaxEnergyWd)
            {
                yield return this.defName + " baseLaunchEnergyWd exceeds corruption guard max " +
                    CorruptionGuardMaxEnergyWd + ".";
            }

            if (!this.IsFiniteFloat(this.energyPerTileWd))
            {
                yield return this.defName + " has non-finite energyPerTileWd.";
            }
            else if (this.energyPerTileWd < 0f)
            {
                yield return this.defName + " has negative energyPerTileWd.";
            }
            else if (this.energyPerTileWd > CorruptionGuardMaxEnergyCostWd)
            {
                yield return this.defName + " energyPerTileWd exceeds corruption guard max " +
                    CorruptionGuardMaxEnergyCostWd + ".";
            }

            if (!this.IsFiniteFloat(this.energyPerKgTileWd))
            {
                yield return this.defName + " has non-finite energyPerKgTileWd.";
            }
            else if (this.energyPerKgTileWd < 0f)
            {
                yield return this.defName + " has negative energyPerKgTileWd.";
            }
            else if (this.energyPerKgTileWd > CorruptionGuardMaxEnergyCostWd)
            {
                yield return this.defName + " energyPerKgTileWd exceeds corruption guard max " +
                    CorruptionGuardMaxEnergyCostWd + ".";
            }

            if (!this.IsFiniteFloat(this.payloadLiftEnergyPerKgWd))
            {
                yield return this.defName + " has non-finite payloadLiftEnergyPerKgWd.";
            }
            else if (this.payloadLiftEnergyPerKgWd < 0f)
            {
                yield return this.defName + " has negative payloadLiftEnergyPerKgWd.";
            }
            else if (this.payloadLiftEnergyPerKgWd > CorruptionGuardMaxEnergyCostWd)
            {
                yield return this.defName + " payloadLiftEnergyPerKgWd exceeds corruption guard max " +
                    CorruptionGuardMaxEnergyCostWd + ".";
            }

            if (!this.IsFiniteFloat(this.reserveEnergyFraction))
            {
                yield return this.defName + " has non-finite reserveEnergyFraction.";
            }
            else if (this.reserveEnergyFraction < 0f || this.reserveEnergyFraction > 1f)
            {
                yield return this.defName + " reserveEnergyFraction must be between 0 and 1.";
            }

            if (this.hardRangeCapTiles < 0)
            {
                yield return this.defName + " has negative hardRangeCapTiles.";
            }
            else if (this.hardRangeCapTiles > CorruptionGuardMaxHardRangeCapTiles)
            {
                yield return this.defName + " hardRangeCapTiles exceeds corruption guard max " +
                    CorruptionGuardMaxHardRangeCapTiles + ".";
            }

            if (this.launchCooldownTicks < 0)
            {
                yield return this.defName + " has negative launchCooldownTicks.";
            }
            else if (this.launchCooldownTicks > CorruptionGuardMaxLaunchCooldownTicks)
            {
                yield return this.defName + " launchCooldownTicks exceeds corruption guard max " +
                    CorruptionGuardMaxLaunchCooldownTicks + ".";
            }

            if (this.baseHullHitPoints < 0)
            {
                yield return this.defName + " has negative baseHullHitPoints.";
            }

            if (!this.IsFiniteFloat(this.minLaunchHullIntegrityPct))
            {
                yield return this.defName + " has non-finite minLaunchHullIntegrityPct.";
            }
            else if (this.minLaunchHullIntegrityPct < 0f || this.minLaunchHullIntegrityPct > 1f)
            {
                yield return this.defName + " minLaunchHullIntegrityPct must be between 0 and 1.";
            }

            if (!this.IsFiniteFloat(this.hullRepairMaterialStagingRadius))
            {
                yield return this.defName + " has non-finite hullRepairMaterialStagingRadius.";
            }
            else if (this.hullRepairMaterialStagingRadius <= 0f)
            {
                yield return this.defName + " hullRepairMaterialStagingRadius must be greater than 0.";
            }
            else if (this.hullRepairMaterialStagingRadius > 20f)
            {
                yield return this.defName + " hullRepairMaterialStagingRadius should be 20 or lower.";
            }
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
