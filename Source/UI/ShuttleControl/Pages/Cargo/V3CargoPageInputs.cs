using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleCargoSnapshot CargoSnapshot;
        internal readonly IReadOnlyList<ShuttleCargoRegionReadModel> CargoRegionSettings;
        internal readonly int CargoSnapshotRevision;

        internal V3CargoPageInputs(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings,
            int cargoSnapshotRevision)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.CargoSnapshot = cargoSnapshot ?? new ShuttleCargoSnapshot();
            this.CargoRegionSettings = cargoRegionSettings;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
        }
    }
}
