using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal sealed class ShuttleAssemblyConstructionSystem
    {
        private readonly ShuttleConstructionResourceBroker resourceBroker;
        private readonly ShuttleAssemblyConstructionOrderStarter orderStarter;
        private readonly ShuttleAssemblyConstructionCompletionService completionService;

        internal ShuttleAssemblyConstructionSystem()
        {
            this.resourceBroker = new ShuttleConstructionResourceBroker();
            this.orderStarter = new ShuttleAssemblyConstructionOrderStarter(this.resourceBroker);
            this.completionService = new ShuttleAssemblyConstructionCompletionService(this.resourceBroker);
        }

        internal ShuttleCommandResult BeginSegmentConstruction(
            ShuttleCommandContext context,
            string segmentSlotID,
            string segmentDefName)
        {
            return this.orderStarter.BeginSegmentConstruction(context, segmentSlotID, segmentDefName);
        }

        internal ShuttleCommandResult BeginSegmentReplacementConstruction(
            ShuttleCommandContext context,
            string segmentSlotID,
            string segmentDefName)
        {
            return this.orderStarter.BeginSegmentReplacementConstruction(context, segmentSlotID, segmentDefName);
        }

        internal ShuttleCommandResult BeginModuleConstruction(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName = null)
        {
            return this.orderStarter.BeginModuleConstruction(
                context,
                segmentInstanceID,
                moduleSlotID,
                moduleDefName,
                selectedStuffDefName);
        }

        internal ShuttleCommandResult BeginModuleReplacementConstruction(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName = null)
        {
            return this.orderStarter.BeginModuleReplacementConstruction(
                context,
                segmentInstanceID,
                moduleSlotID,
                moduleDefName,
                selectedStuffDefName);
        }

        internal ShuttleCommandResult Cancel(ShuttleCommandContext context, string orderID)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(
                context,
                out ShuttleAssemblyConstructionState state,
                out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!ShuttleAssemblyConstructionUtility.MatchesActiveOrder(state, orderID))
            {
                ShuttleAssemblyConstructionOrder removedOrder;
                if (state.TryRemoveQueuedOrder(orderID, out removedOrder))
                {
                    string targetLabel = removedOrder != null && !string.IsNullOrEmpty(removedOrder.TargetLabel)
                        ? removedOrder.TargetLabel
                        : "-";
                    return ShuttleCommandResult.Succeeded(
                        "CT_Shuttle_AssemblyConstruction_QueuedOrderCanceled"
                            .Translate(targetLabel)
                            .ToString(),
                        true);
                }

                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NoActiveCancel".Translate().ToString());
            }

            bool returned = this.resourceBroker.TryReturnToMap(
                context.Host,
                state.StagedIngredients,
                out failureReason);
            if (returned)
            {
                ShuttleAssemblyConstructionEnrouteUtility.InterruptEnrouteHaulers(
                    context.Host);
                state.AdvanceToNextOrder();
                this.RefreshActiveMaterialState(state);
                return ShuttleCommandResult.Succeeded(
                    "CT_Shuttle_AssemblyConstruction_CanceledMaterialsReturned".Translate().ToString(),
                    true);
            }

            string reason = failureReason ??
                "CT_Shuttle_AssemblyConstruction_CancelFailedMaterialsHeld".Translate().ToString();
            state.RecordFailure(reason);
            return ShuttleCommandResult.Failed(reason);
        }

        internal ShuttleCommandResult DebugSetProgress(
            ShuttleCommandContext context,
            string orderID,
            float progress01)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(
                context,
                out ShuttleAssemblyConstructionState state,
                out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!ShuttleAssemblyConstructionUtility.MatchesActiveOrder(state, orderID))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NoActiveDebug".Translate().ToString());
            }

            if (!ShuttleConstructionMaterialUtility.HasAllRequiredMaterials(
                state.ActiveOrder,
                state.StagedIngredients))
            {
                return ShuttleCommandResult.Failed("[DEV] Materials are missing; cannot set construction progress.");
            }

            state.ActiveOrder.SetProgress01(progress01);
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_AssemblyConstruction_DebugProgressSet".Translate().ToString(),
                true);
        }

        internal ShuttleCommandResult DebugComplete(ShuttleCommandContext context, string orderID)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(
                context,
                out ShuttleAssemblyConstructionState state,
                out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!ShuttleAssemblyConstructionUtility.MatchesActiveOrder(state, orderID))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NoActiveComplete".Translate().ToString());
            }

            if (ShuttleAssemblyConstructionUtility.HasActiveRemovalOrder(context, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!ShuttleConstructionMaterialUtility.HasAllRequiredMaterials(
                state.ActiveOrder,
                state.StagedIngredients))
            {
                return ShuttleCommandResult.Failed("[DEV] Materials are missing; cannot complete construction order.");
            }

            state.ActiveOrder.SetProgress01(1f);
            return this.completionService.TryComplete(context, state);
        }

        internal ShuttleCommandResult DebugFillMaterials(ShuttleCommandContext context, string orderID)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(
                context,
                out ShuttleAssemblyConstructionState state,
                out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!ShuttleAssemblyConstructionUtility.MatchesActiveOrder(state, orderID))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NoActiveDebugMaterials".Translate().ToString());
            }

            ShuttleAssemblyConstructionOrder order = state.ActiveOrder;
            if (order == null ||
                order.Status == ShuttleAssemblyConstructionStatus.Completing ||
                order.Status == ShuttleAssemblyConstructionStatus.Failed)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_DebugMaterialsWrongState".Translate().ToString());
            }

            int createdCount = 0;
            IReadOnlyList<ThingDefCountClass> costs = order.RequiredCostList;
            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass cost = costs[i];
                ThingDef thingDef = cost != null ? cost.thingDef : null;
                if (thingDef == null)
                {
                    continue;
                }

                int missing = ShuttleConstructionMaterialUtility.CountMissing(
                    order,
                    state.StagedIngredients,
                    thingDef);
                while (missing > 0)
                {
                    int count = Mathf.Min(missing, Mathf.Max(1, thingDef.stackLimit));
                    Thing thing = ThingMaker.MakeThing(thingDef, null);
                    if (thing == null)
                    {
                        return ShuttleCommandResult.Failed("[DEV] Could not create construction material: " + thingDef.defName);
                    }

                    thing.stackCount = count;
                    if (!state.StagedIngredients.TryAddOrTransfer(thing, false))
                    {
                        if (!thing.Destroyed)
                        {
                            thing.Destroy(DestroyMode.Vanish);
                        }

                        return ShuttleCommandResult.Failed("[DEV] Could not stage construction material: " + thingDef.defName);
                    }

                    createdCount += count;
                    missing -= count;
                }
            }

            ShuttleConstructionMaterialUtility.RefreshMaterialState(order, state.StagedIngredients);
            if (createdCount <= 0)
            {
                return ShuttleCommandResult.Succeeded("[DEV] Assembly construction materials were already loaded.", true);
            }

            return ShuttleCommandResult.Succeeded("[DEV] Filled assembly construction materials: " + createdCount, true);
        }

        internal void Tick(ShuttleCommandContext context)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(
                    context,
                    out ShuttleAssemblyConstructionState state,
                    out string ignored) ||
                !state.HasActiveOrder ||
                state.ActiveOrder == null)
            {
                return;
            }

            ShuttleAssemblyConstructionOrder order = state.ActiveOrder;
            if (order.Status == ShuttleAssemblyConstructionStatus.AwaitingMaterials)
            {
                ShuttleConstructionMaterialUtility.RefreshMaterialState(order, state.StagedIngredients);
            }
        }

        internal void RefreshActiveMaterialState(ShuttleAssemblyConstructionState state)
        {
            if (state == null || !state.HasActiveOrder || state.ActiveOrder == null)
            {
                return;
            }

            ShuttleConstructionMaterialUtility.RefreshMaterialState(
                state.ActiveOrder,
                state.StagedIngredients);
        }

        internal ShuttleCommandResult AddConstructionWork(
            ShuttleCommandContext context,
            Pawn worker,
            int workTicks)
        {
            if (workTicks <= 0)
            {
                return ShuttleCommandResult.Succeeded(
                    "CT_Shuttle_AssemblyConstruction_NoWorkApplied".Translate().ToString(),
                    false);
            }

            if (!ShuttleAssemblyConstructionUtility.TryGetState(
                context,
                out ShuttleAssemblyConstructionState state,
                out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!state.HasActiveOrder || state.ActiveOrder == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NoActiveWork".Translate().ToString());
            }

            ShuttleAssemblyConstructionOrder order = state.ActiveOrder;
            ShuttleConstructionMaterialUtility.RefreshMaterialState(order, state.StagedIngredients);
            if (order.Status != ShuttleAssemblyConstructionStatus.Working)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NotReadyForWork".Translate().ToString());
            }

            if (!ShuttleConstructionMaterialUtility.HasAllRequiredMaterials(order, state.StagedIngredients))
            {
                order.MarkAwaitingMaterials();
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_MaterialsMissingCannotWork".Translate().ToString());
            }

            order.AddWork(workTicks);
            ShuttleAssemblyWorkEffectUtility.TryPlayWorkTickEffect(
                context != null ? context.Host : null,
                worker,
                ShuttleAssemblyWorkEffectKind.Install,
                Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                80);
            if (order.WorkDone >= order.WorkTotal)
            {
                return this.completionService.TryComplete(context, state);
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_AssemblyConstruction_WorkApplied".Translate().ToString(),
                false);
        }
    }
}
