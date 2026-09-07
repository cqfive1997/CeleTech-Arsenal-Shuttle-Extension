using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoCategorySummaryBuilder
    {
        private static readonly V3CargoCategory[] Categories =
        {
            V3CargoCategory.Food,
            V3CargoCategory.Manufactured,
            V3CargoCategory.RawResources,
            V3CargoCategory.Items,
            V3CargoCategory.Weapons,
            V3CargoCategory.Apparel,
            V3CargoCategory.Buildings,
            V3CargoCategory.Chunks,
            V3CargoCategory.Plants,
            V3CargoCategory.Corpses
        };

        internal void BuildCategorySummaries(
            V3CargoPageReadModel pageModel,
            V3CargoItemStatsBuilder statsBuilder)
        {
            if (pageModel == null || statsBuilder == null)
            {
                return;
            }

            for (int i = 0; i < Categories.Length; i++)
            {
                V3CargoCategory category = Categories[i];
                V3CargoCategorySummaryModel summary = new V3CargoCategorySummaryModel();
                summary.Category = category;
                summary.Label = ShuttleUIText.Tr(this.GetCategoryLabelKey(category));
                summary.ThingCount = statsBuilder.GetCategoryThingCount(category);
                pageModel.CategorySummaries.Add(summary);
            }
        }

        private string GetCategoryLabelKey(V3CargoCategory category)
        {
            if (category == V3CargoCategory.Food)
            {
                return "CT_Shuttle_Cargo_Category_Food";
            }

            if (category == V3CargoCategory.Manufactured)
            {
                return "CT_Shuttle_Cargo_Category_Manufactured";
            }

            if (category == V3CargoCategory.RawResources)
            {
                return "CT_Shuttle_Cargo_Category_RawResources";
            }

            if (category == V3CargoCategory.Weapons)
            {
                return "CT_Shuttle_Cargo_Category_Weapons";
            }

            if (category == V3CargoCategory.Apparel)
            {
                return "CT_Shuttle_Cargo_Category_Apparel";
            }

            if (category == V3CargoCategory.Buildings)
            {
                return "CT_Shuttle_Cargo_Category_Buildings";
            }

            if (category == V3CargoCategory.Chunks)
            {
                return "CT_Shuttle_Cargo_Category_Chunks";
            }

            if (category == V3CargoCategory.Plants)
            {
                return "CT_Shuttle_Cargo_Category_Plants";
            }

            if (category == V3CargoCategory.Corpses)
            {
                return "CT_Shuttle_Cargo_Category_Corpses";
            }

            return "CT_Shuttle_Cargo_Category_Items";
        }
    }
}
