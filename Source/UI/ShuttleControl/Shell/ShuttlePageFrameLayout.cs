using UnityEngine;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal struct ShuttlePageFrameLayout
    {
        internal Rect HeaderRect;
        internal Rect BodyRect;

        internal static ShuttlePageFrameLayout HeaderBody(Rect rect)
        {
            ShuttlePageFrameLayout layout = new ShuttlePageFrameLayout();

            layout.HeaderRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                ShuttleUIStyle.HeaderHeight);

            layout.BodyRect = new Rect(
                rect.x,
                layout.HeaderRect.yMax + ShuttleUIStyle.Gap,
                rect.width,
                Mathf.Max(
                    0f,
                    rect.height - ShuttleUIStyle.HeaderHeight - ShuttleUIStyle.Gap));

            return layout;
        }
    }
}
