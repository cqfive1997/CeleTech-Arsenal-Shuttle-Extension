using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseActionPanel
    {
        private readonly V3DefenseText text;
        private readonly V3DefensePanelDrawer panel;
        private readonly V3DefenseWeaponSettingsMenu settingsMenu;

        internal V3DefenseActionPanel(
            V3DefenseText text,
            V3DefensePanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
            this.settingsMenu = new V3DefenseWeaponSettingsMenu();
        }

        internal void DrawSelectedWeapon(
            Rect rect,
            V3DefensePageModel model,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            this.panel.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Defense_SelectedWeaponControl"));
            Rect inner = this.panel.GetPanelInnerRect(rect, 44f);
            V3DefenseWeaponEntryModel weapon =
                model != null && model.DefenseModel != null
                    ? model.DefenseModel.SelectedWeapon
                    : null;
            if (weapon == null)
            {
                this.panel.DrawEmptyPanelMessage(
                    inner,
                    this.text.Tr("CT_Shuttle_Defense_NoSelectedWeapon"));
                return;
            }

            Rect infoRect = new Rect(inner.x, inner.y, inner.width, 122f);
            this.DrawSelectedWeaponInfo(infoRect, weapon, context);
            Rect commandArea = new Rect(
                inner.x,
                infoRect.yMax + 8f,
                inner.width,
                Mathf.Max(0f, inner.yMax - infoRect.yMax - 8f));
            RegisterTutorialTarget(
                context,
                ShuttleTutorialTargetIds.DefenseFireControlPanel,
                commandArea);
            this.DrawSelectedWeaponCommandArea(commandArea, weapon, state, context);
        }

        private void DrawSelectedWeaponInfo(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            ShuttlePageDrawContext context)
        {
            this.DrawSelectedWeaponSummaryFrame(rect);
            Rect iconRect = new Rect(rect.x + 9f, rect.y + 9f, 42f, 42f);
            this.panel.DrawIcon(iconRect, context, weapon.IconKey, "W", 1.1f);
            Color statusColor = this.text.GetWeaponStatusColor(weapon.StatusKey);
            Rect powerStatusRect = new Rect(rect.xMax - 40f, rect.y + 8f, 30f, 30f);
            Rect badgeRect = new Rect(rect.xMax - 72f, rect.y + 8f, 62f, 18f);
            bool drewPowerStatus = this.panel.DrawMainPowerStatusIcon(
                powerStatusRect,
                weapon.StatusKey,
                weapon.Tooltip);
            if (!drewPowerStatus)
            {
                this.DrawLightStatusChip(
                    badgeRect,
                    weapon.StatusLabel,
                    statusColor,
                    weapon.Tooltip);
            }

            float titleRight = drewPowerStatus
                ? powerStatusRect.x - 8f
                : badgeRect.x - 8f;

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(iconRect.xMax + 9f, rect.y + 6f, Mathf.Max(0f, titleRight - iconRect.xMax - 9f), 20f),
                weapon.Label,
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(iconRect.xMax + 9f, rect.y + 28f, Mathf.Max(0f, titleRight - iconRect.xMax - 9f), 16f),
                this.GetSelectedWeaponSubtitle(weapon),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);

            Rect metaRect = new Rect(rect.x + 9f, rect.y + 60f, rect.width - 18f, 52f);
            this.DrawMetadataBlockFrame(metaRect);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(metaRect.x + 7f, metaRect.y + 4f, metaRect.width - 14f, 15f),
                this.text.Tr("CT_Shuttle_Defense_Damage") + ": " + weapon.DamageLabel +
                "   " + this.text.Tr("CT_Shuttle_Defense_WeaponFireRate") + ": " +
                weapon.FireRateLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(Color.white, 0.78f),
                weapon.Tooltip,
                TextAnchor.MiddleLeft);

            Rect ammoRect = new Rect(metaRect.x + 7f, metaRect.y + 20f, metaRect.width - 14f, 14f);
            if (weapon.HasAmmoSystem)
            {
                RegisterTutorialTarget(
                    context,
                    ShuttleTutorialTargetIds.DefenseAmmoPanel,
                    new Rect(ammoRect.x - 4f, ammoRect.y - 4f, ammoRect.width + 8f, ammoRect.height + 8f));
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    ammoRect,
                    this.GetAmmoStatusLine(weapon),
                    GameFont.Tiny,
                    GameFont.Tiny,
                    ShuttleUIStyle.MutedTextColor,
                    weapon.Tooltip,
                    TextAnchor.MiddleLeft);
            }

            Rect forcedTargetRect = new Rect(
                metaRect.x + 7f,
                metaRect.y + (weapon.HasAmmoSystem ? 35f : 20f),
                metaRect.width - 14f,
                14f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                forcedTargetRect,
                this.text.Tr("CT_Shuttle_Defense_ForcedTarget") + ": " +
                this.text.ValueOrDash(weapon.ForcedTargetLabel),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                weapon.Tooltip,
                TextAnchor.MiddleLeft);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            this.text.AddTooltip(rect, weapon.Tooltip);
        }

        private void DrawSelectedWeaponCommandArea(
            Rect rect,
            V3DefenseWeaponEntryModel weapon,
            V3DefensePageState state,
            ShuttlePageDrawContext context)
        {
            if (rect.height <= 0f)
            {
                return;
            }

            this.DrawCommandAreaFrame(rect);
            Rect scrollRect = new Rect(
                rect.x + 1f,
                rect.y + 1f,
                Mathf.Max(0f, rect.width - 2f),
                Mathf.Max(0f, rect.height - 2f));
            List<SelectedWeaponButtonSpec> buttons = this.BuildButtons(context, weapon);
            const int columns = 2;
            const float buttonHeight = 24f;
            const float gap = 6f;
            const float noteHeight = 50f;
            int rows = (buttons.Count + columns - 1) / columns;
            float viewHeight = rows > 0
                ? 6f + rows * buttonHeight + (rows - 1) * gap + gap + noteHeight
                : noteHeight;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, scrollRect.width - 16f), Mathf.Max(scrollRect.height, viewHeight));
            Vector2 scroll = state != null ? state.SelectedWeaponControlScroll : Vector2.zero;

            Widgets.BeginScrollView(scrollRect, ref scroll, viewRect);
            float buttonWidth = (viewRect.width - gap) * 0.5f;
            for (int i = 0; i < buttons.Count; i++)
            {
                SelectedWeaponButtonSpec button = buttons[i];
                int row = i / columns;
                int column = i % columns;
                Rect buttonRect = new Rect(
                    column == 0 ? 0f : buttonWidth + gap,
                    6f + row * (buttonHeight + gap),
                    buttonWidth,
                    buttonHeight);
                if (this.panel.DrawButton(
                    buttonRect,
                    button.Label,
                    button.Enabled,
                    button.AccentColor,
                    button.Tooltip) &&
                    button.Action != null)
                {
                    button.Action();
                }
            }

            float buttonBlockEnd = rows > 0 ? 6f + rows * buttonHeight + rows * gap : 6f;
            Rect noteRect = new Rect(
                0f,
                Mathf.Max(buttonBlockEnd, viewRect.height - noteHeight - 4f),
                viewRect.width,
                noteHeight);
            Widgets.DrawBoxSolid(noteRect, ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, 0.20f));
            ShuttleUILayout.DrawRectBorder(
                noteRect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.18f),
                ShuttleUIStyle.ThinBorder);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(noteRect.x + 7f, noteRect.y + 5f, noteRect.width - 14f, noteRect.height - 10f),
                this.GetWeaponControlStatusLine(weapon));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Widgets.EndScrollView();

            if (state != null)
            {
                state.SelectedWeaponControlScroll = scroll;
            }
        }

        private void DrawSelectedWeaponSummaryFrame(Rect rect)
        {
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.RightBottomCardColor, 0.44f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.28f),
                ShuttleUIStyle.ThinBorder);
        }

        private void DrawMetadataBlockFrame(Rect rect)
        {
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, 0.18f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.16f),
                ShuttleUIStyle.ThinBorder);
        }

        private void DrawCommandAreaFrame(Rect rect)
        {
            Widgets.DrawBoxSolid(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedCardColor, 0.12f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.16f),
                ShuttleUIStyle.ThinBorder);
        }

        private void DrawLightStatusChip(Rect rect, string label, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.WithAlpha(color, 0.05f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(color, 0.32f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    rect.x + 4f,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.width - 8f),
                    Mathf.Max(0f, rect.height - 2f)),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.WithAlpha(color, 0.76f),
                tooltip,
                TextAnchor.MiddleCenter);
        }

        private List<SelectedWeaponButtonSpec> BuildButtons(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            List<SelectedWeaponButtonSpec> buttons = new List<SelectedWeaponButtonSpec>();
            bool targetIsClear = weapon.HasForcedTarget;
            buttons.Add(new SelectedWeaponButtonSpec(
                targetIsClear
                    ? this.text.Tr("CT_Shuttle_Defense_ClearTarget")
                    : this.text.Tr("CT_Shuttle_Defense_Target"),
                this.CanUseForcedTarget(context, weapon),
                this.GetForcedTargetTooltip(context, weapon),
                V3DefenseText.RedColor,
                delegate { this.UseForcedTarget(context, weapon); }));

            buttons.Add(new SelectedWeaponButtonSpec(
                weapon.HoldFire
                    ? this.text.Tr("CT_Shuttle_Defense_OpenFire")
                    : this.text.Tr("CT_Shuttle_Defense_HoldFire"),
                this.CanToggleHoldFire(context, weapon),
                this.GetHoldFireTooltip(context, weapon),
                V3DefenseText.YellowColor,
                delegate { this.ToggleHoldFire(context, weapon); }));

            bool reloadActsAsCancel = weapon.ReloadInProgress || weapon.ReloadRequested;
            bool canReload = weapon.HasAmmoSystem &&
                this.GetAmmoActions(context) != null &&
                (reloadActsAsCancel ? weapon.CanCancelReload : weapon.CanReload);
            buttons.Add(new SelectedWeaponButtonSpec(
                reloadActsAsCancel
                    ? this.text.Tr("CT_Shuttle_WeaponAmmo_CancelReload")
                    : this.text.Tr("CT_Shuttle_WeaponAmmo_Reload"),
                canReload,
                this.GetAmmoCommandTooltip(weapon),
                V3DefenseText.BlueColor,
                delegate
                {
                    if (reloadActsAsCancel)
                    {
                        this.CancelWeaponReload(context, weapon);
                    }
                    else
                    {
                        this.ReloadWeapon(context, weapon);
                    }
                }));

            buttons.Add(new SelectedWeaponButtonSpec(
                this.text.Tr("CT_Shuttle_Defense_Settings"),
                this.HasWeaponSettingsActions(context),
                this.GetSettingsTooltip(context),
                V3DefenseText.BlueColor,
                delegate { this.settingsMenu.Open(context, weapon); }));
            return buttons;
        }

        private bool CanUseForcedTarget(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponTargetingUIActions actions =
                this.GetTargetingActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.CanUseForcedTarget(target);
        }

        private bool UseForcedTarget(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponTargetingUIActions actions =
                this.GetTargetingActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.UseForcedTarget(target);
        }

        private string GetForcedTargetTooltip(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponTargetingUIActions actions =
                this.GetTargetingActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null && target != null
                ? actions.GetForcedTargetTooltip(target)
                : this.GetActionUnavailableTooltip();
        }

        private bool CanToggleHoldFire(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                this.GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.CanToggleHoldFire(target);
        }

        private bool ToggleHoldFire(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                this.GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.ToggleHoldFire(target);
        }

        private string GetHoldFireTooltip(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponFireControlUIActions actions =
                this.GetFireControlActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null && target != null
                ? actions.GetHoldFireTooltip(target)
                : this.GetActionUnavailableTooltip();
        }

        private bool ReloadWeapon(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponAmmoUIActions actions = this.GetAmmoActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.ReloadWeapon(target);
        }

        private bool CancelWeaponReload(
            ShuttlePageDrawContext context,
            V3DefenseWeaponEntryModel weapon)
        {
            IShuttleDefenseWeaponAmmoUIActions actions = this.GetAmmoActions(context);
            ShuttleDefenseWeaponActionTarget target =
                V3DefenseActionTargetFactory.CreateWeapon(weapon);
            return actions != null &&
                target != null &&
                actions.CancelWeaponReload(target);
        }

        private string GetSettingsTooltip(ShuttlePageDrawContext context)
        {
            return this.HasWeaponSettingsActions(context)
                ? this.text.Tr("CT_Shuttle_Defense_SettingsTooltip")
                : this.GetActionUnavailableTooltip();
        }

        private bool HasWeaponSettingsActions(ShuttlePageDrawContext context)
        {
            return this.GetFireControlActions(context) != null ||
                this.GetAmmoActions(context) != null;
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

        private string GetActionUnavailableTooltip()
        {
            return ShuttleUIText.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        private string GetSelectedWeaponSubtitle(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return "-";
            }

            string slot = !string.IsNullOrEmpty(weapon.SlotShortLabel)
                ? weapon.SlotShortLabel
                : weapon.SlotLabel;
            if (string.IsNullOrEmpty(slot))
            {
                return !string.IsNullOrEmpty(weapon.WeaponTypeLabel)
                    ? weapon.WeaponTypeLabel
                    : "-";
            }

            if (string.IsNullOrEmpty(weapon.WeaponTypeLabel))
            {
                return slot;
            }

            return slot + " / " + weapon.WeaponTypeLabel;
        }

        private string GetAmmoStatusLine(V3DefenseWeaponEntryModel weapon)
        {
            return this.text.Tr("CT_Shuttle_WeaponAmmo_Ammo") + ": " +
                this.text.ValueOrDash(weapon.SelectedAmmoLabel) +
                "   " + this.text.Tr("CT_Shuttle_WeaponAmmo_Loaded") + ": " +
                weapon.LoadedAmmoCount.ToString() + "/" +
                weapon.MagazineCapacity.ToString() +
                "   " + this.text.Tr("CT_Shuttle_WeaponAmmo_Stock") + ": " +
                weapon.AmmoStockCount.ToString();
        }

        private string GetAmmoCommandTooltip(V3DefenseWeaponEntryModel weapon)
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

        private string GetWeaponControlStatusLine(V3DefenseWeaponEntryModel weapon)
        {
            string fireControl = this.GetFireControlStatusLine(weapon);
            if (weapon == null || !weapon.HasAmmoSystem)
            {
                return fireControl;
            }

            string reload;
            if (weapon.ReloadInProgress)
            {
                reload = this.text.Tr("CT_Shuttle_WeaponAmmo_Reloading") + " " +
                    Mathf.RoundToInt(weapon.ReloadProgress01 * 100f).ToString() + "%";
            }
            else if (weapon.ManualReloadJobActive)
            {
                reload = this.text.Tr("CT_Shuttle_WeaponAmmo_AwaitingPawnReload");
            }
            else if (weapon.ReloadRequested)
            {
                reload = this.text.Tr("CT_Shuttle_WeaponAmmo_ReloadRequested");
            }
            else
            {
                reload = this.text.GetAutoReloadLabel(
                    weapon.AutoReloadEnabled,
                    weapon.AutoLoaderAvailable);
            }

            return fireControl + "\n" + reload;
        }

        private string GetFireControlStatusLine(V3DefenseWeaponEntryModel weapon)
        {
            if (weapon == null)
            {
                return this.text.Tr("CT_Shuttle_FireControl_Unavailable_Generic");
            }

            if (!string.IsNullOrEmpty(weapon.FireControlStatusLabel))
            {
                return ShuttleUIText.Tr(
                    "CT_Shuttle_FireControl_StatusFormat",
                    weapon.FireControlStatusLabel,
                    weapon.FireControlModeLabel,
                    weapon.TargetPriorityLabel,
                    weapon.AutoFireEnabled
                        ? this.text.Tr("CT_Shuttle_FireControl_AutoFire_On")
                        : this.text.Tr("CT_Shuttle_FireControl_AutoFire_Off"));
            }

            string linked = weapon.FireControlLinked
                ? this.text.Tr("CT_Shuttle_FireControl_Linked")
                : this.text.Tr("CT_Shuttle_FireControl_Unlinked");
            string autoFire = weapon.AutoFireEnabled
                ? this.text.Tr("CT_Shuttle_FireControl_AutoFire_On")
                : this.text.Tr("CT_Shuttle_FireControl_AutoFire_Off");
            return ShuttleUIText.Tr(
                "CT_Shuttle_FireControl_StatusFormat",
                linked,
                weapon.FireControlModeLabel,
                weapon.TargetPriorityLabel,
                autoFire);
        }

        private static void RegisterTutorialTarget(
            ShuttlePageDrawContext context,
            string targetId,
            Rect rect)
        {
            IShuttleTutorialTargetService tutorialTargets =
                context != null && context.DefensePageContext != null
                    ? context.DefensePageContext.TutorialTargets
                    : null;
            if (tutorialTargets != null)
            {
                tutorialTargets.Register(targetId, rect);
            }
        }

        private sealed class SelectedWeaponButtonSpec
        {
            internal SelectedWeaponButtonSpec(
                string label,
                bool enabled,
                string tooltip,
                Color accentColor,
                Action action)
            {
                this.Label = label;
                this.Enabled = enabled;
                this.Tooltip = tooltip;
                this.AccentColor = accentColor;
                this.Action = action;
            }

            internal string Label { get; private set; }

            internal bool Enabled { get; private set; }

            internal string Tooltip { get; private set; }

            internal Color AccentColor { get; private set; }

            internal Action Action { get; private set; }
        }
    }
}
