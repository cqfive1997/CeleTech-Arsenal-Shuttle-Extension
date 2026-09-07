using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    /// <summary>
    /// Build-only sink used by profile contributors. It aggregates values into the current
    /// accumulator without exposing the final ShuttleProfile for mutation.
    /// </summary>
    internal sealed class ShuttleProfileAccumulatorContributionSink :
        IShuttleProfileContributionSink,
        IShuttleWeaponProfileContributionSink,
        IShuttleShieldProfileContributionSink,
        IShuttleCargoLogisticsProfileContributionSink,
        IShuttleHabitatProfileContributionSink,
        IShuttleHabitatJoyProfileContributionSink,
        IShuttleMedicalBayProfileContributionSink,
        IShuttlePrisonCellProfileContributionSink,
        IShuttleMechChargerProfileContributionSink,
        IShuttleHullProfileContributionSink,
        IShuttleFireControlProfileContributionSink
    {
        // These are defensive sanity caps for corrupted defs or third-party contributors.
        // They are deliberately high and are not balance parameters for normal gameplay.
        private const float MaxProfileFloatContribution = 1000000000000f;
        private const int MaxProfileIntContribution = 1000000000;
        private const uint MaxProfileUIntContribution = 1000000000u;

        private readonly ShuttleProfileAccumulator accumulator;
        private readonly IShuttleProfileIssueSink issueSink;
        private readonly string contributionReferenceID;
        private readonly ProfileBuildIssueScope contributionScope;

        internal ShuttleProfileAccumulatorContributionSink(ShuttleProfileAccumulator accumulator)
            : this(accumulator, null, null)
        {
        }

        internal ShuttleProfileAccumulatorContributionSink(
            ShuttleProfileAccumulator accumulator,
            IShuttleProfileIssueSink issueSink,
            string moduleInstanceID)
            : this(accumulator, issueSink, moduleInstanceID, ProfileBuildIssueScope.Module)
        {
        }

        internal ShuttleProfileAccumulatorContributionSink(
            ShuttleProfileAccumulator accumulator,
            IShuttleProfileIssueSink issueSink,
            string contributionReferenceID,
            ProfileBuildIssueScope contributionScope)
        {
            this.accumulator = accumulator;
            this.issueSink = issueSink ?? NullShuttleProfileIssueSink.Instance;
            this.contributionReferenceID = contributionReferenceID;
            this.contributionScope = contributionScope;
        }

        internal void AddSegmentMass(float mass)
        {
            if (this.accumulator != null)
            {
                this.accumulator.SegmentMass = this.AddNonNegativeFinite(
                    this.accumulator.SegmentMass,
                    mass,
                    "segment mass kg");
            }
        }

        internal void AddModuleMassKg(float massKg)
        {
            if (this.accumulator != null)
            {
                this.accumulator.ModuleMass = this.AddNonNegativeFinite(
                    this.accumulator.ModuleMass,
                    massKg,
                    "module mass kg");
            }
        }

        internal void AddInternalIdleDemandWatts(float watts)
        {
            if (this.accumulator != null)
            {
                this.accumulator.InternalIdleDemandWatts = this.AddNonNegativeFinite(
                    this.accumulator.InternalIdleDemandWatts,
                    watts,
                    "module idle power W");
            }
        }

        public void AddCrewCapacity(int count)
        {
            if (this.accumulator != null)
            {
                this.accumulator.CrewCapacity = this.AddNonNegativeInt(
                    this.accumulator.CrewCapacity,
                    count,
                    "crew capacity");
            }
        }

        public void AddEnergyStorageWd(float wattDays)
        {
            if (this.accumulator != null)
            {
                this.accumulator.EnergyStorageCapacityWd = this.AddNonNegativeFinite(
                    this.accumulator.EnergyStorageCapacityWd,
                    wattDays,
                    "energy storage Wd");
            }
        }

        public void AddReactorGenerationWatts(float watts)
        {
            if (this.accumulator != null)
            {
                this.accumulator.ReactorGenerationWatts = this.AddNonNegativeFinite(
                    this.accumulator.ReactorGenerationWatts,
                    watts,
                    "reactor generation W");
            }
        }

        public void AddGridExportCapacityWatts(float watts)
        {
            if (this.accumulator != null)
            {
                this.accumulator.GridExportCapacityWatts = this.AddNonNegativeFinite(
                    this.accumulator.GridExportCapacityWatts,
                    watts,
                    "grid export capacity W");
            }
        }

        public void AddMaxBatteryChargeWatts(float watts)
        {
            if (this.accumulator != null)
            {
                this.accumulator.MaxBatteryChargeWatts = this.AddNonNegativeFinite(
                    this.accumulator.MaxBatteryChargeWatts,
                    watts,
                    "max battery charge W");
            }
        }

        public void AddMaxBatteryDischargeWatts(float watts)
        {
            if (this.accumulator != null)
            {
                this.accumulator.MaxBatteryDischargeWatts = this.AddNonNegativeFinite(
                    this.accumulator.MaxBatteryDischargeWatts,
                    watts,
                    "max battery discharge W");
            }
        }

        internal void IncrementCargoRegionCount()
        {
            this.AddCargoRegionCount(1);
        }

        internal void AddCargoRegionCount(int count)
        {
            if (this.accumulator == null)
            {
                return;
            }

            if (count < 0)
            {
                this.ReportInvalidContribution("cargo region count", count.ToString());
                return;
            }

            this.accumulator.CargoRegionCount = this.AddNonNegativeUInt(
                this.accumulator.CargoRegionCount,
                (uint)count,
                "cargo region count");
        }

        public void AddCargoRegionCount(uint count)
        {
            if (this.accumulator != null)
            {
                this.accumulator.CargoRegionCount = this.AddNonNegativeUInt(
                    this.accumulator.CargoRegionCount,
                    count,
                    "cargo region count");
            }
        }

        public void AddRangeBonusTiles(int tiles)
        {
            if (this.accumulator != null)
            {
                this.accumulator.RangeBonusTiles = this.AddNonNegativeInt(
                    this.accumulator.RangeBonusTiles,
                    tiles,
                    "range bonus tiles");
            }
        }

        public void MultiplyLaunchCooldownFactor(float factor)
        {
            if (this.accumulator != null)
            {
                this.accumulator.LaunchCooldownFactor = this.MultiplyNonNegativeFinite(
                    this.accumulator.LaunchCooldownFactor,
                    factor,
                    "launch cooldown factor");
            }
        }

        public void MultiplyEnergyCostFactor(float factor)
        {
            if (this.accumulator != null)
            {
                this.accumulator.EnergyCostFactor = this.MultiplyPositiveFinite(
                    this.accumulator.EnergyCostFactor,
                    factor,
                    "energy cost factor");
            }
        }

        public void MultiplyLoadedMassEfficiencyFactor(float factor)
        {
            if (this.accumulator != null)
            {
                this.accumulator.LoadedMassEfficiencyFactor = this.MultiplyPositiveFinite(
                    this.accumulator.LoadedMassEfficiencyFactor,
                    factor,
                    "loaded mass efficiency factor");
            }
        }

        internal void MarkHasCockpit()
        {
            if (this.accumulator != null)
            {
                this.accumulator.HasCockpit = true;
            }
        }

        internal void MarkStabilizesAdverseWeather()
        {
            if (this.accumulator != null)
            {
                this.accumulator.StabilizesAdverseWeather = true;
            }
        }

        internal void MarkProvidesAutonomousLaunchControl()
        {
            if (this.accumulator != null)
            {
                this.accumulator.ProvidesAutonomousLaunchControl = true;
            }
        }

        internal void MarkProvidesSecureSignalLink()
        {
            if (this.accumulator != null)
            {
                this.accumulator.ProvidesSecureSignalLink = true;
            }
        }

        public void AddWeaponModule(
            ShuttleWeaponRole weaponRole,
            int mountCount,
            float standbyPowerDrawWatts,
            float activeFiringPowerDrawWatts,
            bool canAutoFire,
            bool canSetForcedTarget,
            bool requiresScanner,
            bool requiresNavigationComputer,
            int maxAmmo)
        {
            if (this.accumulator == null)
            {
                return;
            }

            int effectiveMountCount = mountCount > 0 ? mountCount : 1;
            this.accumulator.TotalWeaponModules = this.SaturatingAdd(this.accumulator.TotalWeaponModules, 1);
            this.accumulator.TotalWeaponMounts = this.AddNonNegativeInt(
                this.accumulator.TotalWeaponMounts,
                effectiveMountCount,
                "weapon mount count");

            if (weaponRole == ShuttleWeaponRole.PointDefense)
            {
                this.accumulator.PointDefenseModules = this.SaturatingAdd(this.accumulator.PointDefenseModules, 1);
                this.accumulator.PointDefenseMounts = this.AddNonNegativeInt(
                    this.accumulator.PointDefenseMounts,
                    effectiveMountCount,
                    "point-defense weapon mount count");
            }
            else if (weaponRole == ShuttleWeaponRole.CloseInWeapon)
            {
                this.accumulator.CloseInWeaponModules = this.SaturatingAdd(this.accumulator.CloseInWeaponModules, 1);
                this.accumulator.CloseInWeaponMounts = this.AddNonNegativeInt(
                    this.accumulator.CloseInWeaponMounts,
                    effectiveMountCount,
                    "close-in weapon mount count");
            }
            else if (weaponRole == ShuttleWeaponRole.Rocket)
            {
                this.accumulator.RocketLauncherModules = this.SaturatingAdd(this.accumulator.RocketLauncherModules, 1);
                this.accumulator.RocketLauncherSlots = this.AddNonNegativeInt(
                    this.accumulator.RocketLauncherSlots,
                    effectiveMountCount,
                    "rocket launcher slot count");
            }

            if (canAutoFire)
            {
                this.accumulator.AutoFireCapableModules = this.SaturatingAdd(this.accumulator.AutoFireCapableModules, 1);
            }

            if (canSetForcedTarget)
            {
                this.accumulator.ForcedTargetCapableModules = this.SaturatingAdd(this.accumulator.ForcedTargetCapableModules, 1);
            }

            if (requiresScanner)
            {
                this.accumulator.ScannerRequiredModules = this.SaturatingAdd(this.accumulator.ScannerRequiredModules, 1);
            }

            if (requiresNavigationComputer)
            {
                this.accumulator.NavigationRequiredModules = this.SaturatingAdd(this.accumulator.NavigationRequiredModules, 1);
            }

            if (maxAmmo != 0)
            {
                this.accumulator.TotalAmmoCapacity = this.AddNonNegativeInt(
                    this.accumulator.TotalAmmoCapacity,
                    maxAmmo,
                    "weapon ammo capacity");
            }

            if (this.IsFinitePositive(standbyPowerDrawWatts, "weapon standby power W"))
            {
                this.accumulator.WeaponStandbyPowerDrawWatts = this.AddPositiveFinite(
                    this.accumulator.WeaponStandbyPowerDrawWatts,
                    standbyPowerDrawWatts,
                    "weapon standby power W");
            }

            if (this.IsFinitePositive(activeFiringPowerDrawWatts, "weapon active firing power W"))
            {
                this.accumulator.MaxActiveFiringPowerDrawWatts = this.AddPositiveFinite(
                    this.accumulator.MaxActiveFiringPowerDrawWatts,
                    activeFiringPowerDrawWatts,
                    "weapon active firing power W");
            }
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
            if (this.accumulator == null)
            {
                return;
            }

            float sanitizedRadius = this.SanitizeNonNegativeFinite(
                pointDefenseRadius,
                "fire-control point-defense radius");
            float sanitizedAccuracyMultiplier = this.SanitizeNonNegativeFinite(
                directFireAccuracyMultiplier,
                "fire-control direct accuracy multiplier");
            float sanitizedAccuracyBonus = this.SanitizeNonNegativeFinite(
                directFireAccuracyBonus,
                "fire-control direct accuracy bonus");
            float sanitizedAccuracyFloor = this.ClampFinite(
                directFireAccuracyFloor,
                0f,
                1f,
                "fire-control direct accuracy floor");
            this.accumulator.FireControlModuleCount =
                this.SaturatingAdd(this.accumulator.FireControlModuleCount, 1);
            this.accumulator.FireControlSupportsAutoDefense |= supportsAutoDefense;
            this.accumulator.FireControlSupportsPointDefense |= supportsPointDefense;
            this.accumulator.FireControlMaxPointDefenseRadius = this.MaxNonNegativeFinite(
                this.accumulator.FireControlMaxPointDefenseRadius,
                sanitizedRadius,
                "fire-control point-defense radius");
            this.accumulator.FireControlDirectFireAccuracyMultiplier = this.MaxNonNegativeFinite(
                this.accumulator.FireControlDirectFireAccuracyMultiplier,
                sanitizedAccuracyMultiplier,
                "fire-control direct accuracy multiplier");
            this.accumulator.FireControlDirectFireAccuracyBonus = this.MaxNonNegativeFinite(
                this.accumulator.FireControlDirectFireAccuracyBonus,
                sanitizedAccuracyBonus,
                "fire-control direct accuracy bonus");
            this.accumulator.FireControlDirectFireAccuracyFloor = this.MaxNonNegativeFinite(
                this.accumulator.FireControlDirectFireAccuracyFloor,
                sanitizedAccuracyFloor,
                "fire-control direct accuracy floor");
            this.accumulator.FireControlForcedMissRadiusMultiplier = this.MinNonNegativeFinite(
                this.accumulator.FireControlForcedMissRadiusMultiplier,
                forcedMissRadiusMultiplier,
                "fire-control forced miss radius multiplier");
        }

        public void AddSurfaceShieldModule(
            int maxHitPoints,
            float rechargeEnergyPerHitPointWd)
        {
            if (this.accumulator == null)
            {
                return;
            }

            int sanitizedMaxHitPoints = this.SanitizeNonNegativeInt(
                maxHitPoints,
                "surface shield max hit points");
            float sanitizedRechargeCost = this.SanitizeNonNegativeFinite(
                rechargeEnergyPerHitPointWd,
                "surface shield recharge energy per HP Wd");

            this.accumulator.SurfaceShieldModuleCount =
                this.SaturatingAdd(this.accumulator.SurfaceShieldModuleCount, 1);
            this.accumulator.TotalSurfaceShieldMaxHitPoints = this.AddNonNegativeInt(
                this.accumulator.TotalSurfaceShieldMaxHitPoints,
                sanitizedMaxHitPoints,
                "surface shield max hit points");
            this.accumulator.BestSurfaceShieldMaxHitPoints = this.MaxNonNegativeInt(
                this.accumulator.BestSurfaceShieldMaxHitPoints,
                sanitizedMaxHitPoints,
                "surface shield best max hit points");

            // "Best" recharge cost is the lowest non-negative energy cost among installed
            // surface shields, representing the most efficient available recharge backend.
            if (!this.accumulator.HasSurfaceShieldRechargeEnergyCost ||
                sanitizedRechargeCost < this.accumulator.BestSurfaceShieldRechargeEnergyPerHitPointWd)
            {
                this.accumulator.BestSurfaceShieldRechargeEnergyPerHitPointWd = sanitizedRechargeCost;
                this.accumulator.HasSurfaceShieldRechargeEnergyCost = true;
            }
        }

        public void AddVanillaInterceptorShieldModule(int maxHitPoints)
        {
            if (this.accumulator == null)
            {
                return;
            }

            int sanitizedMaxHitPoints = this.SanitizeNonNegativeInt(
                maxHitPoints,
                "vanilla interceptor shield max hit points");
            this.accumulator.VanillaInterceptorShieldModuleCount =
                this.SaturatingAdd(this.accumulator.VanillaInterceptorShieldModuleCount, 1);
            this.accumulator.TotalVanillaInterceptorShieldMaxHitPoints = this.AddNonNegativeInt(
                this.accumulator.TotalVanillaInterceptorShieldMaxHitPoints,
                sanitizedMaxHitPoints,
                "vanilla interceptor shield max hit points");
            this.accumulator.BestVanillaInterceptorShieldMaxHitPoints = this.MaxNonNegativeInt(
                this.accumulator.BestVanillaInterceptorShieldMaxHitPoints,
                sanitizedMaxHitPoints,
                "vanilla interceptor shield best max hit points");
        }

        public void AddCargoLogisticsModule(
            bool supportsItemTransfer,
            bool supportsItemConsumption,
            bool supportsItemDeposit,
            bool supportsNutritionDistribution,
            int maxStacksMovedPerTick)
        {
            if (this.accumulator == null)
            {
                return;
            }

            this.accumulator.CargoLogisticsModuleCount =
                this.SaturatingAdd(this.accumulator.CargoLogisticsModuleCount, 1);
            this.accumulator.CargoLogisticsSupportsItemTransfer |= supportsItemTransfer;
            this.accumulator.CargoLogisticsSupportsItemConsumption |= supportsItemConsumption;
            this.accumulator.CargoLogisticsSupportsItemDeposit |= supportsItemDeposit;
            this.accumulator.CargoLogisticsSupportsNutritionDistribution |= supportsNutritionDistribution;

            this.accumulator.CargoLogisticsMaxStacksMovedPerTick =
                this.AddNonNegativeInt(
                    this.accumulator.CargoLogisticsMaxStacksMovedPerTick,
                    maxStacksMovedPerTick,
                    "cargo logistics stacks moved per tick");
        }

        public void AddHabitatModule(
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
            IReadOnlyList<ThingDef> preferredAutoFoodDefs)
        {
            if (this.accumulator == null)
            {
                return;
            }

            this.accumulator.HasHabitat = true;
            this.accumulator.HabitatAnySupportsSleep |= supportsSleep;
            this.accumulator.HabitatAnySupportsDining |= supportsDining;
            this.accumulator.HabitatSleepSlots = this.AddNonNegativeInt(
                this.accumulator.HabitatSleepSlots,
                sleepSlots,
                "habitat sleep slots");
            this.accumulator.HabitatDiningSlots = this.AddNonNegativeInt(
                this.accumulator.HabitatDiningSlots,
                diningSlots,
                "habitat dining slots");

            if (supportsSleep)
            {
                this.accumulator.HabitatSleepThoughtStageIndex = this.MaxNonNegativeInt(
                    this.accumulator.HabitatSleepThoughtStageIndex,
                    sleepThoughtStageIndex,
                    "habitat sleep thought stage index");
            }

            if (supportsDining)
            {
                this.accumulator.HabitatDiningThoughtStageIndex = this.MaxNonNegativeInt(
                    this.accumulator.HabitatDiningThoughtStageIndex,
                    diningThoughtStageIndex,
                    "habitat dining thought stage index");
            }

            this.accumulator.HabitatRestEffectiveness = this.MaxPositiveFinite(
                this.accumulator.HabitatRestEffectiveness,
                restEffectiveness,
                "habitat rest effectiveness");
            this.accumulator.HabitatSuppressSleepDisturbedThoughts |= suppressSleepDisturbedThoughts;
            this.accumulator.HabitatSuppressBarracksThoughts |= suppressBarracksThoughts;
            this.accumulator.HabitatAllowsInventoryFood |= allowInventoryFood;
            this.accumulator.HabitatAllowsCargoFoodWithdrawal |= allowCargoFoodWithdrawal;
            if (allowCargoFoodWithdrawal && requireCargoLogisticsForFoodWithdrawal)
            {
                this.accumulator.HabitatRequiresCargoLogisticsForFoodWithdrawal = true;
            }

            this.AddPreferredAutoFoodDefs(preferredAutoFoodDefs);
        }

        public void AddHabitatJoyModule(
            bool supportsJoy,
            int joySlots,
            int joyKindCapacity,
            int joyThoughtStageIndex,
            float joyGainFactor,
            IReadOnlyList<JoyKindDef> selectedJoyKinds)
        {
            if (this.accumulator == null)
            {
                return;
            }

            if (!supportsJoy)
            {
                return;
            }

            this.accumulator.HasHabitat = true;
            this.accumulator.HabitatAnySupportsJoy = true;
            this.accumulator.HabitatJoySlots = this.AddNonNegativeInt(
                this.accumulator.HabitatJoySlots,
                joySlots,
                "habitat joy slots");
            this.accumulator.HabitatJoyKindCapacity = this.MaxNonNegativeInt(
                this.accumulator.HabitatJoyKindCapacity,
                joyKindCapacity,
                "habitat joy kind capacity");
            this.accumulator.HabitatJoyThoughtStageIndex = this.MaxNonNegativeInt(
                this.accumulator.HabitatJoyThoughtStageIndex,
                joyThoughtStageIndex,
                "habitat joy thought stage index");
            this.accumulator.HabitatJoyGainFactor = this.MaxPositiveFinite(
                this.accumulator.HabitatJoyGainFactor,
                joyGainFactor,
                "habitat joy gain factor");
            this.AddHabitatJoyKinds(selectedJoyKinds);
        }

        public void AddMedicalBayModule(
            int medicalPatientSlots,
            bool supportsMedevacPriority,
            bool supportsStabilization,
            bool supportsPassiveComfort,
            float passiveJoyGainFactor,
            float passiveJoyCapPct,
            int comfortThoughtStageIndex)
        {
            if (this.accumulator == null)
            {
                return;
            }

            this.accumulator.HasMedicalBay = true;
            this.accumulator.MedicalPatientSlots = this.AddNonNegativeInt(
                this.accumulator.MedicalPatientSlots,
                medicalPatientSlots,
                "medical bay patient slots");
            this.accumulator.MedicalBaySupportsMedevacPriority |= supportsMedevacPriority;
            this.accumulator.MedicalBaySupportsStabilization |= supportsStabilization;
            this.accumulator.MedicalBaySupportsPassiveComfort |= supportsPassiveComfort;
            this.accumulator.MedicalBayPassiveJoyGainFactor = this.MaxPositiveFinite(
                this.accumulator.MedicalBayPassiveJoyGainFactor,
                passiveJoyGainFactor,
                "medical bay passive joy gain factor");
            this.accumulator.MedicalBayPassiveJoyCapPct = this.MaxPositiveFinite(
                this.accumulator.MedicalBayPassiveJoyCapPct,
                passiveJoyCapPct,
                "medical bay passive joy cap pct");
            if (this.accumulator.MedicalBayPassiveJoyCapPct > 1f)
            {
                this.ReportClampedContribution(
                    "medical bay passive joy cap pct",
                    this.FormatFloat(this.accumulator.MedicalBayPassiveJoyCapPct),
                    "1");
                this.accumulator.MedicalBayPassiveJoyCapPct = 1f;
            }

            this.accumulator.MedicalBayComfortThoughtStageIndex = this.MaxNonNegativeInt(
                this.accumulator.MedicalBayComfortThoughtStageIndex,
                comfortThoughtStageIndex,
                "medical bay comfort thought stage index");
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
            if (this.accumulator == null)
            {
                return;
            }

            this.accumulator.HasPrisonCell = true;
            this.accumulator.PrisonerSlots = this.AddNonNegativeInt(
                this.accumulator.PrisonerSlots,
                prisonerSlots,
                "prison cell prisoner slots");
            this.accumulator.PrisonCellMaxSecurity = this.MaxNonNegativeFinite(
                this.accumulator.PrisonCellMaxSecurity,
                security,
                "prison cell security");
            this.accumulator.PrisonCellMaxComfort = this.MaxNonNegativeFinite(
                this.accumulator.PrisonCellMaxComfort,
                comfort,
                "prison cell comfort");
            this.accumulator.PrisonCellSupportsFeeding |= supportsFeeding;
            this.accumulator.PrisonCellSupportsTending |= supportsTending;
            if (allowCargoFoodSupply)
            {
                this.accumulator.PrisonCellSupportsCargoFoodSupply = true;
                if (requireCargoLogisticsForFoodSupply)
                {
                    this.accumulator.PrisonCellRequiresCargoLogisticsForFoodSupply = true;
                }
                else
                {
                    this.accumulator.PrisonCellHasNoLogisticsRequiredFoodSupply = true;
                }

                this.accumulator.PrisonCellSupportsRefrigeratedCargoFoodSupply |=
                    allowRefrigeratedCargoFoodSupply;
                if (maximumCargoFoodPreferability <
                    this.accumulator.PrisonCellMaximumCargoFoodPreferability)
                {
                    this.accumulator.PrisonCellMaximumCargoFoodPreferability =
                        maximumCargoFoodPreferability;
                }
            }
        }

        public void AddMechChargerModule(
            int mechChargeSlots,
            float chargeRateFactor)
        {
            if (this.accumulator == null)
            {
                return;
            }

            this.accumulator.HasMechCharger = true;
            this.accumulator.MechChargeSlots = this.AddNonNegativeInt(
                this.accumulator.MechChargeSlots,
                mechChargeSlots,
                "mech charger slots");
            this.accumulator.MaxMechChargeRateFactor = this.MaxPositiveFinite(
                this.accumulator.MaxMechChargeRateFactor,
                chargeRateFactor,
                "mech charger charge rate factor");
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
            if (this.accumulator == null)
            {
                return;
            }

            bool firstHullArmorModule = this.accumulator.HullArmorModuleCount <= 0;
            this.accumulator.HullArmorModuleCount =
                this.SaturatingAdd(this.accumulator.HullArmorModuleCount, 1);
            this.accumulator.HullHitPointsBonus = this.AddNonNegativeInt(
                this.accumulator.HullHitPointsBonus,
                hullHitPointsBonus,
                "hull hit points bonus");

            if (firstHullArmorModule)
            {
                this.accumulator.HullSharpDamageMultiplier = this.SanitizePositiveFinite(
                    sharpDamageMultiplier,
                    "hull sharp damage multiplier");
                this.accumulator.HullBluntDamageMultiplier = this.SanitizePositiveFinite(
                    bluntDamageMultiplier,
                    "hull blunt damage multiplier");
                this.accumulator.HullHeatDamageMultiplier = this.SanitizePositiveFinite(
                    heatDamageMultiplier,
                    "hull heat damage multiplier");
                this.accumulator.HullExplosionDamageMultiplier = this.SanitizePositiveFinite(
                    explosionDamageMultiplier,
                    "hull explosion damage multiplier");
                this.accumulator.HullEmpDamageMultiplier = this.SanitizePositiveFinite(
                    empDamageMultiplier,
                    "hull EMP damage multiplier");
            }
            else
            {
                this.accumulator.HullSharpDamageMultiplier = this.MinPositiveFinite(
                    this.accumulator.HullSharpDamageMultiplier,
                    sharpDamageMultiplier,
                    "hull sharp damage multiplier");
                this.accumulator.HullBluntDamageMultiplier = this.MinPositiveFinite(
                    this.accumulator.HullBluntDamageMultiplier,
                    bluntDamageMultiplier,
                    "hull blunt damage multiplier");
                this.accumulator.HullHeatDamageMultiplier = this.MinPositiveFinite(
                    this.accumulator.HullHeatDamageMultiplier,
                    heatDamageMultiplier,
                    "hull heat damage multiplier");
                this.accumulator.HullExplosionDamageMultiplier = this.MinPositiveFinite(
                    this.accumulator.HullExplosionDamageMultiplier,
                    explosionDamageMultiplier,
                    "hull explosion damage multiplier");
                this.accumulator.HullEmpDamageMultiplier = this.MinPositiveFinite(
                    this.accumulator.HullEmpDamageMultiplier,
                    empDamageMultiplier,
                    "hull EMP damage multiplier");
            }

            this.accumulator.HullFlatDamageReduction = this.MaxNonNegativeFinite(
                this.accumulator.HullFlatDamageReduction,
                flatDamageReduction,
                "hull flat damage reduction");
        }

        private int SaturatingAdd(int current, int delta)
        {
            if (delta <= 0)
            {
                return current;
            }

            if (int.MaxValue - current < delta)
            {
                return int.MaxValue;
            }

            return current + delta;
        }

        private int AddNonNegativeInt(int current, int value, string fieldName)
        {
            if (value < 0)
            {
                this.ReportInvalidContribution(fieldName, value.ToString());
                return current;
            }

            int contribution = value;
            bool contributionWasClamped = false;
            if (contribution > MaxProfileIntContribution)
            {
                this.ReportClampedContribution(fieldName, value.ToString(), MaxProfileIntContribution.ToString());
                contribution = MaxProfileIntContribution;
                contributionWasClamped = true;
            }

            if (MaxProfileIntContribution - current < contribution)
            {
                if (!contributionWasClamped)
                {
                    this.ReportClampedContribution(
                        fieldName,
                        (current + (long)contribution).ToString(),
                        MaxProfileIntContribution.ToString());
                }

                return MaxProfileIntContribution;
            }

            return current + contribution;
        }

        private int MaxNonNegativeInt(int current, int value, string fieldName)
        {
            if (value < 0)
            {
                this.ReportInvalidContribution(fieldName, value.ToString());
                return current;
            }

            int contribution = value;
            if (contribution > MaxProfileIntContribution)
            {
                this.ReportClampedContribution(fieldName, value.ToString(), MaxProfileIntContribution.ToString());
                contribution = MaxProfileIntContribution;
            }

            return contribution > current ? contribution : current;
        }

        private int SanitizeNonNegativeInt(int value, string fieldName)
        {
            if (value < 0)
            {
                this.ReportInvalidContribution(fieldName, value.ToString());
                return 0;
            }

            if (value > MaxProfileIntContribution)
            {
                this.ReportClampedContribution(
                    fieldName,
                    value.ToString(),
                    MaxProfileIntContribution.ToString());
                return MaxProfileIntContribution;
            }

            return value;
        }

        private float SanitizeNonNegativeFinite(float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return 0f;
            }

            if (value > MaxProfileFloatContribution)
            {
                this.ReportClampedContribution(
                    fieldName,
                    this.FormatFloat(value),
                    MaxProfileFloatContribution.ToString("G"));
                return MaxProfileFloatContribution;
            }

            return value;
        }

        private uint AddNonNegativeUInt(uint current, uint value, string fieldName)
        {
            uint contribution = value;
            bool contributionWasClamped = false;
            if (contribution > MaxProfileUIntContribution)
            {
                this.ReportClampedContribution(fieldName, value.ToString(), MaxProfileUIntContribution.ToString());
                contribution = MaxProfileUIntContribution;
                contributionWasClamped = true;
            }

            if (MaxProfileUIntContribution - current < contribution)
            {
                if (!contributionWasClamped)
                {
                    this.ReportClampedContribution(
                        fieldName,
                        (current + (ulong)contribution).ToString(),
                        MaxProfileUIntContribution.ToString());
                }

                return MaxProfileUIntContribution;
            }

            return current + contribution;
        }

        private float AddNonNegativeFinite(float current, float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return current;
            }

            bool contributionWasClamped = value > MaxProfileFloatContribution;
            float contribution = this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
            double sum = (double)current + contribution;
            if (sum > MaxProfileFloatContribution)
            {
                if (!contributionWasClamped)
                {
                    this.ReportClampedContribution(
                        fieldName,
                        sum.ToString("G"),
                        MaxProfileFloatContribution.ToString("G"));
                }

                return MaxProfileFloatContribution;
            }

            return current + contribution;
        }

        private float AddPositiveFinite(float current, float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value <= 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return current;
            }

            bool contributionWasClamped = value > MaxProfileFloatContribution;
            float contribution = this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
            double sum = (double)current + contribution;
            if (sum > MaxProfileFloatContribution)
            {
                if (!contributionWasClamped)
                {
                    this.ReportClampedContribution(
                        fieldName,
                        sum.ToString("G"),
                        MaxProfileFloatContribution.ToString("G"));
                }

                return MaxProfileFloatContribution;
            }

            return current + contribution;
        }

        private float MultiplyPositiveFinite(float current, float factor, string fieldName)
        {
            if (!this.IsFiniteFloat(factor) || factor <= 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(factor));
                return current;
            }

            double product = (double)current * factor;
            if (product > MaxProfileFloatContribution)
            {
                this.ReportClampedContribution(
                    fieldName,
                    product.ToString("G"),
                    MaxProfileFloatContribution.ToString("G"));
                return MaxProfileFloatContribution;
            }

            return current * factor;
        }

        private float MultiplyNonNegativeFinite(float current, float factor, string fieldName)
        {
            if (!this.IsFiniteFloat(factor) || factor < 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(factor));
                return current;
            }

            if (factor == 0f || current == 0f)
            {
                return 0f;
            }

            double product = (double)current * factor;
            if (product > MaxProfileFloatContribution)
            {
                this.ReportClampedContribution(
                    fieldName,
                    product.ToString("G"),
                    MaxProfileFloatContribution.ToString("G"));
                return MaxProfileFloatContribution;
            }

            return current * factor;
        }

        private float MaxPositiveFinite(float current, float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value <= 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return current;
            }

            float contribution = this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
            return contribution > current ? contribution : current;
        }

        private float SanitizePositiveFinite(float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value <= 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return 1f;
            }

            return this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
        }

        private float MinPositiveFinite(float current, float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value <= 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return current;
            }

            float contribution = this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
            return contribution < current ? contribution : current;
        }

        private float MaxNonNegativeFinite(float current, float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return current;
            }

            float contribution = this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
            return contribution > current ? contribution : current;
        }

        private float MinNonNegativeFinite(float current, float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return current;
            }

            float contribution = this.ClampFinite(value, 0f, MaxProfileFloatContribution, fieldName);
            return contribution < current ? contribution : current;
        }

        private void AddPreferredAutoFoodDefs(IReadOnlyList<ThingDef> preferredAutoFoodDefs)
        {
            if (preferredAutoFoodDefs == null || this.accumulator == null)
            {
                return;
            }

            for (int i = 0; i < preferredAutoFoodDefs.Count; i++)
            {
                ThingDef foodDef = preferredAutoFoodDefs[i];
                if (foodDef != null && !this.accumulator.HabitatPreferredAutoFoodDefs.Contains(foodDef))
                {
                    this.accumulator.HabitatPreferredAutoFoodDefs.Add(foodDef);
                }
            }
        }

        private void AddHabitatJoyKinds(IReadOnlyList<JoyKindDef> joyKinds)
        {
            if (joyKinds == null || this.accumulator == null)
            {
                return;
            }

            for (int i = 0; i < joyKinds.Count; i++)
            {
                JoyKindDef joyKind = joyKinds[i];
                if (joyKind != null && !this.accumulator.HabitatJoyKinds.Contains(joyKind))
                {
                    this.accumulator.HabitatJoyKinds.Add(joyKind);
                }
            }
        }

        private float ClampFinite(float value, float min, float max, string fieldName)
        {
            if (!this.IsFiniteFloat(value))
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return min;
            }

            if (value > max)
            {
                this.ReportClampedContribution(fieldName, this.FormatFloat(value), max.ToString("G"));
                return max;
            }

            if (value < min)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return min;
            }

            return value;
        }

        private bool IsFinitePositive(float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value))
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return false;
            }

            if (value < 0f)
            {
                this.ReportInvalidContribution(fieldName, this.FormatFloat(value));
                return false;
            }

            return value > 0f;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private string FormatFloat(float value)
        {
            return value.ToString("G");
        }

        private void ReportInvalidContribution(string fieldName, string value)
        {
            this.issueSink.AddIssue(
                "profile-contribution-invalid",
                "CT_Shuttle_Issue_ProfileContributionInvalid".Translate(
                    this.GetContributionReference(),
                    fieldName,
                    value).ToString(),
                ProfileBuildIssueSeverity.Warning,
                this.contributionScope,
                this.contributionReferenceID);
        }

        private void ReportClampedContribution(string fieldName, string value, string maxValue)
        {
            this.issueSink.AddIssue(
                "profile-contribution-clamped",
                "CT_Shuttle_Issue_ProfileContributionClamped".Translate(
                    this.GetContributionReference(),
                    fieldName,
                    value,
                    maxValue).ToString(),
                ProfileBuildIssueSeverity.Warning,
                this.contributionScope,
                this.contributionReferenceID);
        }

        private string GetContributionReference()
        {
            if (!string.IsNullOrEmpty(this.contributionReferenceID))
            {
                return this.contributionReferenceID;
            }

            return this.contributionScope == ProfileBuildIssueScope.Segment
                ? "unknown segment"
                : "unknown module";
        }
    }
}
