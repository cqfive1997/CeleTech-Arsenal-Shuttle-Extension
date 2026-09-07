using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsSectionPanel
    {
        private const float LeftPanelWidth = 270f;
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3SettingsRowDrawer rowDrawer = new V3SettingsRowDrawer();
        private readonly V3SettingsNavigationPanel navigationPanel;
        private readonly V3SettingsPreviewPanel previewPanel;
        private readonly V3SettingsInfoPanel infoPanel;
        private readonly V3SharedMessagePanel messagePanel;

        internal V3SettingsSectionPanel()
        {
            this.navigationPanel = new V3SettingsNavigationPanel(this.rowDrawer);
            this.previewPanel = new V3SettingsPreviewPanel(this.rowDrawer);
            this.infoPanel = new V3SettingsInfoPanel(this.rowDrawer);
            this.messagePanel = new V3SharedMessagePanel();
        }

        internal void Draw(
            Rect rect,
            V3SettingsPageModel model,
            V3SettingsPageState state,
            ShuttlePageDrawContext context)
        {
            if (model == null || state == null || context == null)
            {
                return;
            }

            float gap = ShuttleUIStyle.Gap;
            float rightWidth = RightPanelWidth;
            float rightX = rect.xMax - rightWidth;
            float leftCenterWidth = Mathf.Max(0f, rect.width - rightWidth - gap);
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;
            float leftWidth = Mathf.Min(
                LeftPanelWidth,
                Mathf.Max(220f, Mathf.Floor(leftCenterWidth * 0.36f)));

            Rect leftRect = new Rect(rect.x, rect.y, leftWidth, upperHeight);
            Rect centerRect = new Rect(
                leftRect.xMax + gap,
                rect.y,
                Mathf.Max(0f, leftCenterWidth - leftWidth - gap),
                upperHeight);
            Rect messageRect = new Rect(
                rect.x,
                leftRect.yMax + gap,
                leftCenterWidth,
                messageLayout.MessageHeight);
            Rect rightRect = new Rect(rightX, rect.y, rightWidth, rect.height);

            this.RegisterTutorialTargets(
                context,
                leftRect,
                centerRect,
                rightRect,
                messageRect);

            this.navigationPanel.Draw(leftRect, model, state, context);
            this.previewPanel.Draw(centerRect, model, state, context);
            this.infoPanel.Draw(rightRect, model, state);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect navigationRect,
            Rect previewRect,
            Rect rightInfoRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets =
                context != null && context.SettingsPageContext != null
                    ? context.SettingsPageContext.TutorialTargets
                    : null;
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(ShuttleTutorialTargetIds.SettingsNavigationPanel, navigationRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.SettingsPreviewPanel, previewRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.SettingsRightInfoPanel, rightInfoRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.SettingsMessagePanel, messageRect);
        }
    }
}
