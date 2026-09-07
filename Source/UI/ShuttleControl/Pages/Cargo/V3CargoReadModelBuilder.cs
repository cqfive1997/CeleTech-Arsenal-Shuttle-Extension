using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoReadModelBuilder
    {
        private readonly V3CargoItemStatsBuilder statsBuilder =
            new V3CargoItemStatsBuilder();

        private readonly V3CargoStackModelBuilder stackBuilder =
            new V3CargoStackModelBuilder();

        private readonly V3CargoCapacitySummaryBuilder capacityBuilder =
            new V3CargoCapacitySummaryBuilder();

        private readonly V3CargoCategorySummaryBuilder categorySummaryBuilder =
            new V3CargoCategorySummaryBuilder();

        private readonly V3CargoLogisticsModelBuilder logisticsBuilder =
            new V3CargoLogisticsModelBuilder();

        private readonly V3CargoBayModelBuilder bayBuilder;

        internal V3CargoReadModelBuilder()
        {
            this.bayBuilder = new V3CargoBayModelBuilder(
                this.stackBuilder,
                this.statsBuilder,
                this.capacityBuilder);
        }

        internal V3CargoPageReadModel Build(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings)
        {
            this.statsBuilder.Reset();

            V3CargoPageReadModel pageModel = new V3CargoPageReadModel();
            controlModel = controlModel ?? new ShuttleControlReadModel();
            cargoSnapshot = cargoSnapshot ?? new ShuttleCargoSnapshot();

            this.AddNormalBays(pageModel, controlModel, cargoSnapshot, cargoRegionSettings);
            this.bayBuilder.AddUnassignedCargoBay(pageModel, controlModel, cargoSnapshot);
            this.bayBuilder.AddRefrigeratedBays(pageModel, cargoSnapshot);
            this.capacityBuilder.ApplyRefrigeratedCapacitySummary(
                pageModel,
                controlModel,
                cargoSnapshot);
            this.statsBuilder.BuildStatLines(pageModel);
            this.categorySummaryBuilder.BuildCategorySummaries(pageModel, this.statsBuilder);
            this.logisticsBuilder.ApplyCargoLogistics(pageModel, controlModel);
            return pageModel;
        }

        private void AddNormalBays(
            V3CargoPageReadModel pageModel,
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings)
        {
            int normalRegionCount = this.capacityBuilder.GetNormalCargoRegionCount(
                controlModel,
                cargoSnapshot);
            bool hasOrdinaryCargo =
                this.capacityBuilder.HasOrdinaryLoadedCargo(cargoSnapshot);
            if (normalRegionCount > 0)
            {
                for (int i = 0; i < normalRegionCount; i++)
                {
                    pageModel.Bays.Add(this.bayBuilder.BuildNormalBay(
                        controlModel,
                        cargoSnapshot,
                        cargoRegionSettings,
                        i,
                        normalRegionCount,
                        false));
                }

                return;
            }

            if (cargoSnapshot.HasTransporter || hasOrdinaryCargo)
            {
                pageModel.Bays.Add(this.bayBuilder.BuildNormalBay(
                    controlModel,
                    cargoSnapshot,
                    cargoRegionSettings,
                    0,
                    1,
                    true));
            }
        }
    }
}
