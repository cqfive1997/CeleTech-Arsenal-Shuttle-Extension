using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;

        internal V3PrisonCellPageInputs(ShuttleControlReadModel controlModel)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
        }
    }
}
