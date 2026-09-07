using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal sealed class ShuttleCargoRegionReadModelsBuilder
    {
        internal IReadOnlyList<ShuttleCargoRegionReadModel> Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            IShuttleCargoReadPort cargoReadPort)
        {
            List<ShuttleCargoRegionReadModel> regions =
                new List<ShuttleCargoRegionReadModel>();
            int regionCount = this.GetRegionCount(controlModel, cargoSnapshot);
            if (regionCount <= 0 || cargoReadPort == null)
            {
                return regions;
            }

            for (int i = 0; i < regionCount; i++)
            {
                ShuttleCargoRegionReadModel settings =
                    this.GetRegionSettingsSafe(cargoReadPort, i, regionCount);
                if (settings != null)
                {
                    regions.Add(settings);
                }
            }

            return regions;
        }

        private int GetRegionCount(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot != null && cargoSnapshot.CargoRegionCount > 0)
            {
                return cargoSnapshot.CargoRegionCount;
            }

            if (controlModel != null && controlModel.CargoRegionCount > 0u)
            {
                return (int)controlModel.CargoRegionCount;
            }

            return 0;
        }

        private ShuttleCargoRegionReadModel GetRegionSettingsSafe(
            IShuttleCargoReadPort cargoReadPort,
            int regionIndex,
            int regionCount)
        {
            try
            {
                return cargoReadPort.GetCargoRegionSettings(regionIndex, regionCount);
            }
            catch
            {
                return null;
            }
        }
    }
}
