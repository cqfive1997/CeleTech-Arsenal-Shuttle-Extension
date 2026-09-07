using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseTooltipBuilder
    {
        internal string BuildWeaponTooltip(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            string tooltip =
                weapon.Label + "\n" +
                this.GetWeaponSlotTooltipLine(weapon) + "\n" +
                weapon.WeaponTypeLabel + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_Damage") + ": " +
                weapon.DamageLabel + " / " +
                ShuttleUIText.Tr("CT_Shuttle_Defense_WeaponFireRate") + ": " +
                weapon.FireRateLabel + "\n" +
                this.GetWeaponDataSourceTooltip() + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_Status") + ": " +
                weapon.StatusLabel + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_ForcedTarget") + ": " +
                weapon.ForcedTargetLabel;

            if (!string.IsNullOrEmpty(weapon.LastForcedTargetFailureReason))
            {
                tooltip += "\n" +
                    ShuttleUIText.Tr("CT_Shuttle_Defense_LastForcedTargetFailure") + ": " +
                    weapon.LastForcedTargetFailureReason;
            }

            if (weapon.HasAmmoSystem)
            {
                tooltip += "\n" +
                    ShuttleUIText.Tr("CT_Shuttle_WeaponAmmo_Ammo") + ": " +
                    weapon.SelectedAmmoLabel + " (" +
                    weapon.LoadedAmmoCount.ToString() + "/" +
                    weapon.MagazineCapacity.ToString() + ")";
            }

            return tooltip + "\n" + this.BuildFireControlTooltip(weapon);
        }

        internal string BuildSlotOnlyWeaponTooltip(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            return weapon.Label + "\n" +
                this.GetWeaponSlotTooltipLine(weapon) + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_WeaponControlDetailsUnavailable") +
                "\n" + this.GetWeaponDataSourceTooltip() +
                "\n" + ShuttleUIText.Tr("CT_Shuttle_Defense_Status") + ": " +
                weapon.StatusLabel;
        }

        internal string BuildShieldTooltip(V3DefenseShieldPanelModel shield)
        {
            if (shield == null)
            {
                return string.Empty;
            }

            string strength = shield.StrengthPct >= 0f
                ? Mathf.RoundToInt(shield.StrengthPct * 100f).ToString() + "%"
                : "-";
            string tooltip =
                ShuttleUIText.Tr("CT_Shuttle_Defense_Backend") + ": " +
                shield.BackendLabel + "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_Tooltip_HP",
                    shield.CurrentHitPoints,
                    shield.MaxHitPoints) + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_Strength") + ": " +
                strength;

            return shield.IsSurfaceShield
                ? this.AppendSurfaceShieldTooltip(tooltip, shield)
                : this.AppendRangeShieldTooltip(tooltip, shield);
        }

        private string BuildFireControlTooltip(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            string linked = weapon.FireControlLinked
                ? ShuttleUIText.Tr("CT_Shuttle_FireControl_Linked")
                : ShuttleUIText.Tr("CT_Shuttle_FireControl_Unlinked");
            string autoFire = weapon.AutoFireEnabled
                ? ShuttleUIText.Tr("CT_Shuttle_FireControl_AutoFire_On")
                : ShuttleUIText.Tr("CT_Shuttle_FireControl_AutoFire_Off");
            string tooltip =
                weapon.FireControlStatusLabel + "\n" +
                ShuttleUIText.Tr("CT_Shuttle_FireControl_LinkStatus", linked) + "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_FireControl_ModeStatus",
                    weapon.FireControlModeLabel) + "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_FireControl_TargetPriorityStatus",
                    weapon.TargetPriorityLabel) + "\n" +
                autoFire;
            if (!string.IsNullOrEmpty(weapon.FireControlStatusTooltip))
            {
                tooltip += "\n" + weapon.FireControlStatusTooltip;
            }

            return tooltip;
        }

        private string AppendSurfaceShieldTooltip(
            string tooltip,
            V3DefenseShieldPanelModel shield)
        {
            tooltip += "\n" +
                shield.RechargeSpeedLabel + "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_RechargeEffectiveFormat",
                    shield.EffectiveRechargeHitPointsPerInterval,
                    shield.RechargeIntervalTicks,
                    shield.EffectiveRechargeEnergyPerIntervalWd.ToString("0.##")) +
                "\n" +
                ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip") +
                "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_Tooltip_EnergyCost",
                    shield.RechargeEnergyPerHitPointWd.ToString("0.##"));

            if (shield.BrokenTicksLeft > 0)
            {
                tooltip += "\n" +
                    ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_Status_Broken") +
                    ": " + shield.BrokenTicksLeft.ToString() +
                    ShuttleUISurfaceShieldText.TicksSuffix;
            }

            if (shield.RechargeBlockedTicksLeft > 0)
            {
                tooltip += "\n" +
                    ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_Status_RechargeBlocked") +
                    ": " + shield.RechargeBlockedTicksLeft.ToString() +
                    ShuttleUISurfaceShieldText.TicksSuffix;
            }

            if (shield.RechargeStalledForNoEnergy)
            {
                tooltip += "\n" +
                    ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_Status_NoEnergy");
            }

            if (shield.StatusKey == ShuttleUIText.StatusOffline)
            {
                tooltip += "\n" +
                    ShuttleUIText.Tr("CT_Shuttle_SurfaceShield_Status_Offline");
            }

            return tooltip;
        }

        private string AppendRangeShieldTooltip(
            string tooltip,
            V3DefenseShieldPanelModel shield)
        {
            return tooltip + "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_SurfaceShield_Tooltip_Radius",
                    shield.RangeValue >= 0f ? shield.RangeValue.ToString("0.#") : "-",
                    shield.MinRange.ToString("0.#"),
                    shield.MaxRange.ToString("0.#"));
        }

        private string GetWeaponDataSourceTooltip()
        {
            return ShuttleUIText.Tr("CT_Shuttle_Defense_DamageUnavailableTooltip") +
                "\n" +
                ShuttleUIText.Tr("CT_Shuttle_Defense_WeaponFireRateTooltip");
        }

        private string GetWeaponSlotTooltipLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return "-";
            }

            string slot = !string.IsNullOrEmpty(weapon.SlotLabel)
                ? weapon.SlotLabel
                : weapon.SlotShortLabel;
            return string.IsNullOrEmpty(slot) ? "-" : slot;
        }
    }
}
