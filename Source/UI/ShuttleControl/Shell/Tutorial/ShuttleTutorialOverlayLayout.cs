using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialOverlayLayout
    {
        internal const float BoxWidth = 330f;
        private const float BoxGap = 14f;
        private const float WindowMargin = 12f;

        internal static Rect GetPaddedTarget(
            Rect targetRect,
            Rect windowRect,
            float padding)
        {
            float safePadding = padding < 0f ? 0f : padding;
            Rect rect = new Rect(
                targetRect.x - safePadding,
                targetRect.y - safePadding,
                targetRect.width + (safePadding * 2f),
                targetRect.height + (safePadding * 2f));
            return ShuttleTutorialRectUtility.Intersect(rect, windowRect);
        }

        internal static Rect CalculateIntroBoxRect(
            Rect windowRect,
            ShuttleTutorialPlacement placement,
            float boxHeight)
        {
            ShuttleTutorialPlacement safePlacement =
                placement == ShuttleTutorialPlacement.Auto
                    ? ShuttleTutorialPlacement.Center
                    : placement;
            Rect rect = GetPlacedBoxRect(windowRect, safePlacement, boxHeight);
            return ClampToWindow(rect, windowRect);
        }

        internal static Rect CalculateBoxRect(
            Rect windowRect,
            Rect targetRect,
            ShuttleTutorialPlacement placement,
            float boxHeight)
        {
            Rect rect = placement == ShuttleTutorialPlacement.Auto
                ? GetAutoBoxRect(windowRect, targetRect, boxHeight)
                : GetPlacedBoxRect(targetRect, placement, boxHeight);
            return ClampToWindow(rect, windowRect);
        }

        private static Rect GetAutoBoxRect(
            Rect windowRect,
            Rect targetRect,
            float boxHeight)
        {
            float rightSpace = windowRect.xMax - targetRect.xMax;
            float leftSpace = targetRect.x - windowRect.x;
            float belowSpace = windowRect.yMax - targetRect.yMax;
            if (rightSpace >= BoxWidth + BoxGap || rightSpace >= leftSpace)
            {
                return GetPlacedBoxRect(
                    targetRect,
                    ShuttleTutorialPlacement.Right,
                    boxHeight);
            }

            if (leftSpace >= BoxWidth + BoxGap)
            {
                return GetPlacedBoxRect(
                    targetRect,
                    ShuttleTutorialPlacement.Left,
                    boxHeight);
            }

            return belowSpace >= boxHeight + BoxGap
                ? GetPlacedBoxRect(targetRect, ShuttleTutorialPlacement.Below, boxHeight)
                : GetPlacedBoxRect(targetRect, ShuttleTutorialPlacement.Center, boxHeight);
        }

        private static Rect GetPlacedBoxRect(
            Rect targetRect,
            ShuttleTutorialPlacement placement,
            float boxHeight)
        {
            if (placement == ShuttleTutorialPlacement.Above)
            {
                return new Rect(
                    targetRect.x,
                    targetRect.y - BoxGap - boxHeight,
                    BoxWidth,
                    boxHeight);
            }

            if (placement == ShuttleTutorialPlacement.Below)
            {
                return new Rect(
                    targetRect.x,
                    targetRect.yMax + BoxGap,
                    BoxWidth,
                    boxHeight);
            }

            if (placement == ShuttleTutorialPlacement.Left)
            {
                return new Rect(
                    targetRect.x - BoxGap - BoxWidth,
                    targetRect.y,
                    BoxWidth,
                    boxHeight);
            }

            if (placement == ShuttleTutorialPlacement.Center)
            {
                return new Rect(
                    targetRect.center.x - (BoxWidth * 0.5f),
                    targetRect.center.y - (boxHeight * 0.5f),
                    BoxWidth,
                    boxHeight);
            }

            return new Rect(
                targetRect.xMax + BoxGap,
                targetRect.y,
                BoxWidth,
                boxHeight);
        }

        private static Rect ClampToWindow(Rect rect, Rect windowRect)
        {
            float x = ClampCoordinate(
                rect.x,
                windowRect.x + WindowMargin,
                windowRect.xMax - WindowMargin - rect.width);
            float y = ClampCoordinate(
                rect.y,
                windowRect.y + WindowMargin,
                windowRect.yMax - WindowMargin - rect.height);
            return new Rect(x, y, rect.width, rect.height);
        }

        private static float ClampCoordinate(float value, float min, float max)
        {
            return max < min ? min : Mathf.Clamp(value, min, max);
        }
    }
}
