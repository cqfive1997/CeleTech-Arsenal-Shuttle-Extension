using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingPageModelBuilder
    {
        private readonly V3ProcessingReadModelBuilder processingBuilder =
            new V3ProcessingReadModelBuilder();

        internal void Fill(
            V3ProcessingPageModel model,
            V3ProcessingPageInputs inputs,
            string selectedWorkbenchId)
        {
            if (model == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                inputs != null ? inputs.ControlModel : null;
            ShuttleAutoWorkTableReadModel workTableModel =
                inputs != null && inputs.WorkTableModel != null
                ? inputs.WorkTableModel
                : new ShuttleAutoWorkTableReadModel();
            model.ControlModel = controlModel ?? new ShuttleControlReadModel();
            model.ProcessingModel = this.processingBuilder.Build(
                workTableModel,
                model.ControlModel,
                selectedWorkbenchId);
        }
    }
}
