using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingReadModelBuilder
    {
        private readonly V3ProcessingBillModelBuilder billBuilder =
            new V3ProcessingBillModelBuilder();
        private readonly V3ProcessingRecipeModelBuilder recipeBuilder =
            new V3ProcessingRecipeModelBuilder();
        private readonly V3ProcessingWorkbenchModelBuilder workbenchBuilder;
        private readonly V3ProcessingMetricModelBuilder metricBuilder =
            new V3ProcessingMetricModelBuilder();

        internal V3ProcessingReadModelBuilder()
        {
            this.workbenchBuilder = new V3ProcessingWorkbenchModelBuilder(
                this.billBuilder,
                this.recipeBuilder);
        }

        internal V3ProcessingReadModel Build(
            ShuttleAutoWorkTableReadModel autoWorkTableModel,
            ShuttleControlReadModel controlModel,
            string selectedWorkbenchId)
        {
            autoWorkTableModel = autoWorkTableModel ?? new ShuttleAutoWorkTableReadModel();
            controlModel = controlModel ?? new ShuttleControlReadModel();

            V3ProcessingReadModel model = new V3ProcessingReadModel();
            this.workbenchBuilder.BuildWorkbenches(model, autoWorkTableModel, controlModel);
            this.workbenchBuilder.SelectWorkbench(model, selectedWorkbenchId);
            this.metricBuilder.BuildMetrics(model);
            return model;
        }
    }
}
