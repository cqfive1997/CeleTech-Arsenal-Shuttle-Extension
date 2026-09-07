using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableIngredientStager
    {
        private readonly AutoWorkTableIngredientResolver ingredientResolver;

        internal AutoWorkTableIngredientStager(
            AutoWorkTableIngredientResolver ingredientResolver)
        {
            this.ingredientResolver = ingredientResolver;
        }

        internal bool TryStageIngredients(
            ShuttleModuleRuntimeContext context,
            AutoWorkTableRecipeCatalogEntry entry,
            AutoWorkTableProductionOrderState order,
            AutoWorkTableRuntimeState state,
            out string failureReason)
        {
            ShuttleCargoInventorySnapshot inventorySnapshot =
                context.CargoResourceBroker.GetInventorySnapshot();
            AutoWorkTableIngredientAvailability ingredientAvailability;
            if (!this.ingredientResolver.TryResolveAvailability(
                entry.RecipeDef,
                inventorySnapshot,
                order != null && order.HasCustomIngredientFilter
                    ? order.CustomIngredientFilterForRead
                    : null,
                out ingredientAvailability))
            {
                state.SetIngredientAvailability(ingredientAvailability);
                failureReason = ingredientAvailability != null
                    ? ingredientAvailability.FailureReason
                    : "AutoWorkTable ingredient scan failed.";
                return false;
            }

            state.SetIngredientAvailability(ingredientAvailability);
            List<CargoIngredientRequirement> requirements =
                ingredientAvailability != null
                    ? ingredientAvailability.Requirements
                    : new List<CargoIngredientRequirement>();
            if (requirements.Count > 0)
            {
                int movedCount;
                // This is the start of the two-phase ingredient transaction: cargo is moved into
                // the module-owned stagedIngredients holder before active production is persisted.
                if (!context.CargoResourceBroker.TryTakeBatchToOwner(
                    requirements,
                    state.StagedIngredients,
                    "AutoWorkTable ingredients",
                    out movedCount,
                    out failureReason))
                {
                    context.CargoResourceBroker.InvalidateInventorySnapshot();
                    return false;
                }
            }

            failureReason = null;
            return true;
        }

        internal bool TryRecoverStagedIngredientsToCargo(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || !state.HasStagedIngredients)
            {
                return true;
            }

            if (context == null || context.CargoResourceBroker == null)
            {
                failureReason = "Cargo broker is unavailable for staged ingredient recovery.";
                return false;
            }

            if (!this.TryValidateCargoAccess(context, moduleDef, out failureReason))
            {
                return false;
            }

            int depositedCount;
            if (!context.CargoResourceBroker.TryDepositFrom(
                state.StagedIngredients,
                "AutoWorkTable staged ingredient recovery",
                out depositedCount,
                out failureReason))
            {
                return false;
            }

            if (state.HasStagedIngredients)
            {
                failureReason = "Staged ingredient recovery did not clear staged ingredients.";
                return false;
            }

            state.ClearActiveProduction();
            return true;
        }

        private bool TryValidateCargoAccess(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            out string failureReason)
        {
            failureReason = null;
            if (context == null)
            {
                failureReason = "AutoWorkTable runtime context is unavailable.";
                return false;
            }

            if (moduleDef.requirePoweredInternalBus && !context.InternalBusPowered)
            {
                failureReason = "Internal power bus is offline.";
                return false;
            }

            if (context.CargoResourceBroker == null || !context.CargoResourceBroker.IsAvailable)
            {
                failureReason = "Cargo broker is not available.";
                return false;
            }

            if (!moduleDef.requiresCargoLogistics)
            {
                return true;
            }

            if (context.Profile == null || context.Profile.CargoLogistics == null)
            {
                failureReason = "Cargo logistics profile is unavailable.";
                return false;
            }

            if (!context.Profile.CargoLogistics.HasCargoLogistics)
            {
                failureReason = "Cargo logistics module is missing.";
                return false;
            }

            if (!context.Profile.CargoLogistics.SupportsItemConsumption)
            {
                failureReason = "Cargo logistics does not support item consumption.";
                return false;
            }

            if (!context.Profile.CargoLogistics.SupportsItemDeposit)
            {
                failureReason = "Cargo logistics does not support item deposit.";
                return false;
            }

            return true;
        }
    }
}
