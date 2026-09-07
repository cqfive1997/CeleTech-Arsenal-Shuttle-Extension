using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3SharedMessageRowDrawer
    {
        internal const float RowHeight = 36f;
        private const float RowGap = 4f;
        private const float ActionButtonWideWidth = 92f;
        private const float ActionButtonCompactWidth = 68f;
        private const float ActionButtonMinRowWidth = 300f;
        private const float ScrollbarAllowance = 18f;

        internal void DrawRows(
            Rect rect,
            IList<V3SharedMessageRow> rows,
            ref Vector2 scroll)
        {
            this.DrawRows(rect, rows, ref scroll, null);
        }

        internal void DrawRows(
            Rect rect,
            IList<V3SharedMessageRow> rows,
            ref Vector2 scroll,
            Action<V3SharedMessageRow> onAction)
        {
            int count = rows != null ? rows.Count : 0;
            float rowWidth = CalculateRowWidth(rect.width);
            Rect viewRect = new Rect(
                0f,
                0f,
                rowWidth,
                Mathf.Max(rect.height, Mathf.Max(1, count) * (RowHeight + RowGap)));

            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            try
            {
                float y = 0f;
                for (int i = 0; i < count; i++)
                {
                    this.DrawRow(
                        new Rect(0f, y, rowWidth, RowHeight),
                        rows[i],
                        onAction);
                    y += RowHeight + RowGap;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        internal void DrawRow(Rect rect, V3SharedMessageRow row)
        {
            this.DrawRow(rect, row, null);
        }

        internal void DrawRow(
            Rect rect,
            V3SharedMessageRow row,
            Action<V3SharedMessageRow> onAction)
        {
            row = row ?? new V3SharedMessageRow();
            Color color = GetSeverityColor(row.Severity, row.IsNormal);
            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.MutedCardColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), color);

            Rect severityRect = new Rect(rect.x + 10f, rect.y + 7f, 58f, 20f);
            DrawBadge(
                severityRect,
                ResolveSeverityLabel(row.Severity, row.IsNormal),
                color);

            Rect categoryRect = new Rect(severityRect.xMax + 8f, rect.y + 7f, 82f, 20f);
            DrawBadge(
                categoryRect,
                ShuttleControlDisplayNameResolver.ResolveCategoryLabel(row.Category),
                ShuttleUIStyle.BlueStatusColor);

            float textRight = rect.xMax - 18f;
            bool hasActionArea = rect.width >= ActionButtonMinRowWidth &&
                !string.IsNullOrEmpty(row.ActionLabel);
            if (hasActionArea)
            {
                float actionWidth = rect.width >= 420f
                    ? ActionButtonWideWidth
                    : ActionButtonCompactWidth;
                Rect actionRect = new Rect(
                    rect.xMax - actionWidth - 6f,
                    rect.y + 6f,
                    actionWidth,
                    22f);
                textRight = actionRect.x - 10f;
                if (!string.IsNullOrEmpty(row.ActionLabel) &&
                    DrawActionButton(actionRect, row) &&
                    onAction != null)
                {
                    onAction(row);
                }
            }

            Rect textRect = new Rect(
                categoryRect.xMax + 10f,
                rect.y + 7f,
                Mathf.Max(0f, textRight - categoryRect.xMax - 10f),
                20f);
            DrawSingleLineText(
                textRect,
                string.IsNullOrEmpty(row.Message) ? "-" : row.Message,
                Color.white);

            if (!string.IsNullOrEmpty(row.Tooltip))
            {
                ShuttleUITooltip.Tip(rect, row.Tooltip);
            }
        }

        private static float CalculateRowWidth(
            float availableWidth)
        {
            return Mathf.Max(0f, availableWidth - ScrollbarAllowance);
        }

        private static bool DrawActionButton(Rect rect, V3SharedMessageRow row)
        {
            if (row == null)
            {
                return false;
            }

            return ShuttleUIActionButtonDrawer.DrawNormalButton(
                rect,
                row.ActionLabel,
                row.ActionEnabled,
                string.IsNullOrEmpty(row.ActionTooltip) ? row.Tooltip : row.ActionTooltip);
        }

        internal static Color GetSeverityColor(string severity, bool isNormal)
        {
            if (isNormal || SeverityEquals(severity, "OK") ||
                SeverityEquals(severity, "Resolved") ||
                SeverityEquals(severity, "Stable") ||
                SeverityEquals(severity, "Normal"))
            {
                return ShuttleUIStyle.GreenStatusColor;
            }

            if (SeverityEquals(severity, "Blocking") ||
                SeverityEquals(severity, "Error") ||
                SeverityEquals(severity, "Critical"))
            {
                return ShuttleUIStyle.RedStatusColor;
            }

            if (SeverityEquals(severity, "Warning") ||
                SeverityEquals(severity, "Moderate"))
            {
                return ShuttleUIStyle.YellowStatusColor;
            }

            return ShuttleUIStyle.BlueStatusColor;
        }

        internal static bool IsWarningOrWorse(string severity)
        {
            return SeverityEquals(severity, "Blocking") ||
                SeverityEquals(severity, "Error") ||
                SeverityEquals(severity, "Critical") ||
                SeverityEquals(severity, "Warning") ||
                SeverityEquals(severity, "Moderate");
        }

        private static void DrawBadge(Rect rect, string label, Color color)
        {
            Widgets.DrawBoxSolid(
                rect,
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    ShuttleUIStyle.StrongStatusBadgeFillAlpha));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(color, 0.70f),
                ShuttleUIStyle.ThinBorder);
            DrawSingleLineText(
                new Rect(rect.x + 3f, rect.y, Mathf.Max(0f, rect.width - 6f), rect.height),
                label,
                color,
                TextAnchor.MiddleCenter);
        }

        private static void DrawSingleLineText(
            Rect rect,
            string text,
            Color color)
        {
            DrawSingleLineText(rect, text, color, TextAnchor.MiddleLeft);
        }

        private static void DrawSingleLineText(
            Rect rect,
            string text,
            Color color,
            TextAnchor anchor)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            GUI.color = color;
            Text.Font = GameFont.Tiny;
            Text.Anchor = anchor;
            Text.WordWrap = false;
            ShuttleUILayout.SafeLabel(
                rect,
                ShuttleUILayout.FitSingleLineLabelText(text, rect.width));
            Text.WordWrap = oldWordWrap;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        private static string ResolveSeverityLabel(string severity, bool isNormal)
        {
            if (isNormal || SeverityEquals(severity, "OK"))
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Severity_OK");
            }

            return ShuttleControlDisplayNameResolver.ResolveSeverityLabel(severity);
        }

        private static bool SeverityEquals(string value, string expected)
        {
            return !string.IsNullOrEmpty(value) &&
                string.Equals(value, expected, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
