using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3SharedMessageSummaryDrawer
    {
        internal void Draw(
            Rect rect,
            IList<V3SharedMessageRow> currentRows,
            IList<V3SharedMessageRow> launchRows,
            IList<V3SharedMessageRow> devRows)
        {
            int errors = this.CountSeverity(currentRows, true, false);
            int warnings = this.CountSeverity(currentRows, false, true);
            int infos = this.CountInfos(currentRows);
            string text = this.GetSummaryText(errors, warnings, infos);
            Color color = errors > 0
                ? ShuttleUIStyle.RedStatusColor
                : V3SharedMessageRowDrawer.GetSeverityColor(
                    warnings > 0 ? "Warning" : "OK",
                    errors == 0 && warnings == 0 && infos == 0);

            Widgets.DrawBoxSolid(rect, new Color(color.r, color.g, color.b, 0.13f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                new Color(color.r, color.g, color.b, 0.50f),
                ShuttleUIStyle.ThinBorder);
            Widgets.DrawBoxSolid(
                new Rect(
                    rect.x + 1f,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.width - 2f),
                    1f),
                ShuttleUIStyle.ButtonInnerHighlightColor);
            DrawTitle(rect, text, color);
            ShuttleUITooltip.Tip(rect, this.BuildTooltip(text, launchRows, devRows));
        }

        internal static void DrawTitle(Rect rect, string text)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(rect, string.IsNullOrEmpty(text) ? "-" : text);
            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }

        internal static void DrawTitle(Rect rect, string text, Color color)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 6f, rect.y + 1f, Mathf.Max(0f, rect.width - 12f), rect.height - 2f),
                string.IsNullOrEmpty(text) ? "-" : text,
                GameFont.Tiny,
                GameFont.Tiny,
                color,
                text,
                TextAnchor.MiddleCenter);
        }

        private string GetSummaryText(int errors, int warnings, int infos)
        {
            if (errors > 0)
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_Error") +
                    " " + errors.ToString();
            }

            if (warnings > 0)
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_Warning") +
                    " " + warnings.ToString();
            }

            if (infos > 0)
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_Info") +
                    " " + infos.ToString();
            }

            return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_Ready");
        }

        private string BuildTooltip(
            string primaryText,
            IList<V3SharedMessageRow> launchRows,
            IList<V3SharedMessageRow> devRows)
        {
            string tooltip = primaryText;
            int launchCount = this.CountActionableRows(launchRows);
            if (launchCount > 0)
            {
                tooltip += "\n" + ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_Launch") +
                    " " + launchCount.ToString();
            }

            if (Prefs.DevMode && DebugSettings.godMode && devRows != null && devRows.Count > 0)
            {
                tooltip += "\n" + ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_Dev") +
                    " " + devRows.Count.ToString();
            }

            return tooltip;
        }

        private int CountSeverity(
            IList<V3SharedMessageRow> rows,
            bool countErrors,
            bool countWarnings)
        {
            if (rows == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                V3SharedMessageRow row = rows[i];
                if (row == null || row.IsNormal)
                {
                    continue;
                }

                bool warning = V3SharedMessageRowDrawer.IsWarningOrWorse(row.Severity);
                bool error = IsErrorSeverity(row.Severity);
                if ((countErrors && error) || (countWarnings && warning && !error))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountInfos(IList<V3SharedMessageRow> rows)
        {
            if (rows == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                V3SharedMessageRow row = rows[i];
                if (row != null && !row.IsNormal &&
                    !V3SharedMessageRowDrawer.IsWarningOrWorse(row.Severity))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountActionableRows(IList<V3SharedMessageRow> rows)
        {
            if (rows == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null && !rows[i].IsNormal)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsErrorSeverity(string severity)
        {
            return !string.IsNullOrEmpty(severity) &&
                (string.Equals(severity, "Blocking", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(severity, "Error", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(severity, "Critical", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
