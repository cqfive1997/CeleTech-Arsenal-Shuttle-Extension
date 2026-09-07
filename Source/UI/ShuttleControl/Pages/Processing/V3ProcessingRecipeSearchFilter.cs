using System;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal static class V3ProcessingRecipeSearchFilter
    {
        internal static void BuildFilteredRows(
            IReadOnlyList<V3ProcessingRecipeModel> recipes,
            string searchText,
            string availabilityFacet,
            string productCategoryFacet,
            string sourceBenchFacet,
            List<int> filteredRowIndices)
        {
            if (filteredRowIndices == null)
            {
                return;
            }

            filteredRowIndices.Clear();
            if (recipes == null)
            {
                return;
            }

            string needle = GetSearchNeedle(searchText);
            bool collapseSourceDuplicates =
                string.IsNullOrEmpty(sourceBenchFacet) || sourceBenchFacet == "All";
            Dictionary<string, int> rowPositionByRecipeDefName =
                collapseSourceDuplicates
                    ? new Dictionary<string, int>(StringComparer.Ordinal)
                    : null;
            for (int i = 0; i < recipes.Count; i++)
            {
                V3ProcessingRecipeModel recipe = recipes[i];
                if (MatchesSearch(recipe, needle) &&
                    MatchesFacets(
                        recipe,
                        availabilityFacet,
                        productCategoryFacet,
                        sourceBenchFacet))
                {
                    if (collapseSourceDuplicates &&
                        !string.IsNullOrEmpty(recipe.RecipeDefName))
                    {
                        int rowPosition;
                        if (rowPositionByRecipeDefName.TryGetValue(
                            recipe.RecipeDefName,
                            out rowPosition))
                        {
                            int existingIndex = filteredRowIndices[rowPosition];
                            V3ProcessingRecipeModel existing =
                                existingIndex >= 0 && existingIndex < recipes.Count
                                    ? recipes[existingIndex]
                                    : null;
                            if (recipe.IsCurrentSelected &&
                                (existing == null || !existing.IsCurrentSelected))
                            {
                                filteredRowIndices[rowPosition] = i;
                            }

                            continue;
                        }

                        rowPositionByRecipeDefName.Add(
                            recipe.RecipeDefName,
                            filteredRowIndices.Count);
                    }

                    filteredRowIndices.Add(i);
                }
            }
        }

        private static bool MatchesFacets(
            V3ProcessingRecipeModel recipe,
            string availabilityFacet,
            string productCategoryFacet,
            string sourceBenchFacet)
        {
            if (recipe == null)
            {
                return false;
            }

            if (availabilityFacet == "Ready" && !recipe.IngredientsAvailable)
            {
                return false;
            }

            if (availabilityFacet == "Missing" && recipe.IngredientsAvailable)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(productCategoryFacet) &&
                productCategoryFacet != "All" &&
                !string.Equals(
                    productCategoryFacet,
                    recipe.ProductCategoryKey,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return string.IsNullOrEmpty(sourceBenchFacet) ||
                sourceBenchFacet == "All" ||
                string.Equals(
                    sourceBenchFacet,
                    recipe.SourceBenchDefName,
                    StringComparison.Ordinal);
        }

        internal static string GetSearchNeedle(string searchText)
        {
            return string.IsNullOrEmpty(searchText)
                ? string.Empty
                : searchText.Trim();
        }

        private static bool MatchesSearch(
            V3ProcessingRecipeModel recipe,
            string needle)
        {
            if (recipe == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(needle))
            {
                return true;
            }

            return ContainsSearch(recipe.SearchText, needle) ||
                ContainsSearch(recipe.Label, needle) ||
                ContainsSearch(recipe.ProductLabel, needle) ||
                ContainsSearch(recipe.SourceBenchLabel, needle) ||
                ContainsSearch(recipe.RecipeDefName, needle) ||
                ContainsSearch(recipe.SourceBenchDefName, needle) ||
                ContainsSearch(recipe.IngredientSummary, needle) ||
                ContainsSearch(recipe.AvailabilityLabel, needle);
        }

        private static bool ContainsSearch(string value, string needle)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
