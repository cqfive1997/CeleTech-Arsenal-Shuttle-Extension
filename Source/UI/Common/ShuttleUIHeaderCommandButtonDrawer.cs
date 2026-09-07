using System;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal struct ShuttleUIHeaderCommandButtonSpec
    {
        internal ShuttleUIHeaderCommandButtonSpec(
            Rect rect,
            Texture2D icon,
            Action onClick,
            string tooltip,
            string disabledTooltip)
        {
            this.Rect = rect;
            this.Icon = icon;
            this.OnClick = onClick;
            this.Tooltip = tooltip;
            this.DisabledTooltip = disabledTooltip;
            this.FallbackText = null;
            this.FallbackBackgroundColor = Color.clear;
            this.DisableIcon = false;
            this.TooltipOnlyWhenMouseOver = false;
            this.Enabled = true;
        }

        internal Rect Rect;
        internal Texture2D Icon;
        internal Action OnClick;
        internal string Tooltip;
        internal string DisabledTooltip;
        internal string FallbackText;
        internal Color FallbackBackgroundColor;
        internal bool DisableIcon;
        internal bool TooltipOnlyWhenMouseOver;
        internal bool Enabled;
    }

    internal static class ShuttleUIHeaderCommandButtonDrawer
    {
        internal static bool Draw(ShuttleUIHeaderCommandButtonSpec spec)
        {
            Rect squareRect = GetSquareRect(spec.Rect);
            bool enabled = spec.Enabled && spec.OnClick != null;
            DrawIconOrFallback(squareRect, spec, enabled);

            if (Mouse.IsOver(squareRect))
            {
                Widgets.DrawHighlight(squareRect);
            }

            bool clickedWithoutAction = false;
            if (Widgets.ButtonInvisible(squareRect))
            {
                if (enabled)
                {
                    spec.OnClick();
                }
                else
                {
                    clickedWithoutAction = true;
                }
            }

            string tooltip = enabled ? spec.Tooltip : spec.DisabledTooltip;
            if (!spec.TooltipOnlyWhenMouseOver || Mouse.IsOver(squareRect))
            {
                ShuttleUITooltip.Tip(squareRect, tooltip);
            }

            return clickedWithoutAction;
        }

        private static Rect GetSquareRect(Rect buttonRect)
        {
            float size = Mathf.Min(buttonRect.width, buttonRect.height);
            return new Rect(
                buttonRect.x + ((buttonRect.width - size) * 0.5f),
                buttonRect.y + ((buttonRect.height - size) * 0.5f),
                size,
                size);
        }

        private static void DrawIconOrFallback(
            Rect iconRect,
            ShuttleUIHeaderCommandButtonSpec spec,
            bool enabled)
        {
            if (!spec.DisableIcon && spec.Icon != null)
            {
                Color oldColor = GUI.color;
                GUI.color = enabled
                    ? Color.white
                    : ShuttleUIStyle.WithAlpha(Color.white, ShuttleUIStyle.DisabledAlpha);
                Widgets.DrawTextureFitted(iconRect, spec.Icon, 1f);
                GUI.color = oldColor;
                return;
            }

            Color backgroundColor = spec.FallbackBackgroundColor.a > 0f
                ? spec.FallbackBackgroundColor
                : ShuttleUIStyle.IconFallbackBackgroundColor;
            Widgets.DrawBoxSolid(iconRect, backgroundColor);
            ShuttleUILayout.DrawRectBorder(iconRect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);

            if (string.IsNullOrEmpty(spec.FallbackText))
            {
                return;
            }

            ShuttleUILayout.DrawFittedSingleLineLabel(
                iconRect,
                spec.FallbackText,
                GameFont.Tiny,
                GameFont.Tiny,
                enabled ? ShuttleUIStyle.ButtonTextColor : ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleCenter);
        }
    }
}
