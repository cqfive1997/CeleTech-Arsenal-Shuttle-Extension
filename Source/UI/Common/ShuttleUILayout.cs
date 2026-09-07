using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal enum ShuttleUIButtonKind
    {
        Normal,
        Primary,
        Danger
    }

    internal struct ShuttleUIBottomMessageLayout
    {
        internal readonly float ContentHeight;
        internal readonly float MessageHeight;

        internal ShuttleUIBottomMessageLayout(float contentHeight, float messageHeight)
        {
            this.ContentHeight = contentHeight;
            this.MessageHeight = messageHeight;
        }
    }

    internal static class ShuttleUILayout
    {
        private const int MaxFitCacheEntries = 2048;

        private static readonly Color MeterTrackColor =
            new Color(0.015f, 0.025f, 0.035f, 0.82f);
        private static readonly Color MeterTrackTopColor =
            new Color(1f, 1f, 1f, 0.045f);
        private static readonly Color MeterHighlightColor =
            new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color MeterBottomShadeColor =
            new Color(0f, 0f, 0f, 0.22f);
        private static readonly System.Collections.Generic.Dictionary<string, string> FitTextCache =
            new System.Collections.Generic.Dictionary<string, string>();
        private static readonly System.Collections.Generic.Dictionary<string, FittedLabelCacheEntry> FittedLabelCache =
            new System.Collections.Generic.Dictionary<string, FittedLabelCacheEntry>();

        internal static void DrawPanelBackground(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.PanelColor);
            DrawRectBorder(rect, ShuttleUIStyle.BorderColor, ShuttleUIStyle.ThickBorder);
        }

        internal static void DrawHeaderBackground(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.HeaderColor);
            DrawRectBorder(rect, ShuttleUIStyle.BorderColor, ShuttleUIStyle.ThickBorder);
        }

        internal static void DrawCardBackground(Rect rect, bool selected, bool disabled)
        {
            DrawCardBackground(rect, selected, disabled, ShuttleUIStyle.CardColor);
        }

        internal static void DrawCardBackground(
            Rect rect,
            bool selected,
            bool disabled,
            Color normalColor)
        {
            Color color = disabled
                ? ShuttleUIStyle.DisabledColor
                : selected ? ShuttleUIStyle.SelectedColor : normalColor;
            Widgets.DrawBoxSolid(rect, color);
            DrawRectBorder(
                rect,
                selected ? ShuttleUIStyle.BlueStatusColor : ShuttleUIStyle.SubtleBorderColor,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
        }

        internal static void DrawSectionHeader(Rect rect, string title)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            SafeLabel(
                new Rect(rect.x + 10f, rect.y + 5f, Mathf.Max(0f, rect.width - 20f), 24f),
                title);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 8f, rect.y + 29f, Mathf.Max(0f, rect.width - 16f), 1f),
                ShuttleUIStyle.SubtleBorderColor);
        }

        internal static bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleUIButtonKind kind,
            string tooltip)
        {
            return DrawIconTextButton(rect, label, null, enabled, kind, tooltip);
        }

        internal static bool DrawIconTextButton(
            Rect rect,
            string label,
            Texture2D icon,
            bool enabled,
            ShuttleUIButtonKind kind,
            string tooltip)
        {
            bool hovered = enabled && Mouse.IsOver(rect);
            Event currentEvent = Event.current;
            bool pressed = hovered &&
                currentEvent != null &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0;

            Color background = ShuttleUIStyle.ButtonBgColor;
            Color border = ShuttleUIStyle.ButtonBorderColor;
            Color text = ShuttleUIStyle.ButtonTextColor;
            if (kind == ShuttleUIButtonKind.Primary)
            {
                background = ShuttleUIStyle.PrimaryButtonBgColor;
                border = ShuttleUIStyle.PrimaryButtonBorderColor;
                text = ShuttleUIStyle.PrimaryButtonTextColor;
            }
            else if (kind == ShuttleUIButtonKind.Danger)
            {
                background = ShuttleUIStyle.DangerButtonBgColor;
                border = ShuttleUIStyle.DangerButtonBorderColor;
                text = ShuttleUIStyle.DangerButtonTextColor;
            }

            if (!enabled)
            {
                background = ShuttleUIStyle.ButtonDisabledBgColor;
                border = ShuttleUIStyle.SubtleBorderColor;
                text = ShuttleUIStyle.MutedTextColor;
            }
            else if (pressed)
            {
                background = Darken(background, ShuttleUIStyle.ButtonPressedDarken);
            }
            else if (hovered)
            {
                background = kind == ShuttleUIButtonKind.Normal
                    ? ShuttleUIStyle.ButtonBgHoverColor
                    : Lighten(
                        background,
                        ShuttleUIStyle.ButtonHoverLighten,
                        ShuttleUIStyle.ButtonHoverAlpha);
            }

            Widgets.DrawBoxSolid(rect, background);
            DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            if (enabled)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), 1f),
                    ShuttleUIStyle.ButtonInnerHighlightColor);
            }

            Rect labelRect = new Rect(rect.x + 6f, rect.y + 1f, rect.width - 12f, rect.height - 2f);
            if (icon != null)
            {
                float iconSize = Mathf.Min(20f, Mathf.Max(12f, rect.height - 10f));
                Rect iconRect = new Rect(rect.x + 7f, rect.y + ((rect.height - iconSize) * 0.5f), iconSize, iconSize);
                Color oldColor = GUI.color;
                GUI.color = enabled
                    ? ShuttleUIStyle.WithAlpha(text, hovered ? 0.98f : 0.82f)
                    : ShuttleUIStyle.WithAlpha(text, 0.42f);
                Widgets.DrawTextureFitted(iconRect, icon, 1f);
                GUI.color = oldColor;
                labelRect = new Rect(
                    iconRect.xMax + 5f,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.xMax - iconRect.xMax - 11f),
                    rect.height - 2f);
            }

            DrawFittedSingleLineLabel(
                labelRect,
                label,
                rect.height <= 24f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                text,
                tooltip,
                icon != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);

            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            bool clicked = Widgets.ButtonInvisible(rect);
            return enabled && clicked;
        }

        internal static void DrawLinearMeter(Rect rect, float value01, Color fillColor)
        {
            float clamped = Mathf.Clamp01(value01);
            Widgets.DrawBoxSolid(rect, MeterTrackColor);
            if (rect.height >= 5f)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), MeterTrackTopColor);
            }

            float fillWidth = Mathf.Floor(rect.width * clamped);
            if (fillWidth >= 1f)
            {
                Rect fillRect = new Rect(rect.x, rect.y, fillWidth, rect.height);
                Widgets.DrawBoxSolid(fillRect, fillColor);
                if (fillRect.width >= 2f && fillRect.height >= 5f)
                {
                    Widgets.DrawBoxSolid(new Rect(fillRect.x, fillRect.y, fillRect.width, 1f), MeterHighlightColor);
                }

                if (fillRect.width >= 2f && fillRect.height >= 7f)
                {
                    Widgets.DrawBoxSolid(new Rect(fillRect.x, fillRect.yMax - 1f, fillRect.width, 1f), MeterBottomShadeColor);
                }
            }

            if (rect.height >= 8f)
            {
                DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            }
        }

        internal static void DrawIconOrFallback(
            Rect rect,
            Texture2D icon,
            string fallbackText,
            float scale)
        {
            if (icon != null)
            {
                Widgets.DrawTextureFitted(rect, icon, Mathf.Clamp(scale, 0.1f, 2f));
                return;
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            SafeLabel(rect, string.IsNullOrEmpty(fallbackText) ? "-" : fallbackText);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        internal static void SafeLabel(Rect rect, string text)
        {
            Widgets.Label(rect, ResolveSafeDisplayText(text));
        }

        internal static string ResolveSafeDisplayText(string text)
        {
            return !string.IsNullOrEmpty(text) ? text : "-";
        }

        internal static string FitSingleLineLabelText(string text, float width)
        {
            return FitSingleLineLabelText(text, width, "-");
        }

        internal static string FitSingleLineLabelText(
            string text,
            float width,
            string fallbackText)
        {
            return FitSingleLineLabelText(text, width, fallbackText, 12f);
        }

        internal static string FitSingleLineLabelText(
            string text,
            float width,
            string fallbackText,
            float minimumWidth)
        {
            string safeText = !string.IsNullOrEmpty(text)
                ? text
                : (fallbackText ?? "-");
            if (width <= Mathf.Max(0f, minimumWidth) ||
                string.IsNullOrEmpty(safeText))
            {
                return string.Empty;
            }

            return FitSingleLineText(safeText, width);
        }

        internal static bool DrawSafeSingleLineLabel(
            Rect rect,
            string text,
            GameFont font,
            Color color,
            TextAnchor anchor,
            string tooltip)
        {
            string safeText = ResolveSafeDisplayText(text);
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
                ShuttleUITooltip.Tip(
                    rect,
                    string.IsNullOrEmpty(tooltip) ? safeText : tooltip);
            }

            return truncated;
        }

        internal static bool DrawFittedSingleLineLabel(
            Rect rect,
            string text,
            GameFont preferredFont,
            GameFont fallbackFont,
            Color color,
            string tooltip,
            TextAnchor anchor)
        {
            string safeText = ResolveSafeDisplayText(text);
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            bool truncated = false;

            try
            {
                GUI.color = color;
                Text.Anchor = GetSingleLineAnchor(anchor);
                Text.WordWrap = false;
                FittedLabelCacheEntry entry =
                    GetFittedLabelCacheEntry(
                        safeText,
                        rect.width,
                        preferredFont,
                        fallbackFont);
                Text.Font = entry.Font;
                truncated = entry.Truncated;
                SafeLabel(
                    MakeVerticallySafeSingleLineRect(rect, Text.Font),
                    entry.Text);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }

            if ((truncated || !string.IsNullOrEmpty(tooltip)) &&
                Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, string.IsNullOrEmpty(tooltip) ? safeText : tooltip);
            }

            return truncated;
        }

        internal static ShuttleUIBottomMessageLayout CalculateBottomMessageLayout(
            float height,
            float gap)
        {
            const float TargetMessageHeight = 160f;
            const float MinContentHeight = 130f;
            const float MinMessageHeight = 132f;

            float usableHeight = Mathf.Max(0f, height - gap);
            if (usableHeight <= 0f)
            {
                return new ShuttleUIBottomMessageLayout(0f, 0f);
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
            return new ShuttleUIBottomMessageLayout(contentHeight, messageHeight);
        }

        internal static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            float line = Mathf.Max(1f, thickness);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, line), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - line, rect.width, line), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, line, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - line, rect.y, line, rect.height), color);
        }

        internal static string FitSingleLineText(string text, float width)
        {
            string safeText = ResolveSafeDisplayText(text);
            int widthKey = Mathf.Max(0, Mathf.RoundToInt(width));
            string cacheKey = ((int)Text.Font).ToString() + "|" +
                widthKey.ToString() + "|" +
                safeText;
            string cachedText;
            if (FitTextCache.TryGetValue(cacheKey, out cachedText))
            {
                return cachedText;
            }

            string fittedText;
            if (width <= 0f || Text.CalcSize(safeText).x <= width)
            {
                fittedText = safeText;
                CacheFitText(cacheKey, fittedText);
                return fittedText;
            }

            const string Ellipsis = "...";
            if (Text.CalcSize(Ellipsis).x >= width)
            {
                fittedText = Ellipsis;
                CacheFitText(cacheKey, fittedText);
                return fittedText;
            }

            for (int length = safeText.Length - 1; length > 0; length--)
            {
                string candidate = safeText.Substring(0, length) + Ellipsis;
                if (Text.CalcSize(candidate).x <= width)
                {
                    fittedText = candidate;
                    CacheFitText(cacheKey, fittedText);
                    return fittedText;
                }
            }

            fittedText = Ellipsis;
            CacheFitText(cacheKey, fittedText);
            return fittedText;
        }

        private static FittedLabelCacheEntry GetFittedLabelCacheEntry(
            string safeText,
            float width,
            GameFont preferredFont,
            GameFont fallbackFont)
        {
            int widthKey = Mathf.Max(0, Mathf.RoundToInt(width));
            string cacheKey = ((int)preferredFont).ToString() + "|" +
                ((int)fallbackFont).ToString() + "|" +
                widthKey.ToString() + "|" +
                safeText;
            FittedLabelCacheEntry cachedEntry;
            if (FittedLabelCache.TryGetValue(cacheKey, out cachedEntry))
            {
                return cachedEntry;
            }

            GameFont oldFont = Text.Font;
            try
            {
                GameFont selectedFont = preferredFont;
                string displayText = safeText;
                bool truncated = false;

                Text.Font = selectedFont;
                if (width > 0f && Text.CalcSize(displayText).x > width)
                {
                    selectedFont = fallbackFont;
                    Text.Font = selectedFont;
                    if (fallbackFont != GameFont.Tiny &&
                        Text.CalcSize(displayText).x > width)
                    {
                        selectedFont = GameFont.Tiny;
                        Text.Font = selectedFont;
                    }

                    if (Text.CalcSize(displayText).x > width)
                    {
                        displayText = FitSingleLineText(displayText, width);
                        truncated = displayText != safeText;
                    }
                }

                FittedLabelCacheEntry entry =
                    new FittedLabelCacheEntry(displayText, selectedFont, truncated);
                CacheFittedLabel(cacheKey, entry);
                return entry;
            }
            finally
            {
                Text.Font = oldFont;
            }
        }

        private static void CacheFitText(string cacheKey, string fittedText)
        {
            if (FitTextCache.Count > MaxFitCacheEntries)
            {
                FitTextCache.Clear();
            }

            FitTextCache[cacheKey] = fittedText;
        }

        private static void CacheFittedLabel(
            string cacheKey,
            FittedLabelCacheEntry entry)
        {
            if (FittedLabelCache.Count > MaxFitCacheEntries)
            {
                FittedLabelCache.Clear();
            }

            FittedLabelCache[cacheKey] = entry;
        }

        private sealed class FittedLabelCacheEntry
        {
            internal readonly string Text;
            internal readonly GameFont Font;
            internal readonly bool Truncated;

            internal FittedLabelCacheEntry(
                string text,
                GameFont font,
                bool truncated)
            {
                this.Text = text;
                this.Font = font;
                this.Truncated = truncated;
            }
        }

        internal static string FitSingleLineTextWithTooltip(
            Rect rect,
            string text,
            float width,
            string tooltip)
        {
            string safeText = ResolveSafeDisplayText(text);
            string fitted = FitSingleLineText(safeText, width);
            if (fitted != safeText || !string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(
                    rect,
                    string.IsNullOrEmpty(tooltip) ? safeText : tooltip);
            }

            return fitted;
        }

        private static Rect MakeVerticallySafeSingleLineRect(Rect rect, GameFont font)
        {
            float safeHeight = GetSafeLineHeight(font);
            float height = Mathf.Max(safeHeight, rect.height);
            float y = rect.y + ((rect.height - height) * 0.5f);
            return new Rect(rect.x, y, rect.width, height);
        }

        private static float GetSafeLineHeight(GameFont font)
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
            return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
        }
    }
}
