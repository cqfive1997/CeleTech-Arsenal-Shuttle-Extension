using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableRecipeCatalog
    {
        private readonly Dictionary<ShuttleAutoWorkTableModuleDef, List<AutoWorkTableRecipeCatalogEntry>> cache =
            new Dictionary<ShuttleAutoWorkTableModuleDef, List<AutoWorkTableRecipeCatalogEntry>>();

        internal IReadOnlyList<AutoWorkTableRecipeCatalogEntry> GetEntries(
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            return this.GetAvailableEntries(moduleDef);
        }

        internal IReadOnlyList<AutoWorkTableRecipeCatalogEntry> GetAvailableEntries(
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            IReadOnlyList<AutoWorkTableRecipeCatalogEntry> entries = this.GetCachedEntries(moduleDef);
            List<AutoWorkTableRecipeCatalogEntry> availableEntries =
                new List<AutoWorkTableRecipeCatalogEntry>();

            for (int i = 0; i < entries.Count; i++)
            {
                AutoWorkTableRecipeCatalogEntry entry = entries[i];
                if (entry != null &&
                    entry.RecipeDef != null &&
                    entry.RecipeDef.AvailableNow)
                {
                    availableEntries.Add(entry);
                }
            }

            return availableEntries;
        }

        private IReadOnlyList<AutoWorkTableRecipeCatalogEntry> GetCachedEntries(
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            if (moduleDef == null)
            {
                return new List<AutoWorkTableRecipeCatalogEntry>();
            }

            List<AutoWorkTableRecipeCatalogEntry> entries;
            if (this.cache.TryGetValue(moduleDef, out entries))
            {
                return entries;
            }

            entries = this.BuildEntries(moduleDef);
            this.cache[moduleDef] = entries;
            return entries;
        }

        internal bool TryResolveEntry(
            ShuttleAutoWorkTableModuleDef moduleDef,
            string sourceBenchDefName,
            string recipeDefName,
            out AutoWorkTableRecipeCatalogEntry entry,
            out string failureReason)
        {
            entry = null;
            failureReason = null;

            if (moduleDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_ModuleDefMissing".Translate().ToString();
                return false;
            }

            if (string.IsNullOrEmpty(sourceBenchDefName) || string.IsNullOrEmpty(recipeDefName))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_SelectionMissing".Translate().ToString();
                return false;
            }

            ThingDef sourceBenchDef = DefDatabase<ThingDef>.GetNamedSilentFail(sourceBenchDefName);
            RecipeDef recipeDef = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
            if (sourceBenchDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_SourceBenchNoLongerResolves"
                    .Translate(sourceBenchDefName)
                    .ToString();
                return false;
            }

            if (recipeDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeNoLongerResolves"
                    .Translate(recipeDefName)
                    .ToString();
                return false;
            }

            if (!this.IsConfiguredSourceBench(moduleDef, sourceBenchDef))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_SourceBenchNotConfigured"
                    .Translate(sourceBenchDefName, moduleDef.defName)
                    .ToString();
                return false;
            }

            if (!this.SourceBenchHasRecipe(sourceBenchDef, recipeDef))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeNotInSourceBench"
                    .Translate(recipeDefName, sourceBenchDefName)
                    .ToString();
                return false;
            }

            string unsupportedReason;
            if (!this.IsAllowedByModuleDef(moduleDef, recipeDef, out unsupportedReason) ||
                !this.IsSupportedRecipe(moduleDef, recipeDef, out unsupportedReason))
            {
                failureReason = unsupportedReason;
                return false;
            }

            entry = new AutoWorkTableRecipeCatalogEntry(sourceBenchDef, recipeDef);
            return true;
        }

        private List<AutoWorkTableRecipeCatalogEntry> BuildEntries(
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            List<AutoWorkTableRecipeCatalogEntry> entries =
                new List<AutoWorkTableRecipeCatalogEntry>();
            List<ThingDef> sourceBenchDefs = this.BuildSourceBenchList(moduleDef);

            for (int i = 0; i < sourceBenchDefs.Count; i++)
            {
                ThingDef sourceBenchDef = sourceBenchDefs[i];
                if (sourceBenchDef == null)
                {
                    continue;
                }

                List<RecipeDef> recipes = this.BuildRecipeList(sourceBenchDef);
                for (int j = 0; j < recipes.Count; j++)
                {
                    RecipeDef recipeDef = recipes[j];
                    string unsupportedReason;
                    if (!this.IsAllowedByModuleDef(moduleDef, recipeDef, out unsupportedReason) ||
                        !this.IsSupportedRecipe(moduleDef, recipeDef, out unsupportedReason))
                    {
                        continue;
                    }

                    this.AddEntryIfMissing(entries, sourceBenchDef, recipeDef);
                }
            }

            return entries;
        }

        private List<RecipeDef> BuildRecipeList(ThingDef sourceBenchDef)
        {
            if (sourceBenchDef == null)
            {
                return new List<RecipeDef>();
            }

            List<RecipeDef> allRecipes = sourceBenchDef.AllRecipes;
            return allRecipes != null
                ? new List<RecipeDef>(allRecipes)
                : new List<RecipeDef>();
        }

        private List<ThingDef> BuildSourceBenchList(ShuttleAutoWorkTableModuleDef moduleDef)
        {
            List<ThingDef> sourceBenchDefs = new List<ThingDef>();
            if (moduleDef == null)
            {
                return sourceBenchDefs;
            }

            this.AddSourceBenches(sourceBenchDefs, moduleDef.sourceBenchDefs);

            if (moduleDef.sourceWorkGiverDefs != null)
            {
                for (int i = 0; i < moduleDef.sourceWorkGiverDefs.Count; i++)
                {
                    WorkGiverDef workGiverDef = moduleDef.sourceWorkGiverDefs[i];
                    if (workGiverDef == null)
                    {
                        continue;
                    }

                    this.AddSourceBenches(sourceBenchDefs, workGiverDef.fixedBillGiverDefs);
                }
            }

            return sourceBenchDefs;
        }

        private void AddSourceBenches(List<ThingDef> destination, List<ThingDef> source)
        {
            if (destination == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ThingDef thingDef = source[i];
                if (thingDef != null && !destination.Contains(thingDef))
                {
                    destination.Add(thingDef);
                }
            }
        }

        private bool IsAllowedByModuleDef(
            ShuttleAutoWorkTableModuleDef moduleDef,
            RecipeDef recipeDef,
            out string failureReason)
        {
            failureReason = null;
            if (recipeDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeMissing".Translate().ToString();
                return false;
            }

            if (moduleDef.blockedRecipes != null && moduleDef.blockedRecipes.Contains(recipeDef))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeBlockedByModuleDef"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (moduleDef.allowedRecipes != null &&
                moduleDef.allowedRecipes.Count > 0 &&
                !moduleDef.allowedRecipes.Contains(recipeDef))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeNotAllowedByModuleDef"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            return true;
        }

        private bool IsConfiguredSourceBench(
            ShuttleAutoWorkTableModuleDef moduleDef,
            ThingDef sourceBenchDef)
        {
            if (moduleDef == null || sourceBenchDef == null)
            {
                return false;
            }

            return this.BuildSourceBenchList(moduleDef).Contains(sourceBenchDef);
        }

        private bool SourceBenchHasRecipe(ThingDef sourceBenchDef, RecipeDef recipeDef)
        {
            if (sourceBenchDef == null || recipeDef == null)
            {
                return false;
            }

            if (sourceBenchDef.AllRecipes != null && sourceBenchDef.AllRecipes.Contains(recipeDef))
            {
                return true;
            }

            if (sourceBenchDef.recipes != null && sourceBenchDef.recipes.Contains(recipeDef))
            {
                return true;
            }

            return recipeDef.recipeUsers != null && recipeDef.recipeUsers.Contains(sourceBenchDef);
        }

        private bool IsSupportedRecipe(
            ShuttleAutoWorkTableModuleDef moduleDef,
            RecipeDef recipeDef,
            out string failureReason)
        {
            failureReason = null;
            if (recipeDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeMissing".Translate().ToString();
                return false;
            }

            if (!moduleDef.allowCustomRecipeWorkers &&
                recipeDef.workerClass != null &&
                recipeDef.workerClass != typeof(RecipeWorker))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeUsesCustomWorker"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowMechanitorOnlyRecipes && recipeDef.mechanitorOnlyRecipe)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeMechanitorOnly"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowGestationRecipes && recipeDef.gestationCycles > 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeUsesGestationCycles"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowMechResurrectionRecipes && recipeDef.mechResurrection)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeMechResurrection"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowFormingTickRecipes && recipeDef.formingTicks > 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeUsesFormingTicks"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowUnfinishedThingRecipes &&
                (recipeDef.UsesUnfinishedThing || recipeDef.unfinishedThingDef != null))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeUsesUnfinishedThings"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef))
            {
                return true;
            }

            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return true;
            }

            if (!moduleDef.allowSpecialProductRecipes &&
                recipeDef.specialProducts != null &&
                recipeDef.specialProducts.Count > 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeHasSpecialProducts"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowStuffProducts && recipeDef.productHasIngredientStuff)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeRequiresStuffProducts"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (!moduleDef.allowSurgeryRecipes && this.IsSurgeryLike(recipeDef))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeSurgeryLike"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            if (recipeDef.products == null || recipeDef.products.Count == 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeHasNoNormalProducts"
                    .Translate(recipeDef.defName)
                    .ToString();
                return false;
            }

            for (int i = 0; i < recipeDef.products.Count; i++)
            {
                ThingDefCountClass product = recipeDef.products[i];
                ThingDef productDef = product != null ? product.thingDef : null;
                if (productDef == null)
                {
                    failureReason = "CT_Shuttle_AutoWorkTable_RecipeHasNullProduct"
                        .Translate(recipeDef.defName)
                        .ToString();
                    return false;
                }

                if (productDef.category == ThingCategory.Pawn && !moduleDef.allowPawnProducts)
                {
                    failureReason = "CT_Shuttle_AutoWorkTable_RecipeProducesPawn"
                        .Translate(recipeDef.defName)
                        .ToString();
                    return false;
                }

                if (productDef.category == ThingCategory.Building && !moduleDef.allowBuildingProducts)
                {
                    failureReason = "CT_Shuttle_AutoWorkTable_RecipeProducesBuilding"
                        .Translate(recipeDef.defName)
                        .ToString();
                    return false;
                }

                if (productDef.category != ThingCategory.Item && !moduleDef.allowNonItemProducts)
                {
                    failureReason = "CT_Shuttle_AutoWorkTable_RecipeProducesNonItem"
                        .Translate(recipeDef.defName)
                        .ToString();
                    return false;
                }

                if (!moduleDef.allowStuffProducts && productDef.MadeFromStuff)
                {
                    failureReason = "CT_Shuttle_AutoWorkTable_RecipeProducesStuffProduct"
                        .Translate(recipeDef.defName)
                        .ToString();
                    return false;
                }

                if (!moduleDef.allowQualityProducts && productDef.HasComp(typeof(CompQuality)))
                {
                    failureReason = "CT_Shuttle_AutoWorkTable_RecipeProducesQualityProduct"
                        .Translate(recipeDef.defName)
                        .ToString();
                    return false;
                }
            }

            return true;
        }

        private bool IsSurgeryLike(RecipeDef recipeDef)
        {
            if (recipeDef == null)
            {
                return false;
            }

            if (recipeDef.addsHediff != null ||
                recipeDef.removesHediff != null ||
                recipeDef.addsHediffOnFailure != null ||
                recipeDef.changesHediffLevel != null)
            {
                return true;
            }

            if (recipeDef.appliedOnFixedBodyParts != null &&
                recipeDef.appliedOnFixedBodyParts.Count > 0)
            {
                return true;
            }

            return recipeDef.appliedOnFixedBodyPartGroups != null &&
                recipeDef.appliedOnFixedBodyPartGroups.Count > 0;
        }

        private void AddEntryIfMissing(
            List<AutoWorkTableRecipeCatalogEntry> entries,
            ThingDef sourceBenchDef,
            RecipeDef recipeDef)
        {
            if (entries == null || sourceBenchDef == null || recipeDef == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AutoWorkTableRecipeCatalogEntry entry = entries[i];
                if (entry != null &&
                    entry.SourceBenchDef == sourceBenchDef &&
                    entry.RecipeDef == recipeDef)
                {
                    return;
                }
            }

            entries.Add(new AutoWorkTableRecipeCatalogEntry(sourceBenchDef, recipeDef));
        }

    }
}
