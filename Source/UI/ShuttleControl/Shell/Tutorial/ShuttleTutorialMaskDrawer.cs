using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialMaskDrawer
    {
        internal static void DrawFullMask(Rect windowRect)
        {
            DrawSolidIfVisible(windowRect, new Color(0f, 0f, 0f, 0.62f));
        }

        internal static void DrawMask(Rect windowRect, Rect targetRect)
        {
            Color mask = new Color(0f, 0f, 0f, 0.62f);
            DrawSolidIfVisible(
                new Rect(
                    windowRect.x,
                    windowRect.y,
                    windowRect.width,
                    Mathf.Max(0f, targetRect.y - windowRect.y)),
                mask);
            DrawSolidIfVisible(
                new Rect(
                    windowRect.x,
                    targetRect.yMax,
                    windowRect.width,
                    Mathf.Max(0f, windowRect.yMax - targetRect.yMax)),
                mask);
            DrawSolidIfVisible(
                new Rect(
                    windowRect.x,
                    targetRect.y,
                    Mathf.Max(0f, targetRect.x - windowRect.x),
                    targetRect.height),
                mask);
            DrawSolidIfVisible(
                new Rect(
                    targetRect.xMax,
                    targetRect.y,
                    Mathf.Max(0f, windowRect.xMax - targetRect.xMax),
                    targetRect.height),
                mask);
        }

        internal static void DrawHighlight(Rect targetRect)
        {
            float pulse = (Mathf.Sin(Time.realtimeSinceStartup * 5f) + 1f) * 0.5f;
            Color border = Color.Lerp(
                new Color(0.25f, 0.72f, 1f, 0.9f),
                new Color(0.85f, 0.96f, 1f, 1f),
                pulse);
            Widgets.DrawBoxSolid(targetRect, new Color(0.2f, 0.65f, 1f, 0.08f));
            ShuttleUILayout.DrawRectBorder(targetRect, border, 3f);
        }

        private static void DrawSolidIfVisible(Rect rect, Color color)
        {
            if (rect.width > 0f && rect.height > 0f)
            {
                Widgets.DrawBoxSolid(rect, color);
            }
        }
    }
}
