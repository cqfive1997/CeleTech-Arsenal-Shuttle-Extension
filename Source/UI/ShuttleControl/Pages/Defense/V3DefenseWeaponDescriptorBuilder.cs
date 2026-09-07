using System;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseWeaponDescriptorBuilder
    {
        internal ShuttleWeaponModuleDef GetWeaponModuleDef(string moduleDefName)
        {
            return string.IsNullOrEmpty(moduleDefName)
                ? null
                : DefDatabase<ShuttleWeaponModuleDef>.GetNamedSilentFail(moduleDefName);
        }

        internal string ResolveModuleDisplayName(
            string defName,
            string label,
            string fallback)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(
                defName,
                label,
                fallback);
        }

        internal string GetDefDisplayName(Def def, string fallback)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(
                def,
                fallback);
        }

        internal string GetWeaponRoleFallbackLabel(ShuttleWeaponModuleDef weaponDef)
        {
            return weaponDef != null
                ? weaponDef.weaponRole.ToString()
                : ShuttleUIText.Tr("CT_Shuttle_Defense_UnknownWeaponType");
        }

        internal string GetDamageLabel(ShuttleWeaponModuleDef weaponDef)
        {
            return ShuttleUIText.Tr("CT_Shuttle_Defense_Unavailable");
        }

        internal string GetCooldownLabel(ShuttleWeaponModuleDef weaponDef)
        {
            if (weaponDef == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Defense_Unavailable");
            }

            return (Mathf.Max(0, weaponDef.cooldownTicks) / 60f).ToString("0.#") +
                "s";
        }

        internal string GetFireRateLabel(ShuttleWeaponModuleDef weaponDef)
        {
            int cooldownTicks = this.GetEffectiveCooldownTicks(weaponDef);
            if (cooldownTicks <= 0)
            {
                return "-";
            }

            float rpm = 3600f / cooldownTicks;
            string value = rpm >= 10f
                ? Mathf.RoundToInt(rpm).ToString()
                : rpm.ToString("0.0");
            return ShuttleUIText.Tr("CT_Shuttle_Defense_WeaponFireRateValue", value);
        }

        internal string GetWeaponIconKey(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponControlReadModel control)
        {
            string text = ((weaponDef != null ? weaponDef.defName : string.Empty) + " " +
                (control != null ? control.ModuleDefName : string.Empty) + " " +
                (control != null ? control.WeaponRoleLabel : string.Empty)).ToLowerInvariant();
            if (text.Contains("70") || text.Contains("rocket") || text.Contains("missile"))
            {
                return "weapons";
            }

            if (text.Contains("6mm") || text.Contains("point"))
            {
                return "module_weapon_6mm_point";
            }

            if (text.Contains("hpj") || text.Contains("particle") || text.Contains("lance"))
            {
                return "module_weapon_particle_lance";
            }

            return "module_weapon_40mm_close_in";
        }

        internal string GetWeaponStatusKey(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return ShuttleUIText.StatusOffline;
            }

            if (!weapon.IsEnabled)
            {
                return ShuttleUIText.StatusDisabled;
            }

            if (!weapon.IsOnline)
            {
                return ShuttleUIText.StatusOffline;
            }

            if (weapon.IsDormant)
            {
                return ShuttleUIText.StatusDormant;
            }

            return weapon.HoldFire
                ? ShuttleUIText.StatusHoldFire
                : ShuttleUIText.StatusOnline;
        }

        internal string GetWeaponStatusLabel(string statusKey)
        {
            if (statusKey == ShuttleUIText.StatusHoldFire)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Defense_HoldFire");
            }

            if (statusKey == ShuttleUIText.StatusDormant)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Dormant");
            }

            if (statusKey == ShuttleUIText.StatusDisabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Disabled");
            }

            return statusKey == ShuttleUIText.StatusOffline
                ? ShuttleUIText.Tr("CT_Shuttle_Defense_Offline")
                : ShuttleUIText.Tr("CT_Shuttle_Defense_Online");
        }

        internal Vector2 GetWeaponAnchor(int index)
        {
            float[] xs = { 0.26f, 0.74f, 0.34f, 0.66f, 0.50f, 0.22f, 0.78f, 0.50f };
            float[] ys = { 0.34f, 0.34f, 0.50f, 0.50f, 0.24f, 0.64f, 0.64f, 0.72f };
            int safeIndex = Mathf.Abs(index) % xs.Length;
            return new Vector2(xs[safeIndex], ys[safeIndex]);
        }

        internal Vector2 GetWeaponAnchorForSlotIndex(int slotIndex, int fallbackIndex)
        {
            if (slotIndex == 1)
            {
                return new Vector2(0.24f, 0.66f);
            }

            if (slotIndex == 2)
            {
                return new Vector2(0.50f, 0.58f);
            }

            if (slotIndex == 3)
            {
                return new Vector2(0.76f, 0.64f);
            }

            return this.GetWeaponAnchor(fallbackIndex);
        }

        private int GetEffectiveCooldownTicks(ShuttleWeaponModuleDef weaponDef)
        {
            if (weaponDef == null || weaponDef.cooldownTicks <= 0)
            {
                return 0;
            }

            ShuttleEffectiveCombatTuning tuning = CeleTechShuttleMod.EffectiveCombatTuning;
            if (tuning == null)
            {
                return Mathf.Max(1, weaponDef.cooldownTicks);
            }

            return tuning.ApplyTicksMultiplier(
                weaponDef.cooldownTicks,
                tuning.WeaponCooldownMultiplier,
                1,
                int.MaxValue);
        }
    }
}
