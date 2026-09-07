using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal static class ShuttleCargoTransferBayPolicy
    {
        internal static bool HasAnyRefrigeratedBay(
            ShuttleCargoPageActionContext pageContext)
        {
            if (pageContext == null || pageContext.Bays == null)
            {
                return false;
            }

            for (int i = 0; i < pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = pageContext.Bays[i];
                if (bay != null && bay.IsRefrigerated)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasOpenableRefrigeratedBay(
            ShuttleCargoPageActionContext pageContext)
        {
            if (pageContext == null || pageContext.Bays == null)
            {
                return false;
            }

            for (int i = 0; i < pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = pageContext.Bays[i];
                if (bay != null &&
                    bay.IsRefrigerated &&
                    bay.CoolingActive &&
                    !string.IsNullOrEmpty(bay.ModuleInstanceID))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsSelectedRefrigeratedBayUsable(
            ShuttleCargoBayActionTarget bay,
            string moduleInstanceID)
        {
            return string.IsNullOrEmpty(GetSelectedRefrigeratedBayBlocker(
                bay,
                moduleInstanceID));
        }

        internal static string GetSelectedRefrigeratedBayBlocker(
            ShuttleCargoBayActionTarget bay,
            string moduleInstanceID)
        {
            if (bay == null ||
                !bay.IsRefrigerated ||
                string.IsNullOrEmpty(moduleInstanceID) ||
                string.IsNullOrEmpty(bay.ModuleInstanceID) ||
                !string.Equals(
                    bay.ModuleInstanceID,
                    moduleInstanceID,
                    StringComparison.Ordinal))
            {
                return Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
            }

            if (!bay.IsEnabled)
            {
                return !string.IsNullOrEmpty(bay.StatusText)
                    ? bay.StatusText
                    : Tr("CT_Shuttle_Command_RefrigeratedCargoModuleDisabled");
            }

            if (!bay.CoolingActive)
            {
                return !string.IsNullOrEmpty(bay.InactiveReason)
                    ? bay.InactiveReason
                    : !string.IsNullOrEmpty(bay.StatusText)
                        ? bay.StatusText
                        : Tr("CT_Shuttle_Logistics_StatusUnavailable");
            }

            return null;
        }

        internal static string GetFirstRefrigeratedBayBlocker(
            ShuttleCargoPageActionContext pageContext)
        {
            for (int i = 0; pageContext != null &&
                pageContext.Bays != null &&
                i < pageContext.Bays.Count; i++)
            {
                ShuttleCargoBayActionTarget bay = pageContext.Bays[i];
                if (bay == null || !bay.IsRefrigerated)
                {
                    continue;
                }

                if (!bay.CoolingActive)
                {
                    return !string.IsNullOrEmpty(bay.InactiveReason)
                        ? bay.InactiveReason
                        : !string.IsNullOrEmpty(bay.StatusText)
                            ? bay.StatusText
                            : Tr("CT_Shuttle_Logistics_StatusUnavailable");
                }

                if (string.IsNullOrEmpty(bay.ModuleInstanceID))
                {
                    return Tr("CT_Shuttle_Command_RefrigeratedCargoModuleMissing");
                }
            }

            return Tr("CT_Shuttle_Command_RefrigeratedCargoModuleDisabled");
        }

        private static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
