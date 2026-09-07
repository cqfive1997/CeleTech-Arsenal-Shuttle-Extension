using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    /// <summary>
    /// Native V3 Prison Cell page. It owns page state, layout, and page drawing.
    /// </summary>
    internal sealed class V3PrisonCellPage : IShuttleIntegratedHeaderPageV3
    {
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3PrisonCellText text = new V3PrisonCellText();
        private readonly V3PrisonCellPageModel model = new V3PrisonCellPageModel();
        private readonly V3PrisonCellPageModelBuilder modelBuilder =
            new V3PrisonCellPageModelBuilder();
        private readonly V3PrisonCellPageState fallbackState = new V3PrisonCellPageState();
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3PrisonCellHeader header;
        private readonly V3PrisonCellListPanel listPanel;
        private readonly V3PrisonCellActionPanel actionPanel;

        internal V3PrisonCellPage()
        {
            this.header = new V3PrisonCellHeader();
            this.listPanel = new V3PrisonCellListPanel(this.text);
            this.actionPanel = new V3PrisonCellActionPanel(this.text);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.PrisonCell; }
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

            V3PrisonCellPageState state = this.GetPageState(context);
            this.modelBuilder.Fill(this.model, context.PrisonCellInputs);
            V3PrisonCellSelection.EnsureSelection(this.model.PrisonCellModel, state);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);

            this.header.Draw(layout.HeaderRect, this.model, context);
            this.DrawBody(layout.BodyRect, this.model, context, state);
        }

        private void DrawBody(
            Rect rect,
            V3PrisonCellPageModel pageModel,
            ShuttlePageDrawContext context,
            V3PrisonCellPageState state)
        {
            float gap = ShuttleUIStyle.Gap;
            float rightX = rect.xMax - RightPanelWidth;
            float leftCenterWidth = rect.width - RightPanelWidth - gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;
            float candidateWidth = Mathf.Clamp(Mathf.Floor(leftCenterWidth * 0.34f), 260f, 330f);
            float heldWidth = leftCenterWidth - candidateWidth - gap;

            Rect candidateRect = new Rect(rect.x, rect.y, candidateWidth, upperHeight);
            Rect heldRect = new Rect(candidateRect.xMax + gap, rect.y, heldWidth, upperHeight);
            Rect actionRect = new Rect(rightX, rect.y, RightPanelWidth, rect.height);
            Rect messageRect = new Rect(
                rect.x,
                heldRect.yMax + gap,
                leftCenterWidth,
                messageLayout.MessageHeight);

            this.RegisterTutorialTargets(
                context,
                rect,
                heldRect,
                candidateRect,
                actionRect,
                messageRect);

            this.listPanel.DrawCandidates(candidateRect, pageModel, state);
            this.listPanel.DrawHeldPrisoners(heldRect, pageModel, state);
            this.actionPanel.Draw(actionRect, pageModel, state, context);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
        }

        private V3PrisonCellPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.PrisonCell != null)
            {
                return context.State.PrisonCell;
            }

            return this.fallbackState;
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect pageRect,
            Rect heldRect,
            Rect candidateRect,
            Rect actionRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonOverviewPanel, pageRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonPrisonerListPanel, heldRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonAdmissionPanel, candidateRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonerDetailPanel, heldRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonCellStatusPanel, actionRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonActionsPanel, actionRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.PrisonMessagesPanel, messageRect);
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.PrisonCellPageContext != null
                ? context.PrisonCellPageContext.TutorialTargets
                : null;
        }
    }
}
