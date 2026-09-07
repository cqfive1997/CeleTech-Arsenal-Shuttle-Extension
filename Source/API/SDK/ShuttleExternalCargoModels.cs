using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public enum ShuttleExternalCargoLocationKind
    {
        Unknown,
        Loaded,
        AssignedToLoad,
        Blocked,
        Refrigerated,
        Queued
    }

    public enum ShuttleExternalCargoQueryScope
    {
        AllKnownCargo,
        LoadedOnly,
        AssignedOnly,
        BlockedOnly,
        RefrigeratedOnly
    }

    public sealed class ShuttleExternalCargoReadSnapshot
    {
        public ShuttleExternalCargoReadSnapshot(
            bool available,
            string unavailableReason,
            int revision,
            int ticksGame,
            float totalCargoMassKg,
            float cargoMassCapacityKg,
            float usedMassKg,
            float freeMassKg,
            int loadedStackCount,
            int loadedUnitCount,
            int assignedStackCount,
            int assignedUnitCount,
            int blockedStackCount,
            int refrigeratedStackCount,
            int refrigeratedUnitCount,
            float refrigeratedMassKg,
            IEnumerable<ShuttleExternalCargoItemRowSnapshot> rows,
            IEnumerable<ShuttleExternalCargoSummaryByDefSnapshot> summaryByDef)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.Revision = revision;
            this.TicksGame = ticksGame;
            this.TotalCargoMassKg = totalCargoMassKg;
            this.CargoMassCapacityKg = cargoMassCapacityKg;
            this.UsedMassKg = usedMassKg;
            this.FreeMassKg = freeMassKg;
            this.LoadedStackCount = loadedStackCount;
            this.LoadedUnitCount = loadedUnitCount;
            this.AssignedStackCount = assignedStackCount;
            this.AssignedUnitCount = assignedUnitCount;
            this.BlockedStackCount = blockedStackCount;
            this.RefrigeratedStackCount = refrigeratedStackCount;
            this.RefrigeratedUnitCount = refrigeratedUnitCount;
            this.RefrigeratedMassKg = refrigeratedMassKg;
            this.Rows = ShuttleExternalSDKCollections.Copy(rows);
            this.SummaryByDef = ShuttleExternalSDKCollections.Copy(summaryByDef);
            this.SourceRowCount = this.Rows.Count;
            this.ReturnedRowCount = this.Rows.Count;
            this.MaxReturnedRows = this.Rows.Count;
            this.RowsTruncated = false;
            this.SummaryByDefSourceCount = this.SummaryByDef.Count;
            this.SummaryByDefReturnedCount = this.SummaryByDef.Count;
            this.MaxSummaryByDefRows = this.SummaryByDef.Count;
            this.SummaryByDefTruncated = false;
            this.SummaryByDefComplete = true;
        }

        public ShuttleExternalCargoReadSnapshot(
            bool available,
            string unavailableReason,
            int revision,
            int ticksGame,
            float totalCargoMassKg,
            float cargoMassCapacityKg,
            float usedMassKg,
            float freeMassKg,
            int loadedStackCount,
            int loadedUnitCount,
            int assignedStackCount,
            int assignedUnitCount,
            int blockedStackCount,
            int refrigeratedStackCount,
            int refrigeratedUnitCount,
            float refrigeratedMassKg,
            int sourceRowCount,
            int returnedRowCount,
            int maxReturnedRows,
            bool rowsTruncated,
            int summaryByDefSourceCount,
            int summaryByDefReturnedCount,
            int maxSummaryByDefRows,
            bool summaryByDefTruncated,
            bool summaryByDefComplete,
            IEnumerable<ShuttleExternalCargoItemRowSnapshot> rows,
            IEnumerable<ShuttleExternalCargoSummaryByDefSnapshot> summaryByDef)
            : this(
                available,
                unavailableReason,
                revision,
                ticksGame,
                totalCargoMassKg,
                cargoMassCapacityKg,
                usedMassKg,
                freeMassKg,
                loadedStackCount,
                loadedUnitCount,
                assignedStackCount,
                assignedUnitCount,
                blockedStackCount,
                refrigeratedStackCount,
                refrigeratedUnitCount,
                refrigeratedMassKg,
                rows,
                summaryByDef)
        {
            this.ReturnedRowCount = this.Rows.Count;
            this.SourceRowCount = MaxCount(NonNegative(sourceRowCount), this.ReturnedRowCount);
            this.MaxReturnedRows = NonNegative(maxReturnedRows);
            this.RowsTruncated = rowsTruncated ||
                this.SourceRowCount > this.ReturnedRowCount;
            this.SummaryByDefReturnedCount = this.SummaryByDef.Count;
            this.SummaryByDefSourceCount = MaxCount(
                NonNegative(summaryByDefSourceCount),
                this.SummaryByDefReturnedCount);
            this.MaxSummaryByDefRows = NonNegative(maxSummaryByDefRows);
            this.SummaryByDefTruncated = summaryByDefTruncated ||
                this.SummaryByDefSourceCount > this.SummaryByDefReturnedCount;
            this.SummaryByDefComplete = summaryByDefComplete &&
                !this.RowsTruncated &&
                !this.SummaryByDefTruncated;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public int Revision { get; private set; }
        public int TicksGame { get; private set; }
        public float TotalCargoMassKg { get; private set; }
        public float CargoMassCapacityKg { get; private set; }
        public float UsedMassKg { get; private set; }
        public float FreeMassKg { get; private set; }
        public int LoadedStackCount { get; private set; }
        public int LoadedUnitCount { get; private set; }
        public int AssignedStackCount { get; private set; }
        public int AssignedUnitCount { get; private set; }
        public int BlockedStackCount { get; private set; }
        public int RefrigeratedStackCount { get; private set; }
        public int RefrigeratedUnitCount { get; private set; }
        public float RefrigeratedMassKg { get; private set; }

        /// <summary>
        /// Number of safe cargo item rows known before the SDK row cap was applied.
        /// </summary>
        public int SourceRowCount { get; private set; }

        /// <summary>
        /// Number of detailed cargo item rows returned in <see cref="Rows" />.
        /// </summary>
        public int ReturnedRowCount { get; private set; }

        /// <summary>
        /// Maximum detailed cargo item rows this snapshot builder may return.
        /// </summary>
        public int MaxReturnedRows { get; private set; }

        /// <summary>
        /// True when known cargo item rows were omitted from <see cref="Rows" />.
        /// </summary>
        public bool RowsTruncated { get; private set; }

        /// <summary>
        /// Number of distinct item summaries before the summary row cap was applied.
        /// Summary input is the returned detailed rows in this SDK version.
        /// </summary>
        public int SummaryByDefSourceCount { get; private set; }

        /// <summary>
        /// Number of item summary rows returned in <see cref="SummaryByDef" />.
        /// </summary>
        public int SummaryByDefReturnedCount { get; private set; }

        /// <summary>
        /// Maximum item summary rows this snapshot builder may return.
        /// </summary>
        public int MaxSummaryByDefRows { get; private set; }

        /// <summary>
        /// True when distinct item summary rows exceeded the summary row cap.
        /// </summary>
        public bool SummaryByDefTruncated { get; private set; }

        /// <summary>
        /// False when source cargo rows or summary rows were capped.
        /// </summary>
        public bool SummaryByDefComplete { get; private set; }

        public IReadOnlyList<ShuttleExternalCargoItemRowSnapshot> Rows { get; private set; }
        public IReadOnlyList<ShuttleExternalCargoSummaryByDefSnapshot> SummaryByDef { get; private set; }

        private static int NonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static int MaxCount(int left, int right)
        {
            return left > right ? left : right;
        }
    }

    public sealed class ShuttleExternalCargoItemRowSnapshot
    {
        public ShuttleExternalCargoItemRowSnapshot(
            string rowId,
            ShuttleExternalCargoLocationKind locationKind,
            string itemDefName,
            string itemLabel,
            string stuffDefName,
            string stuffLabel,
            string categoryDefName,
            string quality,
            int stackCount,
            float massKg,
            float unitMassKg,
            float nutrition,
            float marketValue,
            bool isCorpse,
            bool isCreatureCargo,
            bool isPerishable,
            bool isRefrigerated,
            bool assigned,
            bool blocked,
            string blockedReason,
            string sourceLabel)
        {
            this.RowId = rowId;
            this.LocationKind = locationKind;
            this.ItemDefName = itemDefName;
            this.ItemLabel = itemLabel;
            this.StuffDefName = stuffDefName;
            this.StuffLabel = stuffLabel;
            this.CategoryDefName = categoryDefName;
            this.Quality = quality;
            this.StackCount = stackCount;
            this.MassKg = massKg;
            this.UnitMassKg = unitMassKg;
            this.Nutrition = nutrition;
            this.MarketValue = marketValue;
            this.IsCorpse = isCorpse;
            this.IsCreatureCargo = isCreatureCargo;
            this.IsPerishable = isPerishable;
            this.IsRefrigerated = isRefrigerated;
            this.Assigned = assigned;
            this.Blocked = blocked;
            this.BlockedReason = blockedReason;
            this.SourceLabel = sourceLabel;
        }

        public string RowId { get; private set; }
        public ShuttleExternalCargoLocationKind LocationKind { get; private set; }
        public string ItemDefName { get; private set; }
        public string ItemLabel { get; private set; }
        public string StuffDefName { get; private set; }
        public string StuffLabel { get; private set; }
        public string CategoryDefName { get; private set; }
        public string Quality { get; private set; }
        public int StackCount { get; private set; }
        public float MassKg { get; private set; }
        public float UnitMassKg { get; private set; }
        public float Nutrition { get; private set; }
        public float MarketValue { get; private set; }
        public bool IsCorpse { get; private set; }
        public bool IsCreatureCargo { get; private set; }
        public bool IsPerishable { get; private set; }
        public bool IsRefrigerated { get; private set; }
        public bool Assigned { get; private set; }
        public bool Blocked { get; private set; }
        public string BlockedReason { get; private set; }
        public string SourceLabel { get; private set; }
    }

    public sealed class ShuttleExternalCargoSummaryByDefSnapshot
    {
        public ShuttleExternalCargoSummaryByDefSnapshot(
            string itemDefName,
            string itemLabel,
            int totalStackCount,
            int totalUnitCount,
            float totalMassKg,
            int loadedUnitCount,
            int assignedUnitCount,
            int blockedUnitCount,
            int refrigeratedUnitCount)
        {
            this.ItemDefName = itemDefName;
            this.ItemLabel = itemLabel;
            this.TotalStackCount = totalStackCount;
            this.TotalUnitCount = totalUnitCount;
            this.TotalMassKg = totalMassKg;
            this.LoadedUnitCount = loadedUnitCount;
            this.AssignedUnitCount = assignedUnitCount;
            this.BlockedUnitCount = blockedUnitCount;
            this.RefrigeratedUnitCount = refrigeratedUnitCount;
        }

        public string ItemDefName { get; private set; }
        public string ItemLabel { get; private set; }
        public int TotalStackCount { get; private set; }
        public int TotalUnitCount { get; private set; }
        public float TotalMassKg { get; private set; }
        public int LoadedUnitCount { get; private set; }
        public int AssignedUnitCount { get; private set; }
        public int BlockedUnitCount { get; private set; }
        public int RefrigeratedUnitCount { get; private set; }
    }

    public sealed class ShuttleExternalCargoQuery
    {
        public ShuttleExternalCargoQuery(
            ShuttleExternalCargoQueryScope scope,
            string itemDefName,
            string categoryDefName,
            string stuffDefName,
            string quality,
            bool requireRefrigerated,
            bool includeBlocked,
            bool includeAssigned,
            bool includeLoaded,
            int maxRows)
        {
            this.Scope = scope;
            this.ItemDefName = itemDefName;
            this.CategoryDefName = categoryDefName;
            this.StuffDefName = stuffDefName;
            this.Quality = quality;
            this.RequireRefrigerated = requireRefrigerated;
            this.IncludeBlocked = includeBlocked;
            this.IncludeAssigned = includeAssigned;
            this.IncludeLoaded = includeLoaded;
            this.MaxRows = maxRows;
        }

        public ShuttleExternalCargoQueryScope Scope { get; private set; }
        public string ItemDefName { get; private set; }
        public string CategoryDefName { get; private set; }
        public string StuffDefName { get; private set; }
        public string Quality { get; private set; }
        public bool RequireRefrigerated { get; private set; }
        public bool IncludeBlocked { get; private set; }
        public bool IncludeAssigned { get; private set; }
        public bool IncludeLoaded { get; private set; }
        public int MaxRows { get; private set; }
    }

    public sealed class ShuttleExternalCargoQueryResult
    {
        public ShuttleExternalCargoQueryResult(
            bool available,
            string unavailableReason,
            int matchedStackCount,
            int matchedUnitCount,
            float matchedMassKg,
            IEnumerable<ShuttleExternalCargoItemRowSnapshot> rows)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.MatchedStackCount = matchedStackCount;
            this.MatchedUnitCount = matchedUnitCount;
            this.MatchedMassKg = matchedMassKg;
            this.Rows = ShuttleExternalSDKCollections.Copy(rows);
            this.EvaluatedRowCount = MaxCount(this.Rows.Count, matchedStackCount);
            this.SourceRowsTruncated = false;
            this.ReturnedRowCount = this.Rows.Count;
            this.MaxReturnedRows = this.Rows.Count;
            this.ResultRowsTruncated = matchedStackCount > this.Rows.Count;
        }

        public ShuttleExternalCargoQueryResult(
            bool available,
            string unavailableReason,
            int matchedStackCount,
            int matchedUnitCount,
            float matchedMassKg,
            int evaluatedRowCount,
            bool sourceRowsTruncated,
            int returnedRowCount,
            int maxReturnedRows,
            bool resultRowsTruncated,
            IEnumerable<ShuttleExternalCargoItemRowSnapshot> rows)
            : this(
                available,
                unavailableReason,
                matchedStackCount,
                matchedUnitCount,
                matchedMassKg,
                rows)
        {
            this.EvaluatedRowCount = MaxCount(NonNegative(evaluatedRowCount), this.Rows.Count);
            this.SourceRowsTruncated = sourceRowsTruncated;
            this.ReturnedRowCount = this.Rows.Count;
            this.MaxReturnedRows = NonNegative(maxReturnedRows);
            this.ResultRowsTruncated = resultRowsTruncated ||
                this.MatchedStackCount > this.ReturnedRowCount;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }

        /// <summary>
        /// Number of snapshot rows inspected by the query evaluator.
        /// </summary>
        public int EvaluatedRowCount { get; private set; }

        /// <summary>
        /// True when the source snapshot rows were already capped. When true,
        /// matched counts are complete only over evaluated snapshot rows.
        /// </summary>
        public bool SourceRowsTruncated { get; private set; }

        public int MatchedStackCount { get; private set; }
        public int MatchedUnitCount { get; private set; }
        public float MatchedMassKg { get; private set; }

        /// <summary>
        /// Number of rows returned in <see cref="Rows" />.
        /// </summary>
        public int ReturnedRowCount { get; private set; }

        /// <summary>
        /// Maximum rows this query result may return.
        /// </summary>
        public int MaxReturnedRows { get; private set; }

        /// <summary>
        /// True when matched evaluated rows exceeded returned rows. Counts still
        /// cover all matched evaluated rows unless <see cref="SourceRowsTruncated" /> is true.
        /// </summary>
        public bool ResultRowsTruncated { get; private set; }

        public IReadOnlyList<ShuttleExternalCargoItemRowSnapshot> Rows { get; private set; }

        private static int NonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static int MaxCount(int left, int right)
        {
            return left > right ? left : right;
        }
    }
}
