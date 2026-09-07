using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal static class ShuttlePaintPreviewDrawer
    {
        internal static void Draw(Rect rect, Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        internal static void Draw(Rect rect, Texture2D texture, ShuttlePaintSchemeSnapshot paintScheme)
        {
            Draw(rect, texture);
        }
    }
}
