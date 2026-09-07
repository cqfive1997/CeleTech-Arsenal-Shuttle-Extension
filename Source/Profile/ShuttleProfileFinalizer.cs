using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal sealed class ShuttleProfileFinalizer
    {
        private const float MaxDerivedEnergyWd = 1E+12f;
        private const float MaxDerivedMassKg = 1E+9f;
        private const float MaxDerivedCostWd = 1E+9f;
        private const float MaxProfileMultiplier = 1E+6f;
        private const int MaxDerivedRangeTiles = 10000000;
        private const int MaxDerivedCooldownTicks = 1000000000;
        private const uint MaxCargoRegionCount = 1000000u;

        private readonly ShuttleProfileIssueFactory issueFactory;

        internal ShuttleProfileFinalizer(ShuttleProfileIssueFactory issueFactory)
        {
            this.issueFactory = issueFactory;
        }

        internal ShuttleProfile BuildFinalProfile(
            ShuttleProfileBuildContext context,
            int revision,
            ProfileDirtyReason sourceReasons)
        {
            ShuttleProfileAccumulator accumulator = context.Accumulator;
            ShuttleProfileTuning tuning = this.EnsureTuning(context.Tuning);

            float segmentMassKg = this.ClampFiniteNonNegative(
                accumulator.SegmentMass,
                0f,
                MaxDerivedMassKg);
            float moduleMassKg = this.ClampFiniteNonNegative(
                accumulator.ModuleMass,
                0f,
                MaxDerivedMassKg);
            float energyStorageCapacityWd = this.ClampFiniteNonNegative(
                accumulator.EnergyStorageCapacityWd,
                0f,
                MaxDerivedEnergyWd);
            float energyCostFactor = this.ClampFiniteNonNegative(
                accumulator.EnergyCostFactor,
                1f,
                MaxProfileMultiplier);
            float loadedMassEfficiencyFactor = this.ClampFiniteNonNegative(
                accumulator.LoadedMassEfficiencyFactor,
                1f,
                MaxProfileMultiplier);

            uint cargoRegionCount = this.GetEffectiveCargoRegionCount(tuning, accumulator.CargoRegionCount);
            float reserveEnergyWd = this.GetReserveEnergyWd(tuning, energyStorageCapacityWd);
            int hardRangeCapTiles = this.GetEffectiveHardRangeCapTiles(tuning, accumulator.RangeBonusTiles);
            int launchCooldownTicks = this.GetEffectiveLaunchCooldownTicks(tuning, accumulator.LaunchCooldownFactor);
            float energyPerTileWd = this.MultiplyFiniteNonNegative(
                tuning.EnergyPerTileWd,
                energyCostFactor,
                MaxDerivedCostWd);
            float energyPerKgTileWd = this.MultiplyFiniteNonNegative(
                tuning.EnergyPerKgTileWd,
                loadedMassEfficiencyFactor,
                MaxDerivedCostWd);
            float payloadLiftEnergyPerKgWd = this.MultiplyFiniteNonNegative(
                tuning.PayloadLiftEnergyPerKgWd,
                loadedMassEfficiencyFactor,
                MaxDerivedCostWd);
            float structuralMassKg = this.AddFiniteNonNegative(
                segmentMassKg,
                moduleMassKg,
                MaxDerivedMassKg);
            ShuttleEffectiveCombatTuning combatTuning = CeleTechShuttleMod.EffectiveCombatTuning;
            int tunedHullHitPointsBonus = combatTuning.ApplyIntMultiplier(
                accumulator.HullHitPointsBonus,
                combatTuning.ArmorHitPointsBonusMultiplier,
                0,
                int.MaxValue);
            int hullMaxHitPoints = this.SaturatingAdd(
                tuning.BaseHullHitPoints,
                tunedHullHitPointsBonus);
            hullMaxHitPoints = combatTuning.ApplyIntMultiplier(
                hullMaxHitPoints,
                combatTuning.HullHitPointsMultiplier,
                hullMaxHitPoints > 0 ? 1 : 0,
                int.MaxValue);
            float hullSharpDamageMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.HullSharpDamageMultiplier,
                combatTuning.SharpDamageMultiplier,
                0f,
                float.MaxValue);
            float hullBluntDamageMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.HullBluntDamageMultiplier,
                combatTuning.BluntDamageMultiplier,
                0f,
                float.MaxValue);
            float hullHeatDamageMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.HullHeatDamageMultiplier,
                combatTuning.HeatDamageMultiplier,
                0f,
                float.MaxValue);
            float hullExplosionDamageMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.HullExplosionDamageMultiplier,
                combatTuning.ExplosionDamageMultiplier,
                0f,
                float.MaxValue);
            float hullEmpDamageMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.HullEmpDamageMultiplier,
                combatTuning.EmpDamageMultiplier,
                0f,
                float.MaxValue);
            float hullFlatDamageReduction = combatTuning.ApplyFloatMultiplier(
                accumulator.HullFlatDamageReduction,
                combatTuning.FlatDamageReductionMultiplier,
                0f,
                float.MaxValue);
            int totalAmmoCapacity = combatTuning.ApplyIntMultiplier(
                accumulator.TotalAmmoCapacity,
                combatTuning.WeaponAmmoCapacityMultiplier,
                accumulator.TotalAmmoCapacity > 0 ? 1 : 0,
                int.MaxValue);
            float maxActiveFiringPowerDrawWatts = combatTuning.ApplyFloatMultiplier(
                accumulator.MaxActiveFiringPowerDrawWatts,
                combatTuning.WeaponPowerDrawMultiplier,
                0f,
                float.MaxValue);
            float fireControlMaxPointDefenseRadius = combatTuning.ApplyFloatMultiplier(
                accumulator.FireControlMaxPointDefenseRadius,
                combatTuning.FireControlPointDefenseRadiusMultiplier,
                0f,
                float.MaxValue);
            float fireControlDirectFireAccuracyMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.FireControlDirectFireAccuracyMultiplier,
                combatTuning.FireControlAccuracyMultiplier,
                0f,
                float.MaxValue);
            float fireControlDirectFireAccuracyBonus = accumulator.FireControlDirectFireAccuracyBonus;
            if (accumulator.FireControlModuleCount > 0)
            {
                fireControlDirectFireAccuracyBonus += combatTuning.FireControlAccuracyBonus;
            }

            fireControlDirectFireAccuracyBonus = Mathf.Clamp(
                fireControlDirectFireAccuracyBonus,
                -1f,
                1f);
            float fireControlDirectFireAccuracyFloor = accumulator.FireControlDirectFireAccuracyFloor;
            if (combatTuning.FireControlAccuracyFloor > fireControlDirectFireAccuracyFloor)
            {
                fireControlDirectFireAccuracyFloor = combatTuning.FireControlAccuracyFloor;
            }

            fireControlDirectFireAccuracyFloor = Mathf.Clamp(
                fireControlDirectFireAccuracyFloor,
                0f,
                0.95f);
            float fireControlForcedMissRadiusMultiplier = combatTuning.ApplyFloatMultiplier(
                accumulator.FireControlForcedMissRadiusMultiplier,
                combatTuning.FireControlForcedMissRadiusMultiplier,
                0f,
                float.MaxValue);
            int totalVanillaShieldMaxHitPoints = combatTuning.ApplyIntMultiplier(
                accumulator.TotalVanillaInterceptorShieldMaxHitPoints,
                combatTuning.ShieldHitPointsMultiplier,
                accumulator.TotalVanillaInterceptorShieldMaxHitPoints > 0 ? 1 : 0,
                int.MaxValue);
            int bestVanillaShieldMaxHitPoints = combatTuning.ApplyIntMultiplier(
                accumulator.BestVanillaInterceptorShieldMaxHitPoints,
                combatTuning.ShieldHitPointsMultiplier,
                accumulator.BestVanillaInterceptorShieldMaxHitPoints > 0 ? 1 : 0,
                int.MaxValue);
            int totalSurfaceShieldMaxHitPoints = combatTuning.ApplyIntMultiplier(
                accumulator.TotalSurfaceShieldMaxHitPoints,
                combatTuning.ShieldHitPointsMultiplier,
                accumulator.TotalSurfaceShieldMaxHitPoints > 0 ? 1 : 0,
                int.MaxValue);
            int bestSurfaceShieldMaxHitPoints = combatTuning.ApplyIntMultiplier(
                accumulator.BestSurfaceShieldMaxHitPoints,
                combatTuning.ShieldHitPointsMultiplier,
                accumulator.BestSurfaceShieldMaxHitPoints > 0 ? 1 : 0,
                int.MaxValue);
            float bestSurfaceShieldRechargeEnergyPerHitPointWd = combatTuning.ApplyFloatMultiplier(
                accumulator.HasSurfaceShieldRechargeEnergyCost
                    ? accumulator.BestSurfaceShieldRechargeEnergyPerHitPointWd
                    : 0f,
                combatTuning.ShieldRechargeEnergyCostMultiplier,
                0f,
                float.MaxValue);
            float effectiveEnergyPerKgTileWd =
                ShuttlePayloadCapacityPolicy.ApplyToAuthoredEnergyCost(
                    energyPerKgTileWd);
            float effectivePayloadLiftEnergyPerKgWd =
                ShuttlePayloadCapacityPolicy.ApplyToAuthoredEnergyCost(
                    payloadLiftEnergyPerKgWd);
            float launchMassEnvelopeKg = this.GetLoadRangeMassCapacityKg(
                energyStorageCapacityWd,
                reserveEnergyWd,
                tuning.BaseLaunchEnergyWd,
                energyPerTileWd,
                energyPerKgTileWd,
                effectiveEnergyPerKgTileWd,
                effectivePayloadLiftEnergyPerKgWd,
                hardRangeCapTiles,
                structuralMassKg,
                context);
            launchMassEnvelopeKg = this.ClampFiniteNonNegative(
                launchMassEnvelopeKg,
                structuralMassKg,
                MaxDerivedMassKg);
            float cargoMassCapacityKg = this.ClampFiniteNonNegative(
                (float)((double)launchMassEnvelopeKg - structuralMassKg),
                0f,
                MaxDerivedMassKg);
            MassProfile massProfile = new MassProfile(segmentMassKg, moduleMassKg, launchMassEnvelopeKg);
            HullProfile hullProfile = new HullProfile(
                hullMaxHitPoints > 0,
                hullMaxHitPoints,
                accumulator.HullArmorModuleCount,
                hullSharpDamageMultiplier,
                hullBluntDamageMultiplier,
                hullHeatDamageMultiplier,
                hullExplosionDamageMultiplier,
                hullEmpDamageMultiplier,
                hullFlatDamageReduction,
                tuning.MinLaunchHullIntegrityPct,
                tuning.HullRepairMaterialStagingRadius);
            PowerProfile powerProfile = new PowerProfile(
                energyStorageCapacityWd,
                accumulator.ReactorGenerationWatts,
                accumulator.GridExportCapacityWatts,
                accumulator.InternalIdleDemandWatts,
                accumulator.MaxBatteryChargeWatts,
                accumulator.MaxBatteryDischargeWatts);
            FlightProfile flightProfile = new FlightProfile(
                energyStorageCapacityWd,
                tuning.BaseLaunchEnergyWd,
                energyPerTileWd,
                energyPerKgTileWd,
                payloadLiftEnergyPerKgWd,
                reserveEnergyWd,
                hardRangeCapTiles,
                launchCooldownTicks);
            WeaponProfile weaponProfile = new WeaponProfile(
                accumulator.TotalWeaponModules,
                accumulator.TotalWeaponMounts,
                accumulator.PointDefenseModules,
                accumulator.PointDefenseMounts,
                accumulator.CloseInWeaponModules,
                accumulator.CloseInWeaponMounts,
                accumulator.RocketLauncherModules,
                accumulator.RocketLauncherSlots,
                accumulator.AutoFireCapableModules,
                accumulator.ForcedTargetCapableModules,
                accumulator.ScannerRequiredModules,
                accumulator.NavigationRequiredModules,
                totalAmmoCapacity,
                accumulator.WeaponStandbyPowerDrawWatts,
                maxActiveFiringPowerDrawWatts);
            FireControlProfile fireControlProfile = new FireControlProfile(
                accumulator.FireControlModuleCount > 0,
                accumulator.FireControlModuleCount,
                accumulator.FireControlSupportsAutoDefense,
                accumulator.FireControlSupportsPointDefense,
                fireControlMaxPointDefenseRadius,
                fireControlDirectFireAccuracyMultiplier,
                fireControlDirectFireAccuracyBonus,
                fireControlDirectFireAccuracyFloor,
                fireControlForcedMissRadiusMultiplier);
            CargoLogisticsProfile cargoLogisticsProfile = new CargoLogisticsProfile(
                accumulator.CargoLogisticsModuleCount,
                accumulator.CargoLogisticsSupportsItemTransfer,
                accumulator.CargoLogisticsSupportsItemConsumption,
                accumulator.CargoLogisticsSupportsItemDeposit,
                accumulator.CargoLogisticsSupportsNutritionDistribution,
                accumulator.CargoLogisticsMaxStacksMovedPerTick);
            bool supportsHabitatSleep =
                accumulator.HabitatAnySupportsSleep && accumulator.HabitatSleepSlots > 0;
            bool supportsHabitatDining =
                accumulator.HabitatAnySupportsDining && accumulator.HabitatDiningSlots > 0;
            HabitatProfile habitatProfile = new HabitatProfile(
                accumulator.HasHabitat,
                supportsHabitatSleep,
                supportsHabitatDining,
                accumulator.HabitatSleepSlots,
                accumulator.HabitatDiningSlots,
                supportsHabitatSleep ? accumulator.HabitatSleepThoughtStageIndex : 0,
                supportsHabitatDining ? accumulator.HabitatDiningThoughtStageIndex : 0,
                accumulator.HabitatRestEffectiveness,
                accumulator.HabitatSuppressSleepDisturbedThoughts,
                accumulator.HabitatSuppressBarracksThoughts,
                accumulator.HabitatAllowsInventoryFood,
                accumulator.HabitatAllowsCargoFoodWithdrawal,
                accumulator.HabitatRequiresCargoLogisticsForFoodWithdrawal,
                accumulator.HabitatPreferredAutoFoodDefs,
                accumulator.HabitatAnySupportsJoy,
                accumulator.HabitatJoySlots,
                accumulator.HabitatJoyKindCapacity,
                accumulator.HabitatJoyThoughtStageIndex,
                accumulator.HabitatJoyGainFactor,
                accumulator.HabitatJoyKinds);
            MedicalBayProfile medicalBayProfile = new MedicalBayProfile(
                accumulator.HasMedicalBay,
                accumulator.MedicalPatientSlots,
                accumulator.MedicalBaySupportsMedevacPriority,
                accumulator.MedicalBaySupportsStabilization,
                accumulator.MedicalBaySupportsPassiveComfort,
                accumulator.MedicalBayPassiveJoyGainFactor,
                accumulator.MedicalBayPassiveJoyCapPct,
                accumulator.MedicalBayComfortThoughtStageIndex);
            PrisonCellProfile prisonCellProfile = new PrisonCellProfile(
                accumulator.HasPrisonCell,
                accumulator.PrisonerSlots,
                accumulator.PrisonCellMaxSecurity,
                accumulator.PrisonCellMaxComfort,
                accumulator.PrisonCellSupportsFeeding,
                accumulator.PrisonCellSupportsTending,
                accumulator.PrisonCellSupportsCargoFoodSupply,
                accumulator.PrisonCellSupportsRefrigeratedCargoFoodSupply,
                accumulator.PrisonCellRequiresCargoLogisticsForFoodSupply &&
                    !accumulator.PrisonCellHasNoLogisticsRequiredFoodSupply,
                context.PrisonCellSupplyConfig.CargoFoodSupplyEnabled,
                context.PrisonCellSupplyConfig.RefrigeratedFoodSupplyEnabled,
                this.GetEffectivePrisonCellMaximumFoodPreferability(
                    accumulator.PrisonCellMaximumCargoFoodPreferability,
                    context.PrisonCellSupplyConfig));
            MechChargerProfile mechChargerProfile = new MechChargerProfile(
                accumulator.HasMechCharger,
                accumulator.MechChargeSlots,
                accumulator.MaxMechChargeRateFactor);
            ShieldProfile shieldProfile = new ShieldProfile(
                accumulator.VanillaInterceptorShieldModuleCount > 0 ||
                    accumulator.SurfaceShieldModuleCount > 0,
                accumulator.VanillaInterceptorShieldModuleCount > 0,
                accumulator.SurfaceShieldModuleCount > 0,
                accumulator.VanillaInterceptorShieldModuleCount,
                accumulator.SurfaceShieldModuleCount,
                totalVanillaShieldMaxHitPoints,
                bestVanillaShieldMaxHitPoints,
                totalSurfaceShieldMaxHitPoints,
                bestSurfaceShieldMaxHitPoints,
                accumulator.HasSurfaceShieldRechargeEnergyCost
                    ? bestSurfaceShieldRechargeEnergyPerHitPointWd
                    : 0f);
            this.issueFactory.AddCockpitRequirementIssue(context.Issues, accumulator.HasCockpit);

            return new ShuttleProfile(
                revision,
                sourceReasons,
                new LayoutProfile(
                    context.SegmentEntries,
                    context.ModuleEntries,
                    context.SegmentSlotCount,
                    context.InstalledSegmentCount,
                    context.ModuleSlotCount,
                    context.InstalledModuleCount),
                massProfile,
                new CrewProfile(accumulator.CrewCapacity),
                hullProfile,
                habitatProfile,
                medicalBayProfile,
                prisonCellProfile,
                mechChargerProfile,
                new CommandProfile(
                    accumulator.HasCockpit,
                    accumulator.StabilizesAdverseWeather,
                    accumulator.ProvidesAutonomousLaunchControl,
                    accumulator.ProvidesSecureSignalLink),
                powerProfile,
                shieldProfile,
                fireControlProfile,
                weaponProfile,
                new CargoProfile(cargoRegionCount, cargoMassCapacityKg),
                cargoLogisticsProfile,
                flightProfile,
                new UILayoutProfile(context.SegmentSlotCount, context.ModuleSlotCount),
                context.ExternalExpressions != null
                    ? context.ExternalExpressions.ToSnapshot()
                    : null,
                context.Issues);
        }

        private float GetReserveEnergyWd(ShuttleProfileTuning tuning, float energyStorageCapacityWd)
        {
            tuning = this.EnsureTuning(tuning);
            float reserveFraction = this.ClampFiniteNonNegative(tuning.ReserveEnergyFraction, 0f, 1f);
            float capacityWd = this.ClampFiniteNonNegative(energyStorageCapacityWd, 0f, MaxDerivedEnergyWd);
            return this.MultiplyFiniteNonNegative(capacityWd, reserveFraction, MaxDerivedEnergyWd);
        }

        private float GetLoadRangeMassCapacityKg(
            float energyStorageCapacityWd,
            float reserveEnergyWd,
            float baseLaunchEnergyWd,
            float energyPerTileWd,
            float structuralEnergyPerKgTileWd,
            float payloadEnergyPerKgTileWd,
            float payloadLiftEnergyPerKgWd,
            int hardRangeCapTiles,
            float structuralMassKg,
            ShuttleProfileBuildContext context)
        {
            double capacityWd = this.ClampDoubleNonNegative(
                energyStorageCapacityWd,
                0d,
                MaxDerivedEnergyWd);
            if (capacityWd <= 0f)
            {
                return 0f;
            }

            // Cargo/mass capacity is a gross launch-mass envelope, not a raw Wd-to-kg copy.
            // It is solved from the same launch formula at the default hard range so adding
            // battery capacity raises useful load only when the shuttle can still pay range cost.
            double availableLaunchEnergyWd =
                capacityWd - this.ClampDoubleNonNegative(reserveEnergyWd, 0d, MaxDerivedEnergyWd);
            double distanceTiles = this.ClampDoubleNonNegative(
                hardRangeCapTiles,
                0d,
                MaxDerivedRangeTiles);
            double safeStructuralMassKg = this.ClampDoubleNonNegative(
                structuralMassKg,
                0d,
                MaxDerivedMassKg);
            double fixedRangeEnergyWd = this.SaturatingAdd(
                this.ClampDoubleNonNegative(baseLaunchEnergyWd, 0d, MaxDerivedEnergyWd),
                this.SaturatingAdd(
                    this.SaturatingMultiply(
                        distanceTiles,
                        this.ClampDoubleNonNegative(energyPerTileWd, 0d, MaxDerivedCostWd),
                        MaxDerivedEnergyWd),
                    this.SaturatingMultiply(
                        this.SaturatingMultiply(distanceTiles, safeStructuralMassKg, MaxDerivedEnergyWd),
                        this.ClampDoubleNonNegative(
                            structuralEnergyPerKgTileWd,
                            0d,
                            MaxDerivedCostWd),
                        MaxDerivedEnergyWd),
                    MaxDerivedEnergyWd),
                MaxDerivedEnergyWd);
            double massEnergyBudgetWd = availableLaunchEnergyWd - fixedRangeEnergyWd;
            if (massEnergyBudgetWd <= 0f)
            {
                return this.ClampDoubleToFloat(safeStructuralMassKg, 0f, MaxDerivedMassKg);
            }

            double massDenominator = this.SaturatingAdd(
                this.SaturatingMultiply(
                    distanceTiles,
                    this.ClampDoubleNonNegative(
                        payloadEnergyPerKgTileWd,
                        0d,
                        MaxDerivedCostWd),
                    MaxDerivedCostWd),
                this.ClampDoubleNonNegative(payloadLiftEnergyPerKgWd, 0d, MaxDerivedCostWd),
                MaxDerivedCostWd);
            if (massDenominator <= 0d || double.IsNaN(massDenominator) || double.IsInfinity(massDenominator))
            {
                this.issueFactory.AddIssue(
                    context.Issues,
                    "launch-mass-envelope-invalid",
                    "CT_Shuttle_Issue_LaunchMassEnvelopeInvalid".Translate().ToString(),
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Profile,
                    null);
                return 0f;
            }

            double launchMassEnvelopeKg =
                safeStructuralMassKg + (massEnergyBudgetWd / massDenominator);
            return this.ClampDoubleToFloat(launchMassEnvelopeKg, 0f, MaxDerivedMassKg);
        }

        private uint GetEffectiveCargoRegionCount(ShuttleProfileTuning tuning, uint contributedCargoRegionCount)
        {
            tuning = this.EnsureTuning(tuning);
            return this.SaturatingAdd(
                tuning.BaseCargoRegionCount,
                contributedCargoRegionCount,
                MaxCargoRegionCount);
        }

        private int GetEffectiveHardRangeCapTiles(ShuttleProfileTuning tuning, int rangeBonusTiles)
        {
            tuning = this.EnsureTuning(tuning);
            int baseRange = this.ClampIntNonNegative(tuning.HardRangeCapTiles, MaxDerivedRangeTiles);
            if (rangeBonusTiles <= 0)
            {
                return baseRange;
            }

            long projectedRange = (long)baseRange + rangeBonusTiles;
            return this.ClampIntNonNegative(projectedRange, MaxDerivedRangeTiles);
        }

        private int GetEffectiveLaunchCooldownTicks(ShuttleProfileTuning tuning, float launchCooldownFactor)
        {
            tuning = this.EnsureTuning(tuning);
            double factor = this.ClampDoubleNonNegative(launchCooldownFactor, 1d, MaxProfileMultiplier);

            double cooldownTicks = (double)this.ClampIntNonNegative(
                tuning.LaunchCooldownTicks,
                MaxDerivedCooldownTicks) * factor;
            return this.ClampIntNonNegative(cooldownTicks, MaxDerivedCooldownTicks);
        }

        private ShuttleProfileTuning EnsureTuning(ShuttleProfileTuning tuning)
        {
            return tuning ?? ShuttleProfileTuning.Fallback();
        }

        private float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        private bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private float ClampFiniteNonNegative(float value, float fallback, float max)
        {
            if (!this.IsFinite(value))
            {
                value = fallback;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float MultiplyFiniteNonNegative(float left, float right, float max)
        {
            double product =
                (double)this.ClampFiniteNonNegative(left, 0f, max) *
                this.ClampFiniteNonNegative(right, 1f, MaxProfileMultiplier);
            return this.ClampDoubleToFloat(product, 0f, max);
        }

        private float AddFiniteNonNegative(float left, float right, float max)
        {
            double sum =
                (double)this.ClampFiniteNonNegative(left, 0f, max) +
                this.ClampFiniteNonNegative(right, 0f, max);
            return this.ClampDoubleToFloat(sum, 0f, max);
        }

        private double ClampDoubleNonNegative(double value, double fallback, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                value = fallback;
            }

            if (value < 0d)
            {
                return 0d;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float ClampDoubleToFloat(double value, float fallback, float max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                value = fallback;
            }

            if (value < 0d)
            {
                return 0f;
            }

            if (value > max)
            {
                return max;
            }

            return (float)value;
        }

        private int ClampIntNonNegative(long value, int max)
        {
            if (value <= 0L)
            {
                return 0;
            }

            return value > max ? max : (int)value;
        }

        private int ClampIntNonNegative(double value, int max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                return 0;
            }

            return value > max ? max : (int)value;
        }

        private uint SaturatingAdd(uint left, uint right, uint max)
        {
            if (left >= max || right >= max || max - left < right)
            {
                return max;
            }

            return left + right;
        }

        private double SaturatingAdd(double left, double right, double max)
        {
            left = this.ClampDoubleNonNegative(left, 0d, max);
            right = this.ClampDoubleNonNegative(right, 0d, max);
            if (max - left < right)
            {
                return max;
            }

            return left + right;
        }

        private double SaturatingMultiply(double left, double right, double max)
        {
            left = this.ClampDoubleNonNegative(left, 0d, max);
            right = this.ClampDoubleNonNegative(right, 0d, max);
            if (left <= 0d || right <= 0d)
            {
                return 0d;
            }

            if (left > max / right)
            {
                return max;
            }

            return left * right;
        }

        private int SaturatingAdd(int left, int right)
        {
            if (left < 0)
            {
                left = 0;
            }

            if (right < 0)
            {
                right = 0;
            }

            if (int.MaxValue - left < right)
            {
                return int.MaxValue;
            }

            return left + right;
        }

        private FoodPreferability GetEffectivePrisonCellMaximumFoodPreferability(
            FoodPreferability moduleMaximum,
            ShuttlePrisonCellSupplyConfigState config)
        {
            FoodPreferability sanitizedModuleMaximum =
                ShuttlePrisonCellSupplyConfigState.SanitizeMaximumFoodPreferability(
                    moduleMaximum);
            FoodPreferability configuredMaximum = config != null
                ? config.MaximumFoodPreferability
                : FoodPreferability.MealAwful;
            configuredMaximum =
                ShuttlePrisonCellSupplyConfigState.SanitizeMaximumFoodPreferability(
                    configuredMaximum);
            return configuredMaximum < sanitizedModuleMaximum
                ? configuredMaximum
                : sanitizedModuleMaximum;
        }
    }
}
