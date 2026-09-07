using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    internal sealed class ShuttleAssemblyConstructionCompletionService
    {
        private readonly ShuttleConstructionResourceBroker resourceBroker;

        internal ShuttleAssemblyConstructionCompletionService(ShuttleConstructionResourceBroker resourceBroker)
        {
            this.resourceBroker = resourceBroker ?? new ShuttleConstructionResourceBroker();
        }

        internal ShuttleCommandResult TryComplete(
            ShuttleCommandContext context,
            ShuttleAssemblyConstructionState state)
        {
            ShuttleAssemblyConstructionOrder order = state.ActiveOrder;
            if (order == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_NoActiveComplete".Translate().ToString());
            }

            if (!ShuttleConstructionMaterialUtility.HasAllRequiredMaterials(order, state.StagedIngredients))
            {
                order.MarkAwaitingMaterials();
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_AssemblyConstruction_MaterialsMissingCannotComplete".Translate().ToString());
            }

            string failureReason;
            if (ShuttleAssemblyConstructionUtility.HasActiveRemovalOrder(context, out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            List<ThingDefCountClass> replacementRefunds;
            if (!this.TryBuildReplacementRefundList(
                context,
                order,
                out replacementRefunds,
                out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!this.CanReturnReplacementRefunds(context, replacementRefunds, out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            order.MarkCompleting();
            ShuttleCommandResult result = this.ExecuteFinalCommand(context, order);
            if (result != null && result.Success)
            {
                ShuttleAssemblyConstructionEnrouteUtility.InterruptEnrouteHaulers(
                    context != null ? context.Host : null);
                this.resourceBroker.TryDestroyStaged(state.StagedIngredients);
                bool returnedReplacementRefunds = this.TryReturnReplacementRefunds(
                    context,
                    state,
                    replacementRefunds,
                    out failureReason);

                string completedMessage = this.BuildConstructionCompletedMessage(order);
                if (!returnedReplacementRefunds && !string.IsNullOrEmpty(failureReason))
                {
                    completedMessage = completedMessage + " " + failureReason;
                }

                this.NotifyConstructionCompleted(context, completedMessage);
                state.AdvanceToNextOrder();
                if (state.HasActiveOrder && state.ActiveOrder != null)
                {
                    ShuttleConstructionMaterialUtility.RefreshMaterialState(
                        state.ActiveOrder,
                        state.StagedIngredients);
                }
                return ShuttleCommandResult.Succeeded(completedMessage, true);
            }

            string failure = result != null
                ? result.Message
                : "CT_Shuttle_AssemblyConstruction_FinalValidationFailed".Translate().ToString();
            state.RecordFailure(failure);
            return ShuttleCommandResult.Failed(failure);
        }

        private ShuttleCommandResult ExecuteFinalCommand(
            ShuttleCommandContext context,
            ShuttleAssemblyConstructionOrder order)
        {
            if (context == null || context.CommandExecutor == null || order == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString());
            }

            if (order.Kind == ShuttleAssemblyConstructionKind.Segment)
            {
                return context.CommandExecutor.Execute(new InstallSegmentCommand(order.SegmentSlotID, order.TargetDefName));
            }

            if (order.Kind == ShuttleAssemblyConstructionKind.SegmentReplacement)
            {
                return context.CommandExecutor.Execute(new ReplaceSegmentCommand(order.SegmentSlotID, order.TargetDefName));
            }

            if (order.Kind == ShuttleAssemblyConstructionKind.Module)
            {
                return context.CommandExecutor.Execute(new InstallModuleCommand(
                    order.SegmentInstanceID,
                    order.ModuleSlotID,
                    order.TargetDefName,
                    order.SelectedStuffDefName));
            }

            if (order.Kind == ShuttleAssemblyConstructionKind.ModuleReplacement)
            {
                return context.CommandExecutor.Execute(new ReplaceModuleCommand(
                    order.SegmentInstanceID,
                    order.ModuleSlotID,
                    order.TargetDefName,
                    order.SelectedStuffDefName));
            }

            return ShuttleCommandResult.Failed(
                "CT_Shuttle_AssemblyConstruction_UnknownOrderType".Translate().ToString());
        }

        private string BuildConstructionCompletedMessage(ShuttleAssemblyConstructionOrder order)
        {
            string targetLabel = order != null && !string.IsNullOrEmpty(order.TargetLabel)
                ? order.TargetLabel
                : "-";
            if (order != null && order.Kind == ShuttleAssemblyConstructionKind.SegmentReplacement)
            {
                return "CT_Shuttle_AssemblyConstruction_CompleteSegmentReplacement".Translate(targetLabel).ToString();
            }

            if (order != null && order.Kind == ShuttleAssemblyConstructionKind.Segment)
            {
                return "CT_Shuttle_AssemblyConstruction_CompleteSegment".Translate(targetLabel).ToString();
            }

            if (order != null && order.Kind == ShuttleAssemblyConstructionKind.ModuleReplacement)
            {
                return "CT_Shuttle_AssemblyConstruction_CompleteModuleReplacement".Translate(targetLabel).ToString();
            }

            return "CT_Shuttle_AssemblyConstruction_CompleteModule".Translate(targetLabel).ToString();
        }

        private bool TryBuildReplacementRefundList(
            ShuttleCommandContext context,
            ShuttleAssemblyConstructionOrder order,
            out List<ThingDefCountClass> refunds,
            out string failureReason)
        {
            refunds = null;
            failureReason = null;
            if (order == null)
            {
                return true;
            }

            if (order.Kind == ShuttleAssemblyConstructionKind.ModuleReplacement)
            {
                ShuttleModule module = this.FindInstalledModule(
                    context,
                    order.SegmentInstanceID,
                    order.ModuleSlotID);
                if (module == null)
                {
                    failureReason = "CT_Shuttle_Command_ModuleReplaceTargetMissing".Translate().ToString();
                    return false;
                }

                refunds = ShuttleAssemblyRefundUtility.BuildModuleRefundList(module);
                return this.TryAppendRuntimeRemovalRefunds(
                    context,
                    module,
                    refunds,
                    out failureReason);
            }

            if (order.Kind == ShuttleAssemblyConstructionKind.SegmentReplacement)
            {
                ShuttleSegment segment = this.FindInstalledSegment(
                    context,
                    order.SegmentSlotID);
                if (segment == null)
                {
                    failureReason = "CT_Shuttle_Command_SegmentReplaceMissing".Translate().ToString();
                    return false;
                }

                refunds = ShuttleAssemblyRefundUtility.BuildSegmentRefundList(segment);
                return true;
            }

            return true;
        }

        private bool TryAppendRuntimeRemovalRefunds(
            ShuttleCommandContext context,
            ShuttleModule module,
            List<ThingDefCountClass> refunds,
            out string failureReason)
        {
            failureReason = null;
            if (context == null || context.ModuleRuntimeCoordinator == null || module == null)
            {
                return true;
            }

            List<ThingDefCountClass> runtimeRefunds;
            if (!context.ModuleRuntimeCoordinator.TryCollectModuleRemovalRefunds(
                    context.Host,
                    context.AssemblyState,
                    context.GetProfileForRead(),
                    context.GetRuntimeState(),
                    module,
                    context.StoredEnergySink,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    out runtimeRefunds,
                    out failureReason))
            {
                return false;
            }

            if (refunds != null && runtimeRefunds != null && runtimeRefunds.Count > 0)
            {
                refunds.AddRange(runtimeRefunds);
            }

            return true;
        }

        private bool CanReturnReplacementRefunds(
            ShuttleCommandContext context,
            IReadOnlyList<ThingDefCountClass> replacementRefunds,
            out string failureReason)
        {
            failureReason = null;
            if (replacementRefunds == null || replacementRefunds.Count == 0)
            {
                return true;
            }

            ThingWithComps host = context != null ? context.Host : null;
            if (host == null || host.Map == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_ReturnMapUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool TryReturnReplacementRefunds(
            ShuttleCommandContext context,
            ShuttleAssemblyConstructionState state,
            IReadOnlyList<ThingDefCountClass> replacementRefunds,
            out string failureReason)
        {
            failureReason = null;
            if (replacementRefunds == null || replacementRefunds.Count == 0)
            {
                return true;
            }

            if (state == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_ReplacementRefundFailed".Translate().ToString();
                return false;
            }

            if (!ShuttleAssemblyRefundUtility.TryCreateRefundThings(
                state.StagedIngredients,
                replacementRefunds,
                "CT_Shuttle_AssemblyConstruction_ReplacementRefundFailed",
                out failureReason))
            {
                return false;
            }

            if (this.resourceBroker.TryReturnToMap(
                context != null ? context.Host : null,
                state.StagedIngredients,
                out failureReason))
            {
                return true;
            }

            if (this.TryForceReturnHeldRefunds(
                context != null ? context.Host : null,
                state.StagedIngredients,
                out failureReason))
            {
                return true;
            }

            failureReason = !string.IsNullOrEmpty(failureReason)
                ? failureReason
                : "CT_Shuttle_AssemblyConstruction_ReplacementRefundHeld".Translate().ToString();
            return false;
        }

        private bool TryForceReturnHeldRefunds(
            ThingWithComps host,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            failureReason = null;
            if (source == null || source.Count == 0)
            {
                return true;
            }

            Map map = host != null ? host.Map : null;
            if (map == null)
            {
                failureReason = "CT_Shuttle_AssemblyConstruction_ReturnMapUnavailable".Translate().ToString();
                return false;
            }

            IntVec3 cell = host != null && host.Spawned
                ? host.Position
                : CellFinder.RandomCell(map);

            while (source.Count > 0)
            {
                Thing thing = source[0];
                source.Remove(thing);
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (!GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near))
                {
                    source.TryAddOrTransfer(thing, false);
                    failureReason = "CT_Shuttle_AssemblyConstruction_ReturnMaterialsPartialFailure"
                        .Translate()
                        .ToString();
                    return false;
                }
            }

            return true;
        }

        private ShuttleModule FindInstalledModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID)
        {
            ShuttleAssemblyState assemblyState = context != null ? context.AssemblyState : null;
            if (assemblyState == null)
            {
                return null;
            }

            ShuttleModuleSlot slot = assemblyState.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                return null;
            }

            return assemblyState.GetModule(slot.InstalledModuleInstanceID);
        }

        private ShuttleSegment FindInstalledSegment(
            ShuttleCommandContext context,
            string segmentSlotID)
        {
            ShuttleAssemblyState assemblyState = context != null ? context.AssemblyState : null;
            if (assemblyState == null)
            {
                return null;
            }

            ShuttleSegmentSlot slot = assemblyState.GetSegmentSlot(segmentSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
            {
                return null;
            }

            return assemblyState.GetSegment(slot.InstalledSegmentInstanceID);
        }

        private void NotifyConstructionCompleted(
            ShuttleCommandContext context,
            string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            ThingWithComps host = context != null ? context.Host : null;
            if (host != null && host.Spawned && host.Map != null)
            {
                TargetInfo target = new TargetInfo(host.Position, host.Map, false);
                Messages.Message(
                    message,
                    target,
                    MessageTypeDefOf.PositiveEvent,
                    false);
                this.TryPlayConstructionCompleteSound(target);
                return;
            }

            Messages.Message(
                message,
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private void TryPlayConstructionCompleteSound(TargetInfo target)
        {
            SoundDef sound = SoundDefOf.Building_Complete;
            if (sound == null || sound.sustain)
            {
                return;
            }

            sound.PlayOneShot(target);
        }
    }
}
