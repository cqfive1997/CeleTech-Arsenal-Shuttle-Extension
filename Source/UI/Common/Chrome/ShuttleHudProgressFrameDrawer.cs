using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal static class ShuttleHudProgressFrameDrawer
    {
        private static readonly Color TrackColor =
            new Color(0.10f, 0.22f, 0.28f, 0.42f);

        internal static void Draw(Rect rect, float progress01, Color accentColor)
        {
            if (!ShouldDrawMeterVisualsNow() || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float t = Mathf.Clamp01(progress01);
            Rect frameRect = rect.ContractedBy(4f);
            float thickness = Mathf.Lerp(2.0f, 4.5f, t);
            const float Cut = 12f;

            DrawChamferTrack(frameRect, 1.5f, Cut, TrackColor);
            DrawChamferProgress(
                frameRect,
                t,
                thickness + 4f,
                Cut,
                WithAlpha(accentColor, 0.05f + (0.07f * t)));
            DrawChamferProgress(
                frameRect,
                t,
                thickness + 2f,
                Cut,
                WithAlpha(accentColor, 0.10f + (0.12f * t)));
            DrawChamferProgress(
                frameRect,
                t,
                thickness,
                Cut,
                WithAlpha(accentColor, 0.72f + (0.20f * t)));
        }

        private static bool ShouldDrawMeterVisualsNow()
        {
            Event current = Event.current;
            return current == null || current.type == EventType.Repaint;
        }

        private static void DrawChamferTrack(
            Rect rect,
            float thickness,
            float cut,
            Color color)
        {
            DrawChamferProgress(rect, 1f, thickness, cut, color);
        }

        private static void DrawChamferProgress(
            Rect rect,
            float value01,
            float thickness,
            float cut,
            Color color)
        {
            float t = Mathf.Clamp01(value01);
            if (t <= 0f)
            {
                return;
            }

            float line = Mathf.Max(1f, thickness);
            float safeCut = Mathf.Clamp(cut, 0f, Mathf.Min(rect.width, rect.height) * 0.45f);
            float horizontalLength = Mathf.Max(0f, rect.width - (safeCut * 2f));
            float verticalLength = Mathf.Max(0f, rect.height - (safeCut * 2f));
            float chamferLength = safeCut * 1.41421356f;
            float perimeter = (horizontalLength * 2f) +
                (verticalLength * 2f) +
                (chamferLength * 4f);
            if (perimeter <= 0f)
            {
                return;
            }

            float remaining = perimeter * t;

            remaining = DrawChamferPathSegment(
                new Vector2(rect.x + safeCut, rect.y),
                new Vector2(rect.xMax - safeCut, rect.y),
                horizontalLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            remaining = DrawChamferPathSegment(
                new Vector2(rect.xMax - safeCut, rect.y),
                new Vector2(rect.xMax, rect.y + safeCut),
                chamferLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            remaining = DrawChamferPathSegment(
                new Vector2(rect.xMax, rect.y + safeCut),
                new Vector2(rect.xMax, rect.yMax - safeCut),
                verticalLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            remaining = DrawChamferPathSegment(
                new Vector2(rect.xMax, rect.yMax - safeCut),
                new Vector2(rect.xMax - safeCut, rect.yMax),
                chamferLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            remaining = DrawChamferPathSegment(
                new Vector2(rect.xMax - safeCut, rect.yMax),
                new Vector2(rect.x + safeCut, rect.yMax),
                horizontalLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            remaining = DrawChamferPathSegment(
                new Vector2(rect.x + safeCut, rect.yMax),
                new Vector2(rect.x, rect.yMax - safeCut),
                chamferLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            remaining = DrawChamferPathSegment(
                new Vector2(rect.x, rect.yMax - safeCut),
                new Vector2(rect.x, rect.y + safeCut),
                verticalLength,
                remaining,
                line,
                color);

            if (remaining <= 0f)
            {
                return;
            }

            DrawChamferPathSegment(
                new Vector2(rect.x, rect.y + safeCut),
                new Vector2(rect.x + safeCut, rect.y),
                chamferLength,
                remaining,
                line,
                color);
        }

        private static float DrawChamferPathSegment(
            Vector2 start,
            Vector2 end,
            float segmentLength,
            float remaining,
            float thickness,
            Color color)
        {
            if (segmentLength <= 0f || remaining <= 0f)
            {
                return remaining;
            }

            float drawLength = Mathf.Min(remaining, segmentLength);
            float progress = Mathf.Clamp01(drawLength / segmentLength);
            Vector2 current = Vector2.Lerp(start, end, progress);

            if (Mathf.Approximately(start.y, end.y))
            {
                DrawHorizontalChamferSegment(start, current, thickness, color);
            }
            else if (Mathf.Approximately(start.x, end.x))
            {
                DrawVerticalChamferSegment(start, current, thickness, color);
            }
            else
            {
                DrawDiagonalChamferSegment(start, current, thickness, color);
            }

            return remaining - drawLength;
        }

        private static void DrawHorizontalChamferSegment(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            float width = Mathf.Abs(end.x - start.x);
            if (width <= 0f)
            {
                return;
            }

            float x = Mathf.Min(start.x, end.x);
            float y = start.x <= end.x ? start.y : start.y - thickness;
            Widgets.DrawBoxSolid(new Rect(x, y, width, thickness), color);
        }

        private static void DrawVerticalChamferSegment(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            float height = Mathf.Abs(end.y - start.y);
            if (height <= 0f)
            {
                return;
            }

            float x = start.y <= end.y ? start.x - thickness : start.x;
            float y = Mathf.Min(start.y, end.y);
            Widgets.DrawBoxSolid(new Rect(x, y, thickness, height), color);
        }

        private static void DrawDiagonalChamferSegment(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            float distance = Vector2.Distance(start, end);
            if (distance <= 0f)
            {
                return;
            }

            float step = Mathf.Max(1.5f, thickness * 0.72f);
            int count = Mathf.Max(2, Mathf.CeilToInt(distance / step));
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            for (int i = 0; i <= count; i++)
            {
                float p = i / (float)count;
                Vector2 point = Vector2.Lerp(start, end, p);
                Rect blockRect = GetInsideAlignedChamferBlock(point, dx, dy, thickness);
                Widgets.DrawBoxSolid(blockRect, color);
            }
        }

        private static Rect GetInsideAlignedChamferBlock(
            Vector2 point,
            float dx,
            float dy,
            float thickness)
        {
            bool rightward = dx >= 0f;
            bool downward = dy >= 0f;
            if (rightward && downward)
            {
                return new Rect(point.x - thickness, point.y, thickness, thickness);
            }

            if (!rightward && downward)
            {
                return new Rect(point.x - thickness, point.y - thickness, thickness, thickness);
            }

            if (!rightward && !downward)
            {
                return new Rect(point.x, point.y - thickness, thickness, thickness);
            }

            return new Rect(point.x, point.y, thickness, thickness);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }
    }
}
