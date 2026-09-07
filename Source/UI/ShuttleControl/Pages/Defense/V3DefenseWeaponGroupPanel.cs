using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Draws weapon-group controls from detached state and availability flags.
    /// </summary>
    internal sealed class V3DefenseWeaponGroupPanel
    {
        private const float ButtonGap = 6f;
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;

        internal V3DefenseWeaponGroupPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3DefensePageModel model,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(
                rect,
                this.text.Tr("CT_Shuttle_Defense_GroupControl"));
            Rect inner = this.panel.GetPanelInnerRect(rect, 44f);
            List<V3DefenseWeaponEntryModel> weapons =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.Weapons
                    : null;
            if (weapons == null || weapons.Count == 0)
            {
                this.panel.DrawEmptyPanelMessage(
                    inner,
                    this.text.Tr("CT_Shuttle_Defense_NoWeapons"));
                return;
            }

            this.panel.DrawCardBackground(
                inner,
                false,
                false,
                V3DefenseText.StrongCardColor);
            GroupAvailability availability = GetAvailability(weapons);
            bool hasActions = GetActions(context) != null;
            float buttonWidth = Mathf.Max(
                0f,
                (inner.width - 16f - (ButtonGap * 2f)) / 3f);
            float x = inner.x + 8f;
            float firstY = inner.y + 7f;
            float secondY = firstY + 29f;
            this.DrawButton(
                new Rect(x, firstY, buttonWidth, 24f),
                GroupAction.OpenFire,
                hasActions && availability.HasHoldFireControl,
                availability.OpenFireSelected,
                weapons,
                context);
            this.DrawButton(
                new Rect(x + buttonWidth + ButtonGap, firstY, buttonWidth, 24f),
                GroupAction.HoldFire,
                hasActions && availability.HasHoldFireControl,
                availability.HoldFireSelected,
                weapons,
                context);
            this.DrawButton(
                new Rect(x + ((buttonWidth + ButtonGap) * 2f), firstY, buttonWidth, 24f),
                GroupAction.Reload,
                hasActions && availability.CanReload,
                false,
                weapons,
                context);
            this.DrawButton(
                new Rect(x, secondY, buttonWidth, 24f),
                GroupAction.LinkFireControl,
                hasActions && availability.HasFireControlLink,
                availability.LinkFireControlSelected,
                weapons,
                context);
            this.DrawButton(
                new Rect(x + buttonWidth + ButtonGap, secondY, buttonWidth, 24f),
                GroupAction.UnlinkFireControl,
                hasActions && availability.HasFireControlLink,
                availability.UnlinkFireControlSelected,
                weapons,
                context);
            this.DrawButton(
                new Rect(x + ((buttonWidth + ButtonGap) * 2f), secondY, buttonWidth, 24f),
                GroupAction.ClearTargets,
                hasActions && availability.CanClearTargets,
                false,
                weapons,
                context);
        }

        private void DrawButton(
            Rect rect,
            GroupAction action,
            bool enabled,
            bool selected,
            List<V3DefenseWeaponEntryModel> weapons,
            ShuttlePageDrawContext context)
        {
            string labelKey = GetLabelKey(action);
            string label = this.text.Tr(labelKey);
            string tooltip = selected
                ? ShuttleUIText.Tr(
                    "CT_Shuttle_Defense_GroupSelectedToggleTooltip",
                    label,
                    this.text.Tr(GetLabelKey(GetOppositeAction(action))))
                : this.text.Tr(GetTooltipKey(action));
            if (!ShuttleUIActionButtonDrawer.DrawTintedButton(
                rect,
                label,
                enabled,
                selected ? ShuttleUIStyle.SelectedColor : ShuttleUIStyle.ButtonBgColor,
                selected
                    ? ShuttleUIStyle.BlueStatusColor
                    : ShuttleUIStyle.ButtonBorderColor,
                selected
                    ? ShuttleUIStyle.HeaderTitleTextColor
                    : ShuttleUIStyle.ButtonTextColor,
                tooltip))
            {
                return;
            }

            IShuttleDefenseWeaponGroupUIActions actions = GetActions(context);
            if (actions == null)
            {
                return;
            }

            List<ShuttleDefenseWeaponActionTarget> targets = BuildTargets(weapons);
            if (action == GroupAction.OpenFire)
            {
                actions.SetAllHoldFire(targets, selected);
            }
            else if (action == GroupAction.HoldFire)
            {
                actions.SetAllHoldFire(targets, !selected);
            }
            else if (action == GroupAction.Reload)
            {
                actions.ReloadAll(targets);
            }
            else if (action == GroupAction.LinkFireControl)
            {
                actions.SetAllFireControlLinked(targets, !selected);
            }
            else if (action == GroupAction.UnlinkFireControl)
            {
                actions.SetAllFireControlLinked(targets, selected);
            }
            else
            {
                actions.ClearAllForcedTargets(targets);
            }
        }

        private static GroupAvailability GetAvailability(
            List<V3DefenseWeaponEntryModel> weapons)
        {
            GroupAvailability result = new GroupAvailability();
            for (int i = 0; i < weapons.Count; i++)
            {
                V3DefenseWeaponEntryModel weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                if (weapon.CanToggleHoldFire)
                {
                    result.HasHoldFireControl = true;
                    result.HasHeldWeapon |= weapon.HoldFire;
                    result.HasOpenWeapon |= !weapon.HoldFire;
                }

                // Keep Reload All actionable for ammo weapons even when every magazine is full;
                // the group action can then report the useful all-full result explicitly.
                result.CanReload |= weapon.HasAmmoSystem;
                if (weapon.CanToggleFireControlLink)
                {
                    result.HasFireControlLink = true;
                    result.HasLinkedWeapon |= weapon.FireControlLinked;
                    result.HasUnlinkedWeapon |= !weapon.FireControlLinked;
                }

                result.CanClearTargets |=
                    weapon.HasForcedTarget && weapon.CanClearForcedTarget;
            }

            result.OpenFireSelected =
                result.HasHoldFireControl && !result.HasHeldWeapon;
            result.HoldFireSelected =
                result.HasHoldFireControl && !result.HasOpenWeapon;
            result.LinkFireControlSelected =
                result.HasFireControlLink && !result.HasUnlinkedWeapon;
            result.UnlinkFireControlSelected =
                result.HasFireControlLink && !result.HasLinkedWeapon;
            return result;
        }

        private static List<ShuttleDefenseWeaponActionTarget> BuildTargets(
            List<V3DefenseWeaponEntryModel> weapons)
        {
            List<ShuttleDefenseWeaponActionTarget> targets =
                new List<ShuttleDefenseWeaponActionTarget>(
                    weapons != null ? weapons.Count : 0);
            if (weapons == null)
            {
                return targets;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                ShuttleDefenseWeaponActionTarget target =
                    V3DefenseActionTargetFactory.CreateWeapon(weapons[i]);
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        private static IShuttleDefenseWeaponGroupUIActions GetActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.DefensePageContext != null
                ? context.DefensePageContext.WeaponGroupActions
                : null;
        }

        private static string GetLabelKey(GroupAction action)
        {
            if (action == GroupAction.OpenFire)
            {
                return "CT_Shuttle_Defense_GroupOpenFire";
            }

            if (action == GroupAction.HoldFire)
            {
                return "CT_Shuttle_Defense_GroupHoldFire";
            }

            if (action == GroupAction.Reload)
            {
                return "CT_Shuttle_Defense_GroupReload";
            }

            if (action == GroupAction.LinkFireControl)
            {
                return "CT_Shuttle_Defense_GroupLinkFireControl";
            }

            if (action == GroupAction.UnlinkFireControl)
            {
                return "CT_Shuttle_Defense_GroupUnlinkFireControl";
            }

            return "CT_Shuttle_Defense_GroupClearTargets";
        }

        private static string GetTooltipKey(GroupAction action)
        {
            return GetLabelKey(action) + "Tooltip";
        }

        private static GroupAction GetOppositeAction(GroupAction action)
        {
            if (action == GroupAction.OpenFire)
            {
                return GroupAction.HoldFire;
            }

            if (action == GroupAction.HoldFire)
            {
                return GroupAction.OpenFire;
            }

            if (action == GroupAction.LinkFireControl)
            {
                return GroupAction.UnlinkFireControl;
            }

            if (action == GroupAction.UnlinkFireControl)
            {
                return GroupAction.LinkFireControl;
            }

            return action;
        }

        private struct GroupAvailability
        {
            internal bool HasHoldFireControl;
            internal bool HasHeldWeapon;
            internal bool HasOpenWeapon;
            internal bool OpenFireSelected;
            internal bool HoldFireSelected;
            internal bool CanReload;
            internal bool HasFireControlLink;
            internal bool HasLinkedWeapon;
            internal bool HasUnlinkedWeapon;
            internal bool LinkFireControlSelected;
            internal bool UnlinkFireControlSelected;
            internal bool CanClearTargets;
        }

        private enum GroupAction
        {
            OpenFire,
            HoldFire,
            Reload,
            LinkFireControl,
            UnlinkFireControl,
            ClearTargets
        }
    }
}
