using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Performance
{
    internal sealed class V3PerformanceOverviewDrawer
    {
        internal void Draw(Rect rect, ShuttlePerformanceDashboardReadModel model)
        {
            ShuttlePerformanceDashboardReadModel dashboard = model ??
                ShuttlePerformanceDashboardReadModel.Empty;
            float gap = 8f;
            float compositionHeight = Mathf.Min(128f, rect.height * 0.32f);
            float noteHeight = 96f;
            float trendHeight = Mathf.Max(
                90f,
                rect.height - compositionHeight - noteHeight - (gap * 2f));
            Rect compositionRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                compositionHeight);
            Rect trendRect = new Rect(
                rect.x,
                compositionRect.yMax + gap,
                rect.width,
                trendHeight);
            Rect noteRect = new Rect(
                rect.x,
                trendRect.yMax + gap,
                rect.width,
                Mathf.Max(0f, rect.yMax - trendRect.yMax - gap));
            this.DrawComposition(compositionRect, dashboard);
            this.DrawTrend(trendRect, dashboard.RecentControllerTickMs);
            this.DrawNote(noteRect);
        }

        private void DrawComposition(
            Rect rect,
            ShuttlePerformanceDashboardReadModel model)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false);
            ShuttleUILayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Performance_TickComposition"));
            Rect bar = new Rect(rect.x + 12f, rect.y + 42f, rect.width - 24f, 18f);
            float moduleShare = Mathf.Clamp01(model.ModuleShare01);
            Widgets.DrawBoxSolid(bar, new Color(0f, 0f, 0f, 0.30f));
            Widgets.DrawBoxSolid(
                new Rect(bar.x, bar.y, bar.width * moduleShare, bar.height),
                ShuttleUIStyle.BlueStatusColor);
            Widgets.DrawBoxSolid(
                new Rect(
                    bar.x + (bar.width * moduleShare),
                    bar.y,
                    bar.width * (1f - moduleShare),
                    bar.height),
                new Color(0.55f, 0.38f, 0.78f, 1f));
            this.DrawLegend(
                new Rect(rect.x + 12f, bar.yMax + 9f, rect.width - 24f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Performance_ModuleRuntime"),
                moduleShare,
                ShuttleUIStyle.BlueStatusColor);
            this.DrawLegend(
                new Rect(rect.x + 12f, bar.yMax + 31f, rect.width - 24f, 18f),
                ShuttleUIText.Tr("CT_Shuttle_Performance_CoreLogic"),
                1f - moduleShare,
                new Color(0.55f, 0.38f, 0.78f, 1f));
        }

        private void DrawLegend(Rect rect, string label, float share01, Color color)
        {
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 4f, 9f, 9f),
                color);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 15f, rect.y, rect.width - 70f, rect.height),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleLeft);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.xMax - 58f, rect.y, 58f, rect.height),
                (Mathf.Clamp01(share01) * 100f).ToString("0.0") + "%",
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.HeaderTitleTextColor,
                null,
                TextAnchor.MiddleRight);
        }

        private void DrawTrend(Rect rect, IReadOnlyList<float> values)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false);
            ShuttleUILayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Performance_RecentTicks"));
            Rect graph = new Rect(
                rect.x + 12f,
                rect.y + 40f,
                rect.width - 24f,
                Mathf.Max(0f, rect.height - 52f));
            Widgets.DrawBoxSolid(graph, new Color(0f, 0f, 0f, 0.24f));
            int count = values != null ? values.Count : 0;
            if (count <= 0 || graph.height <= 0f)
            {
                return;
            }

            float peak = 0.1f;
            for (int i = 0; i < count; i++)
            {
                if (values[i] > peak)
                {
                    peak = values[i];
                }
            }

            float columnWidth = Mathf.Max(1f, graph.width / count);
            for (int i = 0; i < count; i++)
            {
                float ratio = Mathf.Clamp01(values[i] / peak);
                float height = Mathf.Max(1f, graph.height * ratio);
                Widgets.DrawBoxSolid(
                    new Rect(
                        graph.x + (i * columnWidth),
                        graph.yMax - height,
                        Mathf.Max(1f, columnWidth - 1f),
                        height),
                    ratio > 0.75f
                        ? ShuttleUIStyle.YellowStatusColor
                        : ShuttleUIStyle.BlueStatusColor);
            }

            TooltipHandler.TipRegion(
                graph,
                ShuttleUIText.Tr("CT_Shuttle_Performance_TrendPeak") + ": " +
                peak.ToString("0.000") + " ms");
        }

        private void DrawNote(Rect rect)
        {
            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.MutedCardColor);
            Rect content = new Rect(
                rect.x + 12f,
                rect.y + 7f,
                rect.width - 24f,
                Mathf.Max(0f, rect.height - 14f));
            float firstHeight = Mathf.Max(0f, (content.height - 5f) * 0.5f);
            this.DrawNoticeLine(
                new Rect(content.x, content.y, content.width, firstHeight),
                ShuttleUIText.Tr("CT_Shuttle_Performance_PageOnlyNotice"),
                ShuttleUIStyle.BlueStatusColor);
            this.DrawNoticeLine(
                new Rect(
                    content.x,
                    content.y + firstHeight + 5f,
                    content.width,
                    Mathf.Max(0f, content.height - firstHeight - 5f)),
                ShuttleUIText.Tr("CT_Shuttle_Performance_DisableNotice"),
                ShuttleUIStyle.YellowStatusColor);
        }

        private void DrawNoticeLine(Rect rect, string text, Color color)
        {
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 3f, 3f, Mathf.Max(0f, rect.height - 6f)),
                color);
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            bool previousWordWrap = Text.WordWrap;
            Color previousColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = true;
                GUI.color = color;
                Widgets.Label(
                    new Rect(rect.x + 9f, rect.y, rect.width - 9f, rect.height),
                    text);
            }
            finally
            {
                GUI.color = previousColor;
                Text.WordWrap = previousWordWrap;
                Text.Anchor = previousAnchor;
                Text.Font = previousFont;
            }
        }
    }
}
