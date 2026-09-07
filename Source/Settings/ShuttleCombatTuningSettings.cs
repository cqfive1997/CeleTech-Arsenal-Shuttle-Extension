using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    public sealed class ShuttleCombatTuningSettings : IExposable
    {
        internal const int DefaultWeaponAutomaticTopOffThreshold = 20;

        public float WeaponCooldownMultiplier = 1f;
        public float WeaponWarmupMultiplier = 1f;
        public float WeaponScanIntervalMultiplier = 1f;
        public float WeaponPowerDrawMultiplier = 1f;
        public float WeaponAmmoCapacityMultiplier = 1f;
        public float WeaponAutoFireRangeMultiplier = 1f;
        public int WeaponAutomaticTopOffThreshold = DefaultWeaponAutomaticTopOffThreshold;

        public float ShieldHitPointsMultiplier = 1f;
        public float ShieldRechargeRateMultiplier = 1f;
        public float ShieldRechargeIntervalMultiplier = 1f;
        public float ShieldRechargeEnergyCostMultiplier = 1f;
        public float ShieldDamageTakenMultiplier = 1f;
        public float ShieldEmpDamageTakenMultiplier = 1f;
        public float ShieldRadiusMultiplier = 1f;
        public float ShieldBrokenDowntimeMultiplier = 1f;

        public float HullHitPointsMultiplier = 1f;
        public float ArmorHitPointsBonusMultiplier = 1f;
        public float SharpDamageMultiplier = 1f;
        public float BluntDamageMultiplier = 1f;
        public float HeatDamageMultiplier = 1f;
        public float ExplosionDamageMultiplier = 1f;
        public float EmpDamageMultiplier = 1f;
        public float FlatDamageReductionMultiplier = 1f;

        public float FireControlPointDefenseRadiusMultiplier = 1f;
        public float FireControlAccuracyMultiplier = 1f;
        public float FireControlAccuracyBonus = 0f;
        public float FireControlAccuracyFloor = 0f;
        public float FireControlForcedMissRadiusMultiplier = 1f;

        public bool AdvancedCombatTuningEnabled = false;

        internal void ResetToDefaults()
        {
            this.WeaponCooldownMultiplier = 1f;
            this.WeaponWarmupMultiplier = 1f;
            this.WeaponScanIntervalMultiplier = 1f;
            this.WeaponPowerDrawMultiplier = 1f;
            this.WeaponAmmoCapacityMultiplier = 1f;
            this.WeaponAutoFireRangeMultiplier = 1f;
            this.WeaponAutomaticTopOffThreshold = DefaultWeaponAutomaticTopOffThreshold;

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

            this.AdvancedCombatTuningEnabled = false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.WeaponCooldownMultiplier, "WeaponCooldownMultiplier", 1f);
            Scribe_Values.Look(ref this.WeaponWarmupMultiplier, "WeaponWarmupMultiplier", 1f);
            Scribe_Values.Look(ref this.WeaponScanIntervalMultiplier, "WeaponScanIntervalMultiplier", 1f);
            Scribe_Values.Look(ref this.WeaponPowerDrawMultiplier, "WeaponPowerDrawMultiplier", 1f);
            Scribe_Values.Look(ref this.WeaponAmmoCapacityMultiplier, "WeaponAmmoCapacityMultiplier", 1f);
            Scribe_Values.Look(ref this.WeaponAutoFireRangeMultiplier, "WeaponAutoFireRangeMultiplier", 1f);
            Scribe_Values.Look(
                ref this.WeaponAutomaticTopOffThreshold,
                "WeaponAutomaticTopOffThreshold",
                DefaultWeaponAutomaticTopOffThreshold);

            Scribe_Values.Look(ref this.ShieldHitPointsMultiplier, "ShieldHitPointsMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldRechargeRateMultiplier, "ShieldRechargeRateMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldRechargeIntervalMultiplier, "ShieldRechargeIntervalMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldRechargeEnergyCostMultiplier, "ShieldRechargeEnergyCostMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldDamageTakenMultiplier, "ShieldDamageTakenMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldEmpDamageTakenMultiplier, "ShieldEmpDamageTakenMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldRadiusMultiplier, "ShieldRadiusMultiplier", 1f);
            Scribe_Values.Look(ref this.ShieldBrokenDowntimeMultiplier, "ShieldBrokenDowntimeMultiplier", 1f);

            Scribe_Values.Look(ref this.HullHitPointsMultiplier, "HullHitPointsMultiplier", 1f);
            Scribe_Values.Look(ref this.ArmorHitPointsBonusMultiplier, "ArmorHitPointsBonusMultiplier", 1f);
            Scribe_Values.Look(ref this.SharpDamageMultiplier, "SharpDamageMultiplier", 1f);
            Scribe_Values.Look(ref this.BluntDamageMultiplier, "BluntDamageMultiplier", 1f);
            Scribe_Values.Look(ref this.HeatDamageMultiplier, "HeatDamageMultiplier", 1f);
            Scribe_Values.Look(ref this.ExplosionDamageMultiplier, "ExplosionDamageMultiplier", 1f);
            Scribe_Values.Look(ref this.EmpDamageMultiplier, "EmpDamageMultiplier", 1f);
            Scribe_Values.Look(ref this.FlatDamageReductionMultiplier, "FlatDamageReductionMultiplier", 1f);

            Scribe_Values.Look(ref this.FireControlPointDefenseRadiusMultiplier, "FireControlPointDefenseRadiusMultiplier", 1f);
            Scribe_Values.Look(ref this.FireControlAccuracyMultiplier, "FireControlAccuracyMultiplier", 1f);
            Scribe_Values.Look(ref this.FireControlAccuracyBonus, "FireControlAccuracyBonus", 0f);
            Scribe_Values.Look(ref this.FireControlAccuracyFloor, "FireControlAccuracyFloor", 0f);
            Scribe_Values.Look(ref this.FireControlForcedMissRadiusMultiplier, "FireControlForcedMissRadiusMultiplier", 1f);

            Scribe_Values.Look(ref this.AdvancedCombatTuningEnabled, "AdvancedCombatTuningEnabled", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ShuttleCombatTuningSanitizer.Sanitize(this);
            }
        }
    }
}
