using System;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUITooltip
    {
        internal static void Tip(Rect rect, string text)
        {
            if (string.IsNullOrEmpty(text) || !Mouse.IsOver(rect))
            {
                return;
            }

            TooltipHandler.TipRegion(rect, text);
        }

        internal static void Tip(Rect rect, Func<string> textFactory)
        {
            if (textFactory == null || !Mouse.IsOver(rect))
            {
                return;
            }

            string text = textFactory();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TooltipHandler.TipRegion(rect, text);
        }

        internal static void DisabledReason(Rect rect, bool enabled, Func<string> reasonFactory)
        {
            if (enabled)
            {
                return;
            }

            Tip(rect, reasonFactory);
        }
    }
}
