using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    internal sealed class ShuttleEffectiveCombatTuning
    {
        internal readonly bool AdvancedCombatTuningEnabled;

        internal readonly float WeaponCooldownMultiplier;
        internal readonly float WeaponWarmupMultiplier;
        internal readonly float WeaponScanIntervalMultiplier;
        internal readonly float WeaponPowerDrawMultiplier;
        internal readonly float WeaponAmmoCapacityMultiplier;
        internal readonly float WeaponAutoFireRangeMultiplier;
        internal readonly int WeaponAutomaticTopOffThreshold;

        internal readonly float ShieldHitPointsMultiplier;
        internal readonly float ShieldRechargeRateMultiplier;
        internal readonly float ShieldRechargeIntervalMultiplier;
        internal readonly float ShieldRechargeEnergyCostMultiplier;
        internal readonly float ShieldDamageTakenMultiplier;
        internal readonly float ShieldEmpDamageTakenMultiplier;
        internal readonly float ShieldRadiusMultiplier;
        internal readonly float ShieldBrokenDowntimeMultiplier;

        internal readonly float HullHitPointsMultiplier;
        internal readonly float ArmorHitPointsBonusMultiplier;
        internal readonly float SharpDamageMultiplier;
        internal readonly float BluntDamageMultiplier;
        internal readonly float HeatDamageMultiplier;
        internal readonly float ExplosionDamageMultiplier;
        internal readonly float EmpDamageMultiplier;
        internal readonly float FlatDamageReductionMultiplier;

        internal readonly float FireControlPointDefenseRadiusMultiplier;
        internal readonly float FireControlAccuracyMultiplier;
        internal readonly float FireControlAccuracyBonus;
        internal readonly float FireControlAccuracyFloor;
        internal readonly float FireControlForcedMissRadiusMultiplier;

        private static readonly ShuttleEffectiveCombatTuning neutral = new ShuttleEffectiveCombatTuning();

        private ShuttleEffectiveCombatTuning()
            : this(ShuttleCombatTuningSettings.DefaultWeaponAutomaticTopOffThreshold)
        {
        }

        private ShuttleEffectiveCombatTuning(int weaponAutomaticTopOffThreshold)
        {
            this.AdvancedCombatTuningEnabled = false;

            this.WeaponCooldownMultiplier = 1f;
            this.WeaponWarmupMultiplier = 1f;
            this.WeaponScanIntervalMultiplier = 1f;
            this.WeaponPowerDrawMultiplier = 1f;
            this.WeaponAmmoCapacityMultiplier = 1f;
            this.WeaponAutoFireRangeMultiplier = 1f;
            this.WeaponAutomaticTopOffThreshold = Mathf.Clamp(
                weaponAutomaticTopOffThreshold,
                0,
                10000);

            this.ShieldHitPointsMultiplier = 1f;
            this.ShieldRechargeRateMultiplier = 1f;
            this.ShieldRechargeIntervalMultiplier = 1f;
            this.ShieldRechargeEnergyCostMultiplier = 1f;
            this.ShieldDamageTakenMultiplier = 1f;
            this.ShieldEmpDamageTakenMultiplier = 1f;
            this.ShieldRadiusMultiplier = 1f;
            this.ShieldBrokenDowntimeMultiplier = 1f;

            this.HullHitPointsMultiplier = 1f;
            this.ArmorHitPointsBonusMultiplier = 1f;
            this.SharpDamageMultiplier = 1f;
            this.BluntDamageMultiplier = 1f;
            this.HeatDamageMultiplier = 1f;
            this.ExplosionDamageMultiplier = 1f;
            this.EmpDamageMultiplier = 1f;
            this.FlatDamageReductionMultiplier = 1f;

            this.FireControlPointDefenseRadiusMultiplier = 1f;
            this.FireControlAccuracyMultiplier = 1f;
            this.FireControlAccuracyBonus = 0f;
            this.FireControlAccuracyFloor = 0f;
            this.FireControlForcedMissRadiusMultiplier = 1f;
        }

        private ShuttleEffectiveCombatTuning(ShuttleCombatTuningSettings settings)
        {
            this.AdvancedCombatTuningEnabled = true;

            this.WeaponCooldownMultiplier = settings.WeaponCooldownMultiplier;
            this.WeaponWarmupMultiplier = settings.WeaponWarmupMultiplier;
            this.WeaponScanIntervalMultiplier = settings.WeaponScanIntervalMultiplier;
            this.WeaponPowerDrawMultiplier = settings.WeaponPowerDrawMultiplier;
            this.WeaponAmmoCapacityMultiplier = settings.WeaponAmmoCapacityMultiplier;
            this.WeaponAutoFireRangeMultiplier = settings.WeaponAutoFireRangeMultiplier;
            this.WeaponAutomaticTopOffThreshold = settings.WeaponAutomaticTopOffThreshold;

            this.ShieldHitPointsMultiplier = settings.ShieldHitPointsMultiplier;
            this.ShieldRechargeRateMultiplier = settings.ShieldRechargeRateMultiplier;
            this.ShieldRechargeIntervalMultiplier = settings.ShieldRechargeIntervalMultiplier;
            this.ShieldRechargeEnergyCostMultiplier = settings.ShieldRechargeEnergyCostMultiplier;
            this.ShieldDamageTakenMultiplier = settings.ShieldDamageTakenMultiplier;
            this.ShieldEmpDamageTakenMultiplier = settings.ShieldEmpDamageTakenMultiplier;
            this.ShieldRadiusMultiplier = settings.ShieldRadiusMultiplier;
            this.ShieldBrokenDowntimeMultiplier = settings.ShieldBrokenDowntimeMultiplier;

            this.HullHitPointsMultiplier = settings.HullHitPointsMultiplier;
            this.ArmorHitPointsBonusMultiplier = settings.ArmorHitPointsBonusMultiplier;
            this.SharpDamageMultiplier = settings.SharpDamageMultiplier;
            this.BluntDamageMultiplier = settings.BluntDamageMultiplier;
            this.HeatDamageMultiplier = settings.HeatDamageMultiplier;
            this.ExplosionDamageMultiplier = settings.ExplosionDamageMultiplier;
            this.EmpDamageMultiplier = settings.EmpDamageMultiplier;
            this.FlatDamageReductionMultiplier = settings.FlatDamageReductionMultiplier;

            this.FireControlPointDefenseRadiusMultiplier = settings.FireControlPointDefenseRadiusMultiplier;
            this.FireControlAccuracyMultiplier = settings.FireControlAccuracyMultiplier;
            this.FireControlAccuracyBonus = settings.FireControlAccuracyBonus;
            this.FireControlAccuracyFloor = settings.FireControlAccuracyFloor;
            this.FireControlForcedMissRadiusMultiplier = settings.FireControlForcedMissRadiusMultiplier;
        }

        internal static ShuttleEffectiveCombatTuning Default
        {
            get
            {
                return neutral;
            }
        }

        internal static ShuttleEffectiveCombatTuning FromSettings(CeleTechShuttleModSettings settings)
        {
            if (settings == null || settings.CombatTuning == null)
            {
                return Default;
            }

            ShuttleCombatTuningSanitizer.Sanitize(settings.CombatTuning);
            if (!settings.CombatTuning.AdvancedCombatTuningEnabled)
            {
                return new ShuttleEffectiveCombatTuning(
                    settings.CombatTuning.WeaponAutomaticTopOffThreshold);
            }

            return new ShuttleEffectiveCombatTuning(settings.CombatTuning);
        }

        internal int ApplyTicksMultiplier(int baseTicks, float multiplier, int minTicks, int maxTicks)
        {
            if (baseTicks <= 0)
            {
                return Mathf.Clamp(baseTicks, minTicks, maxTicks);
            }

            return this.ClampRoundedToInt((double)baseTicks * multiplier, minTicks, maxTicks);
        }

        internal int ApplyIntMultiplier(int baseValue, float multiplier, int min, int max)
        {
            if (baseValue <= 0 && min <= 0)
            {
                return 0;
            }

            return this.ClampRoundedToInt((double)baseValue * multiplier, min, max);
        }

        internal float ApplyFloatMultiplier(float baseValue, float multiplier, float min, float max)
        {
            if (float.IsNaN(baseValue) || float.IsInfinity(baseValue))
            {
                baseValue = 0f;
            }

            return Mathf.Clamp(baseValue * multiplier, min, max);
        }

        internal float ApplyAccuracy(float baseAccuracy)
        {
            if (float.IsNaN(baseAccuracy) || float.IsInfinity(baseAccuracy))
            {
                baseAccuracy = 0f;
            }

            float result = (baseAccuracy * this.FireControlAccuracyMultiplier) + this.FireControlAccuracyBonus;
            if (result < this.FireControlAccuracyFloor)
            {
                result = this.FireControlAccuracyFloor;
            }

            return Mathf.Clamp01(result);
        }

        private int ClampRoundedToInt(double value, int min, int max)
        {
            if (max < min)
            {
                max = min;
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return max;
            }

            if (value <= min)
            {
                return min;
            }

            if (value >= max)
            {
                return max;
            }

            return Mathf.Clamp(Mathf.RoundToInt((float)value), min, max);
        }
    }
}
