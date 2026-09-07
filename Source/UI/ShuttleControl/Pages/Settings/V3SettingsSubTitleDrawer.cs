using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal static class V3SettingsSubTitleDrawer
    {
        internal static void Draw(Rect rect, string title)
        {
            if (rect.height <= 0f)
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y + 4f, rect.width, 20f),
                title);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
