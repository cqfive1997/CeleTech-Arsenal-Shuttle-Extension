using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    /// <summary>
    /// Native V3 Cargo inventory shell. Cargo runtime truth and advanced dialogs
    /// remain behind existing read/action bridges while the normal page path draws
    /// header, bay list, stack detail, logistics summary, and messages natively.
    /// </summary>
    internal sealed class V3CargoPage : IShuttleIntegratedHeaderPageV3
    {
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;

        private readonly V3CargoText text = new V3CargoText();
        private readonly V3CargoPageModelBuilder modelBuilder = new V3CargoPageModelBuilder();
        private readonly V3CargoProjectionKeyBuilder projectionKeyBuilder =
            new V3CargoProjectionKeyBuilder();
        private readonly V3CargoProjectionCache projectionCache =
            new V3CargoProjectionCache();
        private readonly V3CargoVisibleStackCache visibleStackCache =
            new V3CargoVisibleStackCache();
        private readonly V3CargoPageState fallbackState = new V3CargoPageState();
        private readonly V3CargoSelection selection = new V3CargoSelection();
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3CargoHeader header;
        private readonly V3CargoPanelDrawer panel;
        private readonly V3CargoStackListPanel stackListPanel;
        private readonly V3CargoStackDetailPanel stackDetailPanel;
        private readonly V3CargoLogisticsPanel logisticsPanel;

        internal V3CargoPage()
        {
            this.panel = new V3CargoPanelDrawer(this.text);
            this.header = new V3CargoHeader(this.text);
            this.stackListPanel = new V3CargoStackListPanel(
                this.text,
                this.panel,
                this.selection);
            this.stackDetailPanel = new V3CargoStackDetailPanel(
                this.text,
                this.panel);
            this.logisticsPanel = new V3CargoLogisticsPanel(
                this.text,
                this.panel);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Cargo; }
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

            if (ShuttleCargoOpenPathDiagnosticFlags.CargoPageSkeletonOnly)
            {
                this.DrawCargoPageDiagnosticSkeleton(rect);
                return;
            }

            V3CargoPageState state = this.GetPageState(context);
            V3CargoProjectionKey projectionKey =
                this.projectionKeyBuilder.Build(context.CargoInputs);
            V3CargoPageModel pageModel;
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoPageProjection))
            {
                pageModel = this.projectionCache.GetOrBuild(
                    projectionKey,
                    context.CargoInputs,
                    this.modelBuilder);
            }

            state.TrackCargoSnapshotRevision(pageModel.CargoSnapshotRevision);
            List<V3CargoVisibleStackModel> visibleStacks =
                this.visibleStackCache.GetOrBuild(
                    this.projectionCache.ProjectionRevision,
                    state.SelectedCargoCategory,
                    pageModel.CargoPageModel,
                    this.selection);
            V3CargoVisibleStackModel selectedStack =
                this.selection.EnsureSelectedStack(state, visibleStacks);

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.header.Draw(layout.HeaderRect, pageModel, state, context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoBodyDraw))
            {
                this.DrawBody(
                    layout.BodyRect,
                    pageModel,
                    context,
                    state,
                    visibleStacks,
                    selectedStack);
            }
        }

        private void DrawBody(
            Rect rect,
            V3CargoPageModel pageModel,
            ShuttlePageDrawContext context,
            V3CargoPageState state,
            List<V3CargoVisibleStackModel> visibleStacks,
            V3CargoVisibleStackModel selectedStack)
        {
            float gap = ShuttleUIStyle.Gap;
            float rightX = rect.xMax - RightPanelWidth;
            float leftWidth = rect.width - RightPanelWidth - gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = messageLayout.ContentHeight;

            Rect cargoOverviewRect = new Rect(rect.x, rect.y, leftWidth, upperHeight);
            Rect messageRect = new Rect(
                rect.x,
                cargoOverviewRect.yMax + gap,
                leftWidth,
                messageLayout.MessageHeight);
            Rect logisticsRect = new Rect(
                rightX,
                rect.y,
                RightPanelWidth,
                Mathf.Min(304f, rect.height * 0.46f));
            Rect detailRect = new Rect(
                rightX,
                logisticsRect.yMax + gap,
                RightPanelWidth,
                Mathf.Max(0f, rect.height - logisticsRect.height - gap));

            this.RegisterTutorialTargets(
                context,
                rect,
                cargoOverviewRect,
                detailRect,
                logisticsRect,
                messageRect);

            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoStackListDraw))
            {
                this.stackListPanel.Draw(
                    cargoOverviewRect,
                    pageModel,
                    state,
                    context,
                    visibleStacks);
            }

            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoLogisticsDraw))
            {
                this.logisticsPanel.Draw(logisticsRect, pageModel, state, context);
            }

            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoStackDetailDraw))
            {
                this.stackDetailPanel.Draw(detailRect, pageModel, state, context, selectedStack);
            }

            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoMessagePanelDraw))
            {
                this.messagePanel.Draw(
                    messageRect,
                    context,
                    state.MessagePanelState,
                    ref state.CargoMessageScroll);
            }
        }

        private void DrawCargoPageDiagnosticSkeleton(Rect rect)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            Rect inner = rect.ContractedBy(16f);
            Text.Font = GameFont.Medium;
            GUI.color = V3CargoText.AccentColor;
            ShuttleUILayout.SafeLabel(
                new Rect(inner.x, inner.y, inner.width, 30f),
                "Cargo");
            Text.Font = GameFont.Small;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(inner.x, inner.y + 36f, inner.width, 24f),
                "Cargo page diagnostic skeleton active");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private V3CargoPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Cargo != null)
            {
                return context.State.Cargo;
            }

            return this.fallbackState;
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect pageRect,
            Rect cargoOverviewRect,
            Rect stackDetailRect,
            Rect logisticsRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingOverviewPanel, pageRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingCargoBaysPanel, cargoOverviewRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingInventoryPanel, stackDetailRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingCapacityStatsPanel, stackDetailRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingLogisticsModulePanel, logisticsRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingMessagesPanel, messageRect);
            tutorialTargets.Register(ShuttleTutorialTargetIds.LoadingCargoBayCardsPanel, cargoOverviewRect);
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.CargoPageContext != null
                ? context.CargoPageContext.TutorialTargets
                : null;
        }
    }
}
