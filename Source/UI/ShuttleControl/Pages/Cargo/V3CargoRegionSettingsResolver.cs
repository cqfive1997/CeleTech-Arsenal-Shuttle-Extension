using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoRegionSettingsResolver
    {
        internal ShuttleCargoRegionReadModel Resolve(
            IReadOnlyList<ShuttleCargoRegionReadModel> settings,
            int regionIndex)
        {
            if (settings == null || regionIndex < 0)
            {
                return null;
            }

            for (int i = 0; i < settings.Count; i++)
            {
                ShuttleCargoRegionReadModel model = settings[i];
                if (model != null && model.RegionIndex == regionIndex)
                {
                    return model;
                }
            }

            return regionIndex < settings.Count ? settings[regionIndex] : null;
        }
    }
}
