using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal static class ShuttleHeaderStatusCardDrawer
    {
        private static readonly Color CompactStatusCardBackgroundColor =
            new Color(0.060f, 0.078f, 0.105f, 0.94f);
        private static readonly Color CompactStatusTitleColor =
            new Color(0.86f, 0.92f, 0.96f, 0.92f);

        internal static void Draw(Rect rect, ShuttleHeaderStatusSpec spec)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Color accent = ResolveAccent(spec.SeverityColor);
            string tooltip = BuildTooltip(spec);
            Widgets.DrawBoxSolid(rect, CompactStatusCardBackgroundColor);
            DrawMeterChrome(rect, spec.Progress01, accent);
            DrawText(rect, spec, accent, tooltip);
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        private static void DrawMeterChrome(Rect rect, float progress01, Color accent)
        {
            ShuttleHeaderMeterStyle style = CeleTechShuttleMod.EffectiveSettings.HeaderMeterStyle;
            if (style == ShuttleHeaderMeterStyle.NumericOnly)
            {
                ShuttleUILayout.DrawRectBorder(
                    rect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                return;
            }

            if (style == ShuttleHeaderMeterStyle.Linear)
            {
                ShuttleUILayout.DrawRectBorder(
                    rect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                ShuttleUILayout.DrawLinearMeter(
                    new Rect(rect.x + 8f, rect.yMax - 13f, Mathf.Max(0f, rect.width - 16f), 6f),
                    progress01,
                    accent);
                return;
            }

            ShuttleHudProgressFrameDrawer.Draw(rect, progress01, accent);
        }

        private static void DrawText(
            Rect rect,
            ShuttleHeaderStatusSpec spec,
            Color valueColor,
            string tooltip)
        {
            float innerWidth = Mathf.Max(0f, rect.width - 16f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 9f, innerWidth, 18f),
                spec.Label,
                GameFont.Tiny,
                GameFont.Tiny,
                CompactStatusTitleColor,
                tooltip,
                TextAnchor.MiddleCenter);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 32f, innerWidth, 24f),
                spec.Value,
                GameFont.Small,
                GameFont.Tiny,
                valueColor,
                tooltip,
                TextAnchor.MiddleCenter);
        }

        private static Color ResolveAccent(Color color)
        {
            return color.a > 0f ? color : ShuttleUIStyle.MutedTextColor;
        }

        private static string BuildTooltip(ShuttleHeaderStatusSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.Tooltip))
            {
                return spec.Tooltip;
            }

            string label = string.IsNullOrEmpty(spec.Label) ? "-" : spec.Label;
            string value = string.IsNullOrEmpty(spec.Value) ? "-" : spec.Value;
            return label + ": " + value;
        }
    }
}
