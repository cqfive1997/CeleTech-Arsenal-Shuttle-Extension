using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons
{
    internal sealed class ShuttleControlIconService : IShuttleIconService
    {
        private readonly ShuttleControlIconRegistry iconRegistry;

        internal ShuttleControlIconService()
            : this(new ShuttleControlIconRegistry())
        {
        }

        internal ShuttleControlIconService(ShuttleControlIconRegistry iconRegistry)
        {
            this.iconRegistry = iconRegistry;
        }

        public Texture2D GetIcon(string key)
        {
            return this.iconRegistry != null
                ? this.iconRegistry.GetIcon(key)
                : null;
        }
    }
}
