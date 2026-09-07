using System;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableProductionPolicyEvaluator
    {
        internal bool SelectedRecipeSupportsDoUntilStock(
            AutoWorkTableRecipeCatalog catalog,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableRecipeCatalogEntry entry;
            if (!catalog.TryResolveEntry(
                moduleDef,
                state != null ? state.SelectedSourceBenchDefName : null,
                state != null ? state.SelectedRecipeDefName : null,
                out entry,
                out failureReason))
            {
                return false;
            }

            if (this.RecipeRejectsDoUntilStock(entry != null ? entry.RecipeDef : null))
            {
                failureReason = this.GetDoUntilStockUnsupportedReason(entry != null ? entry.RecipeDef : null);
                return false;
            }

            return true;
        }

        internal bool RecipeRejectsDoUntilStock(RecipeDef recipeDef)
        {
            return AutoWorkTableMechanoidDisassemblyUtility.RejectsDoUntilStock(recipeDef) ||
                AutoWorkTableAnimalButcheryUtility.RejectsDoUntilStock(recipeDef);
        }

        internal string GetDoUntilStockUnsupportedReason(RecipeDef recipeDef)
        {
            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return "CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedAnimalButchery".Translate().ToString();
            }

            return "CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts".Translate().ToString();
        }

        internal bool TryGetPrimaryProduct(
            AutoWorkTableRecipeCatalogEntry entry,
            out ThingDef targetDef,
            out string failureReason)
        {
            targetDef = null;
            failureReason = null;
            if (entry == null ||
                entry.RecipeDef == null ||
                entry.RecipeDef.products == null ||
                entry.RecipeDef.products.Count == 0 ||
                entry.RecipeDef.products[0] == null ||
                entry.RecipeDef.products[0].thingDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_DoUntilStockRequiresProduct"
                    .Translate()
                    .ToString();
                return false;
            }

            targetDef = entry.RecipeDef.products[0].thingDef;
            return true;
        }

        internal bool TryCountTargetStock(
            ShuttleModuleRuntimeContext context,
            ThingDef targetDef,
            out int stock,
            out string failureReason)
        {
            stock = 0;
            failureReason = null;
            if (targetDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_DoUntilStockTargetUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if (context == null || context.CargoResourceBroker == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_InventoryScanCargoBrokerMissing"
                    .Translate()
                    .ToString();
                return false;
            }

            ShuttleCargoInventorySnapshot snapshot = context.CargoResourceBroker.GetInventorySnapshot();
            return this.TryCountTargetStock(
                snapshot,
                targetDef,
                out stock,
                out failureReason);
        }

        internal bool TryCountTargetStock(
            ShuttleCargoInventorySnapshot snapshot,
            ThingDef targetDef,
            out int stock,
            out string failureReason)
        {
            stock = 0;
            failureReason = null;
            if (targetDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_DoUntilStockTargetUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if (snapshot == null || !snapshot.IsAvailable)
            {
                failureReason = snapshot != null && !string.IsNullOrEmpty(snapshot.UnavailableReason)
                    ? snapshot.UnavailableReason
                    : "CT_Shuttle_AutoWorkTable_InventoryScanUnavailable".Translate().ToString();
                return false;
            }

            stock = Math.Max(0, snapshot.Count(targetDef));
            return true;
        }
    }
}
