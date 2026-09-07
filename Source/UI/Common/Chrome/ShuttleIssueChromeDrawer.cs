using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal static class ShuttleIssueChromeDrawer
    {
        internal const float HeaderHeight = 34f;
        internal const float RowHeight = 36f;

        private const float BadgeTopPadding = 6f;
        private const float BadgeHeight = 18f;
        private const float SeverityBadgeWidth = 62f;
        private const float CategoryBadgeWidth = 86f;
        private const float BadgeGap = 8f;
        private const float MessageGap = 10f;
        private const float ActionButtonHeight = 20f;
        private const float ActionButtonWideWidth = 92f;
        private const float ActionButtonCompactWidth = 68f;
        private const float ActionButtonMinRowWidth = 300f;

        private static readonly Color RowBackgroundColor = ShuttleUIStyle.MutedCardColor;

        internal static void Draw(
            Rect rect,
            ShuttleIssuePanelSpec spec,
            ref Vector2 scrollPosition)
        {
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            DrawPanelTitle(rect, spec.Title);
            Rect innerRect = GetPanelInnerRect(rect, HeaderHeight);
            IList<ShuttleIssueRowSpec> rows = spec.Rows;
            int count = rows != null ? rows.Count : 0;
            if (count <= 0)
            {
                DrawEmptyMessage(innerRect, spec.EmptyMessage);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, innerRect.width - 16f),
                Mathf.Max(innerRect.height, count * RowHeight));
            Widgets.BeginScrollView(innerRect, ref scrollPosition, viewRect);
            float y = 0f;
            for (int i = 0; i < count; i++)
            {
                DrawRow(new Rect(0f, y, viewRect.width, RowHeight - 4f), rows[i]);
                y += RowHeight;
            }

            Widgets.EndScrollView();
        }

        internal static Color GetSeverityColor(ShuttleIssueSeverity severity)
        {
            if (severity == ShuttleIssueSeverity.Critical ||
                severity == ShuttleIssueSeverity.Error)
            {
                return ShuttleUIStyle.RedStatusColor;
            }

            if (severity == ShuttleIssueSeverity.Warning ||
                severity == ShuttleIssueSeverity.Pending)
            {
                return ShuttleUIStyle.YellowStatusColor;
            }

            if (severity == ShuttleIssueSeverity.Ready)
            {
                return ShuttleUIStyle.GreenStatusColor;
            }

            return ShuttleUIStyle.BlueStatusColor;
        }

        private static void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            ShuttleUILayout.DrawSectionHeader(
                new Rect(rect.x, rect.y, rect.width, 32f),
                string.IsNullOrEmpty(title) ? "-" : title);
        }

        private static Rect GetPanelInnerRect(Rect rect, float topOffset)
        {
            return new Rect(
                rect.x + 8f,
                rect.y + topOffset,
                Mathf.Max(0f, rect.width - 16f),
                Mathf.Max(0f, rect.height - topOffset - 8f));
        }

        private static void DrawEmptyMessage(Rect rect, string label)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                string.IsNullOrEmpty(label) ? "-" : label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleCenter);
        }

        private static void DrawRow(Rect rect, ShuttleIssueRowSpec row)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Color color = GetSeverityColor(row.Severity);
            string tooltip = BuildTooltip(row);
            ShuttleUILayout.DrawCardBackground(rect, false, false, RowBackgroundColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), color);

            Rect severityRect = new Rect(
                rect.x + 6f,
                rect.y + BadgeTopPadding,
                SeverityBadgeWidth,
                BadgeHeight);
            DrawBadge(severityRect, row.BadgeLabel, color, tooltip);

            float messageLeft = severityRect.xMax + BadgeGap;
            if (!string.IsNullOrEmpty(row.CategoryLabel) && rect.width >= 245f)
            {
                Rect categoryRect = new Rect(
                    messageLeft,
                    rect.y + BadgeTopPadding,
                    CategoryBadgeWidth,
                    BadgeHeight);
                DrawBadge(
                    categoryRect,
                    row.CategoryLabel,
                    ShuttleUIStyle.BlueStatusColor,
                    tooltip);
                messageLeft = categoryRect.xMax + MessageGap;
            }

            float messageRight = rect.xMax - 8f;
            if (!string.IsNullOrEmpty(row.ActionLabel) &&
                rect.width >= ActionButtonMinRowWidth)
            {
                float actionWidth = rect.width >= 420f
                    ? ActionButtonWideWidth
                    : ActionButtonCompactWidth;
                Rect actionRect = new Rect(
                    rect.xMax - actionWidth - 6f,
                    rect.y + 5f,
                    actionWidth,
                    ActionButtonHeight);
                messageRight = actionRect.x - 10f;
                DrawActionButton(actionRect, row, tooltip);
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    messageLeft,
                    rect.y + 5f,
                    Mathf.Max(0f, messageRight - messageLeft),
                    20f),
                row.Message,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                tooltip,
                TextAnchor.MiddleLeft);
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        private static void DrawActionButton(
            Rect rect,
            ShuttleIssueRowSpec row,
            string rowTooltip)
        {
            string tooltip = !string.IsNullOrEmpty(row.ActionTooltip)
                ? row.ActionTooltip
                : rowTooltip;
            ShuttleUIActionButtonDrawer.DrawNormalButton(
                rect,
                row.ActionLabel,
                row.ActionEnabled,
                tooltip);
        }

        private static void DrawBadge(Rect rect, string label, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, new Color(color.r, color.g, color.b, 0.30f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(color, 0.70f),
                ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 2f, rect.y, Mathf.Max(0f, rect.width - 4f), rect.height),
                string.IsNullOrEmpty(label) ? "-" : label,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                tooltip,
                TextAnchor.MiddleCenter);
        }

        private static string BuildTooltip(ShuttleIssueRowSpec row)
        {
            if (!string.IsNullOrEmpty(row.Tooltip))
            {
                return row.Tooltip;
            }

            return string.IsNullOrEmpty(row.Message) ? "-" : row.Message;
        }
    }
}
