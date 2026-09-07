using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoPageModelBuilder
    {
        private readonly V3CargoReadModelBuilder cargoBuilder =
            new V3CargoReadModelBuilder();

        internal void Fill(V3CargoPageModel model, V3CargoPageInputs inputs)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            model.CargoSnapshot = inputs != null && inputs.CargoSnapshot != null
                ? inputs.CargoSnapshot
                : new ShuttleCargoSnapshot();
            model.CargoSnapshotRevision = inputs != null
                ? inputs.CargoSnapshotRevision
                : 0;
            model.CargoPageModel = this.cargoBuilder.Build(
                model.ControlModel,
                model.CargoSnapshot,
                inputs != null ? inputs.CargoRegionSettings : null);
        }
    }
}
