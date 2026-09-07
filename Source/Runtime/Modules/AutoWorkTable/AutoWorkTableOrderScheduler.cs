using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Selects the first runnable order by player priority. Blocked orders remain in place and
    /// lower-priority orders may run, matching the useful part of vanilla bill queues without
    /// requiring pawns or map reservations inside the shuttle.
    /// </summary>
    internal sealed class AutoWorkTableOrderScheduler
    {
        private readonly AutoWorkTableRecipeCatalog catalog;
        private readonly AutoWorkTableIngredientResolver ingredientResolver;
        private readonly AutoWorkTableProductionPolicyEvaluator policyEvaluator;

        internal AutoWorkTableOrderScheduler(
            AutoWorkTableRecipeCatalog catalog,
            AutoWorkTableIngredientResolver ingredientResolver,
            AutoWorkTableProductionPolicyEvaluator policyEvaluator)
        {
            this.catalog = catalog;
            this.ingredientResolver = ingredientResolver;
            this.policyEvaluator = policyEvaluator;
        }

        internal bool TrySelectOrder(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            bool profilingEnabled,
            out AutoWorkTableProductionOrderState selectedOrder,
            out AutoWorkTableRecipeCatalogEntry selectedEntry,
            out string failureReason)
        {
            selectedOrder = null;
            selectedEntry = null;
            failureReason = null;
            IReadOnlyList<AutoWorkTableProductionOrderState> orders = state.ProductionOrders;
            AutoWorkTableRuntimeProfiler.Count(
                profilingEnabled,
                AutoWorkTableProfileCounter.QueueChecks);

            if (orders == null || orders.Count == 0)
            {
                failureReason = "No production orders are queued.";
                return false;
            }

            ShuttleCargoInventorySnapshot inventorySnapshot = null;
            AutoWorkTableStatus aggregateStatus = AutoWorkTableStatus.ProductionCompleted;
            string aggregateReason = null;
            for (int i = 0; i < orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = orders[i];
                if (order == null)
                {
                    continue;
                }

                AutoWorkTableRuntimeProfiler.Count(
                    profilingEnabled,
                    AutoWorkTableProfileCounter.OrdersConsidered);
                if (order.Suspended)
                {
                    order.SetTransientStatus(AutoWorkTableStatus.Paused);
                    aggregateStatus = PromoteAggregateStatus(aggregateStatus, AutoWorkTableStatus.Paused);
                    continue;
                }

                AutoWorkTableRecipeCatalogEntry entry;
                string reason;
                if (!this.TryValidateRecipe(moduleDef, order, out entry, out reason))
                {
                    order.SetTransientStatus(AutoWorkTableStatus.Error, reason);
                    aggregateStatus = PromoteAggregateStatus(aggregateStatus, AutoWorkTableStatus.Error);
                    aggregateReason = aggregateReason ?? reason;
                    continue;
                }

                if (!this.PolicyAllowsCycle(order, entry, ref inventorySnapshot, context, out reason))
                {
                    AutoWorkTableStatus policyStatus = order.TransientStatus;
                    aggregateStatus = PromoteAggregateStatus(aggregateStatus, policyStatus);
                    aggregateReason = aggregateReason ?? reason;
                    continue;
                }

                if (inventorySnapshot == null)
                {
                    inventorySnapshot = context.CargoResourceBroker.GetInventorySnapshot();
                    AutoWorkTableRuntimeProfiler.Count(
                        profilingEnabled,
                        AutoWorkTableProfileCounter.InventorySnapshots);
                }

                if (inventorySnapshot == null || !inventorySnapshot.IsAvailable)
                {
                    reason = inventorySnapshot != null
                        ? inventorySnapshot.UnavailableReason
                        : "Shuttle inventory is unavailable.";
                    order.SetTransientStatus(AutoWorkTableStatus.WaitingForCargo, reason);
                    aggregateStatus = PromoteAggregateStatus(
                        aggregateStatus,
                        AutoWorkTableStatus.WaitingForCargo);
                    aggregateReason = aggregateReason ?? reason;
                    continue;
                }

                long ingredientStarted = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
                AutoWorkTableIngredientAvailability availability;
                bool ingredientsAvailable = this.ingredientResolver.TryResolveAvailability(
                    entry.RecipeDef,
                    inventorySnapshot,
                    order.HasCustomIngredientFilter
                        ? order.CustomIngredientFilterForRead
                        : null,
                    out availability);
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.IngredientResolution,
                    ingredientStarted);
                order.SetIngredientAvailability(availability);
                if (!ingredientsAvailable)
                {
                    reason = availability != null ? availability.FailureReason : null;
                    order.SetTransientStatus(AutoWorkTableStatus.WaitingForIngredients, reason);
                    aggregateStatus = PromoteAggregateStatus(
                        aggregateStatus,
                        AutoWorkTableStatus.WaitingForIngredients);
                    aggregateReason = aggregateReason ?? reason;
                    continue;
                }

                order.SetTransientStatus(AutoWorkTableStatus.Idle);
                selectedOrder = order;
                selectedEntry = entry;
                AutoWorkTableRuntimeProfiler.Count(
                    profilingEnabled,
                    AutoWorkTableProfileCounter.OrdersSelected);
                return true;
            }

            state.SetAggregateStatus(aggregateStatus, aggregateReason);
            failureReason = aggregateReason;
            return false;
        }

        private bool TryValidateRecipe(
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableProductionOrderState order,
            out AutoWorkTableRecipeCatalogEntry entry,
            out string failureReason)
        {
            if (!this.catalog.TryResolveEntry(
                moduleDef,
                order.SourceBenchDefName,
                order.RecipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            if (!entry.RecipeDef.AvailableNow)
            {
                failureReason = "Recipe " + entry.RecipeDefName + " is not currently available.";
                return false;
            }

            if (entry.RecipeDef.skillRequirements == null)
            {
                return true;
            }

            for (int i = 0; i < entry.RecipeDef.skillRequirements.Count; i++)
            {
                SkillRequirement requirement = entry.RecipeDef.skillRequirements[i];
                if (requirement != null &&
                    requirement.skill != null &&
                    moduleDef.GetVirtualSkillLevel(requirement.skill) < requirement.minLevel)
                {
                    failureReason = "Virtual skill " + requirement.skill.defName +
                        " does not satisfy recipe " + entry.RecipeDefName + ".";
                    return false;
                }
            }

            return true;
        }

        private bool PolicyAllowsCycle(
            AutoWorkTableProductionOrderState order,
            AutoWorkTableRecipeCatalogEntry entry,
            ref ShuttleCargoInventorySnapshot inventorySnapshot,
            ShuttleModuleRuntimeContext context,
            out string failureReason)
        {
            failureReason = null;
            if (order.IsFiniteProductionComplete)
            {
                order.SetTransientStatus(AutoWorkTableStatus.ProductionCompleted);
                return false;
            }

            if (order.ProductionMode != AutoWorkTableProductionMode.DoUntilStock)
            {
                return true;
            }

            if (this.policyEvaluator.RecipeRejectsDoUntilStock(entry.RecipeDef))
            {
                failureReason = this.policyEvaluator.GetDoUntilStockUnsupportedReason(entry.RecipeDef);
                order.SetTransientStatus(AutoWorkTableStatus.Error, failureReason);
                return false;
            }

            ThingDef targetDef;
            if (!this.policyEvaluator.TryGetPrimaryProduct(entry, out targetDef, out failureReason))
            {
                order.SetTransientStatus(AutoWorkTableStatus.Error, failureReason);
                return false;
            }

            if (inventorySnapshot == null)
            {
                inventorySnapshot = context.CargoResourceBroker.GetInventorySnapshot();
            }

            int stock;
            if (!this.policyEvaluator.TryCountTargetStock(
                inventorySnapshot,
                targetDef,
                out stock,
                out failureReason))
            {
                order.SetTransientStatus(AutoWorkTableStatus.WaitingForCargo, failureReason);
                return false;
            }

            order.SetTargetStockCount(stock);
            if (stock >= order.TargetCount)
            {
                order.SetTransientStatus(AutoWorkTableStatus.TargetSatisfied);
                return false;
            }

            return true;
        }

        private static AutoWorkTableStatus PromoteAggregateStatus(
            AutoWorkTableStatus current,
            AutoWorkTableStatus candidate)
        {
            if (candidate == AutoWorkTableStatus.WaitingForIngredients)
            {
                return candidate;
            }

            if (current == AutoWorkTableStatus.WaitingForIngredients)
            {
                return current;
            }

            if (candidate == AutoWorkTableStatus.WaitingForCargo ||
                candidate == AutoWorkTableStatus.Error)
            {
                return candidate;
            }

            if (current == AutoWorkTableStatus.WaitingForCargo ||
                current == AutoWorkTableStatus.Error)
            {
                return current;
            }

            if (candidate == AutoWorkTableStatus.Paused)
            {
                return candidate;
            }

            if (candidate == AutoWorkTableStatus.TargetSatisfied)
            {
                return current == AutoWorkTableStatus.ProductionCompleted
                    ? candidate
                    : current;
            }

            return current;
        }
    }
}
