using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    internal static class ShuttleCombatTuningSanitizer
    {
        internal static void Sanitize(ShuttleCombatTuningSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.WeaponCooldownMultiplier = ClampFinite(settings.WeaponCooldownMultiplier, 0.25f, 4f, 1f);
            settings.WeaponWarmupMultiplier = ClampFinite(settings.WeaponWarmupMultiplier, 0.25f, 4f, 1f);
            settings.WeaponScanIntervalMultiplier = ClampFinite(settings.WeaponScanIntervalMultiplier, 0.25f, 4f, 1f);
            settings.WeaponPowerDrawMultiplier = ClampFinite(settings.WeaponPowerDrawMultiplier, 0.1f, 10f, 1f);
            settings.WeaponAmmoCapacityMultiplier = ClampFinite(settings.WeaponAmmoCapacityMultiplier, 0.25f, 5f, 1f);
            settings.WeaponAutoFireRangeMultiplier = ClampFinite(settings.WeaponAutoFireRangeMultiplier, 0.25f, 3f, 1f);
            settings.WeaponAutomaticTopOffThreshold = Mathf.Clamp(
                settings.WeaponAutomaticTopOffThreshold,
                0,
                10000);

            settings.ShieldHitPointsMultiplier = ClampFinite(settings.ShieldHitPointsMultiplier, 0.25f, 5f, 1f);
            settings.ShieldRechargeRateMultiplier = ClampFinite(settings.ShieldRechargeRateMultiplier, 0.25f, 5f, 1f);
            settings.ShieldRechargeIntervalMultiplier = ClampFinite(settings.ShieldRechargeIntervalMultiplier, 0.25f, 4f, 1f);
            settings.ShieldRechargeEnergyCostMultiplier = ClampFinite(settings.ShieldRechargeEnergyCostMultiplier, 0.1f, 10f, 1f);
            settings.ShieldDamageTakenMultiplier = ClampFinite(settings.ShieldDamageTakenMultiplier, 0.1f, 5f, 1f);
            settings.ShieldEmpDamageTakenMultiplier = ClampFinite(settings.ShieldEmpDamageTakenMultiplier, 0.1f, 5f, 1f);
            settings.ShieldRadiusMultiplier = ClampFinite(settings.ShieldRadiusMultiplier, 0.5f, 3f, 1f);
            settings.ShieldBrokenDowntimeMultiplier = ClampFinite(settings.ShieldBrokenDowntimeMultiplier, 0.25f, 5f, 1f);

            settings.HullHitPointsMultiplier = ClampFinite(settings.HullHitPointsMultiplier, 0.25f, 5f, 1f);
            settings.ArmorHitPointsBonusMultiplier = ClampFinite(settings.ArmorHitPointsBonusMultiplier, 0.25f, 5f, 1f);
            settings.SharpDamageMultiplier = ClampFinite(settings.SharpDamageMultiplier, 0.1f, 5f, 1f);
            settings.BluntDamageMultiplier = ClampFinite(settings.BluntDamageMultiplier, 0.1f, 5f, 1f);
            settings.HeatDamageMultiplier = ClampFinite(settings.HeatDamageMultiplier, 0.1f, 5f, 1f);
            settings.ExplosionDamageMultiplier = ClampFinite(settings.ExplosionDamageMultiplier, 0.1f, 5f, 1f);
            settings.EmpDamageMultiplier = ClampFinite(settings.EmpDamageMultiplier, 0.1f, 5f, 1f);
            settings.FlatDamageReductionMultiplier = ClampFinite(settings.FlatDamageReductionMultiplier, 0f, 5f, 1f);

            settings.FireControlPointDefenseRadiusMultiplier = ClampFinite(settings.FireControlPointDefenseRadiusMultiplier, 0.25f, 3f, 1f);
            settings.FireControlAccuracyMultiplier = ClampFinite(settings.FireControlAccuracyMultiplier, 0.25f, 3f, 1f);
            settings.FireControlAccuracyBonus = ClampFinite(settings.FireControlAccuracyBonus, -0.5f, 0.5f, 0f);
            settings.FireControlAccuracyFloor = ClampFinite(settings.FireControlAccuracyFloor, 0f, 0.95f, 0f);
            settings.FireControlForcedMissRadiusMultiplier = ClampFinite(settings.FireControlForcedMissRadiusMultiplier, 0.25f, 3f, 1f);
        }

        internal static float ClampFinite(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            return Mathf.Clamp(value, min, max);
        }
    }
}
