using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs
{
    internal enum ShuttleV3DialogButtonKind
    {
        Normal,
        Primary,
        Danger
    }

    internal enum ShuttleV3DialogActionRowKind
    {
        Normal,
        Primary,
        Danger
    }

    internal struct ShuttleV3DialogBottomMessageLayout
    {
        internal readonly float ContentHeight;
        internal readonly float MessageHeight;

        internal ShuttleV3DialogBottomMessageLayout(float contentHeight, float messageHeight)
        {
            this.ContentHeight = contentHeight;
            this.MessageHeight = messageHeight;
        }
    }

    /// <summary>
    /// Shared rectangular IMGUI primitives for V3 dialogs. These helpers intentionally avoid rounded
    /// corners or hidden layout side effects so individual panels stay predictable.
    /// </summary>
    internal static class ShuttleV3DialogLayout
    {
        internal static void DrawPanelBackground(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleV3DialogStyle.PanelColor);
            DrawRectBorder(rect, ShuttleV3DialogStyle.BorderColor, ShuttleV3DialogMetrics.ThickBorder);
        }

        internal static void DrawCardBackground(Rect rect, bool selected, bool disabled)
        {
            DrawCardBackground(rect, selected, disabled, ShuttleV3DialogStyle.CardColor);
        }

        internal static void DrawCardBackground(Rect rect, bool selected, bool disabled, Color normalColor)
        {
            Color color = disabled
                ? ShuttleV3DialogStyle.DisabledColor
                : selected ? ShuttleV3DialogStyle.SelectedColor : normalColor;
            Widgets.DrawBoxSolid(rect, color);
            DrawRectBorder(
                rect,
                selected ? ShuttleV3DialogStyle.BlueStatusColor : ShuttleV3DialogStyle.SubtleBorderColor,
                ShuttleV3DialogMetrics.ThinBorder);
        }

        internal static void DrawSectionHeader(Rect rect, string title)
        {
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 24f);
            Text.Font = GameFont.Small;
            SafeLabel(titleRect, title);
            Rect separator = new Rect(rect.x + 8f, rect.y + 29f, rect.width - 16f, 1f);
            Widgets.DrawBoxSolid(separator, ShuttleV3DialogStyle.SubtleBorderColor);
        }

        internal static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            float line = Mathf.Max(1f, thickness);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, line), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - line, rect.width, line), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, line, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - line, rect.y, line, rect.height), color);
        }

        internal static void DrawIconOrFallback(
            Rect rect,
            Texture2D icon,
            string fallbackText,
            float scale = 1f)
        {
            if (icon != null)
            {
                float clampedScale = Mathf.Clamp(scale, 0.1f, 2f);
                Widgets.DrawTextureFitted(rect, icon, clampedScale);
                return;
            }

            SafeLabel(rect, string.IsNullOrEmpty(fallbackText) ? "-" : fallbackText);
        }

        internal static ShuttleV3DialogBottomMessageLayout CalculateBottomMessageLayout(
            float totalHeight,
            float gap)
        {
            const float TargetMessageHeight = 160f;
            const float MinContentHeight = 130f;
            const float MinMessageHeight = 132f;
            float usableHeight = Mathf.Max(0f, totalHeight - gap);
            if (usableHeight <= 0f)
            {
                return new ShuttleV3DialogBottomMessageLayout(0f, 0f);
            }

            float maxMessageWithContent = usableHeight - MinContentHeight;
            float messageHeight;
            if (maxMessageWithContent >= MinMessageHeight)
            {
                messageHeight = Mathf.Min(TargetMessageHeight, maxMessageWithContent);
            }
            else
            {
                messageHeight = Mathf.Min(MinMessageHeight, usableHeight);
            }

            if (messageHeight > usableHeight)
            {
                messageHeight = usableHeight;
            }

            float contentHeight = Mathf.Max(0f, usableHeight - messageHeight);
            return new ShuttleV3DialogBottomMessageLayout(contentHeight, messageHeight);
        }

        internal static bool DrawDialogButton(
            Rect rect,
            string label,
            bool enabled = true,
            bool danger = false,
            bool primary = false,
            string tooltip = null)
        {
            ShuttleV3DialogButtonKind kind = danger
                ? ShuttleV3DialogButtonKind.Danger
                : primary ? ShuttleV3DialogButtonKind.Primary : ShuttleV3DialogButtonKind.Normal;
            return DrawDialogButton(rect, label, enabled, kind, tooltip);
        }

        internal static bool DrawDialogButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleV3DialogButtonKind kind,
            string tooltip = null)
        {
            bool clicked;
            return DrawDialogButton(rect, label, enabled, kind, tooltip, out clicked);
        }

        internal static bool DrawDialogButton(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            bool primary,
            string tooltip,
            out bool clicked)
        {
            ShuttleV3DialogButtonKind kind = danger
                ? ShuttleV3DialogButtonKind.Danger
                : primary ? ShuttleV3DialogButtonKind.Primary : ShuttleV3DialogButtonKind.Normal;
            return DrawDialogButton(rect, label, enabled, kind, tooltip, out clicked);
        }

        internal static bool DrawDialogButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleV3DialogButtonKind kind,
            string tooltip,
            out bool clicked)
        {
            bool hovered = enabled && Mouse.IsOver(rect);
            Event currentEvent = Event.current;
            bool pressed = hovered &&
                currentEvent != null &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;

            Color background = ShuttleV3DialogStyle.ButtonBgColor;
            Color border = ShuttleV3DialogStyle.ButtonBorderColor;
            Color text = ShuttleV3DialogStyle.ButtonTextColor;
            if (kind == ShuttleV3DialogButtonKind.Primary)
            {
                background = ShuttleV3DialogStyle.PrimaryButtonBgColor;
                border = ShuttleV3DialogStyle.PrimaryButtonBorderColor;
                text = ShuttleV3DialogStyle.PrimaryButtonTextColor;
            }
            else if (kind == ShuttleV3DialogButtonKind.Danger)
            {
                background = ShuttleV3DialogStyle.DangerButtonBgColor;
                border = ShuttleV3DialogStyle.DangerButtonBorderColor;
                text = ShuttleV3DialogStyle.DangerButtonTextColor;
            }

            if (!enabled)
            {
                background = ShuttleV3DialogStyle.ButtonDisabledBgColor;
                border = ShuttleV3DialogStyle.SubtleBorderColor;
                text = ShuttleV3DialogStyle.MutedTextColor;
            }
            else if (pressed)
            {
                background = Darken(background, ShuttleV3DialogMetrics.ButtonPressedDarken);
            }
            else if (hovered)
            {
                background = kind == ShuttleV3DialogButtonKind.Normal
                    ? ShuttleV3DialogStyle.ButtonBgHoverColor
                    : Lighten(
                        background,
                        ShuttleV3DialogMetrics.ButtonHoverLighten,
                        ShuttleV3DialogMetrics.ButtonHoverAlpha);
            }

            Widgets.DrawBoxSolid(rect, background);
            DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleV3DialogMetrics.ThickBorder : ShuttleV3DialogMetrics.ThinBorder);
            if (enabled)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), 1f),
                    ShuttleV3DialogStyle.ButtonInnerHighlightColor);
            }

            Rect labelRect = new Rect(rect.x + 6f, rect.y + 1f, rect.width - 12f, rect.height - 2f);
            GameFont labelFont = rect.height <= 24f ? GameFont.Tiny : GameFont.Small;
            DrawFittedSingleLineLabel(
                labelRect,
                label,
                labelFont,
                GameFont.Tiny,
                text,
                tooltip,
                TextAnchor.MiddleCenter);

            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            Text.WordWrap = oldWordWrap;

            clicked = Widgets.ButtonInvisible(rect);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, tooltip);
            }

            return enabled && clicked;
        }

        internal static bool DrawAccentButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            bool clicked;
            return DrawAccentButton(rect, label, enabled, accent, tooltip, out clicked);
        }

        internal static bool DrawAccentButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip,
            out bool clicked)
        {
            return DrawDialogButtonCore(
                rect,
                label,
                enabled,
                BuildAccentButtonBackground(accent),
                BuildAccentButtonBorder(accent),
                enabled ? accent : ShuttleV3DialogStyle.MutedTextColor,
                accent,
                tooltip,
                TextAnchor.MiddleCenter,
                6f,
                0f,
                true,
                out clicked);
        }

        internal static bool DrawAccentActionRowButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            bool clicked;
            return DrawAccentActionRowButton(rect, label, enabled, accent, tooltip, out clicked);
        }

        internal static bool DrawAccentActionRowButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip,
            out bool clicked)
        {
            return DrawDialogButtonCore(
                rect,
                label,
                enabled,
                BuildAccentActionRowBackground(accent),
                BuildAccentActionRowBorder(accent),
                enabled ? accent : ShuttleV3DialogStyle.MutedTextColor,
                accent,
                tooltip,
                TextAnchor.MiddleLeft,
                16f,
                5f,
                false,
                out clicked);
        }

        internal static bool DrawTintedButton(
            Rect rect,
            string label,
            bool enabled,
            Color background,
            Color border,
            Color text,
            string tooltip)
        {
            bool clicked;
            return DrawTintedButton(
                rect,
                label,
                enabled,
                background,
                border,
                text,
                tooltip,
                out clicked);
        }

        internal static bool DrawTintedButton(
            Rect rect,
            string label,
            bool enabled,
            Color background,
            Color border,
            Color text,
            string tooltip,
            out bool clicked)
        {
            return DrawDialogButtonCore(
                rect,
                label,
                enabled,
                background,
                border,
                text,
                Color.clear,
                tooltip,
                TextAnchor.MiddleCenter,
                6f,
                0f,
                true,
                out clicked);
        }

        internal static bool DrawActionRowButton(
            Rect rect,
            string label,
            bool enabled = true,
            ShuttleV3DialogActionRowKind kind = ShuttleV3DialogActionRowKind.Normal,
            string tooltip = null)
        {
            bool clicked;
            return DrawActionRowButton(rect, label, enabled, kind, tooltip, out clicked);
        }

        internal static bool DrawActionRowButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleV3DialogActionRowKind kind,
            string tooltip,
            out bool clicked)
        {
            Color background = ShuttleV3DialogStyle.ActionRowBgColor;
            Color border = ShuttleV3DialogStyle.ActionRowBorderColor;
            Color accent = ShuttleV3DialogStyle.ActionRowAccentNormalColor;
            Color text = ShuttleV3DialogStyle.ActionRowTextColor;
            if (kind == ShuttleV3DialogActionRowKind.Primary)
            {
                accent = ShuttleV3DialogStyle.ActionRowAccentPrimaryColor;
                text = ShuttleV3DialogStyle.ActionRowTextPrimaryColor;
            }
            else if (kind == ShuttleV3DialogActionRowKind.Danger)
            {
                background = ShuttleV3DialogStyle.ActionRowDangerBgColor;
                border = ShuttleV3DialogStyle.ActionRowDangerBorderColor;
                accent = ShuttleV3DialogStyle.ActionRowAccentDangerColor;
                text = ShuttleV3DialogStyle.DangerButtonTextColor;
            }

            if (!enabled)
            {
                background = ShuttleV3DialogStyle.ActionRowDisabledBgColor;
                border = ShuttleV3DialogStyle.SubtleBorderColor;
                accent = ShuttleV3DialogStyle.MutedTextColor;
                text = ShuttleV3DialogStyle.MutedTextColor;
            }

            return DrawDialogButtonCore(
                rect,
                label,
                enabled,
                background,
                border,
                text,
                accent,
                tooltip,
                TextAnchor.MiddleLeft,
                16f,
                5f,
                false,
                out clicked);
        }

        private static bool DrawDialogButtonCore(
            Rect rect,
            string label,
            bool enabled,
            Color background,
            Color border,
            Color text,
            Color accentStripeColor,
            string tooltip,
            TextAnchor textAnchor,
            float labelLeftPadding,
            float accentStripeWidth,
            bool centerLabel,
            out bool clicked)
        {
            bool hovered = enabled && Mouse.IsOver(rect);
            Event currentEvent = Event.current;
            bool pressed = hovered &&
                currentEvent != null &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;

            if (!enabled)
            {
                background = accentStripeWidth > 0f
                    ? ShuttleV3DialogStyle.ActionRowDisabledBgColor
                    : ShuttleV3DialogStyle.ButtonDisabledBgColor;
                border = ShuttleV3DialogStyle.SubtleBorderColor;
                text = ShuttleV3DialogStyle.MutedTextColor;
            }
            else if (pressed)
            {
                background = Darken(background, ShuttleV3DialogMetrics.ButtonPressedDarken);
            }
            else if (hovered)
            {
                background = Lighten(
                    background,
                    ShuttleV3DialogMetrics.ButtonHoverLighten,
                    ShuttleV3DialogMetrics.ButtonHoverAlpha);
            }

            Widgets.DrawBoxSolid(rect, background);
            DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleV3DialogMetrics.ThickBorder : ShuttleV3DialogMetrics.ThinBorder);
            if (enabled)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), 1f),
                    ShuttleV3DialogStyle.ButtonInnerHighlightColor);
            }

            if (accentStripeWidth > 0f)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x, rect.y, accentStripeWidth, rect.height),
                    enabled ? accentStripeColor : ShuttleV3DialogStyle.MutedTextColor);
            }

            Rect labelRect = centerLabel
                ? new Rect(rect.x + labelLeftPadding, rect.y + 1f, rect.width - (labelLeftPadding * 2f), rect.height - 2f)
                : new Rect(rect.x + labelLeftPadding, rect.y + 1f, rect.width - labelLeftPadding - 8f, rect.height - 2f);
            GameFont labelFont = rect.height <= 24f ? GameFont.Tiny : GameFont.Small;
            DrawFittedSingleLineLabel(
                labelRect,
                label,
                labelFont,
                GameFont.Tiny,
                text,
                tooltip,
                textAnchor);

            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            Text.WordWrap = oldWordWrap;

            clicked = Widgets.ButtonInvisible(rect);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, tooltip);
            }

            return enabled && clicked;
        }

        private static Color BuildAccentButtonBackground(Color accent)
        {
            return new Color(accent.r * 0.23f, accent.g * 0.23f, accent.b * 0.23f, 0.94f);
        }

        private static Color BuildAccentButtonBorder(Color accent)
        {
            return new Color(
                Mathf.Min(1f, accent.r * 0.80f + 0.12f),
                Mathf.Min(1f, accent.g * 0.80f + 0.12f),
                Mathf.Min(1f, accent.b * 0.80f + 0.12f),
                0.95f);
        }

        private static Color BuildAccentActionRowBackground(Color accent)
        {
            return new Color(accent.r * 0.20f, accent.g * 0.20f, accent.b * 0.20f, 0.94f);
        }

        private static Color BuildAccentActionRowBorder(Color accent)
        {
            return new Color(
                Mathf.Min(1f, accent.r * 0.62f + 0.10f),
                Mathf.Min(1f, accent.g * 0.62f + 0.10f),
                Mathf.Min(1f, accent.b * 0.62f + 0.10f),
                0.86f);
        }

        private static Color Lighten(Color color, float amount, float alpha)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                alpha);
        }

        private static Color Darken(Color color, float multiplier)
        {
            return new Color(
                color.r * multiplier,
                color.g * multiplier,
                color.b * multiplier,
                color.a);
        }

        internal static void SafeLabel(Rect rect, string text)
        {
            string safeText = string.IsNullOrEmpty(text) ? "-" : text;
            Widgets.Label(rect, safeText);
        }

        internal static float GetSafeLineHeight(GameFont font)
        {
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;

            try
            {
                Text.Font = font;
                Text.WordWrap = false;
                return Mathf.Ceil(Mathf.Max(Text.LineHeight, Text.CalcHeight("Agjp", 9999f))) + 2f;
            }
            finally
            {
                Text.Font = oldFont;
                Text.WordWrap = oldWordWrap;
            }
        }

        internal static Rect MakeVerticallySafeSingleLineRect(Rect rect, GameFont font)
        {
            float safeHeight = GetSafeLineHeight(font);
            float height = Mathf.Max(safeHeight, rect.height);
            float y = rect.y + ((rect.height - height) * 0.5f);
            return new Rect(rect.x, y, rect.width, height);
        }

        private static TextAnchor GetSingleLineAnchor(TextAnchor anchor)
        {
            if (anchor == TextAnchor.UpperRight ||
                anchor == TextAnchor.MiddleRight ||
                anchor == TextAnchor.LowerRight)
            {
                return TextAnchor.MiddleRight;
            }

            if (anchor == TextAnchor.UpperCenter ||
                anchor == TextAnchor.MiddleCenter ||
                anchor == TextAnchor.LowerCenter)
            {
                return TextAnchor.MiddleCenter;
            }

            return TextAnchor.MiddleLeft;
        }

        internal static bool DrawSafeSingleLineLabel(
            Rect rect,
            string text,
            GameFont font,
            Color color,
            TextAnchor anchor,
            string tooltip = null)
        {
            string safeText = string.IsNullOrEmpty(text) ? "-" : text;
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            bool truncated = false;

            try
            {
                GUI.color = color;
                Text.Font = font;
                Text.Anchor = GetSingleLineAnchor(anchor);
                Text.WordWrap = false;

                string displayText = safeText;
                if (rect.width > 0f && Text.CalcSize(displayText).x > rect.width)
                {
                    displayText = FitSingleLineText(displayText, rect.width);
                    truncated = displayText != safeText;
                }

                SafeLabel(MakeVerticallySafeSingleLineRect(rect, font), displayText);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }

            if (truncated || !string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, string.IsNullOrEmpty(tooltip) ? safeText : tooltip);
            }

            return truncated;
        }

        internal static bool DrawFittedSingleLineLabel(
            Rect rect,
            string text,
            GameFont preferredFont,
            GameFont fallbackFont,
            Color color,
            string tooltip = null,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            string safeText = string.IsNullOrEmpty(text) ? "-" : text;
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            bool truncated = false;
            GameFont finalFont = preferredFont;

            try
            {
                GUI.color = color;
                Text.Anchor = GetSingleLineAnchor(anchor);
                Text.WordWrap = false;
                Text.Font = finalFont;

                string displayText = safeText;
                if (rect.width > 0f && Text.CalcSize(displayText).x > rect.width)
                {
                    finalFont = fallbackFont;
                    Text.Font = finalFont;
                    if (fallbackFont != GameFont.Tiny && Text.CalcSize(displayText).x > rect.width)
                    {
                        finalFont = GameFont.Tiny;
                        Text.Font = finalFont;
                    }

                    if (Text.CalcSize(displayText).x > rect.width)
                    {
                        displayText = FitSingleLineText(displayText, rect.width);
                        truncated = displayText != safeText;
                    }
                }

                SafeLabel(MakeVerticallySafeSingleLineRect(rect, finalFont), displayText);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }

            if (truncated || !string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, string.IsNullOrEmpty(tooltip) ? safeText : tooltip);
            }

            return truncated;
        }

        internal static string FitSingleLineTextWithTooltip(
            Rect rect,
            string text,
            float width,
            string tooltip = null)
        {
            string safeText = string.IsNullOrEmpty(text) ? "-" : text;
            string fitted = FitSingleLineText(safeText, width);
            if (fitted != safeText || !string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, string.IsNullOrEmpty(tooltip) ? safeText : tooltip);
            }

            return fitted;
        }

        internal static void DrawSummaryRibbonBackground(Rect rect)
        {
            DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.CardColor);
        }

        internal static void DrawSummaryMetricCell(
            Rect rect,
            string label,
            string value,
            Color valueColor,
            string tooltip = null)
        {
            string safeLabel = string.IsNullOrEmpty(label) ? "-" : label;
            string safeValue = string.IsNullOrEmpty(value) ? "-" : value;
            string fullTooltip = string.IsNullOrEmpty(tooltip)
                ? safeLabel + ": " + safeValue
                : tooltip;

            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.12f));
            DrawMetricLabelValue(rect, safeLabel, safeValue, valueColor, fullTooltip);
            ShuttleUITooltip.Tip(rect, fullTooltip);
        }

        internal static void DrawMetricLabelValue(
            Rect rect,
            string label,
            string value,
            Color valueColor,
            string tooltip = null)
        {
            string safeLabel = string.IsNullOrEmpty(label) ? "-" : label;
            string safeValue = string.IsNullOrEmpty(value) ? "-" : value;
            string fullTooltip = tooltip;
            if (string.IsNullOrEmpty(fullTooltip))
            {
                fullTooltip = safeLabel + ": " + safeValue;
            }

            float padding = rect.width < 90f ? 5f : 6f;
            GameFont valueFont = GameFont.Small;
            float labelHeight = GetSafeLineHeight(GameFont.Tiny);
            float valueHeight = GetSafeLineHeight(valueFont);
            float gap = rect.height < 46f ? 0f : 2f;
            float totalHeight = labelHeight + valueHeight + gap;
            if (totalHeight > rect.height)
            {
                valueFont = GameFont.Tiny;
                valueHeight = GetSafeLineHeight(valueFont);
                totalHeight = labelHeight + valueHeight;
                gap = totalHeight <= rect.height ? 0f : -2f;
                totalHeight = labelHeight + valueHeight + gap;
            }

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

            DrawFittedSingleLineLabel(
                labelRect,
                safeLabel,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleV3DialogStyle.MutedTextColor,
                fullTooltip,
                TextAnchor.MiddleCenter);
            DrawFittedSingleLineLabel(
                valueRect,
                safeValue,
                valueFont,
                GameFont.Tiny,
                valueColor,
                fullTooltip,
                TextAnchor.MiddleCenter);
        }

        internal static string FitSingleLineText(string text, float width)
        {
            string safeText = string.IsNullOrEmpty(text) ? "-" : text;
            if (width <= 0f || Text.CalcSize(safeText).x <= width)
            {
                return safeText;
            }

            const string Ellipsis = "...";
            float ellipsisWidth = Text.CalcSize(Ellipsis).x;
            if (ellipsisWidth >= width)
            {
                return Ellipsis;
            }

            for (int length = safeText.Length - 1; length > 0; length--)
            {
                string candidate = safeText.Substring(0, length) + Ellipsis;
                if (Text.CalcSize(candidate).x <= width)
                {
                    return candidate;
                }
            }

            return Ellipsis;
        }
    }
}
