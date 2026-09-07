using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws and dispatches only the direct actions hosted by one weapon card.
    /// </summary>
    internal sealed class V3DefenseWeaponCardActionDrawer
    {
        private const float ButtonGap = 4f;

        private readonly V3DefenseText text;
        private readonly V3DefenseWeaponCardText cardText;
        private readonly V3DefenseWeaponSettingsMenu settingsMenu =
            new V3DefenseWeaponSettingsMenu();

        internal V3DefenseWeaponCardActionDrawer(
            V3DefenseText text,
            V3DefenseWeaponCardText cardText)
        {
            this.text = text ?? new V3DefenseText();
            this.cardText = cardText ?? new V3DefenseWeaponCardText(this.text);
        }

        internal void DrawActions(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            if (weapon == null || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float buttonWidth = Mathf.Max(
                0f,
                (rect.width - (ButtonGap * 3f)) / 4f);
            Rect targetRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            Rect holdRect = new Rect(
                targetRect.xMax + ButtonGap,
                rect.y,
                buttonWidth,
                rect.height);
            Rect reloadRect = new Rect(
                holdRect.xMax + ButtonGap,
                rect.y,
                buttonWidth,
                rect.height);
            Rect fireControlRect = new Rect(
                reloadRect.xMax + ButtonGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - reloadRect.xMax - ButtonGap),
                rect.height);

            this.DrawTargetButton(targetRect, weapon, context);
            this.DrawHoldFireButton(holdRect, weapon, context);
            this.DrawReloadButton(reloadRect, weapon, context);
            this.DrawFireControlButton(fireControlRect, weapon, context);
        }

        private void DrawTargetButton(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            IShuttleDefenseWeaponTargetingUIActions actions =
                this.GetTargetingActions(context);
            bool enabled = actions != null &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID) &&
                (weapon.HasForcedTarget
                    ? weapon.CanClearForcedTarget
                    : weapon.CanSetForcedTarget);
            ShuttleDefenseWeaponActionTarget target = null;
            string tooltip = null;
            if (Mouse.IsOver(rect) && actions != null)
            {
                target = V3DefenseActionTargetFactory.CreateWeapon(weapon);
                enabled = target != null && actions.CanUseForcedTarget(target);
                tooltip = target != null
                    ? actions.GetForcedTargetTooltip(target)
                    : this.GetUnavailableTooltip();
            }

            string label = weapon.HasForcedTarget
                ? this.text.Tr("CT_Shuttle_Defense_ClearTarget")
                : this.text.Tr("CT_Shuttle_Defense_Target");
            if (ShuttleUIActionButtonDrawer.DrawAccentButton(
                    rect,
                    label,
                    enabled,
                    V3DefenseText.RedColor,
                    tooltip))
            {
                target = target ?? V3DefenseActionTargetFactory.CreateWeapon(weapon);
                if (target != null)
                {
                    actions.UseForcedTarget(target);
                }
            }
        }

        private void DrawHoldFireButton(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                this.GetFireControlActions(context);
            bool enabled = actions != null &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID) &&
                weapon.CanToggleHoldFire;
            ShuttleDefenseWeaponActionTarget target = null;
            string tooltip = null;
            if (Mouse.IsOver(rect) && actions != null)
            {
                target = V3DefenseActionTargetFactory.CreateWeapon(weapon);
                enabled = target != null && actions.CanToggleHoldFire(target);
                tooltip = target != null
                    ? actions.GetHoldFireTooltip(target)
                    : this.GetUnavailableTooltip();
            }

            string label = weapon.HoldFire
                ? this.text.Tr("CT_Shuttle_Defense_OpenFire")
                : this.text.Tr("CT_Shuttle_Defense_HoldFire");
            if (ShuttleUIActionButtonDrawer.DrawAccentButton(
                    rect,
                    label,
                    enabled,
                    V3DefenseText.YellowColor,
                    tooltip))
            {
                target = target ?? V3DefenseActionTargetFactory.CreateWeapon(weapon);
                if (target != null)
                {
                    actions.ToggleHoldFire(target);
                }
            }
        }

        private void DrawReloadButton(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            IShuttleDefenseWeaponAmmoUIActions actions = this.GetAmmoActions(context);
            bool cancel = weapon.ReloadInProgress || weapon.ReloadRequested;
            bool enabled = actions != null &&
                weapon.HasAmmoSystem &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID) &&
                (cancel ? weapon.CanCancelReload : weapon.CanReload);
            string label = cancel
                ? this.text.Tr("CT_Shuttle_WeaponAmmo_CancelReload")
                : this.text.Tr("CT_Shuttle_WeaponAmmo_Reload");
            string tooltip = Mouse.IsOver(rect)
                ? this.cardText.GetReloadCommandTooltip(weapon)
                : null;
            if (!ShuttleUIActionButtonDrawer.DrawAccentButton(
                    rect,
                    label,
                    enabled,
                    V3DefenseText.BlueColor,
                    tooltip))
            {
                return;
            }

            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            if (target == null)
            {
                return;
            }

            if (cancel)
            {
                actions.CancelWeaponReload(target);
            }
            else
            {
                actions.ReloadWeapon(target);
            }
        }

        private void DrawFireControlButton(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            bool enabled = this.HasSettingsActions(context) &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID);
            string tooltip = Mouse.IsOver(rect)
                ? (enabled
                    ? this.text.Tr("CT_Shuttle_Defense_SettingsTooltip")
                    : this.GetUnavailableTooltip())
                : null;
            if (ShuttleUIActionButtonDrawer.DrawAccentButton(
                    rect,
                    this.text.Tr("CT_Shuttle_Defense_Settings"),
                    enabled,
                    V3DefenseText.BlueColor,
                    tooltip))
            {
                this.settingsMenu.Open(context, weapon);
            }
        }

        private bool HasSettingsActions(ShuttlePageDrawContext context)
        {
            return context != null &&
                context.DefensePageContext != null &&
                (context.DefensePageContext.WeaponFireControlActions != null ||
                 context.DefensePageContext.WeaponAmmoActions != null);
        }

        private IShuttleDefenseWeaponTargetingUIActions GetTargetingActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.WeaponTargetingActions
                : null;
        }

        private IShuttleDefenseWeaponFireControlUIActions GetFireControlActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.WeaponFireControlActions
                : null;
        }

        private IShuttleDefenseWeaponAmmoUIActions GetAmmoActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.WeaponAmmoActions
                : null;
        }

        private string GetUnavailableTooltip()
        {
            return this.text.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }
    }
}
