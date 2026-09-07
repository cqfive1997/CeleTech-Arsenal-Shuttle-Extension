using UnityEngine;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal struct ShuttlePageBottomMessageLayout
    {
        internal float ContentHeight;
        internal float MessageHeight;

        internal static ShuttlePageBottomMessageLayout FromBodyHeight(
            float bodyHeight,
            float gap)
        {
            ShuttleUIBottomMessageLayout commonLayout =
                ShuttleUILayout.CalculateBottomMessageLayout(bodyHeight, gap);

            ShuttlePageBottomMessageLayout layout = new ShuttlePageBottomMessageLayout();
            layout.ContentHeight = commonLayout.ContentHeight;
            layout.MessageHeight = commonLayout.MessageHeight;
            return layout;
        }

        internal Rect ContentRect(
            Rect rect,
            float width)
        {
            return new Rect(
                rect.x,
                rect.y,
                width,
                this.ContentHeight);
        }

        internal Rect MessageRect(
            Rect rect,
            float contentBottomY,
            float width,
            float gap)
        {
            return new Rect(
                rect.x,
                contentBottomY + gap,
                width,
                this.MessageHeight);
        }
    }
}
