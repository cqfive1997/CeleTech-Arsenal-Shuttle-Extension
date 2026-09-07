using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    /// <summary>
    /// Native V3 Processing page. It owns page state, layout, and page drawing.
    /// </summary>
    internal sealed class V3ProcessingPage : IShuttleIntegratedHeaderPageV3
    {
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3ProcessingText text = new V3ProcessingText();
        private readonly V3ProcessingPageModel model = new V3ProcessingPageModel();
        private readonly V3ProcessingPageModelBuilder modelBuilder =
            new V3ProcessingPageModelBuilder();
        private readonly V3ProcessingPageState fallbackState = new V3ProcessingPageState();
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3ProcessingHeader header;
        private readonly V3ProcessingActionPanel actionPanel;
        private readonly V3ProcessingWorkTablePanel workTablePanel;

        internal V3ProcessingPage()
        {
            this.header = new V3ProcessingHeader(this.text);
            this.actionPanel = new V3ProcessingActionPanel(this.text);
            this.workTablePanel = new V3ProcessingWorkTablePanel(this.text);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Processing; }
        }

        public void OnEnter(ShuttlePageDrawContext context)
        {
        }

        public void OnExit(ShuttlePageDrawContext context)
        {
        }

        public void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            V3ProcessingPageState state = this.GetPageState(context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ProcessingProjection))
            {
                this.modelBuilder.Fill(
                    this.model,
                    context.ProcessingInputs,
                    state.SelectedWorkbenchId);
            }
            this.EnsureSelectedWorkbench(this.model.ProcessingModel, state);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);

            this.header.Draw(layout.HeaderRect, this.model, context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.ProcessingBodyDraw))
            {
                this.DrawBody(layout.BodyRect, this.model, context, state);
            }
        }

        private void DrawBody(
            Rect rect,
            V3ProcessingPageModel pageModel,
            ShuttlePageDrawContext context,
            V3ProcessingPageState state)
        {
            float gap = ShuttleUIStyle.Gap;
            float rightX = rect.xMax - RightPanelWidth;
            float leftCenterWidth = rect.width - RightPanelWidth - gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;
            float leftWidth = Mathf.Clamp(Mathf.Floor(leftCenterWidth * 0.42f), 300f, 384f);

            Rect billRect = new Rect(rect.x, rect.y, leftWidth, upperHeight);
            Rect recipeRect = new Rect(
                billRect.xMax + gap,
                rect.y,
                leftCenterWidth - leftWidth - gap,
                upperHeight);
            Rect messageRect = new Rect(
                rect.x,
                billRect.yMax + gap,
                leftCenterWidth,
                messageLayout.MessageHeight);
            Rect workbenchRect = new Rect(rightX, rect.y, RightPanelWidth, rect.height);

            this.RegisterTutorialTargets(
                context,
                rect,
                billRect,
                recipeRect,
                workbenchRect,
                messageRect);

            this.actionPanel.DrawBills(billRect, pageModel, state, context);
            this.actionPanel.DrawRecipes(recipeRect, pageModel, state, context);
            this.workTablePanel.Draw(workbenchRect, pageModel, state, context);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private V3ProcessingPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Processing != null)
            {
                return context.State.Processing;
            }

            return this.fallbackState;
        }

        private void EnsureSelectedWorkbench(
            V3ProcessingReadModel processingModel,
            V3ProcessingPageState state)
        {
            if (processingModel == null || state == null)
            {
                return;
            }

            if (processingModel.SelectedWorkbench != null)
            {
                state.SelectedWorkbenchId = processingModel.SelectedWorkbench.Id;
            }
            else if (processingModel.Workbenches == null || processingModel.Workbenches.Count == 0)
            {
                state.SelectedWorkbenchId = null;
            }
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect pageRect,
            Rect billRect,
            Rect recipeRect,
            Rect workbenchRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingOverviewPanel, pageRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingQueuePanel, billRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingSelectedWorktablePanel, workbenchRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingActionsPanel, billRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingRecipePanel, recipeRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingInputOutputPanel, recipeRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingWorktableListPanel, workbenchRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingStatusPanel, workbenchRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.ProcessingMessagesPanel, messageRect);
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.ProcessingPageContext != null
                ? context.ProcessingPageContext.TutorialTargets
                : null;
        }
    }
}
