using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    /// <summary>
    /// Temporary build-only bucket used while deriving a ShuttleProfile from AssemblyState.
    /// It is not persisted and must not become runtime truth.
    /// </summary>
    internal sealed class ShuttleProfileAccumulator
    {
        // TODO: If third-party profile contributions need to add custom typed sections later,
        // expose that through a narrow contribution context instead of exposing this raw accumulator.
        internal float SegmentMass;
        internal float ModuleMass;
        internal int CrewCapacity;
        internal float EnergyStorageCapacityWd;
        internal float ReactorGenerationWatts;
        internal float GridExportCapacityWatts;
        internal float InternalIdleDemandWatts;
        internal float MaxBatteryChargeWatts;
        internal float MaxBatteryDischargeWatts;
        internal uint CargoRegionCount;
        internal int RangeBonusTiles;
        internal float LaunchCooldownFactor = 1f;
        internal float EnergyCostFactor = 1f;
        internal float LoadedMassEfficiencyFactor = 1f;
        internal bool HasCockpit;
        internal bool StabilizesAdverseWeather;
        internal bool ProvidesAutonomousLaunchControl;
        internal bool ProvidesSecureSignalLink;
        internal int TotalWeaponModules;
        internal int TotalWeaponMounts;
        internal int PointDefenseModules;
        internal int PointDefenseMounts;
        internal int CloseInWeaponModules;
        internal int CloseInWeaponMounts;
        internal int RocketLauncherModules;
        internal int RocketLauncherSlots;
        internal int AutoFireCapableModules;
        internal int ForcedTargetCapableModules;
        internal int ScannerRequiredModules;
        internal int NavigationRequiredModules;
        internal int TotalAmmoCapacity;
        internal float WeaponStandbyPowerDrawWatts;
        internal float MaxActiveFiringPowerDrawWatts;
        internal int FireControlModuleCount;
        internal bool FireControlSupportsAutoDefense;
        internal bool FireControlSupportsPointDefense;
        internal float FireControlMaxPointDefenseRadius;
        internal float FireControlDirectFireAccuracyMultiplier = 1f;
        internal float FireControlDirectFireAccuracyBonus;
        internal float FireControlDirectFireAccuracyFloor;
        internal float FireControlForcedMissRadiusMultiplier = 1f;
        internal int VanillaInterceptorShieldModuleCount;
        internal int TotalVanillaInterceptorShieldMaxHitPoints;
        internal int BestVanillaInterceptorShieldMaxHitPoints;
        internal int SurfaceShieldModuleCount;
        internal int TotalSurfaceShieldMaxHitPoints;
        internal int BestSurfaceShieldMaxHitPoints;
        internal bool HasSurfaceShieldRechargeEnergyCost;
        internal float BestSurfaceShieldRechargeEnergyPerHitPointWd;
        internal int CargoLogisticsModuleCount;
        internal bool CargoLogisticsSupportsItemTransfer;
        internal bool CargoLogisticsSupportsItemConsumption;
        internal bool CargoLogisticsSupportsItemDeposit;
        internal bool CargoLogisticsSupportsNutritionDistribution;
        internal int CargoLogisticsMaxStacksMovedPerTick;
        internal bool HasHabitat;
        internal bool HabitatAnySupportsSleep;
        internal bool HabitatAnySupportsDining;
        internal int HabitatSleepSlots;
        internal int HabitatDiningSlots;
        internal int HabitatSleepThoughtStageIndex;
        internal int HabitatDiningThoughtStageIndex;
        internal float HabitatRestEffectiveness = 1f;
        internal bool HabitatSuppressSleepDisturbedThoughts;
        internal bool HabitatSuppressBarracksThoughts;
        internal bool HabitatAllowsInventoryFood;
        internal bool HabitatAllowsCargoFoodWithdrawal;
        internal bool HabitatRequiresCargoLogisticsForFoodWithdrawal;
        internal readonly List<ThingDef> HabitatPreferredAutoFoodDefs =
            new List<ThingDef>();
        internal bool HabitatAnySupportsJoy;
        internal int HabitatJoySlots;
        internal int HabitatJoyKindCapacity;
        internal int HabitatJoyThoughtStageIndex;
        internal float HabitatJoyGainFactor = 1f;
        internal readonly List<JoyKindDef> HabitatJoyKinds =
            new List<JoyKindDef>();
        internal bool HasMedicalBay;
        internal int MedicalPatientSlots;
        internal bool MedicalBaySupportsMedevacPriority;
        internal bool MedicalBaySupportsStabilization;
        internal bool MedicalBaySupportsPassiveComfort;
        internal float MedicalBayPassiveJoyGainFactor = 1f;
        internal float MedicalBayPassiveJoyCapPct = 0.5f;
        internal int MedicalBayComfortThoughtStageIndex;
        internal bool HasPrisonCell;
        internal int PrisonerSlots;
        internal float PrisonCellMaxSecurity;
        internal float PrisonCellMaxComfort;
        internal bool PrisonCellSupportsFeeding;
        internal bool PrisonCellSupportsTending;
        internal bool PrisonCellSupportsCargoFoodSupply;
        internal bool PrisonCellSupportsRefrigeratedCargoFoodSupply;
        internal bool PrisonCellRequiresCargoLogisticsForFoodSupply;
        internal bool PrisonCellHasNoLogisticsRequiredFoodSupply;
        internal FoodPreferability PrisonCellMaximumCargoFoodPreferability =
            FoodPreferability.MealLavish;
        internal bool HasMechCharger;
        internal int MechChargeSlots;
        internal float MaxMechChargeRateFactor = 1f;
        internal int HullHitPointsBonus;
        internal int HullArmorModuleCount;
        internal float HullSharpDamageMultiplier = 1f;
        internal float HullBluntDamageMultiplier = 1f;
        internal float HullHeatDamageMultiplier = 1f;
        internal float HullExplosionDamageMultiplier = 1f;
        internal float HullEmpDamageMultiplier = 1f;
        internal float HullFlatDamageReduction;
    }
}
