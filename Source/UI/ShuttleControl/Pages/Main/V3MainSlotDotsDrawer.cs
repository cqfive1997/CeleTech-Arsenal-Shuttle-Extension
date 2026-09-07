using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal static class V3MainSlotDotsDrawer
    {
        internal static void Draw(Rect rect, List<ShuttleControlModuleSlotModel> slots)
        {
            if (slots == null || slots.Count == 0 || rect.width <= 0f)
            {
                return;
            }

            float dotSize = 7f;
            float gap = 3f;
            float plusWidth = 28f;
            int rawMaxDots = Mathf.FloorToInt((rect.width + gap) / (dotSize + gap));
            if (rawMaxDots < 1)
            {
                return;
            }

            bool overflow = slots.Count > rawMaxDots;
            int visibleDots;
            if (overflow)
            {
                float availableDotWidth = Mathf.Max(0f, rect.width - plusWidth);
                visibleDots = Mathf.FloorToInt((availableDotWidth + gap) / (dotSize + gap));
                visibleDots = Mathf.Clamp(visibleDots, 1, slots.Count);
            }
            else
            {
                visibleDots = slots.Count;
            }

            float x = rect.x;
            for (int i = 0; i < visibleDots; i++)
            {
                ShuttleControlModuleSlotModel slot = slots[i];
                bool filled = slot != null && !string.IsNullOrEmpty(slot.InstalledModuleInstanceID);
                Rect dotRect = new Rect(x, rect.y, dotSize, dotSize);
                Widgets.DrawBoxSolid(
                    dotRect,
                    filled ? ShuttleUIStyle.GreenStatusColor : new Color(0.24f, 0.27f, 0.30f, 1f));
                x += dotSize + gap;
            }

            if (overflow)
            {
                Color oldColor = GUI.color;
                GameFont oldFont = Text.Font;
                GUI.color = new Color(0.78f, 0.82f, 0.86f, 1f);
                Text.Font = GameFont.Tiny;
                ShuttleUILayout.SafeLabel(
                    new Rect(x, rect.y - 5f, Mathf.Max(plusWidth, rect.xMax - x), rect.height + 10f),
                    "+" + (slots.Count - visibleDots).ToString());
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }
    }
}
