using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing
{
    internal interface IShuttleProcessingProductionPolicyActions
    {
        bool CanSaveProductionPolicy(
            ShuttleProcessingOrderActionTarget order,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount);

        bool SaveProductionPolicy(
            ShuttleProcessingOrderActionTarget order,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount);

        string GetProductionPolicyTooltip(ShuttleProcessingOrderActionTarget order);

        string GetDoUntilStockUnsupportedReason(ShuttleProcessingOrderActionTarget order);
    }
}
