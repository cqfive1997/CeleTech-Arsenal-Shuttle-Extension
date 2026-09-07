using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleAutoWorkTableReadModel WorkTableModel;

        internal V3ProcessingPageInputs(
            ShuttleControlReadModel controlModel,
            ShuttleAutoWorkTableReadModel workTableModel)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.WorkTableModel = workTableModel ?? new ShuttleAutoWorkTableReadModel();
        }
    }
}
