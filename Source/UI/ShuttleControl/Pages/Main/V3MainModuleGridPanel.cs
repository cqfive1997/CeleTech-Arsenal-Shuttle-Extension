using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainModuleGridPanel
    {
        private readonly V3MainText text;

        internal V3MainModuleGridPanel(V3MainText text)
        {
            this.text = text;
        }

        internal void Draw(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context)
        {
            V3MainFunctionModuleDecorationDrawer.Draw(rect, this.text);
        }
    }
}
