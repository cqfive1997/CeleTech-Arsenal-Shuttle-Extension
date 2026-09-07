using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoLogisticsConnectionBuilder
    {
        internal V3CargoLogisticsConnectionModel BuildLogisticsLink(
            string key,
            string label,
            string iconKey,
            bool installed,
            bool enabled,
            bool connected,
            bool placeholderWhenReady,
            V3CargoLogisticsState logisticsState,
            string description,
            string statusOverrideKey,
            bool requiresLogistics)
        {
            string statusKey = this.GetLogisticsLinkStatusKey(
                installed,
                enabled,
                connected,
                placeholderWhenReady,
                logisticsState,
                statusOverrideKey,
                requiresLogistics);

            V3CargoLogisticsConnectionModel model =
                new V3CargoLogisticsConnectionModel();
            model.Key = key;
            model.Label = label;
            model.IconKey = iconKey;
            model.StatusKey = statusKey;
            model.StatusLabel = this.GetLogisticsLinkStatusLabel(statusKey);
            model.Description = this.GetTextOrUnavailable(description);
            model.Tooltip = this.BuildTooltip(model, logisticsState);
            model.ResolvedIconKey = this.ResolveIconKey(model);
            model.Installed = installed;
            model.Enabled = enabled;
            model.Connected = connected;
            model.IsPlaceholder = statusKey == "Pending";
            return model;
        }

        internal string BuildCargoLogisticsCapabilitySummary(
            V3CargoLogisticsState logisticsState)
        {
            if (!logisticsState.Installed)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_CapabilityCoreNotInstalled");
            }

            if (!logisticsState.Enabled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_CapabilityCoreDisabled");
            }

            return ShuttleUIText.Tr(
                "CT_Shuttle_Logistics_CapabilitySummary",
                this.FormatCapabilityFlag(logisticsState.SupportsItemTransfer),
                this.FormatCapabilityFlag(logisticsState.SupportsItemConsumption),
                this.FormatCapabilityFlag(logisticsState.SupportsItemDeposit));
        }

        private string GetLogisticsLinkStatusKey(
            bool installed,
            bool enabled,
            bool connected,
            bool placeholderWhenReady,
            V3CargoLogisticsState logisticsState,
            string statusOverrideKey,
            bool requiresLogistics)
        {
            if (!installed)
            {
                return "Missing";
            }

            if (!enabled)
            {
                return "Disabled";
            }

            if (!string.IsNullOrEmpty(statusOverrideKey))
            {
                return statusOverrideKey;
            }

            if (requiresLogistics && !logisticsState.Installed)
            {
                return "NeedsLogistics";
            }

            if (requiresLogistics && !logisticsState.Enabled)
            {
                return "LogisticsDisabled";
            }

            if (placeholderWhenReady)
            {
                return "Pending";
            }

            return connected ? "Connected" : "Offline";
        }

        private string GetLogisticsLinkStatusLabel(string statusKey)
        {
            if (statusKey == "Connected")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusConnected");
            }

            if (statusKey == "Missing")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusNotInstalled");
            }

            if (statusKey == "Disabled" || statusKey == "LogisticsDisabled")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusDisabled");
            }

            if (statusKey == "Offline" || statusKey == "AutoTransferOff")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusDisconnected");
            }

            if (statusKey == "SupplyDisabled")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusPolicyDisabled");
            }

            if (statusKey == "Unpowered")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusUnpowered");
            }

            if (statusKey == "NeedsItemConsumption")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusCapabilityMissing");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusUnavailable");
        }

        private string BuildTooltip(
            V3CargoLogisticsConnectionModel model,
            V3CargoLogisticsState logisticsState)
        {
            string tooltip = model.Label + "\n" +
                model.StatusLabel + "\n" +
                this.GetTextOrUnavailable(model.Description);
            if (!string.IsNullOrEmpty(logisticsState.Label))
            {
                tooltip += "\n" + ShuttleUIText.Tr("CT_Shuttle_Cargo_LogisticsModule") +
                    ": " + logisticsState.Label;
            }

            return tooltip;
        }

        private string ResolveIconKey(V3CargoLogisticsConnectionModel model)
        {
            if (model == null)
            {
                return "module_option";
            }

            if (model.Key == "standard-cargo")
            {
                return "module_cargo";
            }

            return !string.IsNullOrEmpty(model.IconKey)
                ? model.IconKey
                : "module_option";
        }

        private string FormatCapabilityFlag(bool value)
        {
            return value
                ? ShuttleUIText.Tr("CT_Shuttle_Logistics_CapabilityOn")
                : ShuttleUIText.Tr("CT_Shuttle_Logistics_CapabilityOff");
        }

        private string GetTextOrUnavailable(string value)
        {
            return !string.IsNullOrEmpty(value)
                ? value
                : ShuttleUIText.Tr("CT_Shuttle_Logistics_StatusUnavailable");
        }
    }
}
