using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellPageModelBuilder
    {
        private readonly V3PrisonCellReadModelBuilder prisonCellBuilder =
            new V3PrisonCellReadModelBuilder();

        internal void Fill(
            V3PrisonCellPageModel model,
            V3PrisonCellPageInputs inputs)
        {
            if (model == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel = inputs != null
                ? inputs.ControlModel
                : null;
            model.ControlModel = controlModel ?? new ShuttleControlReadModel();
            model.PrisonCellModel = this.prisonCellBuilder.Build(model.ControlModel);
        }
    }
}
