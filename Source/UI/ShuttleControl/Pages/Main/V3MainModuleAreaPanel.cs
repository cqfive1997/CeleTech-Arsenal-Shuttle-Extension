using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal struct V3MainModuleAreaLayout
    {
        private const float SegmentPanelWidth = 270f;
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;
        private const float RightPanelHeightRatio = 0.28f;
        private const float RightPanelMaxHeight = 238f;

        internal Rect BodyRect;
        internal Rect SegmentRect;
        internal Rect VisualizationRect;
        internal Rect ModuleGridRect;
        internal Rect SelectedModuleRect;
        internal Rect MessageRect;

        internal static V3MainModuleAreaLayout FromBodyRect(Rect bodyRect)
        {
            float gap = ShuttleUIStyle.Gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(bodyRect.height, gap);
            float upperHeight = messageLayout.ContentHeight;
            float rightX = bodyRect.xMax - RightPanelWidth;
            float leftCenterWidth = bodyRect.width - RightPanelWidth - gap;
            float rightTopHeight = Mathf.Min(
                RightPanelMaxHeight,
                Mathf.Floor(bodyRect.height * RightPanelHeightRatio));

            V3MainModuleAreaLayout layout = new V3MainModuleAreaLayout();
            layout.BodyRect = bodyRect;
            layout.SegmentRect = messageLayout.ContentRect(bodyRect, SegmentPanelWidth);
            layout.VisualizationRect = new Rect(
                layout.SegmentRect.xMax + gap,
                bodyRect.y,
                Mathf.Max(0f, leftCenterWidth - SegmentPanelWidth - gap),
                upperHeight);
            layout.MessageRect = messageLayout.MessageRect(
                bodyRect,
                layout.SegmentRect.yMax,
                leftCenterWidth,
                gap);
            layout.ModuleGridRect = new Rect(
                rightX,
                bodyRect.y,
                RightPanelWidth,
                rightTopHeight);
            layout.SelectedModuleRect = new Rect(
                rightX,
                layout.ModuleGridRect.yMax + gap,
                RightPanelWidth,
                Mathf.Max(0f, bodyRect.height - rightTopHeight - gap));
            return layout;
        }
    }

    internal sealed class V3MainModuleAreaPanel
    {
        private readonly V3MainModuleSelection selection;
        private readonly V3MainSegmentListPanel segmentListPanel;
        private readonly V3MainVisualizationPanel visualizationPanel;
        private readonly V3MainModuleGridPanel moduleGridPanel;
        private readonly V3MainSelectedModulePanel selectedModulePanel;

        internal V3MainModuleAreaPanel(
            V3MainText text,
            V3MainPanelDrawer panel,
            V3MainModuleSelection selection)
        {
            this.selection = selection;
            V3MainSegmentCardDrawer segmentCardDrawer =
                new V3MainSegmentCardDrawer(text, panel, selection);
            V3MainModuleCardDrawer moduleCardDrawer =
                new V3MainModuleCardDrawer(text, panel, selection);
            this.segmentListPanel =
                new V3MainSegmentListPanel(text, panel, segmentCardDrawer, selection);
            this.visualizationPanel =
                new V3MainVisualizationPanel(text, panel, selection);
            this.moduleGridPanel =
                new V3MainModuleGridPanel(text);
            this.selectedModulePanel =
                new V3MainSelectedModulePanel(
                    text,
                    panel,
                    moduleCardDrawer,
                    new V3MainConstructionPanel(text, panel),
                    selection);
        }

        internal void Draw(
            V3MainModuleAreaLayout layout,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context)
        {
            if (model == null || state == null)
            {
                return;
            }

            V3MainAssemblyFocusFeedback.ClearExpired(state);
            this.selection.EnsureSelectedSegment(state, model.ControlModel);
            this.RegisterTutorialTargets(context, layout);
            this.segmentListPanel.Draw(layout.SegmentRect, model, state, context);
            this.visualizationPanel.Draw(layout.VisualizationRect, model, state, context);
            this.moduleGridPanel.Draw(layout.ModuleGridRect, model, state, context);
            this.selectedModulePanel.Draw(layout.SelectedModuleRect, model, state, context);
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            V3MainModuleAreaLayout layout)
        {
            IShuttleTutorialTargetService tutorialTargets =
                context != null && context.MainPageContext != null
                    ? context.MainPageContext.TutorialTargets
                    : null;
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(ShuttleTutorialTargetIds.MainSegmentPanel, layout.SegmentRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.MainVisualizationPanel, layout.VisualizationRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.MainFunctionModulePanel, layout.ModuleGridRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.MainSelectedSegmentModulesPanel, layout.SelectedModuleRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.MainMessagePanel, layout.MessageRect);
        }
    }
}
