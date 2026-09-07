using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Performance
{
    internal sealed class V3PerformancePage : IShuttleIntegratedHeaderPageV3
    {
        private const float ToolbarHeight = 40f;
        private const float FooterHeight = 30f;
        private readonly V3PerformancePageState fallbackState =
            new V3PerformancePageState();
        private readonly V3PerformanceHeader header = new V3PerformanceHeader();
        private readonly V3PerformanceSummaryDrawer summaryDrawer =
            new V3PerformanceSummaryDrawer();
        private readonly V3PerformanceMetricsDrawer metricsDrawer =
            new V3PerformanceMetricsDrawer();
        private readonly V3PerformanceOverviewDrawer overviewDrawer =
            new V3PerformanceOverviewDrawer();

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Performance; }
        }

        public void OnEnter(ShuttlePageDrawContext context)
        {
            V3PerformancePageState state = this.GetState(context);
            IShuttlePerformanceCaptureControlPort control = GetCaptureControl(context);
            if (state.CaptureLeaseID <= 0 && control != null)
            {
                state.CaptureLeaseID = control.BeginPerformanceCapture();
            }
        }

        public void OnExit(ShuttlePageDrawContext context)
        {
            V3PerformancePageState state = this.GetState(context);
            if (state.CaptureLeaseID <= 0)
            {
                return;
            }

            IShuttlePerformanceCaptureControlPort control = GetCaptureControl(context);
            if (control != null)
            {
                control.EndPerformanceCapture(state.CaptureLeaseID);
            }

            state.CaptureLeaseID = 0;
        }

        public void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            ShuttlePerformanceDashboardReadModel dashboard = this.GetDashboard(context);
            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.header.Draw(layout.HeaderRect, dashboard, context);
            this.DrawBody(layout.BodyRect, dashboard, context);
        }

        private void DrawBody(
            Rect rect,
            ShuttlePerformanceDashboardReadModel dashboard,
            ShuttlePageDrawContext context)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            Rect inner = rect.ContractedBy(10f);
            Rect toolbar = new Rect(inner.x, inner.y, inner.width, ToolbarHeight);
            Rect summary = new Rect(
                inner.x,
                toolbar.yMax + 6f,
                inner.width,
                V3PerformanceSummaryDrawer.Height);
            Rect footer = new Rect(
                inner.x,
                inner.yMax - FooterHeight,
                inner.width,
                FooterHeight);
            Rect lower = new Rect(
                inner.x,
                summary.yMax + 8f,
                inner.width,
                Mathf.Max(0f, footer.y - summary.yMax - 14f));
            float rightWidth = Mathf.Clamp(lower.width * 0.31f, 280f, 360f);
            Rect metrics = new Rect(
                lower.x,
                lower.y,
                Mathf.Max(0f, lower.width - rightWidth - 8f),
                lower.height);
            Rect overview = new Rect(
                metrics.xMax + 8f,
                lower.y,
                rightWidth,
                lower.height);

            this.DrawToolbar(toolbar, dashboard, context);
            this.summaryDrawer.Draw(summary, dashboard);
            this.metricsDrawer.Draw(metrics, dashboard, this.GetState(context));
            this.overviewDrawer.Draw(overview, dashboard);
            this.DrawFooter(footer, dashboard);
        }

        private void DrawToolbar(
            Rect rect,
            ShuttlePerformanceDashboardReadModel dashboard,
            ShuttlePageDrawContext context)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x, rect.y, 230f, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_Title"),
                GameFont.Medium,
                GameFont.Small,
                ShuttleUIStyle.HeaderTitleTextColor,
                null,
                TextAnchor.MiddleLeft);
            string status = dashboard.IsCapturing
                ? ShuttleUIText.Tr("CT_Shuttle_Performance_Capturing")
                : ShuttleUIText.Tr("CT_Shuttle_Performance_Cached");
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 238f, rect.y + 15f, 10f, 10f),
                dashboard.IsCapturing
                    ? ShuttleUIStyle.GreenStatusColor
                    : ShuttleUIStyle.YellowStatusColor);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 256f, rect.y, rect.width - 480f, rect.height),
                ShuttleUIText.Tr(
                    "CT_Shuttle_Performance_SampleStatusFormat",
                    status,
                    dashboard.ActiveTickSamples),
                GameFont.Small,
                GameFont.Tiny,
                dashboard.IsCapturing
                    ? ShuttleUIStyle.GreenStatusColor
                    : ShuttleUIStyle.YellowStatusColor,
                null,
                TextAnchor.MiddleLeft);
            Rect reset = new Rect(rect.xMax - 138f, rect.y + 3f, 138f, 34f);
            V3PerformancePageState state = this.GetState(context);
            if (ShuttleUILayout.DrawButton(
                    reset,
                    ShuttleUIText.Tr("CT_Shuttle_Performance_Reset"),
                    state.CaptureLeaseID > 0,
                    ShuttleUIButtonKind.Normal,
                    ShuttleUIText.Tr("CT_Shuttle_Performance_ResetTooltip")))
            {
                IShuttlePerformanceCaptureControlPort control = GetCaptureControl(context);
                if (control != null)
                {
                    control.ResetPerformanceCapture(state.CaptureLeaseID);
                    state.MetricScroll = Vector2.zero;
                }
            }
        }

        private void DrawFooter(
            Rect rect,
            ShuttlePerformanceDashboardReadModel dashboard)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.MutedCardColor);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 8f, rect.y + 10f, 8f, 8f),
                dashboard.IsCapturing
                    ? ShuttleUIStyle.GreenStatusColor
                    : ShuttleUIStyle.YellowStatusColor);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 24f, rect.y, rect.width - 32f, rect.height),
                ShuttleUIText.Tr("CT_Shuttle_Performance_Footer"),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleLeft);
        }

        private ShuttlePerformanceDashboardReadModel GetDashboard(
            ShuttlePageDrawContext context)
        {
            IShuttlePerformanceDashboardReadPort readPort =
                context != null && context.Services != null
                    ? context.Services.PerformanceDashboardReadPort
                    : null;
            return readPort != null
                ? readPort.GetPerformanceDashboardReadModel()
                : ShuttlePerformanceDashboardReadModel.Empty;
        }

        private V3PerformancePageState GetState(ShuttlePageDrawContext context)
        {
            return context != null &&
                context.State != null &&
                context.State.Performance != null
                    ? context.State.Performance
                    : this.fallbackState;
        }

        private static IShuttlePerformanceCaptureControlPort GetCaptureControl(
            ShuttlePageDrawContext context)
        {
            return context != null && context.Services != null
                ? context.Services.PerformanceCaptureControlPort
                : null;
        }
    }
}
