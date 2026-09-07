using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal enum ShuttleUIActionRowKind
    {
        Normal,
        Primary,
        Danger
    }

    internal static class ShuttleUIActionButtonDrawer
    {
        internal static bool DrawNormalButton(Rect rect, string label, bool enabled, string tooltip)
        {
            return DrawButton(rect, label, enabled, ShuttleUIButtonKind.Normal, tooltip);
        }

        internal static bool DrawPrimaryButton(Rect rect, string label, bool enabled, string tooltip)
        {
            return DrawButton(rect, label, enabled, ShuttleUIButtonKind.Primary, tooltip);
        }

        internal static bool DrawDangerButton(Rect rect, string label, bool enabled, string tooltip)
        {
            return DrawButton(rect, label, enabled, ShuttleUIButtonKind.Danger, tooltip);
        }

        internal static bool DrawButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleUIButtonKind kind,
            string tooltip)
        {
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

            return DrawButtonCore(
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
                true);
        }

        internal static bool DrawAccentButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            return DrawButtonCore(
                rect,
                label,
                enabled,
                BuildAccentButtonBackground(accent),
                BuildAccentButtonBorder(accent),
                enabled ? accent : ShuttleUIStyle.MutedTextColor,
                accent,
                tooltip,
                TextAnchor.MiddleCenter,
                6f,
                0f,
                true);
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
            return DrawButtonCore(
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
                true);
        }

        internal static bool DrawActionRowButton(
            Rect rect,
            string label,
            bool enabled,
            ShuttleUIActionRowKind kind,
            string tooltip)
        {
            Color background = ShuttleUIStyle.ActionRowBgColor;
            Color border = ShuttleUIStyle.ActionRowBorderColor;
            Color accent = ShuttleUIStyle.ActionRowAccentNormalColor;
            Color text = ShuttleUIStyle.ActionRowTextColor;
            if (kind == ShuttleUIActionRowKind.Primary)
            {
                accent = ShuttleUIStyle.ActionRowAccentPrimaryColor;
                text = ShuttleUIStyle.ActionRowTextPrimaryColor;
            }
            else if (kind == ShuttleUIActionRowKind.Danger)
            {
                background = ShuttleUIStyle.ActionRowDangerBgColor;
                border = ShuttleUIStyle.ActionRowDangerBorderColor;
                accent = ShuttleUIStyle.ActionRowAccentDangerColor;
                text = ShuttleUIStyle.DangerButtonTextColor;
            }

            return DrawButtonCore(
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
                false);
        }

        internal static bool DrawAccentActionRowButton(
            Rect rect,
            string label,
            bool enabled,
            Color accent,
            string tooltip)
        {
            return DrawButtonCore(
                rect,
                label,
                enabled,
                BuildAccentActionRowBackground(accent),
                BuildAccentActionRowBorder(accent),
                enabled ? accent : ShuttleUIStyle.MutedTextColor,
                accent,
                tooltip,
                TextAnchor.MiddleLeft,
                16f,
                5f,
                false);
        }

        private static bool DrawButtonCore(
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
            bool centerLabel)
        {
            bool hovered = enabled && Mouse.IsOver(rect);
            bool pressed = IsPressed(hovered);

            if (!enabled)
            {
                background = accentStripeWidth > 0f
                    ? ShuttleUIStyle.ActionRowDisabledBgColor
                    : ShuttleUIStyle.ButtonDisabledBgColor;
                border = ShuttleUIStyle.SubtleBorderColor;
                text = ShuttleUIStyle.MutedTextColor;
                accentStripeColor = ShuttleUIStyle.MutedTextColor;
            }
            else if (pressed)
            {
                background = Darken(background, ShuttleUIStyle.ButtonPressedDarken);
            }
            else if (hovered)
            {
                background = Lighten(
                    background,
                    ShuttleUIStyle.ButtonHoverLighten,
                    ShuttleUIStyle.ButtonHoverAlpha);
            }

            Widgets.DrawBoxSolid(rect, background);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            if (enabled)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), 1f),
                    ShuttleUIStyle.ButtonInnerHighlightColor);
            }

            if (accentStripeWidth > 0f)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x, rect.y, accentStripeWidth, rect.height),
                    accentStripeColor);
            }

            Rect labelRect = centerLabel
                ? new Rect(
                    rect.x + labelLeftPadding,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.width - (labelLeftPadding * 2f)),
                    rect.height - 2f)
                : new Rect(
                    rect.x + labelLeftPadding,
                    rect.y + 1f,
                    Mathf.Max(0f, rect.width - labelLeftPadding - 8f),
                    rect.height - 2f);
            GameFont labelFont = rect.height <= 24f ? GameFont.Tiny : GameFont.Small;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                labelRect,
                label,
                labelFont,
                GameFont.Tiny,
                text,
                tooltip,
                textAnchor);

            bool clicked = Widgets.ButtonInvisible(rect);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, tooltip);
            }

            return enabled && clicked;
        }

        private static bool IsPressed(bool hovered)
        {
            Event currentEvent = Event.current;
            return hovered &&
                currentEvent != null &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0;
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
    }
}
