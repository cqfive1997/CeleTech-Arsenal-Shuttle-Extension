using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal static class V3MainIconTileDrawer
    {
        internal static void DrawFrameless(
            Rect rect,
            Texture2D icon,
            string fallback,
            Color fallbackColor,
            bool disabled,
            float scale)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float padding = Mathf.Min(3f, Mathf.Min(rect.width, rect.height) * 0.10f);
            Rect iconRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                Mathf.Max(0f, rect.width - (padding * 2f)),
                Mathf.Max(0f, rect.height - (padding * 2f)));

            Color oldColor = GUI.color;
            GUI.color = icon != null
                ? (disabled ? ShuttleUIStyle.WithAlpha(Color.white, 0.42f) : Color.white)
                : (disabled ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.58f) : fallbackColor);
            ShuttleUILayout.DrawIconOrFallback(iconRect, icon, fallback, scale);
            GUI.color = oldColor;
        }

        internal static void DrawHudTile(
            Rect rect,
            Texture2D icon,
            string fallback,
            Color accent,
            bool disabled)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Color tint = disabled ? ShuttleUIStyle.MutedTextColor : accent;
            Widgets.DrawBoxSolid(rect, new Color(0.014f, 0.040f, 0.050f, disabled ? 0.46f : 0.72f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.WithAlpha(tint, disabled ? 0.24f : 0.48f),
                1f);
            V3MainFrameDrawer.DrawAllCornerLines(
                rect,
                ShuttleUIStyle.WithAlpha(tint, disabled ? 0.16f : 0.32f));

            Rect inner = new Rect(
                rect.x + 4f,
                rect.y + 4f,
                Mathf.Max(0f, rect.width - 8f),
                Mathf.Max(0f, rect.height - 8f));
            DrawFrameless(inner, icon, fallback, tint, disabled, 1.08f);
        }
    }
}
