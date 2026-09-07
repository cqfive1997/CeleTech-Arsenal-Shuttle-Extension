using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPageModelBuilder
    {
        private readonly V3CrewReadModelBuilder crewReadModelBuilder =
            new V3CrewReadModelBuilder();

        internal void Fill(V3CrewPageModel model, V3CrewPageInputs inputs)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            ShuttleCargoSnapshot cargoSnapshot = inputs != null && inputs.CargoSnapshot != null
                ? inputs.CargoSnapshot
                : new ShuttleCargoSnapshot();
            model.CrewData = this.crewReadModelBuilder.Build(
                model.ControlModel,
                cargoSnapshot,
                inputs != null ? inputs.PawnPresenceSnapshot : null,
                inputs != null ? inputs.PawnDynamicStatusSnapshot : null);
        }
    }
}
