using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions
{
    /// <summary>
    /// Narrow write surface for profile contribution values.
    /// Implementations aggregate values without exposing ShuttleProfile for mutation.
    /// </summary>
    public interface IShuttleProfileContributionSink
    {
        void AddCrewCapacity(int count);

        void AddEnergyStorageWd(float wattDays);

        void AddReactorGenerationWatts(float watts);

        void AddGridExportCapacityWatts(float watts);

        void AddMaxBatteryChargeWatts(float watts);

        void AddMaxBatteryDischargeWatts(float watts);

        void AddCargoRegionCount(uint count);

        void AddRangeBonusTiles(int tiles);

        void MultiplyLaunchCooldownFactor(float factor);

        void MultiplyEnergyCostFactor(float factor);

        void MultiplyLoadedMassEfficiencyFactor(float factor);
    }

    public interface IShuttleWeaponProfileContributionSink
    {
        void AddWeaponModule(
            ShuttleWeaponRole weaponRole,
            int mountCount,
            float standbyPowerDrawWatts,
            float activeFiringPowerDrawWatts,
            bool canAutoFire,
            bool canSetForcedTarget,
            bool requiresScanner,
            bool requiresNavigationComputer,
            int maxAmmo);
    }

    public interface IShuttleShieldProfileContributionSink
    {
        void AddSurfaceShieldModule(
            int maxHitPoints,
            float rechargeEnergyPerHitPointWd);

        void AddVanillaInterceptorShieldModule(int maxHitPoints);
    }

    public interface IShuttleFireControlProfileContributionSink
    {
        void AddFireControlRadarModule(
            bool supportsAutoDefense,
            bool supportsPointDefense,
            float pointDefenseRadius,
            float directFireAccuracyMultiplier,
            float directFireAccuracyBonus,
            float directFireAccuracyFloor,
            float forcedMissRadiusMultiplier);
    }

    public interface IShuttleCargoLogisticsProfileContributionSink
    {
        void AddCargoLogisticsModule(
            bool supportsItemTransfer,
            bool supportsItemConsumption,
            bool supportsItemDeposit,
            bool supportsNutritionDistribution,
            int maxStacksMovedPerTick);
    }

    public interface IShuttleHabitatProfileContributionSink
    {
        void AddHabitatModule(
            bool supportsSleep,
            int sleepSlots,
            int sleepThoughtStageIndex,
            bool supportsDining,
            int diningSlots,
            int diningThoughtStageIndex,
            float restEffectiveness,
            bool suppressSleepDisturbedThoughts,
            bool suppressBarracksThoughts,
            bool allowInventoryFood,
            bool allowCargoFoodWithdrawal,
            bool requireCargoLogisticsForFoodWithdrawal,
            IReadOnlyList<ThingDef> preferredAutoFoodDefs);
    }

    public interface IShuttleHabitatJoyProfileContributionSink
    {
        void AddHabitatJoyModule(
            bool supportsJoy,
            int joySlots,
            int joyKindCapacity,
            int joyThoughtStageIndex,
            float joyGainFactor,
            IReadOnlyList<JoyKindDef> selectedJoyKinds);
    }

    public interface IShuttleMedicalBayProfileContributionSink
    {
        void AddMedicalBayModule(
            int medicalPatientSlots,
            bool supportsMedevacPriority,
            bool supportsStabilization,
            bool supportsPassiveComfort,
            float passiveJoyGainFactor,
            float passiveJoyCapPct,
            int comfortThoughtStageIndex);
    }

    public interface IShuttlePrisonCellProfileContributionSink
    {
        void AddPrisonCellModule(
            int prisonerSlots,
            float security,
            float comfort,
            bool supportsFeeding,
            bool supportsTending,
            bool allowCargoFoodSupply,
            bool allowRefrigeratedCargoFoodSupply,
            bool requireCargoLogisticsForFoodSupply,
            FoodPreferability maximumCargoFoodPreferability);
    }

    public interface IShuttleMechChargerProfileContributionSink
    {
        void AddMechChargerModule(
            int mechChargeSlots,
            float chargeRateFactor);
    }

    public interface IShuttleHullProfileContributionSink
    {
        void AddHullPlatingModule(
            int hullHitPointsBonus,
            float sharpDamageMultiplier,
            float bluntDamageMultiplier,
            float heatDamageMultiplier,
            float explosionDamageMultiplier,
            float empDamageMultiplier,
            float flatDamageReduction);
    }

    /// <summary>
    /// No-op sink used by skeleton contexts that are not wired to a profile builder yet.
    /// </summary>
    public sealed class NullShuttleProfileContributionSink :
        IShuttleProfileContributionSink,
        IShuttleHullProfileContributionSink,
        IShuttleFireControlProfileContributionSink,
        IShuttlePrisonCellProfileContributionSink
    {
        private static readonly NullShuttleProfileContributionSink instance = new NullShuttleProfileContributionSink();

        private NullShuttleProfileContributionSink()
        {
        }

        public static NullShuttleProfileContributionSink Instance
        {
            get
            {
                return instance;
            }
        }

        public void AddCrewCapacity(int count)
        {
        }

        public void AddEnergyStorageWd(float wattDays)
        {
        }

        public void AddReactorGenerationWatts(float watts)
        {
        }

        public void AddGridExportCapacityWatts(float watts)
        {
        }

        public void AddMaxBatteryChargeWatts(float watts)
        {
        }

        public void AddMaxBatteryDischargeWatts(float watts)
        {
        }

        public void AddCargoRegionCount(uint count)
        {
        }

        public void AddRangeBonusTiles(int tiles)
        {
        }

        public void MultiplyLaunchCooldownFactor(float factor)
        {
        }

        public void MultiplyEnergyCostFactor(float factor)
        {
        }

        public void MultiplyLoadedMassEfficiencyFactor(float factor)
        {
        }

        public void AddHullPlatingModule(
            int hullHitPointsBonus,
            float sharpDamageMultiplier,
            float bluntDamageMultiplier,
            float heatDamageMultiplier,
            float explosionDamageMultiplier,
            float empDamageMultiplier,
            float flatDamageReduction)
        {
        }

        public void AddFireControlRadarModule(
            bool supportsAutoDefense,
            bool supportsPointDefense,
            float pointDefenseRadius,
            float directFireAccuracyMultiplier,
            float directFireAccuracyBonus,
            float directFireAccuracyFloor,
            float forcedMissRadiusMultiplier)
        {
        }

        public void AddPrisonCellModule(
            int prisonerSlots,
            float security,
            float comfort,
            bool supportsFeeding,
            bool supportsTending,
            bool allowCargoFoodSupply,
            bool allowRefrigeratedCargoFoodSupply,
            bool requireCargoLogisticsForFoodSupply,
            FoodPreferability maximumCargoFoodPreferability)
        {
        }

    }
}
