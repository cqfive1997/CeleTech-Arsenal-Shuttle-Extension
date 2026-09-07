namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal static class CombatTuningSettingDefinitions
    {
        internal static readonly CombatTuningCategoryDefinition[] Categories =
        {
            new CombatTuningCategoryDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_Category_Weapon", "CT_Shuttle_CombatTuning_SandboxWarning", null),
            new CombatTuningCategoryDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_Category_Shield", "CT_Shuttle_CombatTuning_Help_Shield", "CT_Shuttle_CombatTuning_Help_ShieldSurfaceScope"),
            new CombatTuningCategoryDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_Category_Armor", "CT_Shuttle_CombatTuning_SandboxWarning", null),
            new CombatTuningCategoryDefinition(ShuttleCombatTuningCategory.FireControl, "CT_Shuttle_CombatTuning_Category_FireControl", "CT_Shuttle_CombatTuning_SandboxWarning", null),
            new CombatTuningCategoryDefinition(ShuttleCombatTuningCategory.Advanced, "CT_Shuttle_CombatTuning_Category_Advanced", "CT_Shuttle_CombatTuning_EnableAdvancedTooltip", null),
        };

        internal static readonly DefenseSettingDefinition[] Settings =
        {
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_WeaponCooldown", "CT_Shuttle_CombatTuning_WeaponCooldownTooltip", 0.25f, 4f, 0.05f, s => s.WeaponCooldownMultiplier, (s, v) => s.WeaponCooldownMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_WeaponWarmup", null, 0.25f, 4f, 0.05f, s => s.WeaponWarmupMultiplier, (s, v) => s.WeaponWarmupMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_WeaponScanInterval", null, 0.25f, 4f, 0.05f, s => s.WeaponScanIntervalMultiplier, (s, v) => s.WeaponScanIntervalMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_WeaponPowerDraw", null, 0.1f, 10f, 0.1f, s => s.WeaponPowerDrawMultiplier, (s, v) => s.WeaponPowerDrawMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_WeaponAmmoCapacity", null, 0.25f, 5f, 0.05f, s => s.WeaponAmmoCapacityMultiplier, (s, v) => s.WeaponAmmoCapacityMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Weapon, "CT_Shuttle_CombatTuning_WeaponAutoFireRange", null, 0.25f, 3f, 0.05f, s => s.WeaponAutoFireRangeMultiplier, (s, v) => s.WeaponAutoFireRangeMultiplier = v),

            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldHitPoints", null, 0.25f, 5f, 0.05f, s => s.ShieldHitPointsMultiplier, (s, v) => s.ShieldHitPointsMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldRechargeRate", "CT_Shuttle_CombatTuning_Help_ShieldSurfaceScope", 0.25f, 5f, 0.05f, s => s.ShieldRechargeRateMultiplier, (s, v) => s.ShieldRechargeRateMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldRechargeInterval", "CT_Shuttle_CombatTuning_Help_ShieldSurfaceScope", 0.25f, 4f, 0.05f, s => s.ShieldRechargeIntervalMultiplier, (s, v) => s.ShieldRechargeIntervalMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldRechargeEnergyCost", null, 0.1f, 10f, 0.1f, s => s.ShieldRechargeEnergyCostMultiplier, (s, v) => s.ShieldRechargeEnergyCostMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldDamageTaken", "CT_Shuttle_CombatTuning_Help_ShieldSurfaceScope", 0.1f, 5f, 0.05f, s => s.ShieldDamageTakenMultiplier, (s, v) => s.ShieldDamageTakenMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldEmpDamageTaken", "CT_Shuttle_CombatTuning_Help_ShieldSurfaceScope", 0.1f, 5f, 0.05f, s => s.ShieldEmpDamageTakenMultiplier, (s, v) => s.ShieldEmpDamageTakenMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldRadius", "CT_Shuttle_CombatTuning_Help_ShieldSurfaceScope", 0.5f, 3f, 0.05f, s => s.ShieldRadiusMultiplier, (s, v) => s.ShieldRadiusMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Shield, "CT_Shuttle_CombatTuning_ShieldBrokenDowntime", "CT_Shuttle_CombatTuning_Help_ShieldBrokenDowntimeScope", 0.25f, 5f, 0.05f, s => s.ShieldBrokenDowntimeMultiplier, (s, v) => s.ShieldBrokenDowntimeMultiplier = v),

            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_HullHitPoints", null, 0.25f, 5f, 0.05f, s => s.HullHitPointsMultiplier, (s, v) => s.HullHitPointsMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_ArmorHitPointsBonus", null, 0.25f, 5f, 0.05f, s => s.ArmorHitPointsBonusMultiplier, (s, v) => s.ArmorHitPointsBonusMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_SharpDamage", null, 0.1f, 5f, 0.05f, s => s.SharpDamageMultiplier, (s, v) => s.SharpDamageMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_BluntDamage", null, 0.1f, 5f, 0.05f, s => s.BluntDamageMultiplier, (s, v) => s.BluntDamageMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_HeatDamage", null, 0.1f, 5f, 0.05f, s => s.HeatDamageMultiplier, (s, v) => s.HeatDamageMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_ExplosionDamage", null, 0.1f, 5f, 0.05f, s => s.ExplosionDamageMultiplier, (s, v) => s.ExplosionDamageMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_EmpDamage", null, 0.1f, 5f, 0.05f, s => s.EmpDamageMultiplier, (s, v) => s.EmpDamageMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.Armor, "CT_Shuttle_CombatTuning_FlatDamageReduction", null, 0f, 5f, 0.05f, s => s.FlatDamageReductionMultiplier, (s, v) => s.FlatDamageReductionMultiplier = v),

            new DefenseSettingDefinition(ShuttleCombatTuningCategory.FireControl, "CT_Shuttle_CombatTuning_PointDefenseRadius", null, 0.25f, 3f, 0.05f, s => s.FireControlPointDefenseRadiusMultiplier, (s, v) => s.FireControlPointDefenseRadiusMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.FireControl, "CT_Shuttle_CombatTuning_AccuracyMultiplier", null, 0.25f, 3f, 0.05f, s => s.FireControlAccuracyMultiplier, (s, v) => s.FireControlAccuracyMultiplier = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.FireControl, "CT_Shuttle_CombatTuning_AccuracyBonus", null, -0.5f, 0.5f, 0.01f, s => s.FireControlAccuracyBonus, (s, v) => s.FireControlAccuracyBonus = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.FireControl, "CT_Shuttle_CombatTuning_AccuracyFloor", null, 0f, 0.95f, 0.01f, s => s.FireControlAccuracyFloor, (s, v) => s.FireControlAccuracyFloor = v),
            new DefenseSettingDefinition(ShuttleCombatTuningCategory.FireControl, "CT_Shuttle_CombatTuning_ForcedMissRadius", null, 0.25f, 3f, 0.05f, s => s.FireControlForcedMissRadiusMultiplier, (s, v) => s.FireControlForcedMissRadiusMultiplier = v),
        };

        internal static CombatTuningCategoryDefinition GetCategory(
            ShuttleCombatTuningCategory category)
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                if (Categories[i].Category == category)
                {
                    return Categories[i];
                }
            }

            return Categories[0];
        }
    }
}
