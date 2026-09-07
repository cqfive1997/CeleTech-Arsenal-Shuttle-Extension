using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal interface IShuttleIconService
    {
        Texture2D GetIcon(string key);
    }
}
