using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsNavigationPanel
    {
        private const float PanelContentTopOffset = 44f;

        private readonly V3SettingsRowDrawer rowDrawer;

        internal V3SettingsNavigationPanel(V3SettingsRowDrawer rowDrawer)
        {
            this.rowDrawer = rowDrawer;
        }

        internal void Draw(
            Rect rect,
            V3SettingsPageModel model,
            V3SettingsPageState state,
            ShuttlePageDrawContext context)
        {
            if (this.rowDrawer == null || state == null || context == null)
            {
                return;
            }

            IShuttleSettingsUIActions actions =
                context.SettingsPageContext != null
                    ? context.SettingsPageContext.SettingsActions
                    : null;
            if (actions == null)
            {
                return;
            }

            this.rowDrawer.DrawPanelTitle(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_Settings"));
            Rect inner = this.GetPanelInnerRect(rect);
            Rect hubRect = new Rect(inner.x, inner.y, inner.width, 48f);
            this.DrawHubButton(
                hubRect,
                model != null ? model.ControlModel : null,
                model != null ? model.WeaponBayModel : null,
                actions,
                GetTutorialTargets(context));

            float tutorialHeight = 164f;
            Rect tutorialRect = new Rect(
                inner.x,
                Mathf.Max(hubRect.yMax + 10f, inner.yMax - tutorialHeight),
                inner.width,
                tutorialHeight);
            Rect logRect = new Rect(
                inner.x,
                hubRect.yMax + 10f,
                inner.width,
                Mathf.Max(0f, tutorialRect.y - hubRect.yMax - 20f));
            this.DrawConfigLog(
                logRect,
                model != null ? model.SettingsModel : null,
                state);
            this.DrawTutorialControls(
                tutorialRect,
                actions,
                GetTutorialTargets(context));
        }

        private void DrawHubButton(
            Rect rect,
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            IShuttleSettingsUIActions actions,
            IShuttleTutorialTargetService tutorialTargets)
        {
            RegisterHubTargets(tutorialTargets, rect);
            if (this.rowDrawer.DrawActionButton(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_SettingsHub_Open"),
                false,
                ShuttleUIText.Tr("CT_Shuttle_SettingsHub_OpenTooltip")))
            {
                actions.OpenSettingsHub(controlModel, weaponBayModel);
            }
        }

        private void DrawConfigLog(
            Rect rect,
            ShuttleSettingsReadModel settingsModel,
            V3SettingsPageState state)
        {
            if (rect.height <= 0f)
            {
                return;
            }

            V3SettingsSubTitleDrawer.Draw(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_CurrentConfigLog"));
            Rect listRect = new Rect(rect.x, rect.y + 28f, rect.width, rect.height - 28f);
            this.rowDrawer.DrawRows(
                listRect,
                settingsModel != null ? settingsModel.SettingRows : null,
                ref state.ConfigLogScroll,
                58f);
        }

        private void DrawTutorialControls(
            Rect rect,
            IShuttleSettingsUIActions actions,
            IShuttleTutorialTargetService tutorialTargets)
        {
            V3SettingsSubTitleDrawer.Draw(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_GameplayGuide"));
            float y = rect.y + 28f;
            bool tutorialsEnabled = actions.TutorialsEnabled;
            Rect enabledRect = new Rect(rect.x, y, rect.width, 36f);
            RegisterTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.SettingsTutorialsEnabledToggle,
                enabledRect);
            if (this.rowDrawer.DrawCheckbox(
                enabledRect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_TutorialsEnabled"),
                ref tutorialsEnabled,
                null))
            {
                actions.SetTutorialsEnabled(tutorialsEnabled);
            }

            y += 42f;
            bool autoStart = actions.TutorialsAutoStart;
            Rect autoStartRect = new Rect(rect.x, y, rect.width, 36f);
            RegisterTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.SettingsTutorialsAutoStartToggle,
                autoStartRect);
            if (this.rowDrawer.DrawCheckbox(
                autoStartRect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_TutorialsAutoStart"),
                ref autoStart,
                null))
            {
                actions.SetTutorialsAutoStart(autoStart);
            }

            y += 42f;
            Rect resetRect = new Rect(rect.x, y, rect.width, 38f);
            RegisterTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.SettingsResetTutorialsButton,
                resetRect);
            if (this.rowDrawer.DrawActionButton(
                resetRect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_ResetTutorials"),
                true,
                ShuttleUIText.Tr("CT_Shuttle_Settings_ResetTutorialsDesc")))
            {
                actions.ResetTutorialProgress();
            }
        }

        private static void RegisterHubTargets(
            IShuttleTutorialTargetService tutorialTargets,
            Rect rect)
        {
            RegisterTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.SettingsPerformanceButton,
                rect);
            RegisterTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.SettingsCombatButton,
                rect);
            RegisterTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.SettingsPaintButton,
                rect);
        }

        private static void RegisterTarget(
            IShuttleTutorialTargetService tutorialTargets,
            string targetId,
            Rect rect)
        {
            if (tutorialTargets != null)
            {
                tutorialTargets.Register(targetId, rect);
            }
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.SettingsPageContext != null
                ? context.SettingsPageContext.TutorialTargets
                : null;
        }

        private Rect GetPanelInnerRect(Rect rect)
        {
            return new Rect(
                rect.x + 10f,
                rect.y + PanelContentTopOffset,
                Mathf.Max(0f, rect.width - 20f),
                Mathf.Max(0f, rect.height - PanelContentTopOffset - 10f));
        }
    }
}
