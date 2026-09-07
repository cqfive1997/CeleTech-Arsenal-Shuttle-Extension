using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUIStatusBadgeDrawer
    {
        internal static void DrawBadge(Rect rect, string label, Color accent, string tooltip)
        {
            DrawBadge(rect, label, accent, true, false, tooltip);
        }

        internal static void DrawBadge(
            Rect rect,
            string label,
            Color accent,
            bool enabled,
            bool strong,
            string tooltip)
        {
            Color fill = enabled
                ? ShuttleUIStyle.WithAlpha(
                    accent,
                    strong
                        ? ShuttleUIStyle.StrongStatusBadgeFillAlpha
                        : ShuttleUIStyle.StatusBadgeFillAlpha)
                : ShuttleUIStyle.ButtonDisabledBgColor;
            Color border = enabled ? accent : ShuttleUIStyle.SubtleBorderColor;
            Color text = enabled ? accent : ShuttleUIStyle.MutedTextColor;

            Widgets.DrawBoxSolid(rect, fill);
            ShuttleUILayout.DrawRectBorder(rect, border, ShuttleUIStyle.ThinBorder);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(4f),
                label,
                rect.height <= 22f ? GameFont.Tiny : GameFont.Small,
                GameFont.Tiny,
                text,
                tooltip,
                TextAnchor.MiddleCenter);

            if (!string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, tooltip);
            }
        }

        internal static void DrawSquareIndicator(Rect rect, Color accent, string tooltip)
        {
            DrawSquareIndicator(rect, accent, true, tooltip);
        }

        internal static void DrawSquareIndicator(Rect rect, Color accent, bool enabled, string tooltip)
        {
            Color fill = enabled
                ? ShuttleUIStyle.WithAlpha(accent, ShuttleUIStyle.StatusBadgeFillAlpha)
                : ShuttleUIStyle.ButtonDisabledBgColor;
            Color border = enabled ? accent : ShuttleUIStyle.SubtleBorderColor;

            Widgets.DrawBoxSolid(rect, fill);
            ShuttleUILayout.DrawRectBorder(rect, border, ShuttleUIStyle.ThinBorder);
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShuttleUITooltip.Tip(rect, tooltip);
            }
        }
    }
}
