using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3SharedMessagePanel
    {
        private const float Padding = 8f;
        private const float TabHeight = 28f;
        private const float TabGap = 6f;
        private const float MinTabWidth = 88f;
        private const float MaxTabWidth = 126f;
        private const float ContentGap = 6f;

        private readonly V3SharedMessageRowDrawer rowDrawer =
            new V3SharedMessageRowDrawer();
        private readonly V3SharedMessageRowsBuilder rowsBuilder =
            new V3SharedMessageRowsBuilder();
        private readonly V3SharedMessageSummaryDrawer summaryDrawer =
            new V3SharedMessageSummaryDrawer();
        private readonly ShuttleIssueNavigationHandler issueNavigationHandler =
            new ShuttleIssueNavigationHandler();
        private readonly V3SharedMessagePanelState fallbackState =
            new V3SharedMessagePanelState();

        internal void Draw(
            Rect rect,
            ShuttlePageDrawContext context,
            V3SharedMessagePanelState state,
            ref Vector2 currentIssuesScroll)
        {
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            V3SharedMessagePanelState activeState = state ?? this.fallbackState;
            List<V3SharedMessageRow> currentRows =
                this.rowsBuilder.BuildCurrentIssueRows(context);
            List<V3SharedMessageRow> launchRows =
                this.rowsBuilder.BuildLaunchRows(context);
            List<V3SharedMessageRow> devRows =
                this.rowsBuilder.BuildDeveloperRows(context);
            this.EnsureValidTab(activeState);

            ShuttleUILayout.DrawPanelBackground(rect);
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            Rect tabRect = new Rect(
                rect.x + Padding,
                rect.y + Padding,
                Mathf.Max(0f, rect.width - (Padding * 2f)),
                TabHeight);
            this.DrawTabs(
                tabRect,
                activeState,
                currentRows,
                launchRows,
                devRows,
                tutorialTargets);

            Rect listRect = new Rect(
                tabRect.x,
                tabRect.yMax + ContentGap,
                tabRect.width,
                Mathf.Max(0f, rect.yMax - tabRect.yMax - ContentGap - Padding));
            RegisterTutorialTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.InfoPanelContentList,
                listRect);
            this.DrawSelectedRows(
                listRect,
                activeState,
                currentRows,
                launchRows,
                devRows,
                context,
                ref currentIssuesScroll);

            if (activeState.Expanded)
            {
                this.DrawExpandedOverlay(
                    rect,
                    activeState,
                    currentRows,
                    launchRows,
                    devRows,
                    context,
                    ref currentIssuesScroll);
            }
        }

        internal void Draw(
            Rect rect,
            ShuttlePageDrawContext context,
            ref Vector2 currentIssuesScroll)
        {
            this.Draw(rect, context, this.fallbackState, ref currentIssuesScroll);
        }

        private void DrawTabs(
            Rect rect,
            V3SharedMessagePanelState state,
            IList<V3SharedMessageRow> currentRows,
            IList<V3SharedMessageRow> launchRows,
            IList<V3SharedMessageRow> devRows,
            IShuttleTutorialTargetService tutorialTargets)
        {
            bool showDev = IsGodModeEnabled();
            bool showExpand = state.CurrentTab != V3SharedMessagePanelTab.DeveloperDiagnostics;
            int itemCount = 2 + (showDev ? 1 : 0) + 1 + (showExpand ? 1 : 0);
            float itemWidth = CalculateItemWidth(rect.width, itemCount);

            Rect currentRect = new Rect(rect.x, rect.y, itemWidth, rect.height);
            Rect launchRect = new Rect(currentRect.xMax + TabGap, rect.y, itemWidth, rect.height);
            Rect devRect = showDev
                ? new Rect(launchRect.xMax + TabGap, rect.y, itemWidth, rect.height)
                : Rect.zero;
            RegisterTutorialTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.InfoPanelCurrentIssuesTab,
                currentRect);
            RegisterTutorialTarget(
                tutorialTargets,
                ShuttleTutorialTargetIds.InfoPanelLaunchChecklistTab,
                launchRect);

            if (this.DrawTabButton(
                currentRect,
                ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Tab_CurrentIssues"),
                state.CurrentTab == V3SharedMessagePanelTab.CurrentIssues))
            {
                state.CurrentTab = V3SharedMessagePanelTab.CurrentIssues;
            }

            if (this.DrawTabButton(
                launchRect,
                ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Tab_LaunchDiagnostics"),
                state.CurrentTab == V3SharedMessagePanelTab.LaunchDiagnostics))
            {
                state.CurrentTab = V3SharedMessagePanelTab.LaunchDiagnostics;
            }

            if (showDev &&
                this.DrawTabButton(
                    devRect,
                    ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Tab_DeveloperDiagnostics"),
                    state.CurrentTab == V3SharedMessagePanelTab.DeveloperDiagnostics))
            {
                state.CurrentTab = V3SharedMessagePanelTab.DeveloperDiagnostics;
            }

            float rightWidth = itemWidth + (showExpand ? itemWidth + TabGap : 0f);
            Rect summaryRect = new Rect(rect.xMax - rightWidth, rect.y, itemWidth, rect.height);
            this.summaryDrawer.Draw(summaryRect, currentRows, launchRows, devRows);

            if (showExpand)
            {
                Rect expandRect = new Rect(
                    summaryRect.xMax + TabGap,
                    rect.y,
                    itemWidth,
                    rect.height);
                if (this.DrawTabButton(
                    expandRect,
                    ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Expand"),
                    state.Expanded))
                {
                    state.Expanded = true;
                }
            }
        }

        private void DrawSelectedRows(
            Rect rect,
            V3SharedMessagePanelState state,
            IList<V3SharedMessageRow> currentRows,
            IList<V3SharedMessageRow> launchRows,
            IList<V3SharedMessageRow> devRows,
            ShuttlePageDrawContext context,
            ref Vector2 currentIssuesScroll)
        {
            if (state.CurrentTab == V3SharedMessagePanelTab.LaunchDiagnostics)
            {
                this.rowDrawer.DrawRows(
                    rect,
                    launchRows,
                    ref state.LaunchChecklistScroll,
                    delegate(V3SharedMessageRow row)
                    {
                        this.ExecuteIssueAction(row, context);
                    });
                return;
            }

            if (state.CurrentTab == V3SharedMessagePanelTab.DeveloperDiagnostics &&
                IsGodModeEnabled())
            {
                this.rowDrawer.DrawRows(rect, devRows, ref state.DeveloperDiagnosticsScroll);
                return;
            }

            this.rowDrawer.DrawRows(
                rect,
                currentRows,
                ref currentIssuesScroll,
                delegate(V3SharedMessageRow row)
                {
                    this.ExecuteIssueAction(row, context);
                });
        }

        private void DrawExpandedOverlay(
            Rect compactRect,
            V3SharedMessagePanelState state,
            IList<V3SharedMessageRow> currentRows,
            IList<V3SharedMessageRow> launchRows,
            IList<V3SharedMessageRow> devRows,
            ShuttlePageDrawContext context,
            ref Vector2 currentIssuesScroll)
        {
            Rect expandedRect;
            if (!TryCalculateExpandedRect(compactRect, out expandedRect))
            {
                state.Expanded = false;
                return;
            }

            ShuttleUILayout.DrawPanelBackground(expandedRect);
            Widgets.DrawBox(expandedRect, 2);

            Rect titleRect = new Rect(expandedRect.x + 8f, expandedRect.y + 6f,
                expandedRect.width - 16f, 28f);
            Rect closeRect = new Rect(titleRect.xMax - 86f, titleRect.y + 2f, 82f, 24f);
            Rect labelRect = new Rect(titleRect.x, titleRect.y,
                Mathf.Max(0f, closeRect.x - titleRect.x - 8f), titleRect.height);
            V3SharedMessageSummaryDrawer.DrawTitle(labelRect, this.GetCurrentTabTitle(state));
            if (this.DrawTabButton(
                closeRect,
                ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Collapse"),
                false))
            {
                state.Expanded = false;
            }

            Rect listRect = new Rect(
                expandedRect.x + 8f,
                titleRect.yMax + 6f,
                expandedRect.width - 16f,
                Mathf.Max(0f, expandedRect.yMax - titleRect.yMax - 14f));
            this.DrawSelectedRows(
                listRect,
                state,
                currentRows,
                launchRows,
                devRows,
                context,
                ref currentIssuesScroll);
        }

        private void ExecuteIssueAction(
            V3SharedMessageRow row,
            ShuttlePageDrawContext context)
        {
            if (!this.issueNavigationHandler.Execute(row, context))
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"),
                    false);
            }
        }

        private bool DrawTabButton(Rect rect, string label, bool selected)
        {
            return ShuttleUIActionButtonDrawer.DrawTintedButton(
                rect,
                label,
                true,
                selected ? ShuttleUIStyle.SelectedColor : ShuttleUIStyle.ButtonBgColor,
                selected ? ShuttleUIStyle.BlueStatusColor : ShuttleUIStyle.ButtonBorderColor,
                selected ? ShuttleUIStyle.HeaderTitleTextColor : ShuttleUIStyle.ButtonTextColor,
                label);
        }

        private void EnsureValidTab(V3SharedMessagePanelState state)
        {
            if (state != null &&
                !IsGodModeEnabled() &&
                state.CurrentTab == V3SharedMessagePanelTab.DeveloperDiagnostics)
            {
                state.CurrentTab = V3SharedMessagePanelTab.CurrentIssues;
            }
        }

        private string GetCurrentTabTitle(V3SharedMessagePanelState state)
        {
            if (state != null &&
                state.CurrentTab == V3SharedMessagePanelTab.LaunchDiagnostics)
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Tab_LaunchDiagnostics");
            }

            if (state != null &&
                state.CurrentTab == V3SharedMessagePanelTab.DeveloperDiagnostics &&
                IsGodModeEnabled())
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Tab_DeveloperDiagnostics");
            }

            return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Tab_CurrentIssues");
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.Services != null
                ? context.Services.TutorialTargets
                : null;
        }

        private static void RegisterTutorialTarget(
            IShuttleTutorialTargetService tutorialTargets,
            string targetId,
            Rect rect)
        {
            if (tutorialTargets != null)
            {
                tutorialTargets.Register(targetId, rect);
            }
        }

        private static bool IsGodModeEnabled()
        {
            return Prefs.DevMode && DebugSettings.godMode;
        }

        private static float CalculateItemWidth(float availableWidth, int itemCount)
        {
            if (itemCount <= 0)
            {
                return MaxTabWidth;
            }

            float gapWidth = TabGap * Mathf.Max(0, itemCount - 1);
            float width = Mathf.Floor((availableWidth - gapWidth) / itemCount);
            return Mathf.Max(MinTabWidth, Mathf.Min(MaxTabWidth, width));
        }

        private static bool TryCalculateExpandedRect(
            Rect compactRect,
            out Rect expandedRect)
        {
            float availableHeight = Mathf.Max(0f, compactRect.yMax);
            float height = Mathf.Min(520f, Mathf.Max(260f, compactRect.height + 300f));
            height = Mathf.Min(height, availableHeight);
            if (height < compactRect.height + 120f)
            {
                expandedRect = Rect.zero;
                return false;
            }

            expandedRect = new Rect(compactRect.x, compactRect.yMax - height, compactRect.width, height);
            return true;
        }
    }
}
