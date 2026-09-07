using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Formats the compact, localized weapon-card projection.
    /// </summary>
    internal sealed class V3DefenseWeaponCardText
    {
        private readonly V3DefenseText text;

        internal V3DefenseWeaponCardText(V3DefenseText text)
        {
            this.text = text ?? new V3DefenseText();
        }

        internal string GetSlotLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return this.text.Tr("CT_Shuttle_Common_None");
            }

            if (!string.IsNullOrEmpty(weapon.SlotShortLabel))
            {
                return weapon.SlotShortLabel;
            }

            return this.text.ValueOrDash(weapon.SlotLabel);
        }

        internal string GetTypeStatusLine(V3DefenseWeaponEntryModel weapon)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_Defense_CardTypeStatusFormat",
                this.text.ValueOrDash(weapon != null ? weapon.WeaponTypeLabel : null),
                this.text.ValueOrDash(weapon != null ? weapon.StatusLabel : null));
        }

        internal string GetPerformanceLine(V3DefenseWeaponEntryModel weapon)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_Defense_CardPerformanceFormat",
                this.text.ValueOrDash(weapon != null ? weapon.DamageLabel : null),
                this.text.ValueOrDash(weapon != null ? weapon.FireRateLabel : null));
        }

        internal string GetFireControlLine(V3DefenseWeaponEntryModel weapon)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_Defense_CardFireControlFormat",
                this.text.ValueOrDash(weapon != null ? weapon.FireControlModeLabel : null),
                this.text.ValueOrDash(weapon != null ? weapon.TargetPriorityLabel : null));
        }

        internal string GetFireControlPurposeLine(V3DefenseWeaponEntryModel weapon)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_Defense_CardFireControlPurposeFormat",
                this.text.ValueOrDash(weapon != null ? weapon.FireControlModeLabel : null),
                this.text.ValueOrDash(weapon != null ? weapon.TargetPriorityLabel : null));
        }

        internal string GetAmmoLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                return this.text.Tr("CT_Shuttle_WeaponAmmo_NoAmmoSystem");
            }

            return ShuttleUIText.Tr(
                "CT_Shuttle_Defense_CardAmmoFormat",
                this.text.ValueOrDash(weapon.SelectedAmmoLabel),
                weapon.LoadedAmmoCount,
                weapon.MagazineCapacity,
                weapon.AmmoStockCount);
        }

        internal string GetReloadLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                return string.Empty;
            }

            string state;
            if (weapon.ReloadInProgress)
            {
                state = ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_CardReloadProgressFormat",
                    Mathf.RoundToInt(weapon.ReloadProgress01 * 100f));
            }
            else if (weapon.ManualReloadJobActive)
            {
                state = this.text.Tr("CT_Shuttle_WeaponAmmo_AwaitingPawnReload");
            }
            else if (weapon.ReloadRequested)
            {
                state = this.text.Tr("CT_Shuttle_WeaponAmmo_ReloadRequested");
            }
            else
            {
                state = this.text.Tr("CT_Shuttle_Defense_CardReloadStandby");
            }

            return ShuttleUIText.Tr(
                "CT_Shuttle_Defense_CardReloadFormat",
                state);
        }

        internal string GetReloadPolicyLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                return string.Empty;
            }

            return this.text.GetAutoReloadLabel(
                weapon.AutoReloadEnabled,
                weapon.AutoLoaderAvailable);
        }

        internal string GetReloadCommandTooltip(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                return this.text.Tr("CT_Shuttle_WeaponAmmo_NoAmmoSystem");
            }

            if (!string.IsNullOrEmpty(weapon.LastAmmoFailureReason))
            {
                return weapon.LastAmmoFailureReason;
            }

            if (!string.IsNullOrEmpty(weapon.LastReloadBlockerReason))
            {
                return weapon.LastReloadBlockerReason;
            }

            if (weapon.MagazineCapacity > 0 &&
                weapon.LoadedAmmoCount >= weapon.MagazineCapacity)
            {
                return this.text.Tr("CT_Shuttle_WeaponAmmo_Full");
            }

            return weapon.ReloadInProgress || weapon.ReloadRequested
                ? this.text.Tr("CT_Shuttle_WeaponAmmo_CancelReload")
                : this.text.Tr("CT_Shuttle_WeaponAmmo_Reload");
        }
    }
}
