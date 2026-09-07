using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoPageModel
    {
        internal ShuttleControlReadModel ControlModel;
        internal ShuttleCargoSnapshot CargoSnapshot;
        internal V3CargoPageReadModel CargoPageModel;
        internal int CargoSnapshotRevision;
    }
}
