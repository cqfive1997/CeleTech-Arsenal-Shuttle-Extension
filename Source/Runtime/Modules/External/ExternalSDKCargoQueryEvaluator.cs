using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoQueryEvaluator
    {
        private const int DefaultMaxRows = 128;
        private const int AbsoluteMaxRows = 256;
        private const int MaxFilterLength = 128;

        internal ShuttleExternalCargoQueryResult Evaluate(
            ShuttleExternalCargoReadSnapshot snapshot,
            ShuttleExternalCargoQuery query)
        {
            if (snapshot == null || !snapshot.Available)
            {
                return Unavailable(snapshot != null
                    ? snapshot.UnavailableReason
                    : "cargo snapshot is unavailable");
            }

            if (query == null)
            {
                return this.MatchAll(snapshot, DefaultMaxRows);
            }

            string itemDefName;
            string categoryDefName;
            string stuffDefName;
            string quality;
            string failureReason;
            if (!this.TryNormalizeQueryStrings(
                query,
                out itemDefName,
                out categoryDefName,
                out stuffDefName,
                out quality,
                out failureReason))
            {
                return Unavailable(failureReason);
            }

            int maxRows = this.ClampMaxRows(query.MaxRows);
            List<ShuttleExternalCargoItemRowSnapshot> rows =
                new List<ShuttleExternalCargoItemRowSnapshot>();
            int matchedStacks = 0;
            int matchedUnits = 0;
            float matchedMass = 0f;
            IReadOnlyList<ShuttleExternalCargoItemRowSnapshot> sourceRows = snapshot.Rows;
            int evaluatedRowCount = sourceRows != null ? sourceRows.Count : 0;
            for (int i = 0; sourceRows != null && i < sourceRows.Count; i++)
            {
                ShuttleExternalCargoItemRowSnapshot row = sourceRows[i];
                if (!this.Matches(
                    row,
                    query,
                    itemDefName,
                    categoryDefName,
                    stuffDefName,
                    quality))
                {
                    continue;
                }

                matchedStacks++;
                matchedUnits += row.StackCount;
                matchedMass += row.MassKg;
                if (rows.Count < maxRows)
                {
                    rows.Add(row);
                }
            }

            return new ShuttleExternalCargoQueryResult(
                true,
                null,
                matchedStacks,
                matchedUnits,
                matchedMass,
                evaluatedRowCount,
                snapshot.RowsTruncated,
                rows.Count,
                maxRows,
                matchedStacks > rows.Count,
                rows);
        }

        internal static ShuttleExternalCargoQueryResult Unavailable(string reason)
        {
            return new ShuttleExternalCargoQueryResult(
                false,
                reason,
                0,
                0,
                0f,
                0,
                false,
                0,
                0,
                false,
                null);
        }

        private ShuttleExternalCargoQueryResult MatchAll(
            ShuttleExternalCargoReadSnapshot snapshot,
            int maxRows)
        {
            List<ShuttleExternalCargoItemRowSnapshot> rows =
                new List<ShuttleExternalCargoItemRowSnapshot>();
            int matchedStacks = 0;
            int matchedUnits = 0;
            float matchedMass = 0f;
            IReadOnlyList<ShuttleExternalCargoItemRowSnapshot> sourceRows =
                snapshot != null ? snapshot.Rows : null;
            int evaluatedRowCount = sourceRows != null ? sourceRows.Count : 0;
            for (int i = 0; sourceRows != null && i < sourceRows.Count; i++)
            {
                ShuttleExternalCargoItemRowSnapshot row = sourceRows[i];
                if (row == null)
                {
                    continue;
                }

                matchedStacks++;
                matchedUnits += row.StackCount;
                matchedMass += row.MassKg;
                if (rows.Count < maxRows)
                {
                    rows.Add(row);
                }
            }

            return new ShuttleExternalCargoQueryResult(
                true,
                null,
                matchedStacks,
                matchedUnits,
                matchedMass,
                evaluatedRowCount,
                snapshot != null && snapshot.RowsTruncated,
                rows.Count,
                maxRows,
                matchedStacks > rows.Count,
                rows);
        }

        private bool Matches(
            ShuttleExternalCargoItemRowSnapshot row,
            ShuttleExternalCargoQuery query,
            string itemDefName,
            string categoryDefName,
            string stuffDefName,
            string quality)
        {
            if (row == null || query == null)
            {
                return false;
            }

            if (!this.MatchesScope(row, query.Scope))
            {
                return false;
            }

            if (query.RequireRefrigerated && !row.IsRefrigerated)
            {
                return false;
            }

            if (!this.MatchesIncludeFlags(row, query))
            {
                return false;
            }

            return this.MatchesString(row.ItemDefName, itemDefName) &&
                this.MatchesString(row.CategoryDefName, categoryDefName) &&
                this.MatchesString(row.StuffDefName, stuffDefName) &&
                this.MatchesString(row.Quality, quality);
        }

        private bool MatchesScope(
            ShuttleExternalCargoItemRowSnapshot row,
            ShuttleExternalCargoQueryScope scope)
        {
            if (row == null)
            {
                return false;
            }

            if (scope == ShuttleExternalCargoQueryScope.LoadedOnly)
            {
                return row.LocationKind == ShuttleExternalCargoLocationKind.Loaded;
            }

            if (scope == ShuttleExternalCargoQueryScope.AssignedOnly)
            {
                return row.Assigned ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.AssignedToLoad ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Queued;
            }

            if (scope == ShuttleExternalCargoQueryScope.BlockedOnly)
            {
                return row.Blocked ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Blocked;
            }

            if (scope == ShuttleExternalCargoQueryScope.RefrigeratedOnly)
            {
                return row.IsRefrigerated ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Refrigerated;
            }

            return true;
        }

        private bool MatchesIncludeFlags(
            ShuttleExternalCargoItemRowSnapshot row,
            ShuttleExternalCargoQuery query)
        {
            bool hasExplicitIncludes =
                query.IncludeLoaded ||
                query.IncludeAssigned ||
                query.IncludeBlocked;
            if (!hasExplicitIncludes)
            {
                return true;
            }

            if (query.IncludeLoaded &&
                row.LocationKind == ShuttleExternalCargoLocationKind.Loaded)
            {
                return true;
            }

            if (query.IncludeAssigned &&
                (row.Assigned ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.AssignedToLoad ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Queued))
            {
                return true;
            }

            if (query.IncludeBlocked &&
                (row.Blocked ||
                    row.LocationKind == ShuttleExternalCargoLocationKind.Blocked))
            {
                return true;
            }

            return false;
        }

        private bool MatchesString(string actual, string expected)
        {
            return string.IsNullOrEmpty(expected) ||
                string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private bool TryNormalizeQueryStrings(
            ShuttleExternalCargoQuery query,
            out string itemDefName,
            out string categoryDefName,
            out string stuffDefName,
            out string quality,
            out string failureReason)
        {
            itemDefName = null;
            categoryDefName = null;
            stuffDefName = null;
            quality = null;
            failureReason = null;
            return this.TryNormalizeFilter(query.ItemDefName, "itemDefName", out itemDefName, out failureReason) &&
                this.TryNormalizeFilter(query.CategoryDefName, "categoryDefName", out categoryDefName, out failureReason) &&
                this.TryNormalizeFilter(query.StuffDefName, "stuffDefName", out stuffDefName, out failureReason) &&
                this.TryNormalizeFilter(query.Quality, "quality", out quality, out failureReason);
        }

        private bool TryNormalizeFilter(
            string value,
            string fieldName,
            out string normalized,
            out string failureReason)
        {
            normalized = null;
            failureReason = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            normalized = value.Trim();
            if (normalized.Length > MaxFilterLength)
            {
                failureReason = fieldName + " exceeds " + MaxFilterLength + " characters";
                return false;
            }

            return true;
        }

        private int ClampMaxRows(int maxRows)
        {
            if (maxRows <= 0)
            {
                return DefaultMaxRows;
            }

            return maxRows > AbsoluteMaxRows ? AbsoluteMaxRows : maxRows;
        }
    }
}
