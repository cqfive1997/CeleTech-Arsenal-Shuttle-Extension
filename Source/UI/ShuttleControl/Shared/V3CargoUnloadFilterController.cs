using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoUnloadSourceOption
    {
        internal string Id;
        internal string Label;
        internal int StackCount;
        internal int ThingCount;
    }

    internal sealed class V3CargoUnloadFilterController
    {
        internal const string AllSourceId = "all";

        private readonly V3CargoLoadCategoryResolver categoryResolver;
        private readonly List<int> filteredIndices = new List<int>();
        private V3CargoUnloadModel model;
        private string search = string.Empty;
        private string activeCategory = V3CargoLoadFilterIds.All;
        private string activeSource = AllSourceId;
        private bool dirty = true;

        internal V3CargoUnloadFilterController(
            V3CargoLoadCategoryResolver categoryResolver)
        {
            this.categoryResolver = categoryResolver;
        }

        internal string Search
        {
            get { return this.search; }
            set
            {
                string resolved = value ?? string.Empty;
                if (!string.Equals(this.search, resolved, StringComparison.Ordinal))
                {
                    this.search = resolved;
                    this.dirty = true;
                }
            }
        }

        internal string ActiveCategoryLabel
        {
            get
            {
                return this.categoryResolver != null
                    ? this.categoryResolver.GetFilterLabel(this.activeCategory)
                    : V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterAll");
            }
        }

        internal string SourceLabel
        {
            get
            {
                if (IsAllSource(this.activeSource))
                {
                    return V3CargoLoadText.Tr("CT_Shuttle_Unload_SourceAll");
                }

                V3CargoUnloadRowModel row = this.FindSourceRow(this.activeSource);
                if (row != null && !string.IsNullOrEmpty(row.SourceLabel))
                {
                    return row.SourceLabel;
                }

                return V3CargoLoadText.Tr("CT_Shuttle_Unload_SourceAll");
            }
        }

        internal IList<int> FilteredIndices
        {
            get
            {
                this.EnsureFiltered();
                return this.filteredIndices;
            }
        }

        internal void SetModel(V3CargoUnloadModel model)
        {
            this.model = model;
            if (!IsAllSource(this.activeSource) &&
                this.FindSourceRow(this.activeSource) == null)
            {
                this.activeSource = AllSourceId;
            }

            this.dirty = true;
        }

        internal void MarkDirty()
        {
            this.dirty = true;
        }

        internal bool MatchesSource(V3CargoUnloadRowModel row)
        {
            return row != null &&
                (IsAllSource(this.activeSource) ||
                    string.Equals(
                        row.SourceKey,
                        this.activeSource,
                        StringComparison.Ordinal));
        }

        internal bool MatchesCategory(V3CargoUnloadRowModel row, string category)
        {
            return row != null &&
                (V3CargoLoadFilterIds.IsAll(category) ||
                    string.Equals(row.StandardFilterId, category, StringComparison.Ordinal) ||
                    string.Equals(row.CustomFilterId, category, StringComparison.Ordinal));
        }

        internal void OpenSourceMenu(Action changed)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            string allLabel = V3CargoLoadText.Tr(
                "CT_Shuttle_LoadCargo_FilterOptionCount",
                V3CargoLoadText.Tr("CT_Shuttle_Unload_SourceAll"),
                this.model != null ? this.model.Rows.Count : 0);
            if (IsAllSource(this.activeSource))
            {
                allLabel = "✓ " + allLabel;
            }

            options.Add(new FloatMenuOption(
                allLabel,
                delegate
                {
                    this.SelectSource(AllSourceId, changed);
                }));

            List<V3CargoUnloadSourceOption> sourceOptions =
                this.BuildSourceOptions();
            for (int i = 0; i < sourceOptions.Count; i++)
            {
                V3CargoUnloadSourceOption option = sourceOptions[i];
                V3CargoUnloadSourceOption optionSnapshot = option;
                string label = V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_FilterOptionCount",
                    option.Label,
                    option.StackCount);
                if (string.Equals(
                        this.activeSource,
                        option.Id,
                        StringComparison.Ordinal))
                {
                    label = "✓ " + label;
                }

                options.Add(new FloatMenuOption(
                    label,
                    delegate
                    {
                        this.SelectSource(optionSnapshot.Id, changed);
                    }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        internal void OpenCategoryMenu()
        {
            List<V3CargoLoadFilterOption> options =
                this.BuildCategoryOptions(true);
            List<FloatMenuOption> menu = new List<FloatMenuOption>();
            for (int i = 0; i < options.Count; i++)
            {
                V3CargoLoadFilterOption option = options[i];
                string id = option.Id;
                string label = V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_FilterOptionCount",
                    option.Label,
                    option.Count);
                if (string.Equals(id, this.activeCategory, StringComparison.Ordinal))
                {
                    label = "✓ " + label;
                }

                menu.Add(new FloatMenuOption(
                    label,
                    delegate
                    {
                        this.activeCategory = id;
                        this.dirty = true;
                    }));
            }

            if (menu.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(menu));
            }
        }

        private void EnsureFiltered()
        {
            if (!this.dirty)
            {
                return;
            }

            this.filteredIndices.Clear();
            string needle = (this.search ?? string.Empty).Trim().ToLowerInvariant();
            for (int i = 0; this.model != null && i < this.model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = this.model.Rows[i];
                if (this.MatchesSource(row) &&
                    this.MatchesCategory(row, this.activeCategory) &&
                    (string.IsNullOrEmpty(needle) ||
                        (row.SearchText ?? string.Empty).Contains(needle)))
                {
                    this.filteredIndices.Add(i);
                }
            }

            this.dirty = false;
        }

        internal List<V3CargoUnloadSourceOption> BuildSourceOptions()
        {
            Dictionary<string, V3CargoUnloadSourceOption> byId =
                new Dictionary<string, V3CargoUnloadSourceOption>(StringComparer.Ordinal);
            for (int i = 0; this.model != null && i < this.model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = this.model.Rows[i];
                if (row == null || string.IsNullOrEmpty(row.SourceKey))
                {
                    continue;
                }

                V3CargoUnloadSourceOption option;
                if (!byId.TryGetValue(row.SourceKey, out option))
                {
                    option = new V3CargoUnloadSourceOption();
                    option.Id = row.SourceKey;
                    option.Label = row.SourceLabel;
                    byId.Add(row.SourceKey, option);
                }

                option.StackCount++;
                option.ThingCount += row.AvailableCount;
            }

            List<V3CargoUnloadSourceOption> result =
                new List<V3CargoUnloadSourceOption>(byId.Values);
            result.Sort(delegate(
                V3CargoUnloadSourceOption left,
                V3CargoUnloadSourceOption right)
            {
                int labelCompare = string.Compare(
                    left != null ? left.Label : string.Empty,
                    right != null ? right.Label : string.Empty,
                    StringComparison.CurrentCultureIgnoreCase);
                return labelCompare != 0
                    ? labelCompare
                    : string.Compare(
                        left != null ? left.Id : string.Empty,
                        right != null ? right.Id : string.Empty,
                        StringComparison.Ordinal);
            });
            this.DisambiguateDuplicateSourceLabels(result);
            return result;
        }

        internal List<V3CargoLoadFilterOption> BuildCategoryOptions(
            bool respectActiveSource)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            int total = 0;
            for (int i = 0; this.model != null && i < this.model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = this.model.Rows[i];
                if (respectActiveSource && !this.MatchesSource(row))
                {
                    continue;
                }

                total++;
                Increment(counts, row.StandardFilterId);
                Increment(counts, row.CustomFilterId);
            }

            List<V3CargoLoadFilterOption> result = new List<V3CargoLoadFilterOption>();
            result.Add(new V3CargoLoadFilterOption(
                V3CargoLoadFilterIds.All,
                this.categoryResolver.GetFilterLabel(V3CargoLoadFilterIds.All),
                total));
            string[] order = V3CargoLoadFilterIds.GetStandardOrder(false);
            for (int i = 0; i < order.Length; i++)
            {
                int count;
                if (counts.TryGetValue(order[i], out count) && count > 0)
                {
                    result.Add(new V3CargoLoadFilterOption(
                        order[i],
                        this.categoryResolver.GetFilterLabel(order[i]),
                        count));
                }
            }

            List<string> custom = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (!string.IsNullOrEmpty(
                    V3CargoLoadFilterIds.GetCustomCategoryDefName(pair.Key)) &&
                    pair.Value > 0)
                {
                    custom.Add(pair.Key);
                }
            }

            custom.Sort(delegate(string left, string right)
            {
                return string.Compare(
                    this.categoryResolver.GetFilterLabel(left),
                    this.categoryResolver.GetFilterLabel(right),
                    StringComparison.CurrentCultureIgnoreCase);
            });
            for (int i = 0; i < custom.Count; i++)
            {
                result.Add(new V3CargoLoadFilterOption(
                    custom[i],
                    this.categoryResolver.GetFilterLabel(custom[i]),
                    counts[custom[i]]));
            }

            return result;
        }

        private void SelectSource(string sourceId, Action changed)
        {
            this.activeSource = string.IsNullOrEmpty(sourceId)
                ? AllSourceId
                : sourceId;
            this.dirty = true;
            if (changed != null)
            {
                changed();
            }
        }

        private V3CargoUnloadRowModel FindSourceRow(string sourceId)
        {
            for (int i = 0; this.model != null && i < this.model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = this.model.Rows[i];
                if (row != null && string.Equals(
                        row.SourceKey,
                        sourceId,
                        StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        private static bool IsAllSource(string sourceId)
        {
            return string.IsNullOrEmpty(sourceId) ||
                string.Equals(sourceId, AllSourceId, StringComparison.Ordinal);
        }

        private void DisambiguateDuplicateSourceLabels(
            List<V3CargoUnloadSourceOption> options)
        {
            Dictionary<string, int> totals =
                new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            for (int i = 0; options != null && i < options.Count; i++)
            {
                string label = options[i] != null
                    ? options[i].Label ?? string.Empty
                    : string.Empty;
                Increment(totals, label);
            }

            Dictionary<string, int> ordinals =
                new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            for (int i = 0; options != null && i < options.Count; i++)
            {
                V3CargoUnloadSourceOption option = options[i];
                string label = option != null
                    ? option.Label ?? string.Empty
                    : string.Empty;
                int total;
                if (option == null || !totals.TryGetValue(label, out total) || total <= 1)
                {
                    continue;
                }

                int ordinal;
                ordinals.TryGetValue(label, out ordinal);
                ordinal++;
                ordinals[label] = ordinal;
                option.Label = label + " (" + ordinal + ")";
            }
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + 1;
        }
    }
}
