using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal static class ShuttleSummaryRibbonDrawer
    {
        private const float InnerInset = 8f;
        private const float VerticalInset = 6f;
        private const float CellGap = 4f;
        private const float ProgressGap = 16f;
        private const float ProgressDividerGap = 6f;
        private static readonly Color CellBackgroundColor = new Color(0f, 0f, 0f, 0.12f);

        internal static void Draw(Rect rect, IList<ShuttleHeaderMetricSpec> metrics)
        {
            Draw(
                rect,
                new ShuttleHeaderRibbonSpec(
                    ShuttleHeaderProgressSpec.Empty,
                    metrics));
        }

        internal static void Draw(Rect rect, ShuttleHeaderRibbonSpec spec)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            ShuttleUILayout.DrawCardBackground(rect, false, false, ShuttleUIStyle.CardColor);
            if (spec.Progress.Visible)
            {
                DrawProgressRibbon(rect, spec);
                return;
            }

            DrawMetricCells(GetMetricsRect(rect), spec.Metrics);
        }

        private static void DrawProgressRibbon(Rect rect, ShuttleHeaderRibbonSpec spec)
        {
            Rect progressRect = GetProgressRect(rect, GetCount(spec.Metrics));
            DrawProgressBlock(progressRect, spec.Progress);
            Widgets.DrawBoxSolid(
                new Rect(progressRect.xMax + ProgressDividerGap, rect.y + 8f, 1f, rect.height - 16f),
                ShuttleUIStyle.SubtleBorderColor);
            Rect metricsRect = new Rect(
                progressRect.xMax + ProgressGap,
                rect.y + VerticalInset,
                Mathf.Max(0f, rect.xMax - progressRect.xMax - ProgressGap - InnerInset),
                Mathf.Max(0f, rect.height - (VerticalInset * 2f)));
            DrawMetricCells(metricsRect, spec.Metrics);
        }

        private static Rect GetProgressRect(Rect rect, int metricCount)
        {
            float minWidth = metricCount >= 6 ? 188f : 220f;
            float maxWidth = metricCount >= 6 ? 230f : 286f;
            float scale = metricCount >= 6 ? 0.19f : 0.22f;
            float width = Mathf.Clamp(rect.width * scale, minWidth, maxWidth);
            width = Mathf.Min(width, Mathf.Max(0f, rect.width - (InnerInset * 2f)));
            return new Rect(
                rect.x + InnerInset,
                rect.y + VerticalInset,
                width,
                Mathf.Max(0f, rect.height - (VerticalInset * 2f)));
        }

        private static Rect GetMetricsRect(Rect rect)
        {
            return new Rect(
                rect.x + InnerInset,
                rect.y + VerticalInset,
                Mathf.Max(0f, rect.width - (InnerInset * 2f)),
                Mathf.Max(0f, rect.height - (VerticalInset * 2f)));
        }

        private static void DrawMetricCells(Rect metricsRect, IList<ShuttleHeaderMetricSpec> metrics)
        {
            int count = GetCount(metrics);
            if (count <= 0)
            {
                return;
            }

            float cellWidth = (metricsRect.width - (CellGap * (count - 1))) / count;
            if (cellWidth <= 0f)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Rect cellRect = new Rect(
                    metricsRect.x + ((cellWidth + CellGap) * i),
                    metricsRect.y,
                    cellWidth,
                    metricsRect.height);
                DrawMetricCell(cellRect, metrics[i]);
            }
        }

        private static void DrawProgressBlock(Rect rect, ShuttleHeaderProgressSpec progress)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            string tooltip = string.IsNullOrEmpty(progress.Tooltip)
                ? progress.Label
                : progress.Tooltip;
            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Tiny;
            float labelWidth;
            try
            {
                labelWidth = Mathf.Clamp(
                    Text.CalcSize(string.IsNullOrEmpty(progress.Label) ? "-" : progress.Label).x + 4f,
                    42f,
                    Mathf.Min(76f, rect.width * 0.34f));
            }
            finally
            {
                Text.Font = oldFont;
            }

            Rect labelRect = new Rect(rect.x, rect.y + 13f, labelWidth, 20f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                progress.Label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);
            Rect progressRect = new Rect(
                labelRect.xMax + 3f,
                rect.y + 16f,
                Mathf.Max(0f, rect.xMax - labelRect.xMax - 3f),
                13f);
            ShuttleUILayout.DrawLinearMeter(
                progressRect,
                progress.Progress01,
                ResolveAccent(progress.AccentColor));
            ShuttleUITooltip.Tip(progressRect, tooltip);
        }

        internal static void DrawMetricCell(Rect rect, ShuttleHeaderMetricSpec metric)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Color accent = ResolveAccent(metric.AccentColor);
            Widgets.DrawBoxSolid(rect, CellBackgroundColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, 0.52f),
                ShuttleUIStyle.ThinBorder);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 4f, rect.yMax - 2f, Mathf.Max(0f, rect.width - 8f), 1f),
                ShuttleUIStyle.WithAlpha(accent, 0.62f));
            DrawMetricLabelValue(
                rect,
                GetDisplayLabel(metric, rect.width),
                metric.Value,
                accent,
                BuildTooltip(metric));
        }

        private static void DrawMetricLabelValue(
            Rect rect,
            string label,
            string value,
            Color valueColor,
            string tooltip)
        {
            float padding = rect.width < 90f ? 5f : 6f;
            float labelHeight = 16f;
            float valueHeight = rect.height < 36f ? 16f : 20f;
            float gap = rect.height < 42f ? 0f : 2f;
            float totalHeight = labelHeight + valueHeight + gap;
            float startY = rect.y + ((rect.height - totalHeight) * 0.5f);
            Rect labelRect = new Rect(
                rect.x + padding,
                startY,
                Mathf.Max(0f, rect.width - (padding * 2f)),
                labelHeight);
            Rect valueRect = new Rect(
                rect.x + padding,
                startY + labelHeight + gap,
                Mathf.Max(0f, rect.width - (padding * 2f)),
                valueHeight);

            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleCenter);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                valueRect,
                value,
                rect.height < 36f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                valueColor,
                tooltip,
                TextAnchor.MiddleCenter);
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        private static int GetCount(IList<ShuttleHeaderMetricSpec> metrics)
        {
            return metrics != null ? metrics.Count : 0;
        }

        private static Color ResolveAccent(Color color)
        {
            return color.a > 0f ? color : ShuttleUIStyle.MutedTextColor;
        }

        private static string GetDisplayLabel(ShuttleHeaderMetricSpec metric, float width)
        {
            string label = string.IsNullOrEmpty(metric.Label) ? "-" : metric.Label;
            string compact = string.IsNullOrEmpty(metric.CompactLabel)
                ? label
                : metric.CompactLabel;
            if (LabelFits(label, width))
            {
                return label;
            }

            return compact;
        }

        private static bool LabelFits(string label, float width)
        {
            if (string.IsNullOrEmpty(label))
            {
                return true;
            }

            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Tiny;
                return Text.CalcSize(label).x <= Mathf.Max(0f, width - 12f);
            }
            finally
            {
                Text.Font = oldFont;
            }
        }

        private static string BuildTooltip(ShuttleHeaderMetricSpec metric)
        {
            if (!string.IsNullOrEmpty(metric.Tooltip))
            {
                return metric.Tooltip;
            }

            string label = string.IsNullOrEmpty(metric.Label) ? "-" : metric.Label;
            string value = string.IsNullOrEmpty(metric.Value) ? "-" : metric.Value;
            return label + ": " + value;
        }
    }
}
