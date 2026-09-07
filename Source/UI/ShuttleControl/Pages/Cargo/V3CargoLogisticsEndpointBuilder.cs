using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoLogisticsEndpointBuilder
    {
        private readonly V3CargoLogisticsConnectionBuilder connectionBuilder;

        internal V3CargoLogisticsEndpointBuilder(
            V3CargoLogisticsConnectionBuilder connectionBuilder)
        {
            this.connectionBuilder = connectionBuilder;
        }

        internal void AddConnections(
            V3CargoPageReadModel pageModel,
            V3CargoBayLogisticsState bayState,
            V3CargoModuleLogisticsState moduleState,
            V3CargoLogisticsState logisticsState)
        {
            if (pageModel == null || this.connectionBuilder == null)
            {
                return;
            }

            this.AddStandardCargoLink(pageModel, bayState, logisticsState);
            this.AddRefrigeratedCargoLink(pageModel, bayState, logisticsState);
            this.AddHabitatLink(pageModel, moduleState.HabitatState, logisticsState);
            this.AddPrisonLink(pageModel, moduleState.PrisonState, logisticsState);
            this.AddMedicalLink(pageModel, moduleState, logisticsState);
            this.AddProcessingLink(pageModel, moduleState, logisticsState);
        }

        private void AddStandardCargoLink(
            V3CargoPageReadModel pageModel,
            V3CargoBayLogisticsState bayState,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.LogisticsConnections.Add(this.connectionBuilder.BuildLogisticsLink(
                "standard-cargo",
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Bay"),
                "module_cargo",
                bayState.HasStandardCargo,
                bayState.HasStandardCargo,
                bayState.HasStandardCargo,
                false,
                logisticsState,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsDescription"),
                null,
                false));
        }

        private void AddRefrigeratedCargoLink(
            V3CargoPageReadModel pageModel,
            V3CargoBayLogisticsState bayState,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.LogisticsConnections.Add(this.connectionBuilder.BuildLogisticsLink(
                "refrigerated-cargo",
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Cold"),
                "refrigerated_cargo",
                bayState.HasRefrigeratedCargo,
                bayState.RefrigeratedEnabled,
                logisticsState.Installed && logisticsState.Enabled &&
                    bayState.RefrigeratedEnabled && bayState.RefrigeratedAutoTransfer,
                false,
                logisticsState,
                bayState.RefrigeratedAutoTransfer
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOn")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_AutoTransferOff"),
                bayState.RefrigeratedEnabled &&
                    !bayState.RefrigeratedAutoTransfer ? "AutoTransferOff" : null,
                true));
        }

        private void AddHabitatLink(
            V3CargoPageReadModel pageModel,
            V3CargoHabitatFoodState habitatState,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.LogisticsConnections.Add(this.connectionBuilder.BuildLogisticsLink(
                "habitat",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Status_Habitat"),
                "module_habitat_iv",
                habitatState.Installed,
                habitatState.Enabled,
                this.IsHabitatCargoFoodConnected(habitatState, logisticsState),
                false,
                logisticsState,
                this.BuildHabitatCargoFoodDescription(habitatState, logisticsState),
                this.GetHabitatCargoFoodStatusOverride(habitatState, logisticsState),
                habitatState.RequiresCargoLogistics));
        }

        private void AddMedicalLink(
            V3CargoPageReadModel pageModel,
            V3CargoModuleLogisticsState moduleState,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.LogisticsConnections.Add(this.connectionBuilder.BuildLogisticsLink(
                "medical",
                ShuttleUIText.Tr("CT_Shuttle_Crew_Status_MedicalBay"),
                "medical_bay",
                moduleState.HasMedical,
                moduleState.MedicalEnabled,
                moduleState.MedicalEnabled,
                false,
                logisticsState,
                ShuttleUIText.Tr("CT_Shuttle_Medical_StatusPanelHint"),
                null,
                false));
        }

        private void AddPrisonLink(
            V3CargoPageReadModel pageModel,
            V3CargoPrisonFoodState prisonState,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.LogisticsConnections.Add(this.connectionBuilder.BuildLogisticsLink(
                "prison-cell",
                ShuttleUIText.Tr("CT_Shuttle_Cargo_PrisonSupplyEndpoint"),
                "module_prison_cell",
                prisonState.Installed,
                prisonState.Enabled,
                this.IsPrisonFoodConnected(prisonState, logisticsState),
                false,
                logisticsState,
                this.BuildPrisonFoodDescription(prisonState),
                this.GetPrisonFoodStatusOverride(prisonState, logisticsState),
                prisonState.RequiresCargoLogistics));
        }

        private void AddProcessingLink(
            V3CargoPageReadModel pageModel,
            V3CargoModuleLogisticsState moduleState,
            V3CargoLogisticsState logisticsState)
        {
            pageModel.LogisticsConnections.Add(this.connectionBuilder.BuildLogisticsLink(
                "processing",
                ShuttleUIText.Tr("CT_Shuttle_AutoWorkTable_Workbench"),
                "workbench_module",
                moduleState.HasProcessing,
                moduleState.ProcessingEnabled,
                logisticsState.SupportsPoweredItemConsumptionAndDeposit &&
                    moduleState.ProcessingEnabled,
                false,
                logisticsState,
                moduleState.ProcessingEnabled &&
                    logisticsState.SupportsPoweredItemConsumptionAndDeposit
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsDescription")
                    : ShuttleUIText.Tr("CT_Shuttle_Logistics_CapabilityCoreNotInstalled"),
                this.GetProcessingStatusOverride(
                    moduleState.ProcessingEnabled,
                    logisticsState),
                true));
        }

        private bool IsHabitatCargoFoodConnected(
            V3CargoHabitatFoodState habitatState,
            V3CargoLogisticsState logisticsState)
        {
            if (!habitatState.Installed ||
                !habitatState.Enabled ||
                !habitatState.AllowsCargoFoodWithdrawal)
            {
                return false;
            }

            return habitatState.HasNoLogisticsRequiredCargoFoodWithdrawal ||
                (habitatState.RequiresCargoLogistics &&
                    logisticsState.SupportsPoweredItemTransfer);
        }

        private string BuildHabitatCargoFoodDescription(
            V3CargoHabitatFoodState habitatState,
            V3CargoLogisticsState logisticsState)
        {
            if (!habitatState.Installed)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Status_Habitat") + ": " +
                    ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusNotInstalled");
            }

            if (!habitatState.Enabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Crew_Status_Habitat") + ": " +
                    ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusDisabled");
            }

            if (!habitatState.AllowsCargoFoodWithdrawal)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusUnavailable");
            }

            if (habitatState.HasNoLogisticsRequiredCargoFoodWithdrawal ||
                (habitatState.RequiresCargoLogistics &&
                    logisticsState.SupportsPoweredItemTransfer))
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsDescription");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Logistics_CapabilityCoreNotInstalled");
        }

        private string GetHabitatCargoFoodStatusOverride(
            V3CargoHabitatFoodState habitatState,
            V3CargoLogisticsState logisticsState)
        {
            if (!habitatState.Installed ||
                !habitatState.Enabled ||
                !habitatState.AllowsCargoFoodWithdrawal ||
                !habitatState.RequiresCargoLogistics ||
                habitatState.HasNoLogisticsRequiredCargoFoodWithdrawal ||
                !logisticsState.Installed ||
                !logisticsState.Enabled)
            {
                return null;
            }

            if (!logisticsState.SupportsItemTransfer)
            {
                return "NeedsItemTransfer";
            }

            return !logisticsState.InternalBusPowered ? "Unpowered" : null;
        }

        private string GetProcessingStatusOverride(
            bool processingEnabled,
            V3CargoLogisticsState logisticsState)
        {
            if (!processingEnabled)
            {
                return null;
            }

            if (!logisticsState.Installed)
            {
                return "NeedsLogistics";
            }

            if (!logisticsState.Enabled)
            {
                return "LogisticsDisabled";
            }

            if (!logisticsState.SupportsItemConsumption)
            {
                return "NeedsItemConsumption";
            }

            if (!logisticsState.SupportsItemDeposit)
            {
                return "NeedsItemDeposit";
            }

            return !logisticsState.InternalBusPowered ? "Unpowered" : null;
        }

        private bool IsPrisonFoodConnected(
            V3CargoPrisonFoodState prisonState,
            V3CargoLogisticsState logisticsState)
        {
            if (!prisonState.Installed ||
                !prisonState.Enabled ||
                !prisonState.SupplyEnabled ||
                !logisticsState.InternalBusPowered)
            {
                return false;
            }

            return !prisonState.RequiresCargoLogistics ||
                logisticsState.SupportsPoweredItemConsumption;
        }

        private string BuildPrisonFoodDescription(
            V3CargoPrisonFoodState prisonState)
        {
            if (!prisonState.Installed)
            {
                return ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NotInstalled");
            }

            if (!prisonState.Enabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusDisabled");
            }

            if (!prisonState.SupplyEnabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_PrisonSupplyDisabledDescription");
            }

            return ShuttleUIText.Tr(
                "CT_Shuttle_Cargo_PrisonSupplyDescription",
                ShuttleFoodPreferabilityText.GetMaximumTierLabel(
                    prisonState.MaximumFoodPreferability),
                prisonState.RefrigeratedSupplyEnabled
                    ? ShuttleUIText.Tr("CT_Shuttle_Cargo_PrisonSupplyColdOn")
                    : ShuttleUIText.Tr("CT_Shuttle_Cargo_PrisonSupplyColdOff"));
        }

        private string GetPrisonFoodStatusOverride(
            V3CargoPrisonFoodState prisonState,
            V3CargoLogisticsState logisticsState)
        {
            if (!prisonState.Installed || !prisonState.Enabled)
            {
                return null;
            }

            if (!prisonState.SupplyEnabled)
            {
                return "SupplyDisabled";
            }

            if (prisonState.RequiresCargoLogistics)
            {
                if (!logisticsState.Installed)
                {
                    return "NeedsLogistics";
                }

                if (!logisticsState.Enabled)
                {
                    return "LogisticsDisabled";
                }

                if (!logisticsState.SupportsItemConsumption)
                {
                    return "NeedsItemConsumption";
                }
            }

            return !logisticsState.InternalBusPowered ? "Unpowered" : null;
        }
    }
}
