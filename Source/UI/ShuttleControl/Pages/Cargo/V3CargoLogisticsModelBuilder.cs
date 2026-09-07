using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoLogisticsModelBuilder
    {
        private readonly V3CargoModuleLookup moduleLookup =
            new V3CargoModuleLookup();

        private readonly V3CargoLogisticsBayStateBuilder bayStateBuilder =
            new V3CargoLogisticsBayStateBuilder();

        private readonly V3CargoLogisticsModuleStateBuilder moduleStateBuilder =
            new V3CargoLogisticsModuleStateBuilder();

        private readonly V3CargoLogisticsConnectionBuilder connectionBuilder =
            new V3CargoLogisticsConnectionBuilder();

        private readonly V3CargoLogisticsEndpointBuilder endpointBuilder;

        internal V3CargoLogisticsModelBuilder()
        {
            this.endpointBuilder =
                new V3CargoLogisticsEndpointBuilder(this.connectionBuilder);
        }

        internal void ApplyCargoLogistics(
            V3CargoPageReadModel pageModel,
            ShuttleControlReadModel controlModel)
        {
            if (pageModel == null)
            {
                return;
            }

            V3CargoLogisticsState logisticsState =
                this.moduleLookup.GetCargoLogisticsState(controlModel);
            V3CargoBayLogisticsState bayState =
                this.bayStateBuilder.Build(pageModel);
            V3CargoModuleLogisticsState moduleState =
                this.moduleStateBuilder.Build(controlModel, this.moduleLookup);

            this.CopyLogisticsFlags(pageModel, logisticsState);
            this.endpointBuilder.AddConnections(
                pageModel,
                bayState,
                moduleState,
                logisticsState);
        }

        private void CopyLogisticsFlags(
            V3CargoPageReadModel pageModel,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.HasCargoLogisticsModule = logisticsState.Installed;
            pageModel.CargoLogisticsModuleEnabled = logisticsState.Enabled;
            pageModel.CargoLogisticsSupportsItemTransfer =
                logisticsState.SupportsItemTransfer;
            pageModel.CargoLogisticsSupportsItemConsumption =
                logisticsState.SupportsItemConsumption;
            pageModel.CargoLogisticsSupportsItemDeposit =
                logisticsState.SupportsItemDeposit;
            pageModel.CargoLogisticsInternalBusPowered =
                logisticsState.InternalBusPowered;
            pageModel.CargoLogisticsCapabilitySummary =
                this.connectionBuilder.BuildCargoLogisticsCapabilitySummary(logisticsState);
            pageModel.CargoLogisticsStatusLabel = logisticsState.Installed
                ? logisticsState.Enabled
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsStatusEnabled")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsStatusDisabled")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsStatusMissing");
            pageModel.CargoLogisticsDescription = logisticsState.Installed
                ? ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsDescription")
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsModuleNotInstalled");
        }
    }
}
