using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingRecipeModelBuilder
    {
        private const string WorkbenchModuleIconKey = "workbench_module";

        internal void BuildRecipes(
            V3ProcessingWorkbenchModel workbench,
            ShuttleAutoWorkTableModuleReadModel module)
        {
            if (workbench == null)
            {
                return;
            }

            if (module == null || module.Recipes == null)
            {
                return;
            }

            for (int i = 0; i < module.Recipes.Count; i++)
            {
                ShuttleAutoWorkTableRecipeReadModel source = module.Recipes[i];
                if (source == null)
                {
                    continue;
                }

                workbench.Recipes.Add(this.BuildRecipe(source, module, i));
            }
        }

        private V3ProcessingRecipeModel BuildRecipe(
            ShuttleAutoWorkTableRecipeReadModel source,
            ShuttleAutoWorkTableModuleReadModel module,
            int index)
        {
            RecipeDef recipeDef = this.GetRecipeDef(source.RecipeDefName);
            V3ProcessingRecipeModel recipe = new V3ProcessingRecipeModel();
            recipe.Id = (!string.IsNullOrEmpty(source.SourceBenchDefName) ? source.SourceBenchDefName : "bench") +
                ":" +
                (!string.IsNullOrEmpty(source.RecipeDefName) ? source.RecipeDefName : index.ToString());
            recipe.Label = V3ProcessingDisplayTextResolver.ResolveRecipeLabel(
                source.RecipeDefName,
                source.RecipeLabel);
            recipe.SourceBenchDefName = source.SourceBenchDefName;
            recipe.SourceBenchLabel =
                V3ProcessingDisplayTextResolver.ResolveSourceBenchLabel(
                    source.SourceBenchDefName,
                    source.SourceBenchLabel);
            recipe.RecipeDefName = source.RecipeDefName;
            recipe.IconKey = WorkbenchModuleIconKey;
            recipe.ProductIcon = this.GetPrimaryProductIcon(recipeDef);
            recipe.ProductLabel = this.BuildProductSummary(recipeDef, source);
            recipe.ProductCategoryKey = !string.IsNullOrEmpty(source.ProductCategoryKey)
                ? source.ProductCategoryKey
                : "Other";

            List<IngredientLineSnapshot> ingredientLines = this.BuildIngredientLines(source);
            this.CopyIngredientReadModel(recipe, source, recipeDef, ingredientLines);

            recipe.WorkAmountLabel = this.BuildWorkAmountLabel(recipeDef);
            recipe.AvailabilityKey = recipeDef == null
                ? "Pending"
                : recipeDef.AvailableNow ? "Available" : "ResearchLocked";
            recipe.AvailabilityLabel =
                V3ProcessingDisplayTextResolver.ResolveRecipeAvailabilityLabel(
                    recipe.AvailabilityKey);
            recipe.CanAdd = recipe.AvailabilityKey == "Available";
            recipe.SupportsDoUntilStock = source.SupportsDoUntilStock;
            recipe.DoUntilStockUnsupportedReason =
                !string.IsNullOrEmpty(source.DoUntilStockUnsupportedReason)
                    ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.DoUntilStockUnsupportedReason)
                    : null;
            recipe.IsCurrentSelected = module != null &&
                !string.IsNullOrEmpty(module.SelectedRecipeDefName) &&
                module.SelectedRecipeDefName == source.RecipeDefName &&
                module.SelectedSourceBenchDefName == source.SourceBenchDefName;
            recipe.IsPlaceholder = recipeDef == null;
            recipe.Tooltip =
                recipe.Label + "\n" +
                this.Tr("CT_Shuttle_Processing_Label_Availability") + ": " + recipe.AvailabilityLabel + "\n" +
                this.Tr("CT_Shuttle_Processing_Label_Product") + ": " + recipe.ProductLabel + "\n" +
                this.Tr("CT_Shuttle_Processing_Label_Ingredients") + ": " + recipe.IngredientSummary + "\n" +
                this.BuildRecipeIngredientTooltip(recipe, ingredientLines) +
                this.BuildMechanoidDisassemblyTooltip(source, recipeDef) +
                this.BuildAnimalButcheryTooltip(source, recipeDef) +
                this.BuildDoUntilStockUnsupportedTooltip(source) +
                "\n" +
                this.Tr("CT_Shuttle_AutoWorkTable_SelectRecipeTooltip");
            recipe.SearchText = this.BuildRecipeSearchText(recipe);
            return recipe;
        }

        private RecipeDef GetRecipeDef(string recipeDefName)
        {
            return string.IsNullOrEmpty(recipeDefName)
                ? null
                : DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
        }

        private void CopyIngredientReadModel(
            V3ProcessingRecipeModel recipe,
            ShuttleAutoWorkTableRecipeReadModel source,
            RecipeDef recipeDef,
            List<IngredientLineSnapshot> ingredientLines)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.IngredientsAvailable = source != null && source.IngredientsAvailable;
            recipe.MissingIngredientSummary = source != null
                ? !string.IsNullOrEmpty(source.MissingIngredientSummary)
                    ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.MissingIngredientSummary)
                    : null
                : null;
            recipe.IngredientStatusKey = this.GetRecipeIngredientStatusKey(
                recipe,
                ingredientLines,
                source,
                recipeDef);
            recipe.IngredientSummary = this.BuildRecipeIngredientSummary(
                recipe,
                ingredientLines,
                source,
                recipeDef);
        }

        private List<IngredientLineSnapshot> BuildIngredientLines(
            ShuttleAutoWorkTableRecipeReadModel source)
        {
            List<IngredientLineSnapshot> lines = new List<IngredientLineSnapshot>();
            IReadOnlyList<ShuttleAutoWorkTableIngredientLineReadModel> sourceLines =
                source != null ? source.IngredientLines : null;
            if (sourceLines == null)
            {
                return lines;
            }

            for (int i = 0; i < sourceLines.Count; i++)
            {
                ShuttleAutoWorkTableIngredientLineReadModel sourceLine = sourceLines[i];
                if (sourceLine == null)
                {
                    continue;
                }

                IngredientLineSnapshot line = new IngredientLineSnapshot();
                line.Label = !string.IsNullOrEmpty(sourceLine.Label)
                    ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        sourceLine.Label)
                    : this.Tr("CT_Shuttle_Processing_Ingredient_Matching");
                line.RequiredCount = sourceLine.RequiredCount;
                line.AvailableCount = sourceLine.AvailableCount;
                line.MissingCount = sourceLine.MissingCount;
                lines.Add(line);
            }

            return lines;
        }

        private string GetRecipeIngredientStatusKey(
            V3ProcessingRecipeModel recipe,
            List<IngredientLineSnapshot> ingredientLines,
            ShuttleAutoWorkTableRecipeReadModel source,
            RecipeDef recipeDef)
        {
            if (recipeDef == null)
            {
                return "Pending";
            }

            if (recipeDef.ingredients == null || recipeDef.ingredients.Count == 0)
            {
                return "Ready";
            }

            if (ingredientLines != null && ingredientLines.Count > 0)
            {
                return recipe != null && recipe.IngredientsAvailable ? "Ready" : "Missing";
            }

            if (source != null && !string.IsNullOrEmpty(source.MissingIngredientSummary))
            {
                return "InventoryUnavailable";
            }

            return "Pending";
        }

        private string BuildRecipeIngredientSummary(
            V3ProcessingRecipeModel recipe,
            List<IngredientLineSnapshot> ingredientLines,
            ShuttleAutoWorkTableRecipeReadModel source,
            RecipeDef recipeDef)
        {
            if (AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef) &&
                source != null)
            {
                return V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_MechanoidCorpseStock",
                    Mathf.Max(0, source.MechanoidDisassemblyCorpseCount).ToString(),
                    "1");
            }

            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef) &&
                source != null)
            {
                return V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_AnimalCorpseStock",
                    Mathf.Max(0, source.AnimalButcheryCorpseCount).ToString(),
                    "1");
            }

            if (ingredientLines != null && ingredientLines.Count > 0)
            {
                string text = string.Empty;
                int max = Mathf.Min(2, ingredientLines.Count);
                for (int i = 0; i < max; i++)
                {
                    IngredientLineSnapshot line = ingredientLines[i];
                    string entry = this.BuildIngredientLineShortText(line);
                    text = string.IsNullOrEmpty(text) ? entry : text + "; " + entry;
                }

                if (ingredientLines.Count > max)
                {
                    text += "...";
                }

                return text;
            }

            if (recipeDef == null || recipeDef.ingredients == null || recipeDef.ingredients.Count == 0)
            {
                return this.Tr("CT_Shuttle_Processing_Ingredient_NoneRequired");
            }

            if (source != null && !string.IsNullOrEmpty(source.MissingIngredientSummary))
            {
                return V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                    source.MissingIngredientSummary);
            }

            if (source != null && !string.IsNullOrEmpty(source.IngredientSummary))
            {
                return V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                    source.IngredientSummary);
            }

            return this.Tr("CT_Shuttle_Processing_CargoInventoryUnavailable");
        }

        private string BuildIngredientLineShortText(IngredientLineSnapshot line)
        {
            if (line == null)
            {
                return "-";
            }

            return this.ValueOrDash(line.Label) +
                ": " + this.Tr("CT_Shuttle_Processing_Ingredient_Need") +
                " " + Mathf.Max(0, line.RequiredCount).ToString() +
                " / " + this.Tr("CT_Shuttle_Processing_Ingredient_Stock") +
                " " + Mathf.Max(0, line.AvailableCount).ToString() +
                " / " + this.Tr("CT_Shuttle_Processing_Ingredient_Shortfall") +
                " " + Mathf.Max(0, line.MissingCount).ToString();
        }

        private string BuildRecipeIngredientTooltip(
            V3ProcessingRecipeModel recipe,
            List<IngredientLineSnapshot> ingredientLines)
        {
            if (ingredientLines == null || ingredientLines.Count == 0)
            {
                return this.Tr("CT_Shuttle_Processing_Ingredient_Details") + ": " +
                    (recipe != null ? this.ValueOrDash(recipe.IngredientSummary) : "-");
            }

            string text = this.Tr("CT_Shuttle_Processing_Ingredient_Details") + ":";
            for (int i = 0; i < ingredientLines.Count; i++)
            {
                IngredientLineSnapshot line = ingredientLines[i];
                text += "\n" + this.BuildIngredientLineShortText(line);
            }

            return text;
        }

        private string BuildMechanoidDisassemblyTooltip(
            ShuttleAutoWorkTableRecipeReadModel source,
            RecipeDef recipeDef)
        {
            if (!AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef) ||
                source == null)
            {
                return string.Empty;
            }

            string text = string.Empty;
            if (!string.IsNullOrEmpty(source.MechanoidDisassemblyCandidateLabel))
            {
                text += "\n" + V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_MechanoidDisassemblyCandidate",
                    V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.MechanoidDisassemblyCandidateLabel));
            }

            if (!string.IsNullOrEmpty(source.MechanoidDisassemblyProductPreview))
            {
                text += "\n" + V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_MechanoidDisassemblyPreviewProducts",
                    V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.MechanoidDisassemblyProductPreview));
            }
            else
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_MechanoidDisassemblyProductsVary");
            }

            if (source.MechanoidDisassemblyProductsVary)
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_MechanoidDisassemblyProductsVaryByCorpse");
            }

            if (source.MechanoidDisassemblyCorpseCount <= 0)
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_NoMechanoidCorpses");
            }

            return text;
        }

        private string BuildDoUntilStockUnsupportedTooltip(
            ShuttleAutoWorkTableRecipeReadModel source)
        {
            if (source == null || source.SupportsDoUntilStock)
            {
                return string.Empty;
            }

            string reason = !string.IsNullOrEmpty(source.DoUntilStockUnsupportedReason)
                ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                    source.DoUntilStockUnsupportedReason)
                : this.Tr("CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts");
            return "\n" + reason + "\n" +
                this.Tr("CT_Shuttle_AutoWorkTable_DynamicProductsUseRepeatForever");
        }

        private string BuildAnimalButcheryTooltip(
            ShuttleAutoWorkTableRecipeReadModel source,
            RecipeDef recipeDef)
        {
            if (!AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef) ||
                source == null)
            {
                return string.Empty;
            }

            string text = string.Empty;
            if (!string.IsNullOrEmpty(source.AnimalButcheryCandidateLabel))
            {
                text += "\n" + V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_AnimalButcheryCandidate",
                    V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.AnimalButcheryCandidateLabel));
            }

            if (!string.IsNullOrEmpty(source.AnimalButcheryProductPreview))
            {
                text += "\n" + V3ProcessingDisplayTextResolver.Tr(
                    "CT_Shuttle_AutoWorkTable_AnimalButcheryPreviewProducts",
                    V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.AnimalButcheryProductPreview));
            }
            else
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_AnimalButcheryProductsVary");
            }

            if (source.AnimalButcheryProductsVary)
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_AnimalButcheryProductsVaryByCorpse");
            }

            text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_AnimalButcheryUnsupportedCorpse");
            text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_AnimalButcheryRottenIgnoredAtRuntime");

            if (source.AnimalButcheryCorpseCount <= 0)
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_NoAnimalCorpses");
            }

            return text;
        }

        private string BuildProductSummary(
            RecipeDef recipeDef,
            ShuttleAutoWorkTableRecipeReadModel source)
        {
            if (AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef))
            {
                if (source != null && !string.IsNullOrEmpty(source.MechanoidDisassemblyProductPreview))
                {
                    return V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.MechanoidDisassemblyProductPreview);
                }

                return this.Tr("CT_Shuttle_AutoWorkTable_MechanoidDisassemblyProductsVary");
            }

            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                if (source != null && !string.IsNullOrEmpty(source.AnimalButcheryProductPreview))
                {
                    return V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        source.AnimalButcheryProductPreview);
                }

                return this.Tr("CT_Shuttle_AutoWorkTable_AnimalButcheryProductsVary");
            }

            if (recipeDef == null || recipeDef.products == null || recipeDef.products.Count == 0)
            {
                return this.Tr("CT_Shuttle_Processing_ProductUnavailable");
            }

            string text = string.Empty;
            int max = Mathf.Min(2, recipeDef.products.Count);
            for (int i = 0; i < max; i++)
            {
                ThingDefCountClass product = recipeDef.products[i];
                if (product == null || product.thingDef == null)
                {
                    continue;
                }

                string label = V3ProcessingDisplayTextResolver.ResolveThingLabel(
                    product.thingDef.defName,
                    product.thingDef.LabelCap.ToString());
                string entry = label + " x" + product.count.ToString();
                text = string.IsNullOrEmpty(text) ? entry : text + ", " + entry;
            }

            if (recipeDef.products.Count > max)
            {
                text += "...";
            }

            return string.IsNullOrEmpty(text)
                ? this.Tr("CT_Shuttle_Processing_ProductUnavailable")
                : text;
        }

        private Texture2D GetPrimaryProductIcon(RecipeDef recipeDef)
        {
            ThingDef thingDef = this.GetPrimaryProductDef(recipeDef);
            return thingDef != null ? thingDef.uiIcon : null;
        }

        private ThingDef GetPrimaryProductDef(RecipeDef recipeDef)
        {
            if (recipeDef == null ||
                recipeDef.products == null ||
                recipeDef.products.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < recipeDef.products.Count; i++)
            {
                ThingDefCountClass product = recipeDef.products[i];
                if (product != null && product.thingDef != null)
                {
                    return product.thingDef;
                }
            }

            return null;
        }

        private string BuildRecipeSearchText(V3ProcessingRecipeModel recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            this.AppendSearchPart(builder, recipe.Label);
            this.AppendSearchPart(builder, recipe.ProductLabel);
            this.AppendSearchPart(builder, recipe.SourceBenchLabel);
            this.AppendSearchPart(builder, recipe.RecipeDefName);
            this.AppendSearchPart(builder, recipe.SourceBenchDefName);
            this.AppendSearchPart(builder, recipe.IngredientSummary);
            this.AppendSearchPart(builder, recipe.AvailabilityLabel);
            return builder.ToString();
        }

        private void AppendSearchPart(StringBuilder builder, string value)
        {
            if (builder == null || string.IsNullOrEmpty(value))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(value);
        }

        private string BuildWorkAmountLabel(RecipeDef recipeDef)
        {
            if (recipeDef == null)
            {
                return this.Tr("CT_Shuttle_Processing_Unavailable");
            }

            float workAmount = Mathf.Max(0f, recipeDef.WorkAmountForStuff(null));
            return workAmount.ToString("0") + " " + this.Tr("CT_Shuttle_Processing_Unit_WorkLower");
        }

        private string ValueOrDash(string value)
        {
            return V3ProcessingDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        private string Tr(string key)
        {
            return V3ProcessingDisplayTextResolver.Tr(key);
        }

        private sealed class IngredientLineSnapshot
        {
            internal string Label;
            internal int RequiredCount;
            internal int AvailableCount;
            internal int MissingCount;
        }
    }
}
