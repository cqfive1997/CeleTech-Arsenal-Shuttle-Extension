using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleAutoWorkTableReadModelBuilder
    {
        private readonly AutoWorkTableRecipeCatalog recipeCatalog =
            new AutoWorkTableRecipeCatalog();
        private readonly AutoWorkTableIngredientResolver ingredientResolver =
            new AutoWorkTableIngredientResolver();
        private readonly AutoWorkTableMechanoidDisassemblyPreviewUtility mechanoidPreviewUtility =
            new AutoWorkTableMechanoidDisassemblyPreviewUtility();
        private readonly AutoWorkTableAnimalButcheryPreviewUtility animalButcheryPreviewUtility =
            new AutoWorkTableAnimalButcheryPreviewUtility();

        internal ShuttleAutoWorkTableReadModel Build(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoInventorySnapshot inventorySnapshot = null)
        {
            ShuttleAutoWorkTableReadModel model = new ShuttleAutoWorkTableReadModel();
            if (assemblyState == null)
            {
                return model;
            }

            assemblyState.EnsureInitialized();
            if (runtimeState != null)
            {
                runtimeState.EnsureInitialized();
            }

            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            if (modules == null)
            {
                return model;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                ShuttleAutoWorkTableModuleDef moduleDef =
                    module != null ? module.ModuleDef as ShuttleAutoWorkTableModuleDef : null;
                if (module == null || moduleDef == null)
                {
                    continue;
                }

                model.Modules.Add(this.BuildModuleModel(module, moduleDef, runtimeState, inventorySnapshot));
            }

            return model;
        }

        private ShuttleAutoWorkTableModuleReadModel BuildModuleModel(
            ShuttleModule module,
            ShuttleAutoWorkTableModuleDef moduleDef,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoInventorySnapshot inventorySnapshot)
        {
            ShuttleAutoWorkTableModuleReadModel model =
                new ShuttleAutoWorkTableModuleReadModel();
            model.ModuleInstanceID = module.ModuleInstanceID;
            model.ModuleDefName = moduleDef.defName;
            model.ModuleLabel = !string.IsNullOrEmpty(moduleDef.label)
                ? moduleDef.LabelCap.ToString()
                : moduleDef.defName;
            model.IsEnabled = module.IsEnabled;

            AutoWorkTableRuntimeState state = this.GetRuntimeState(
                runtimeState,
                module.ModuleInstanceID);
            if (state != null)
            {
                model.HasRuntimeState = true;
                model.IsPaused = state.IsPaused;
                model.Status = state.Status.ToString();
                model.HasActiveProduction = state.HasActiveProduction;
                model.HasPendingProducts = state.HasPendingProducts;
                model.PendingProductCount = state.PendingProductCount;
                model.SelectedSourceBenchDefName = state.SelectedSourceBenchDefName;
                model.SelectedRecipeDefName = state.SelectedRecipeDefName;
                model.ActiveSourceBenchDefName = state.ActiveSourceBenchDefName;
                model.ActiveRecipeDefName = state.ActiveRecipeDefName;
                model.WorkLeft = state.WorkLeft;
                model.LastFailureReason = state.LastFailureReason;
                model.IngredientStatusSummary = state.IngredientStatusSummary;
                model.MissingIngredientSummary = state.MissingIngredientSummary;
                model.ProductionModeKey = state.ProductionMode.ToString();
                model.RequestedCount = state.RequestedCount;
                model.RemainingCount = state.RemainingCount;
                model.TargetCount = state.TargetCount;
                model.CompletedCount = state.CompletedCount;
                model.TargetStockCount = state.TargetStockCount;
                model.QueueRevision = state.QueueRevision;
                this.AddOrders(model, moduleDef, state);
                this.ApplyPrimaryProductReadModel(model, moduleDef, state);
            }
            else
            {
                model.Status = "NoRuntimeState";
                model.ProductionModeKey = AutoWorkTableProductionMode.OneShot.ToString();
                model.RequestedCount = 1;
                model.RemainingCount = 1;
                model.TargetCount = 1;
            }

            this.AddRecipes(model, moduleDef, inventorySnapshot);
            return model;
        }

        private void ApplyPrimaryProductReadModel(
            ShuttleAutoWorkTableModuleReadModel model,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            if (model == null || moduleDef == null || state == null)
            {
                return;
            }

            AutoWorkTableRecipeCatalogEntry entry;
            string failureReason;
            if (!this.recipeCatalog.TryResolveEntry(
                moduleDef,
                state.SelectedSourceBenchDefName,
                state.SelectedRecipeDefName,
                out entry,
                out failureReason) ||
                entry == null ||
                entry.RecipeDef == null ||
                entry.RecipeDef.products == null ||
                entry.RecipeDef.products.Count == 0 ||
                entry.RecipeDef.products[0] == null ||
                entry.RecipeDef.products[0].thingDef == null)
            {
                return;
            }

            ThingDef thingDef = entry.RecipeDef.products[0].thingDef;
            model.TargetProductDefName = thingDef.defName;
            model.TargetProductLabel = !string.IsNullOrEmpty(thingDef.label)
                ? thingDef.LabelCap.ToString()
                : thingDef.defName;
        }

        private AutoWorkTableRuntimeState GetRuntimeState(
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID)
        {
            if (runtimeState == null || string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            IShuttleModuleRuntimeState payload;
            if (!runtimeState.Modules.TryGetState(
                moduleInstanceID,
                AutoWorkTableRuntimeSystem.AutoWorkTableRuntimeSystemKey,
                out payload))
            {
                return null;
            }

            return payload as AutoWorkTableRuntimeState;
        }

        private void AddRecipes(
            ShuttleAutoWorkTableModuleReadModel moduleModel,
            ShuttleAutoWorkTableModuleDef moduleDef,
            ShuttleCargoInventorySnapshot inventorySnapshot)
        {
            if (moduleModel == null || moduleDef == null)
            {
                return;
            }

            IReadOnlyList<AutoWorkTableRecipeCatalogEntry> entries =
                this.recipeCatalog.GetAvailableEntries(moduleDef);
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AutoWorkTableRecipeCatalogEntry entry = entries[i];
                if (entry == null || entry.SourceBenchDef == null || entry.RecipeDef == null)
                {
                    continue;
                }

                ShuttleAutoWorkTableRecipeReadModel recipe =
                    new ShuttleAutoWorkTableRecipeReadModel();
                recipe.SourceBenchDefName = entry.SourceBenchDefName;
                recipe.SourceBenchLabel = !string.IsNullOrEmpty(entry.SourceBenchDef.label)
                    ? entry.SourceBenchDef.LabelCap.ToString()
                    : entry.SourceBenchDefName;
                recipe.RecipeDefName = entry.RecipeDefName;
                recipe.RecipeLabel = !string.IsNullOrEmpty(entry.RecipeDef.label)
                    ? entry.RecipeDef.LabelCap.ToString()
                    : entry.RecipeDefName;
                recipe.ProductCategoryKey = this.ResolveProductCategoryKey(entry.RecipeDef);
                this.ApplyProductionPolicyReadModel(recipe, entry.RecipeDef);
                this.ApplyIngredientReadModel(recipe, entry.RecipeDef, inventorySnapshot);
                this.ApplyMechanoidDisassemblyPreview(recipe, entry.RecipeDef, inventorySnapshot);
                this.ApplyAnimalButcheryPreview(recipe, entry.RecipeDef, inventorySnapshot);
                moduleModel.Recipes.Add(recipe);
            }

            this.ApplySelectedRecipePolicyReadModel(moduleModel);
        }

        private void AddOrders(
            ShuttleAutoWorkTableModuleReadModel moduleModel,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            IReadOnlyList<AutoWorkTableProductionOrderState> orders = state.ProductionOrders;
            for (int i = 0; i < orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = orders[i];
                if (order == null)
                {
                    continue;
                }

                ShuttleAutoWorkTableOrderReadModel readModel =
                    new ShuttleAutoWorkTableOrderReadModel();
                readModel.OrderId = order.OrderId;
                readModel.QueueIndex = i;
                readModel.IsActive = state.ActiveOrderId == order.OrderId;
                readModel.IsSuspended = order.Suspended;
                readModel.CanMoveUp = i > 0;
                readModel.CanMoveDown = i + 1 < orders.Count;
                readModel.SourceBenchDefName = order.SourceBenchDefName;
                readModel.RecipeDefName = order.RecipeDefName;
                readModel.Status = order.TransientStatus.ToString();
                readModel.FailureReason = order.TransientFailureReason;
                readModel.IngredientStatusSummary = order.IngredientStatusSummary;
                readModel.MissingIngredientSummary = order.MissingIngredientSummary;
                readModel.ProductionModeKey = order.ProductionMode.ToString();
                readModel.RequestedCount = order.RequestedCount;
                readModel.RemainingCount = order.RemainingCount;
                readModel.TargetCount = order.TargetCount;
                readModel.CompletedCount = order.CompletedCount;
                readModel.TargetStockCount = order.TargetStockCount;
                readModel.HasCustomIngredientFilter = order.HasCustomIngredientFilter;
                readModel.IngredientFilterRevision = order.FilterRevision;
                readModel.IngredientFilter = order.HasCustomIngredientFilter
                    ? AutoWorkTableIngredientFilterUtility.CopyOrNull(
                        order.CustomIngredientFilterForRead)
                    : null;

                AutoWorkTableRecipeCatalogEntry entry;
                string failureReason;
                if (this.recipeCatalog.TryResolveEntry(
                    moduleDef,
                    order.SourceBenchDefName,
                    order.RecipeDefName,
                    out entry,
                    out failureReason) && entry != null)
                {
                    readModel.SourceBenchLabel = entry.SourceBenchDef != null &&
                        !string.IsNullOrEmpty(entry.SourceBenchDef.label)
                            ? entry.SourceBenchDef.LabelCap.ToString()
                            : entry.SourceBenchDefName;
                    readModel.RecipeLabel = entry.RecipeDef != null &&
                        !string.IsNullOrEmpty(entry.RecipeDef.label)
                            ? entry.RecipeDef.LabelCap.ToString()
                            : entry.RecipeDefName;
                    readModel.SupportsDoUntilStock =
                        !AutoWorkTableMechanoidDisassemblyUtility.RejectsDoUntilStock(entry.RecipeDef) &&
                        !AutoWorkTableAnimalButcheryUtility.RejectsDoUntilStock(entry.RecipeDef);
                    readModel.DoUntilStockUnsupportedReason = readModel.SupportsDoUntilStock
                        ? null
                        : this.GetDoUntilStockUnsupportedReason(entry.RecipeDef);
                }
                else
                {
                    readModel.SourceBenchLabel = order.SourceBenchDefName;
                    readModel.RecipeLabel = order.RecipeDefName;
                }

                moduleModel.Orders.Add(readModel);
            }
        }

        private string ResolveProductCategoryKey(RecipeDef recipeDef)
        {
            ThingDef productDef = recipeDef != null &&
                recipeDef.products != null &&
                recipeDef.products.Count > 0 &&
                recipeDef.products[0] != null
                    ? recipeDef.products[0].thingDef
                    : null;
            if (productDef == null)
            {
                return "Other";
            }

            if (productDef.IsWeapon)
            {
                return "Weapons";
            }

            if (productDef.IsApparel)
            {
                return "Apparel";
            }

            if (productDef.IsMedicine)
            {
                return "Medicine";
            }

            if (productDef.IsIngestible)
            {
                return "Food";
            }

            if (productDef.stuffProps != null || productDef.IsStuff)
            {
                return "Materials";
            }

            return "Other";
        }

        private void ApplyProductionPolicyReadModel(
            ShuttleAutoWorkTableRecipeReadModel recipe,
            RecipeDef recipeDef)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.SupportsDoUntilStock =
                !AutoWorkTableMechanoidDisassemblyUtility.RejectsDoUntilStock(recipeDef) &&
                !AutoWorkTableAnimalButcheryUtility.RejectsDoUntilStock(recipeDef);
            recipe.DoUntilStockUnsupportedReason = recipe.SupportsDoUntilStock
                ? null
                : this.GetDoUntilStockUnsupportedReason(recipeDef);
        }

        private void ApplySelectedRecipePolicyReadModel(
            ShuttleAutoWorkTableModuleReadModel moduleModel)
        {
            if (moduleModel == null || moduleModel.Recipes == null)
            {
                return;
            }

            string selectedRecipeDefName = !string.IsNullOrEmpty(moduleModel.SelectedRecipeDefName)
                ? moduleModel.SelectedRecipeDefName
                : moduleModel.ActiveRecipeDefName;
            string selectedSourceBenchDefName = !string.IsNullOrEmpty(moduleModel.SelectedSourceBenchDefName)
                ? moduleModel.SelectedSourceBenchDefName
                : moduleModel.ActiveSourceBenchDefName;
            for (int i = 0; i < moduleModel.Recipes.Count; i++)
            {
                ShuttleAutoWorkTableRecipeReadModel recipe = moduleModel.Recipes[i];
                if (recipe == null ||
                    recipe.RecipeDefName != selectedRecipeDefName ||
                    recipe.SourceBenchDefName != selectedSourceBenchDefName)
                {
                    continue;
                }

                moduleModel.SelectedRecipeSupportsDoUntilStock = recipe.SupportsDoUntilStock;
                moduleModel.SelectedRecipeDoUntilStockUnsupportedReason = recipe.DoUntilStockUnsupportedReason;
                return;
            }
        }

        private void ApplyMechanoidDisassemblyPreview(
            ShuttleAutoWorkTableRecipeReadModel recipe,
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot)
        {
            if (recipe == null || recipeDef == null)
            {
                return;
            }

            AutoWorkTableMechanoidDisassemblyPreview preview;
            if (!this.mechanoidPreviewUtility.TryBuildPreview(
                recipeDef,
                inventorySnapshot,
                out preview) ||
                preview == null)
            {
                return;
            }

            recipe.MechanoidDisassemblyCorpseCount = preview.CorpseCount;
            recipe.MechanoidDisassemblyCandidateLabel = preview.CandidateLabel;
            recipe.MechanoidDisassemblyProductPreview = preview.ProductSummary;
            recipe.MechanoidDisassemblyProductsVary = preview.ProductsVary;
            recipe.HasMechanoidDisassemblyProductPreview = preview.HasProductPreview;
        }

        private void ApplyAnimalButcheryPreview(
            ShuttleAutoWorkTableRecipeReadModel recipe,
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot)
        {
            if (recipe == null || recipeDef == null)
            {
                return;
            }

            AutoWorkTableAnimalButcheryPreview preview;
            if (!this.animalButcheryPreviewUtility.TryBuildPreview(
                recipeDef,
                inventorySnapshot,
                out preview) ||
                preview == null)
            {
                return;
            }

            recipe.AnimalButcheryCorpseCount = preview.CorpseCount;
            recipe.AnimalButcheryCandidateLabel = preview.CandidateLabel;
            recipe.AnimalButcheryProductPreview = preview.ProductSummary;
            recipe.AnimalButcheryProductsVary = preview.ProductsVary;
            recipe.HasAnimalButcheryProductPreview = preview.HasProductPreview;
        }

        private string GetDoUntilStockUnsupportedReason(RecipeDef recipeDef)
        {
            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return "CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedAnimalButchery".Translate().ToString();
            }

            return "CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts".Translate().ToString();
        }

        private void ApplyIngredientReadModel(
            ShuttleAutoWorkTableRecipeReadModel recipe,
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot)
        {
            if (recipe == null || recipeDef == null)
            {
                return;
            }

            AutoWorkTableIngredientAvailability availability;
            if (!this.ingredientResolver.TryResolveAvailability(
                recipeDef,
                inventorySnapshot,
                out availability) &&
                availability == null)
            {
                recipe.IngredientsAvailable = false;
                recipe.IngredientSummary =
                    "CT_Shuttle_Processing_CargoInventoryUnavailable"
                    .Translate()
                    .ToString();
                recipe.MissingIngredientSummary = recipe.IngredientSummary;
                return;
            }

            recipe.IngredientsAvailable = availability != null && availability.Available;
            recipe.IngredientSummary = availability != null ? availability.Summary : null;
            recipe.MissingIngredientSummary = availability != null ? availability.MissingSummary : null;
            IReadOnlyList<AutoWorkTableIngredientNeedStatus> needs =
                availability != null ? availability.Needs : null;
            if (needs == null)
            {
                return;
            }

            for (int i = 0; i < needs.Count; i++)
            {
                AutoWorkTableIngredientNeedStatus need = needs[i];
                if (need == null)
                {
                    continue;
                }

                ShuttleAutoWorkTableIngredientLineReadModel line =
                    new ShuttleAutoWorkTableIngredientLineReadModel();
                line.Label = need.Label;
                line.DefName = need.DefName;
                line.RequiredCount = need.RequiredCount;
                line.AvailableCount = need.AvailableCount;
                line.MissingCount = need.MissingCount;
                line.Available = need.MissingCount <= 0;
                line.RegularCargoCount = need.RegularCargoCount;
                line.RefrigeratedCargoCount = need.RefrigeratedCargoCount;
                line.Tooltip = need.Label + " " + need.AvailableCount.ToString() +
                    "/" + need.RequiredCount.ToString();
                recipe.IngredientLines.Add(line);
            }
        }
    }
}
