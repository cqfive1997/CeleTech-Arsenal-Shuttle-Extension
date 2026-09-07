using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    /// <summary>
    /// Builds transient manifest edits for cargo-bay and semantic-category scopes.
    /// It never starts an unload command and never touches cargo holders.
    /// </summary>
    internal sealed class V3CargoUnloadBatchMenu
    {
        private readonly V3CargoLoadCategoryResolver categoryResolver;
        private readonly V3CargoUnloadFilterController filterController;
        private readonly V3CargoUnloadSelectionModel selection;
        private readonly Action onSelectionChanged;

        internal V3CargoUnloadBatchMenu(
            V3CargoLoadCategoryResolver categoryResolver,
            V3CargoUnloadFilterController filterController,
            V3CargoUnloadSelectionModel selection,
            Action onSelectionChanged)
        {
            this.categoryResolver = categoryResolver;
            this.filterController = filterController;
            this.selection = selection;
            this.onSelectionChanged = onSelectionChanged;
        }

        internal void Open(V3CargoUnloadModel model)
        {
            if (Find.WindowStack == null || model == null)
            {
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            this.AddSourceOptions(options, model);
            this.AddCategoryOptions(options, model);
            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void AddSourceOptions(
            List<FloatMenuOption> options,
            V3CargoUnloadModel model)
        {
            List<V3CargoUnloadSourceOption> sources =
                this.filterController != null
                    ? this.filterController.BuildSourceOptions()
                    : new List<V3CargoUnloadSourceOption>();
            if (sources.Count == 0)
            {
                return;
            }

            options.Add(new FloatMenuOption(
                V3CargoLoadText.Tr("CT_Shuttle_Unload_BatchByBayHeader"),
                null));
            for (int i = 0; i < sources.Count; i++)
            {
                V3CargoUnloadSourceOption source = sources[i];
                V3CargoUnloadSourceOption sourceSnapshot = source;
                options.Add(new FloatMenuOption(
                    V3CargoLoadText.Tr(
                        "CT_Shuttle_Unload_BatchGroupOption",
                        source.Label,
                        source.StackCount,
                        source.ThingCount),
                    delegate
                    {
                        this.SelectSource(model, sourceSnapshot);
                    }));
            }
        }

        private void AddCategoryOptions(
            List<FloatMenuOption> options,
            V3CargoUnloadModel model)
        {
            string[] order = V3CargoLoadFilterIds.GetStandardOrder(false);
            List<V3CargoUnloadBatchGroup> groups =
                new List<V3CargoUnloadBatchGroup>();
            for (int categoryIndex = 0; categoryIndex < order.Length; categoryIndex++)
            {
                string categoryId = order[categoryIndex];
                V3CargoUnloadBatchGroup group = new V3CargoUnloadBatchGroup();
                group.Label = this.categoryResolver != null
                    ? this.categoryResolver.GetFilterLabel(categoryId)
                    : categoryId;
                for (int rowIndex = 0; rowIndex < model.Rows.Count; rowIndex++)
                {
                    V3CargoUnloadRowModel row = model.Rows[rowIndex];
                    if (row == null || !string.Equals(
                            row.StandardFilterId,
                            categoryId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    group.Rows.Add(row);
                    group.ThingCount += row.AvailableCount;
                }

                if (group.Rows.Count > 0)
                {
                    groups.Add(group);
                }
            }

            if (groups.Count == 0)
            {
                return;
            }

            options.Add(new FloatMenuOption(
                V3CargoLoadText.Tr("CT_Shuttle_Unload_BatchByCategoryHeader"),
                null));
            for (int i = 0; i < groups.Count; i++)
            {
                V3CargoUnloadBatchGroup group = groups[i];
                V3CargoUnloadBatchGroup groupSnapshot = group;
                options.Add(new FloatMenuOption(
                    V3CargoLoadText.Tr(
                        "CT_Shuttle_Unload_BatchGroupOption",
                        group.Label,
                        group.Rows.Count,
                        group.ThingCount),
                    delegate
                    {
                        this.SelectRows(groupSnapshot.Label, groupSnapshot.Rows);
                    }));
            }
        }

        private void SelectSource(
            V3CargoUnloadModel model,
            V3CargoUnloadSourceOption source)
        {
            List<V3CargoUnloadRowModel> rows = new List<V3CargoUnloadRowModel>();
            for (int i = 0; source != null && i < model.Rows.Count; i++)
            {
                V3CargoUnloadRowModel row = model.Rows[i];
                if (row != null && string.Equals(
                        row.SourceKey,
                        source.Id,
                        StringComparison.Ordinal))
                {
                    rows.Add(row);
                }
            }

            this.SelectRows(source != null ? source.Label : string.Empty, rows);
        }

        private void SelectRows(
            string label,
            List<V3CargoUnloadRowModel> rows)
        {
            int changed = 0;
            for (int i = 0; this.selection != null && rows != null && i < rows.Count; i++)
            {
                V3CargoUnloadRowModel row = rows[i];
                if (this.selection.Get(row) != row.AvailableCount)
                {
                    this.selection.Set(row, row.AvailableCount);
                    changed++;
                }
            }

            if (changed <= 0)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    V3CargoLoadText.Tr("CT_Shuttle_Unload_BatchNoChanges"));
                return;
            }

            if (this.onSelectionChanged != null)
            {
                this.onSelectionChanged();
            }

            ShuttleUICommandFeedback.ShowNeutral(
                V3CargoLoadText.Tr(
                    "CT_Shuttle_Unload_BatchApplied",
                    label,
                    changed));
        }

        private sealed class V3CargoUnloadBatchGroup
        {
            internal string Label;
            internal readonly List<V3CargoUnloadRowModel> Rows =
                new List<V3CargoUnloadRowModel>();
            internal int ThingCount;
        }
    }
}
