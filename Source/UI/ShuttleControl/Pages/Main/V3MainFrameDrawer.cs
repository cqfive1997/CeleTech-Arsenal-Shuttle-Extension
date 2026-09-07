using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal static class V3MainFrameDrawer
    {
        internal static void DrawSegmentCardFrame(Rect rect, bool selected, bool hovered, bool menuOpen)
        {
            Color background = selected
                ? new Color(0.070f, 0.145f, 0.190f, 0.96f)
                : (hovered || menuOpen
                    ? new Color(0.060f, 0.115f, 0.150f, 0.95f)
                    : new Color(0.042f, 0.070f, 0.095f, 0.93f));
            Color border = selected
                ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.98f)
                : (menuOpen
                    ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.86f)
                    : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, hovered ? 0.62f : 0.34f));

            Widgets.DrawBoxSolid(rect, background);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f), 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, hovered || selected || menuOpen ? 0.22f : 0.10f));
            ShuttleUILayout.DrawRectBorder(rect, border, selected || menuOpen ? 2f : 1f);
            DrawTopCornerLines(rect, border, 14f, 2f);
            if (selected || menuOpen)
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 2f, rect.yMax - 3f, Mathf.Max(0f, rect.width - 4f), 2f),
                    ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, selected ? 0.42f : 0.28f));
            }
        }

        internal static void DrawModuleRowFrame(
            Rect rect,
            bool installed,
            bool enabled,
            bool selected,
            bool activeOperation)
        {
            bool hovered = Mouse.IsOver(rect);
            Color background = installed
                ? new Color(0.052f, 0.095f, 0.116f, hovered ? 0.96f : 0.88f)
                : new Color(0.042f, 0.052f, 0.068f, hovered ? 0.92f : 0.82f);
            if (activeOperation)
            {
                background = new Color(0.095f, 0.080f, 0.052f, hovered ? 0.96f : 0.88f);
            }

            Widgets.DrawBoxSolid(rect, background);

            Color accent = installed
                ? (enabled ? ShuttleUIStyle.BlueStatusColor : ShuttleUIStyle.YellowStatusColor)
                : ShuttleUIStyle.MutedTextColor;
            if (activeOperation)
            {
                accent = ShuttleUIStyle.YellowStatusColor;
            }

            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 4f, 2f, Mathf.Max(0f, rect.height - 8f)),
                ShuttleUIStyle.WithAlpha(accent, installed || activeOperation ? 0.72f : 0.34f));

            Color border = selected
                ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.78f)
                : (installed
                    ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, hovered ? 0.52f : 0.34f)
                    : ShuttleUIStyle.WithAlpha(ShuttleUIStyle.SubtleBorderColor, hovered ? 0.76f : 0.48f));
            ShuttleUILayout.DrawRectBorder(rect, border, selected ? 2f : 1f);
        }

        internal static void DrawSegmentNodeFrame(
            Rect rect,
            bool selected,
            bool hovered,
            bool installed,
            bool optional)
        {
            Color background = installed
                ? new Color(0.048f, 0.080f, 0.108f, selected ? 0.95f : 0.88f)
                : (optional
                    ? new Color(0.038f, 0.060f, 0.080f, hovered ? 0.88f : 0.80f)
                    : new Color(0.030f, 0.046f, 0.062f, hovered ? 0.86f : 0.76f));
            Color border = installed
                ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, selected ? 0.86f : (hovered ? 0.62f : 0.38f))
                : ShuttleUIStyle.WithAlpha(optional ? ShuttleUIStyle.BlueStatusColor : ShuttleUIStyle.MutedTextColor, selected ? 0.70f : (hovered ? 0.44f : 0.24f));

            Widgets.DrawBoxSolid(rect, background);
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f), 1f),
                ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, hovered || selected ? 0.20f : 0.09f));
            ShuttleUILayout.DrawRectBorder(rect, border, selected ? 2f : 1f);
            DrawTopCornerLines(rect, border, 9f, 1f);
        }

        internal static void DrawMetricFrame(Rect rect, Color accent)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.060f, 0.078f, 0.105f, 0.94f));
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 3f, 4f, Mathf.Max(0f, rect.height - 6f)),
                ShuttleUIStyle.WithAlpha(accent, 0.70f));
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f), 1f),
                ShuttleUIStyle.WithAlpha(accent, 0.12f));
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.WithAlpha(accent, 0.34f), 1f);
            DrawTopCornerLines(rect, ShuttleUIStyle.WithAlpha(accent, 0.46f), 12f, 1f);
        }

        internal static void DrawSummaryFrame(Rect rect, Color accent, bool selected, bool disabled)
        {
            Color background = disabled
                ? ShuttleUIStyle.DisabledColor
                : (selected
                    ? new Color(0.070f, 0.145f, 0.190f, 0.96f)
                    : new Color(0.060f, 0.078f, 0.105f, 0.91f));
            Widgets.DrawBoxSolid(rect, background);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y + 3f, 4f, Mathf.Max(0f, rect.height - 6f)),
                ShuttleUIStyle.WithAlpha(disabled ? ShuttleUIStyle.MutedTextColor : accent, selected ? 0.92f : 0.70f));
            Widgets.DrawBoxSolid(
                new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f), 1f),
                ShuttleUIStyle.WithAlpha(accent, selected ? 0.20f : 0.09f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                selected
                    ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.78f)
                    : ShuttleUIStyle.WithAlpha(accent, 0.34f),
                selected ? 2f : 1f);
            DrawTopCornerLines(rect, ShuttleUIStyle.WithAlpha(accent, selected ? 0.72f : 0.46f), 11f, 1f);
        }

        internal static void DrawAllCornerLines(Rect rect, Color color)
        {
            float length = 7f;
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, length, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, length), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - length, rect.y, length, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, length), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, length, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - length, 1f, length), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - length, rect.yMax - 1f, length, 1f), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.yMax - length, 1f, length), color);
        }

        internal static void DrawFocusBorder(Rect rect)
        {
            Color color = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.YellowStatusColor, 0.96f);
            ShuttleUILayout.DrawRectBorder(rect, color, 2f);
            DrawAllCornerLines(rect, color);
        }

        private static void DrawTopCornerLines(Rect rect, Color color, float length, float thickness)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, length, thickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, thickness, length), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - length, rect.y, length, thickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - thickness, rect.y, thickness, length), color);
        }
    }
}
