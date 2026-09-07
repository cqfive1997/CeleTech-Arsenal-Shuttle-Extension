using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyMutation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal sealed class ShuttleAssemblyConstructionOrderStarter
    {
        private readonly ShuttleConstructionResourceBroker resourceBroker;
        private readonly ShuttleAssemblyMutationGuard mutationGuard = new ShuttleAssemblyMutationGuard();

        internal ShuttleAssemblyConstructionOrderStarter(ShuttleConstructionResourceBroker resourceBroker)
        {
            this.resourceBroker = resourceBroker ?? new ShuttleConstructionResourceBroker();
        }

        internal ShuttleCommandResult BeginSegmentConstruction(
            ShuttleCommandContext context,
            string segmentSlotID,
            string segmentDefName)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(context, out ShuttleAssemblyConstructionState state, out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (ShuttleAssemblyConstructionUtility.HasActiveRemovalOrder(context, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleSegmentBaseDef segmentDef = DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(segmentDefName);
            if (segmentDef == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString());
            }

            if (!segmentDef.playerInstallable)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleDefNotInstallable".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentResearchLocked".Translate().ToString());
            }

            if (!this.mutationGuard.CanInstallSegment(
                context,
                segmentSlotID,
                segmentDef,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return this.TryStartOrder(
                state,
                ShuttleAssemblyConstructionKind.Segment,
                segmentSlotID,
                null,
                null,
                segmentDef.defName,
                null,
                "CT_Shuttle_AssemblyConstruction_OrderLabel".Translate().ToString(),
                ShuttleAssemblyConstructionUtility.GetDefLabel(segmentDef),
                ShuttleConstructionCostUtility.GetSegmentConstructionWorkTicks(segmentDef),
                ShuttleConstructionCostUtility.GetSegmentConstructionCost(segmentDef));
        }

        internal ShuttleCommandResult BeginSegmentReplacementConstruction(
            ShuttleCommandContext context,
            string segmentSlotID,
            string segmentDefName)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(context, out ShuttleAssemblyConstructionState state, out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (ShuttleAssemblyConstructionUtility.HasActiveRemovalOrder(context, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleSegmentBaseDef segmentDef = DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(segmentDefName);
            if (segmentDef == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentReplaceMissingDef".Translate().ToString());
            }

            if (!segmentDef.playerInstallable)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleDefNotInstallable".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(segmentDef))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SegmentResearchLocked".Translate().ToString());
            }

            if (!this.mutationGuard.CanReplaceSegment(
                context,
                segmentSlotID,
                segmentDef,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return this.TryStartOrder(
                state,
                ShuttleAssemblyConstructionKind.SegmentReplacement,
                segmentSlotID,
                null,
                null,
                segmentDef.defName,
                null,
                "CT_Shuttle_AssemblyConstruction_OrderLabel".Translate().ToString(),
                ShuttleAssemblyConstructionUtility.GetDefLabel(segmentDef),
                ShuttleConstructionCostUtility.GetSegmentConstructionWorkTicks(segmentDef),
                ShuttleConstructionCostUtility.GetSegmentConstructionCost(segmentDef));
        }

        internal ShuttleCommandResult BeginModuleConstruction(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName)
        {
            return this.BeginModuleConstructionInternal(
                context,
                ShuttleAssemblyConstructionKind.Module,
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
            string selectedStuffDefName)
        {
            return this.BeginModuleConstructionInternal(
                context,
                ShuttleAssemblyConstructionKind.ModuleReplacement,
                segmentInstanceID,
                moduleSlotID,
                moduleDefName,
                selectedStuffDefName);
        }

        private ShuttleCommandResult BeginModuleConstructionInternal(
            ShuttleCommandContext context,
            ShuttleAssemblyConstructionKind kind,
            string segmentInstanceID,
            string moduleSlotID,
            string moduleDefName,
            string selectedStuffDefName)
        {
            if (!ShuttleAssemblyConstructionUtility.TryGetState(context, out ShuttleAssemblyConstructionState state, out string failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (ShuttleAssemblyConstructionUtility.HasActiveRemovalOrder(context, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModuleBaseDef moduleDef = DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(moduleDefName);
            if (moduleDef == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleInstallMissingDef".Translate().ToString());
            }

            if (!moduleDef.playerInstallable)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleDefNotInstallable".Translate().ToString());
            }

            if (!ShuttleResearchGateUtility.IsUnlocked(moduleDef))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleResearchLocked".Translate().ToString());
            }

            bool replacing = kind == ShuttleAssemblyConstructionKind.ModuleReplacement;
            bool canMutate = replacing
                ? this.mutationGuard.CanReplaceModule(
                    context,
                    segmentInstanceID,
                    moduleSlotID,
                    moduleDef,
                    ShuttleTickUtility.TicksGameOrMinusOne(),
                    out failureReason)
                : this.mutationGuard.CanInstallModule(
                    context,
                    segmentInstanceID,
                    moduleSlotID,
                    moduleDef,
                    false,
                    out failureReason);
            if (!canMutate)
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            string resolvedSelectedStuffDefName = selectedStuffDefName;
            List<ThingDefCountClass> costList;
            ShuttleHullPlatingModuleDef hullDef = moduleDef as ShuttleHullPlatingModuleDef;
            if (hullDef != null)
            {
                ThingDef selectedStuff =
                    ShuttleHullArmorStuffUtility.ResolveSelectedStuffOrFallback(selectedStuffDefName);
                resolvedSelectedStuffDefName = selectedStuff != null ? selectedStuff.defName : null;
                if (hullDef.stuffCostCount > 0 && selectedStuff == null)
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_AssemblyConstruction_NoValidHullArmorMaterial".Translate().ToString());
                }

                costList = ShuttleConstructionCostUtility.GetModuleConstructionCost(moduleDef, selectedStuff);
            }
            else
            {
                costList = ShuttleConstructionCostUtility.GetModuleConstructionCost(moduleDef);
            }

            return this.TryStartOrder(
                state,
                kind,
                FindSegmentSlotID(context.AssemblyState, segmentInstanceID),
                segmentInstanceID,
                moduleSlotID,
                moduleDef.defName,
                resolvedSelectedStuffDefName,
                "CT_Shuttle_AssemblyConstruction_OrderLabel".Translate().ToString(),
                ShuttleAssemblyConstructionUtility.GetDefLabel(moduleDef),
                ShuttleConstructionCostUtility.GetModuleConstructionWorkTicks(moduleDef),
                costList);
        }

        private static string FindSegmentSlotID(
            ShuttleAssemblyState assemblyState,
            string segmentInstanceID)
        {
            if (assemblyState == null ||
                assemblyState.SegmentSlots == null ||
                string.IsNullOrEmpty(segmentInstanceID))
            {
                return null;
            }

            for (int i = 0; i < assemblyState.SegmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = assemblyState.SegmentSlots[i];
                if (slot != null && slot.InstalledSegmentInstanceID == segmentInstanceID)
                {
                    return slot.SlotID;
                }
            }

            return null;
        }

        private ShuttleCommandResult TryStartOrder(
            ShuttleAssemblyConstructionState state,
            ShuttleAssemblyConstructionKind kind,
            string segmentSlotID,
            string segmentInstanceID,
            string moduleSlotID,
            string targetDefName,
            string selectedStuffDefName,
            string label,
            string targetLabel,
            int workTicks,
            List<ThingDefCountClass> costList)
        {
            List<ThingDefCountClass> requiredCosts =
                ShuttleConstructionCostUtility.CloneValidCostList(costList);

            ShuttleAssemblyConstructionOrder order = new ShuttleAssemblyConstructionOrder(
                state.AllocateOrderID(),
                kind,
                segmentSlotID,
                segmentInstanceID,
                moduleSlotID,
                targetDefName,
                selectedStuffDefName,
                label,
                targetLabel,
                Mathf.Max(0, workTicks),
                this.resourceBroker.BuildCostSummary(requiredCosts),
                requiredCosts);
            string failureReason;
            if (!ShuttleAssemblyConstructionQueuePolicy.CanEnqueue(state, order, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            bool becameActive = state.EnqueueOrder(order);
            if (becameActive)
            {
                ShuttleConstructionMaterialUtility.RefreshMaterialState(order, state.StagedIngredients);
            }

            return ShuttleCommandResult.Succeeded(
                (becameActive
                    ? "CT_Shuttle_AssemblyConstruction_OrderStarted"
                    : "CT_Shuttle_AssemblyConstruction_OrderQueued")
                    .Translate(targetLabel)
                    .ToString(),
                true);
        }
    }
}
