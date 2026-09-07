using System.Collections.Generic;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoLoadRowFilter
    {
        internal static void BuildFilteredRows(
            List<TransferableOneWay> transferables,
            string searchNeedle,
            string categoryFilterId,
            bool passengers,
            List<int> filteredRowIndices,
            V3CargoLoadCategoryResolver categoryResolver,
            V3CargoLoadTransferableMetricsResolver metricsResolver,
            V3CargoLoadSelectionModel selectionModel)
        {
            if (filteredRowIndices == null)
            {
                return;
            }

            filteredRowIndices.Clear();
            if (transferables == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (categoryResolver != null &&
                    !categoryResolver.Matches(
                        transferable,
                        passengers,
                        categoryFilterId))
                {
                    continue;
                }

                if (V3CargoLoadSearchFilter.MatchesSearch(
                    transferable,
                    searchNeedle,
                    metricsResolver,
                    selectionModel))
                {
                    filteredRowIndices.Add(i);
                }
            }
        }
    }
}
