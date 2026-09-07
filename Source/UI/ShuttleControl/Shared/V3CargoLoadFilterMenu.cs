using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadFilterMenu
    {
        private readonly V3CargoLoadCategoryResolver categoryResolver;
        private readonly Action<string> selectFilter;

        internal V3CargoLoadFilterMenu(
            V3CargoLoadCategoryResolver categoryResolver,
            Action<string> selectFilter)
        {
            this.categoryResolver = categoryResolver;
            this.selectFilter = selectFilter;
        }

        internal string GetActiveLabel(string filterId)
        {
            return this.categoryResolver != null
                ? this.categoryResolver.GetFilterLabel(filterId)
                : V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterAll");
        }

        internal void Open(
            List<TransferableOneWay> transferables,
            bool passengers,
            string activeFilterId)
        {
            if (Find.WindowStack == null)
            {
                return;
            }

            List<V3CargoLoadFilterOption> filterOptions =
                this.BuildOptions(transferables, passengers);
            List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
            bool customHeaderAdded = false;
            for (int i = 0; i < filterOptions.Count; i++)
            {
                V3CargoLoadFilterOption option = filterOptions[i];
                string optionId = option.Id;
                if (!customHeaderAdded &&
                    !string.IsNullOrEmpty(
                        V3CargoLoadFilterIds.GetCustomCategoryDefName(optionId)))
                {
                    menuOptions.Add(new FloatMenuOption(
                        V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterMoreCategories"),
                        null));
                    customHeaderAdded = true;
                }

                string label = V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_FilterOptionCount",
                    option.Label,
                    option.Count);
                if (string.Equals(activeFilterId, optionId, StringComparison.Ordinal))
                {
                    label = "✓ " + label;
                }

                menuOptions.Add(new FloatMenuOption(
                    label,
                    delegate
                    {
                        if (this.selectFilter != null)
                        {
                            this.selectFilter(optionId);
                        }
                    }));
            }

            if (menuOptions.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(menuOptions));
            }
        }

        private List<V3CargoLoadFilterOption> BuildOptions(
            List<TransferableOneWay> transferables,
            bool passengers)
        {
            int totalCount = transferables != null ? transferables.Count : 0;
            Dictionary<string, int> standardCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, V3CargoLoadFilterOption> customOptions =
                new Dictionary<string, V3CargoLoadFilterOption>(StringComparer.Ordinal);

            if (transferables != null && this.categoryResolver != null)
            {
                for (int i = 0; i < transferables.Count; i++)
                {
                    V3CargoLoadCategoryMatch match =
                        this.categoryResolver.Resolve(transferables[i], passengers);
                    if (match == null)
                    {
                        continue;
                    }

                    Increment(standardCounts, match.StandardFilterId);
                    if (passengers || match.CustomCategory == null)
                    {
                        continue;
                    }

                    string customId =
                        V3CargoLoadFilterIds.ForCustomCategory(match.CustomCategory);
                    if (string.IsNullOrEmpty(customId))
                    {
                        continue;
                    }

                    V3CargoLoadFilterOption existing;
                    if (customOptions.TryGetValue(customId, out existing))
                    {
                        customOptions[customId] = new V3CargoLoadFilterOption(
                            customId,
                            existing.Label,
                            existing.Count + 1);
                    }
                    else
                    {
                        customOptions.Add(
                            customId,
                            new V3CargoLoadFilterOption(
                                customId,
                                match.CustomCategory.LabelCap.ToString(),
                                1));
                    }
                }
            }

            List<V3CargoLoadFilterOption> options = new List<V3CargoLoadFilterOption>();
            options.Add(new V3CargoLoadFilterOption(
                V3CargoLoadFilterIds.All,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterAll"),
                totalCount));

            string[] order = V3CargoLoadFilterIds.GetStandardOrder(passengers);
            for (int i = 0; i < order.Length; i++)
            {
                int count;
                if (!standardCounts.TryGetValue(order[i], out count) || count <= 0)
                {
                    continue;
                }

                options.Add(new V3CargoLoadFilterOption(
                    order[i],
                    this.categoryResolver.GetFilterLabel(order[i]),
                    count));
            }

            if (customOptions.Count > 0)
            {
                List<V3CargoLoadFilterOption> sortedCustom =
                    new List<V3CargoLoadFilterOption>(customOptions.Values);
                sortedCustom.Sort(delegate(
                    V3CargoLoadFilterOption left,
                    V3CargoLoadFilterOption right)
                {
                    return string.Compare(
                        left != null ? left.Label : string.Empty,
                        right != null ? right.Label : string.Empty,
                        StringComparison.CurrentCultureIgnoreCase);
                });
                options.AddRange(sortedCustom);
            }

            return options;
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            if (counts == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + 1;
        }
    }
}
