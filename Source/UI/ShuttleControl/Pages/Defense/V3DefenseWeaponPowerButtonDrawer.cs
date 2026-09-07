using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Adapts one weapon card to the established module enablement action and icon.
    /// </summary>
    internal sealed class V3DefenseWeaponPowerButtonDrawer
    {
        private readonly V3DefenseText text;

        internal V3DefenseWeaponPowerButtonDrawer(V3DefenseText text)
        {
            this.text = text ?? new V3DefenseText();
        }

        internal void Draw(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            if (weapon == null)
            {
                return;
            }

            IShuttleMainModuleEnablementUIActions actions =
                context != null && context.MainPageContext != null
                    ? context.MainPageContext.ModuleEnablementActions
                    : null;
            if (weapon.SegmentSlot == null || weapon.ModuleSlot == null)
            {
                V3MainCardActionRailDrawer.DrawStatusIndicator(
                    rect,
                    !string.IsNullOrEmpty(weapon.ModuleInstanceID),
                    weapon.IsEnabled,
                    false,
                    Mouse.IsOver(rect)
                        ? this.text.Tr("CT_Shuttle_Defense_CommandUnavailable")
                        : null);
                return;
            }

            bool currentEnabled = weapon.ModuleSlot.InstalledModuleEnabled;
            bool requestedEnabled = !currentEnabled;
            bool canToggle = actions != null &&
                actions.CanSetModuleEnabled(
                    weapon.SegmentSlot,
                    weapon.ModuleSlot,
                    requestedEnabled);
            string tooltip = Mouse.IsOver(rect) && actions != null
                ? actions.GetModuleEnablementTooltip(
                    weapon.SegmentSlot,
                    weapon.ModuleSlot,
                    requestedEnabled)
                : null;
            if (V3MainCardActionRailDrawer.DrawToggleButton(
                    rect,
                    currentEnabled,
                    canToggle,
                    tooltip))
            {
                actions.SetModuleEnabled(
                    weapon.SegmentSlot,
                    weapon.ModuleSlot,
                    requestedEnabled);
            }
        }

        internal void DrawPassiveStatus(
            Rect rect,
            V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return;
            }

            bool installed = weapon.ModuleSlot != null ||
                !string.IsNullOrEmpty(weapon.ModuleInstanceID);
            bool enabled = weapon.ModuleSlot != null
                ? weapon.ModuleSlot.InstalledModuleEnabled
                : weapon.IsEnabled;
            V3MainCardActionRailDrawer.DrawStatusIndicator(
                rect,
                installed,
                enabled,
                false,
                null);
        }
    }
}
