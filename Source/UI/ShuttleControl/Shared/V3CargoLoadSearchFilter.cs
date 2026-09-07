using System;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoLoadSearchFilter
    {
        internal static string GetSearchNeedle(string searchText)
        {
            return string.IsNullOrEmpty(searchText)
                ? string.Empty
                : searchText.Trim().ToLowerInvariant();
        }

        internal static bool MatchesSearch(
            TransferableOneWay transferable,
            string needle,
            V3CargoLoadTransferableMetricsResolver metricsResolver,
            V3CargoLoadSelectionModel selectionModel)
        {
            if (string.IsNullOrEmpty(needle))
            {
                return true;
            }

            string searchText =
                metricsResolver != null ? metricsResolver.GetSearchText(transferable) : null;
            if (!string.IsNullOrEmpty(searchText) &&
                searchText.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            ShuttleCargoLoadAdmissionResult admission =
                selectionModel != null ? selectionModel.GetAdmission(transferable) : null;
            return ContainsSearch(
                    V3CargoLoadText.GetDestinationText(selectionModel, transferable),
                    needle) ||
                ContainsSearch(
                    V3CargoLoadText.GetAdmissionReasonText(selectionModel, transferable),
                    needle) ||
                ContainsSearch(admission != null ? admission.RefrigeratedModuleLabel : null, needle);
        }

        private static bool ContainsSearch(string value, string needle)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
