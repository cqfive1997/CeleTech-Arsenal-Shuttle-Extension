using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoBayConfigFilterDrawer
    {
        private const float VanillaThingFilterBulkButtonStripHeight = 27f;

        internal void Draw(
            Rect rect,
            ThingFilterUI.UIState state,
            ThingFilter filter,
            bool enabled)
        {
            if (state == null ||
                filter == null ||
                rect.width <= 0f ||
                rect.height <= 0f)
            {
                return;
            }

            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && enabled;
            GUI.BeginGroup(rect);
            try
            {
                Rect shiftedRect = new Rect(
                    0f,
                    -VanillaThingFilterBulkButtonStripHeight,
                    rect.width,
                    rect.height + VanillaThingFilterBulkButtonStripHeight);
                ThingFilterUI.DoThingFilterConfigWindow(
                    shiftedRect,
                    state,
                    filter,
                    StorageSettings.EverStorableFixedSettings().filter,
                    8,
                    null,
                    null,
                    false,
                    false,
                    false,
                    null,
                    null);
            }
            finally
            {
                GUI.EndGroup();
                GUI.enabled = oldEnabled;
            }
        }
    }
}
