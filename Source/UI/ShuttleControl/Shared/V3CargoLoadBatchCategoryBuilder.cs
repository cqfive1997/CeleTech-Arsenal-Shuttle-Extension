using System;
using System.Collections.Generic;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadBatchCategoryBuilder
    {
        private readonly V3CargoLoadCategoryResolver categoryResolver;
        private readonly V3CargoLoadTransferableMetricsResolver metricsResolver;

        internal V3CargoLoadBatchCategoryBuilder(
            V3CargoLoadCategoryResolver categoryResolver,
            V3CargoLoadTransferableMetricsResolver metricsResolver)
        {
            this.categoryResolver = categoryResolver;
            this.metricsResolver = metricsResolver;
        }

        internal List<V3CargoLoadBatchCategory> Build(
            List<TransferableOneWay> transferables,
            bool passengers)
        {
            Dictionary<string, List<TransferableOneWay>> targetsByCategory =
                new Dictionary<string, List<TransferableOneWay>>(StringComparer.Ordinal);
            Dictionary<string, long> countsByCategory =
                new Dictionary<string, long>(StringComparer.Ordinal);
            if (transferables != null && this.categoryResolver != null)
            {
                for (int i = 0; i < transferables.Count; i++)
                {
                    TransferableOneWay transferable = transferables[i];
                    V3CargoLoadCategoryMatch match =
                        this.categoryResolver.Resolve(transferable, passengers);
                    if (transferable == null ||
                        match == null ||
                        string.IsNullOrEmpty(match.StandardFilterId))
                    {
                        continue;
                    }

                    List<TransferableOneWay> targets;
                    if (!targetsByCategory.TryGetValue(match.StandardFilterId, out targets))
                    {
                        targets = new List<TransferableOneWay>();
                        targetsByCategory.Add(match.StandardFilterId, targets);
                    }

                    targets.Add(transferable);
                    long availableCount = this.metricsResolver != null
                        ? this.metricsResolver.GetMetrics(transferable).MaxCount
                        : 0L;
                    long currentCount;
                    countsByCategory.TryGetValue(match.StandardFilterId, out currentCount);
                    countsByCategory[match.StandardFilterId] = AddClamped(
                        currentCount,
                        availableCount);
                }
            }

            List<V3CargoLoadBatchCategory> categories =
                new List<V3CargoLoadBatchCategory>();
            string[] categoryOrder = V3CargoLoadFilterIds.GetStandardOrder(passengers);
            for (int i = 0; i < categoryOrder.Length; i++)
            {
                string categoryId = categoryOrder[i];
                List<TransferableOneWay> targets;
                if (!targetsByCategory.TryGetValue(categoryId, out targets) ||
                    targets == null ||
                    targets.Count == 0)
                {
                    continue;
                }

                long availableCount;
                countsByCategory.TryGetValue(categoryId, out availableCount);
                categories.Add(new V3CargoLoadBatchCategory(
                    categoryId,
                    this.categoryResolver.GetFilterLabel(categoryId),
                    targets,
                    availableCount));
            }

            return categories;
        }

        private static long AddClamped(long left, long right)
        {
            if (right <= 0L)
            {
                return left > 0L ? left : 0L;
            }

            if (left > long.MaxValue - right)
            {
                return long.MaxValue;
            }

            return left + right;
        }
    }
}
