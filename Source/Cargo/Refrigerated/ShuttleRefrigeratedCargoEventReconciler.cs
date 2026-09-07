using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    /// <summary>
    /// Applies refrigerated routing to ordinary cargo only when a relevant event requests it.
    /// Holder scanning and mutation remain inside the existing cold-transfer service.
    /// </summary>
    internal static class ShuttleRefrigeratedCargoEventReconciler
    {
        internal static bool TryReconcile(
            ShuttleAssemblyState assemblyState,
            string moduleInstanceID,
            IShuttleCargoColdTransferService transferService,
            string reason,
            out int movedStackCount,
            out int movedThingCount,
            out string failureReason)
        {
            movedStackCount = 0;
            movedThingCount = 0;
            failureReason = null;
            if (assemblyState == null ||
                string.IsNullOrEmpty(moduleInstanceID) ||
                transferService == null)
            {
                failureReason = "Refrigerated cargo reconciliation context is unavailable.";
                return false;
            }

            ShuttleModule module = assemblyState.GetModule(moduleInstanceID);
            ShuttleRefrigeratedCargoModuleDef moduleDef = module != null
                ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef
                : null;
            if (module == null || moduleDef == null || !module.IsEnabled)
            {
                failureReason = "Refrigerated cargo module is unavailable for reconciliation.";
                return false;
            }

            ShuttleRefrigeratedCargoConfigState configState =
                assemblyState.RefrigeratedCargoConfig;
            ShuttleRefrigeratedCargoAutoTransferConfig config = configState != null
                ? configState.BuildEffectiveAutoTransferConfig(moduleInstanceID, moduleDef)
                : ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(
                    moduleInstanceID,
                    moduleDef);
            if (config == null || !config.AutoTransferEnabled)
            {
                return true;
            }

            return transferService.TryAutoTransferLoadedCargoToCold(
                moduleInstanceID,
                int.MaxValue,
                config.AutoTransferFilter,
                config.HasCustomAutoTransferFilter,
                string.IsNullOrEmpty(reason)
                    ? "event-cold-reconciliation"
                    : reason,
                out movedStackCount,
                out movedThingCount,
                out failureReason);
        }
    }
}
