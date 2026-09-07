using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoBayConfigDialogChrome
    {
        private const float SectionHeaderOffset = 38f;

        private static readonly Color NoticeCardColor =
            new Color(0.145f, 0.122f, 0.070f, 0.94f);

        internal static void DrawWindowBackground(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
        }

        internal static void DrawTitle(Rect rect, string title, string subtitle)
        {
            string safeTitle = TextOrUnavailable(title);
            Widgets.DrawBoxSolid(rect, ShuttleV3DialogStyle.HeaderColor);
            ShuttleV3DialogLayout.DrawRectBorder(
                rect,
                ShuttleV3DialogStyle.BorderColor,
                ShuttleV3DialogMetrics.ThickBorder);

            Rect labelRect = new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, 26f);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                labelRect,
                safeTitle,
                GameFont.Medium,
                GameFont.Small,
                ShuttleV3DialogStyle.HeaderTitleTextColor,
                safeTitle);
            TooltipHandler.TipRegion(
                rect,
                string.IsNullOrEmpty(subtitle) ? safeTitle : safeTitle + "\n" + subtitle);
        }

        internal static void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(
                new Rect(rect.x, rect.y, rect.width, 32f),
                title);
        }

        internal static Rect GetPanelInnerRect(Rect rect)
        {
            return new Rect(
                rect.x + 8f,
                rect.y + SectionHeaderOffset,
                rect.width - 16f,
                rect.height - SectionHeaderOffset - 8f);
        }

        internal static void DrawNotice(Rect rect, string text)
        {
            string safeText = TextOrUnavailable(text);
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, NoticeCardColor);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y, 4f, rect.height),
                ShuttleV3DialogStyle.YellowStatusColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 7f, rect.width - 18f, rect.height - 12f),
                FitMultiLineText(safeText, rect.width - 18f, rect.height - 12f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(rect, safeText);
        }

        internal static void DrawUnavailableEditor(Rect rect, string text)
        {
            string safeText = TextOrUnavailable(text);
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, true);
            Text.Font = GameFont.Small;
            GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 26f),
                ShuttleUIText.Tr("CT_Shuttle_UI_Unavailable"));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 8f, rect.y + 40f, rect.width - 16f, rect.height - 48f),
                FitMultiLineText(safeText, rect.width - 16f, rect.height - 48f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(rect, safeText);
        }

        internal static void DrawInfoRow(ref float y, Rect inner, string label, string value)
        {
            Rect rowRect = new Rect(inner.x, y, inner.width, 23f);
            DrawInfoRowAbsolute(rowRect, label, value, ShuttleV3DialogStyle.MutedTextColor);
            y += 25f;
        }

        internal static void DrawInfoLine(
            ref float y,
            Rect viewRect,
            string label,
            string value,
            Color valueColor)
        {
            Rect rowRect = new Rect(0f, y, viewRect.width, 24f);
            DrawInfoRowAbsolute(rowRect, label, value, valueColor);
            y += 26f;
        }

        internal static void DrawWrappedInfoBlock(Rect rect, string label, string value)
        {
            string safeLabel = TextOrUnavailable(label);
            string safeValue = TextOrUnavailable(value);
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false);
            Rect labelRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 16f);
            Rect valueRect = new Rect(rect.x + 8f, rect.y + 24f, rect.width - 16f, rect.height - 28f);
            DrawTinyCaption(labelRect, safeLabel);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(
                valueRect,
                FitMultiLineText(safeValue, valueRect.width, valueRect.height));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(rect, safeLabel + ": " + safeValue);
        }

        internal static void DrawTinyCaption(Rect rect, string label)
        {
            string safeLabel = TextOrUnavailable(label);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                rect,
                safeLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor,
                safeLabel);
        }

        internal static bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleV3DialogButtonKind kind,
            string tooltip,
            out bool clicked)
        {
            return ShuttleV3DialogLayout.DrawDialogButton(
                rect,
                label,
                enabled,
                kind,
                tooltip,
                out clicked);
        }

        internal static bool DrawFeedbackButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleV3DialogButtonKind kind,
            string tooltip)
        {
            bool clicked;
            bool activated = DrawButton(
                rect,
                label,
                enabled,
                kind,
                tooltip,
                out clicked);
            if (clicked && !enabled)
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip)
                        ? ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable")
                        : tooltip,
                    false);
            }

            return activated;
        }

        internal static string TextOrUnavailable(string value)
        {
            return string.IsNullOrEmpty(value)
                ? ShuttleUIText.Tr("CT_Shuttle_UI_Unavailable")
                : value;
        }

        internal static string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(
                text,
                width,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown"),
                8f);
        }

        private static void DrawInfoRowAbsolute(
            Rect rect,
            string label,
            string value,
            Color valueColor)
        {
            string safeLabel = TextOrUnavailable(label);
            string safeValue = TextOrUnavailable(value);
            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.14f));
            float labelWidth = Mathf.Min(118f, Mathf.Max(48f, rect.width * 0.38f));
            Rect labelRect = new Rect(rect.x + 6f, rect.y + 4f, labelWidth, 16f);
            Rect valueRect = new Rect(
                labelRect.xMax + 8f,
                rect.y + 4f,
                Mathf.Max(0f, rect.xMax - labelRect.xMax - 14f),
                16f);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                labelRect,
                safeLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor,
                safeLabel);
            ShuttleV3DialogLayout.DrawFittedSingleLineLabel(
                valueRect,
                safeValue,
                GameFont.Tiny,
                GameFont.Tiny,
                valueColor,
                safeLabel + ": " + safeValue);
            TooltipHandler.TipRegion(rect, safeLabel + ": " + safeValue);
        }

        private static string FitMultiLineText(string text, float width, float height)
        {
            string safeText = TextOrUnavailable(text);
            if (Text.CalcHeight(safeText, width) <= height)
            {
                return safeText;
            }

            return ShuttleUILayout.FitSingleLineLabelText(
                safeText,
                width,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown"),
                0f);
        }
    }
}
