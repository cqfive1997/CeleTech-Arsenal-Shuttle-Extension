using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoSummaryBuilder
    {
        internal const int MaxSummaryRows = 256;

        internal ExternalSDKCargoSummaryBuildResult Build(
            IReadOnlyList<ShuttleExternalCargoItemRowSnapshot> rows)
        {
            Dictionary<string, SummaryAccumulator> byDef =
                new Dictionary<string, SummaryAccumulator>(StringComparer.Ordinal);
            for (int i = 0; rows != null && i < rows.Count; i++)
            {
                ShuttleExternalCargoItemRowSnapshot row = rows[i];
                if (row == null || string.IsNullOrEmpty(row.ItemDefName))
                {
                    continue;
                }

                SummaryAccumulator accumulator;
                if (!byDef.TryGetValue(row.ItemDefName, out accumulator))
                {
                    accumulator = new SummaryAccumulator(row.ItemDefName, row.ItemLabel);
                    byDef.Add(row.ItemDefName, accumulator);
                }

                accumulator.Add(row);
            }

            List<ShuttleExternalCargoSummaryByDefSnapshot> result =
                new List<ShuttleExternalCargoSummaryByDefSnapshot>();
            foreach (KeyValuePair<string, SummaryAccumulator> pair in byDef)
            {
                if (pair.Value != null)
                {
                    result.Add(pair.Value.ToSnapshot());
                }
            }

            result.Sort(CompareSummaryRows);
            int sourceSummaryCount = result.Count;
            bool summaryRowsTruncated = false;
            if (result.Count > MaxSummaryRows)
            {
                result.RemoveRange(MaxSummaryRows, result.Count - MaxSummaryRows);
                summaryRowsTruncated = true;
            }

            return new ExternalSDKCargoSummaryBuildResult(
                result,
                sourceSummaryCount,
                result.Count,
                MaxSummaryRows,
                summaryRowsTruncated);
        }

        private static int CompareSummaryRows(
            ShuttleExternalCargoSummaryByDefSnapshot left,
            ShuttleExternalCargoSummaryByDefSnapshot right)
        {
            string leftKey = left != null ? left.ItemDefName : string.Empty;
            string rightKey = right != null ? right.ItemDefName : string.Empty;
            return string.CompareOrdinal(leftKey, rightKey);
        }

        private sealed class SummaryAccumulator
        {
            private readonly string itemDefName;
            private readonly string itemLabel;
            private int totalStackCount;
            private int totalUnitCount;
            private float totalMassKg;
            private int loadedUnitCount;
            private int assignedUnitCount;
            private int blockedUnitCount;
            private int refrigeratedUnitCount;

            internal SummaryAccumulator(string itemDefName, string itemLabel)
            {
                this.itemDefName = itemDefName;
                this.itemLabel = itemLabel;
            }

            internal void Add(ShuttleExternalCargoItemRowSnapshot row)
            {
                if (row == null)
                {
                    return;
                }

                this.totalStackCount++;
                this.totalUnitCount += row.StackCount;
                this.totalMassKg += row.MassKg;
                if (row.LocationKind == ShuttleExternalCargoLocationKind.Loaded)
                {
                    this.loadedUnitCount += row.StackCount;
                }

                if (row.Assigned ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.AssignedToLoad ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Queued)
                {
                    this.assignedUnitCount += row.StackCount;
                }

                if (row.Blocked || row.LocationKind == ShuttleExternalCargoLocationKind.Blocked)
                {
                    this.blockedUnitCount += row.StackCount;
                }

                if (row.IsRefrigerated ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Refrigerated)
                {
                    this.refrigeratedUnitCount += row.StackCount;
                }
            }

            internal ShuttleExternalCargoSummaryByDefSnapshot ToSnapshot()
            {
                return new ShuttleExternalCargoSummaryByDefSnapshot(
                    this.itemDefName,
                    this.itemLabel,
                    this.totalStackCount,
                    this.totalUnitCount,
                    this.totalMassKg,
                    this.loadedUnitCount,
                    this.assignedUnitCount,
                    this.blockedUnitCount,
                    this.refrigeratedUnitCount);
            }
        }
    }

    internal sealed class ExternalSDKCargoSummaryBuildResult
    {
        internal ExternalSDKCargoSummaryBuildResult(
            List<ShuttleExternalCargoSummaryByDefSnapshot> rows,
            int sourceSummaryCount,
            int returnedSummaryCount,
            int maxSummaryRows,
            bool summaryRowsTruncated)
        {
            this.Rows = rows ?? new List<ShuttleExternalCargoSummaryByDefSnapshot>();
            this.SourceSummaryCount = sourceSummaryCount;
            this.ReturnedSummaryCount = returnedSummaryCount;
            this.MaxSummaryRows = maxSummaryRows;
            this.SummaryRowsTruncated = summaryRowsTruncated;
        }

        internal List<ShuttleExternalCargoSummaryByDefSnapshot> Rows { get; private set; }

        internal int SourceSummaryCount { get; private set; }

        internal int ReturnedSummaryCount { get; private set; }

        internal int MaxSummaryRows { get; private set; }

        internal bool SummaryRowsTruncated { get; private set; }
    }
}
