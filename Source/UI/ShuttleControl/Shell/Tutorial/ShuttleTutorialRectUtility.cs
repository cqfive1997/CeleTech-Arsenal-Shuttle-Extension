using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialRectUtility
    {
        internal static Rect Intersect(Rect rect, Rect bounds)
        {
            float xMin = Mathf.Max(rect.xMin, bounds.xMin);
            float yMin = Mathf.Max(rect.yMin, bounds.yMin);
            float xMax = Mathf.Min(rect.xMax, bounds.xMax);
            float yMax = Mathf.Min(rect.yMax, bounds.yMax);
            if (xMax <= xMin || yMax <= yMin)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        internal static bool IsValid(Rect rect)
        {
            return rect.width > 1f && rect.height > 1f;
        }
    }
}
