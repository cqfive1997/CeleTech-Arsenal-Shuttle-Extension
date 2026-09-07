using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyMutation;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    internal sealed class ShuttleModuleRemovalService
    {
        private const int DefaultRemovalWorkTicks = 2500;
        private const int MinimumSegmentRemovalWorkTicks = 2000;
        private const float SegmentRemovalWorkFactor = 0.4f;
        private const int WaitingForWorkerGraceTicks = 120;
        private readonly ShuttleAssemblyMutationGuard mutationGuard = new ShuttleAssemblyMutationGuard();

        internal ShuttleCommandResult StartRemoval(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            string moduleInstanceID)
        {
            ShuttleModuleRemovalState state;
            string failureReason;
            if (!this.TryGetState(context, out state, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (state.HasActiveRemoval)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalAlreadyActive".Translate().ToString());
            }

            if (this.HasActiveConstructionOrder(context))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_AssemblyConstructionAlreadyActive".Translate().ToString());
            }

            ShuttleModule module;
            if (!this.TryResolveInstalledModule(
                context,
                segmentInstanceID,
                moduleSlotID,
                moduleInstanceID,
                out module,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!this.CanRemoveModuleNow(context, module, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModuleRemovalRecord record = new ShuttleModuleRemovalRecord(
                module.ModuleInstanceID,
                module.ParentSegmentInstanceID,
                module.ParentSlotID,
                module.moduleDefName,
                this.GetTicksGameSafe(),
                ShuttleConstructionWorkTuning.ApplyToAuthoredWork(
                    DefaultRemovalWorkTicks));
            state.BeginRemoval(record);
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_ModuleRemovalStarted".Translate(this.GetModuleLabel(module)).ToString(),
                true);
        }

        internal ShuttleCommandResult StartSegmentRemoval(
            ShuttleCommandContext context,
            string segmentSlotID)
        {
            ShuttleModuleRemovalState state;
            string failureReason;
            if (!this.TryGetState(context, out state, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (state.HasActiveRemoval)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalAlreadyActive".Translate().ToString());
            }

            if (this.HasActiveConstructionOrder(context))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_AssemblyConstructionAlreadyActive".Translate().ToString());
            }

            ShuttleSegmentSlot slot;
            ShuttleSegment segment;
            if (!this.TryResolveInstalledSegment(
                context,
                segmentSlotID,
                out slot,
                out segment,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!this.CanRemoveSegmentNow(context, slot, segment, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            int workTicks = this.GetSegmentRemovalWorkTicks(segment);
            ShuttleModuleRemovalRecord record = ShuttleModuleRemovalRecord.ForSegment(
                segment.SegmentInstanceID,
                slot.SlotID,
                segment.segmentDefName,
                this.GetTicksGameSafe(),
                workTicks);
            state.BeginRemoval(record);
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_SegmentRemovalStarted".Translate(this.GetSegmentLabel(segment)).ToString(),
                true);
        }

        internal ShuttleCommandResult AssignRemovalWorker(
            ShuttleCommandContext context,
            string targetInstanceID)
        {
            ShuttleModuleRemovalState state;
            string failureReason;
            if (!this.TryGetState(context, out state, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModuleRemovalRecord record = state.ActiveRecord;
            if (record == null ||
                !record.IsActive ||
                (!string.IsNullOrEmpty(targetInstanceID) && record.TargetInstanceID != targetInstanceID && record.SlotID != targetInstanceID))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalNoneActive".Translate().ToString());
            }

            if (record.Status == ShuttleModuleRemovalStatus.RefundPending ||
                record.WorkTicksDone >= record.WorkTicksRequired)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalNoWorkAvailable".Translate().ToString());
            }

            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (host == null || map == null || !host.Spawned)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                ShuttleModuleRemovalWorkUtility.DeconstructModuleJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalJobDefMissing".Translate().ToString());
            }

            Pawn worker = this.FindAvailableRemovalWorker(map, host);
            if (worker == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalNoWorker".Translate().ToString());
            }

            Job job = JobMaker.MakeJob(jobDef, host);
            job.playerForced = true;
            if (!worker.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalWorkerUnavailable".Translate(worker.LabelShortCap).ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_ModuleRemovalWorkerAssigned".Translate(worker.LabelShortCap).ToString(),
                true);
        }

        internal ShuttleCommandResult AddRemovalWork(
            ShuttleCommandContext context,
            Pawn worker,
            int workTicks)
        {
            if (workTicks <= 0)
            {
                return ShuttleCommandResult.Succeeded(null);
            }

            ShuttleModuleRemovalState state;
            string failureReason;
            if (!this.TryGetState(context, out state, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModuleRemovalRecord record = state.ActiveRecord;
            if (record == null || !record.IsActive)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalNoneActive".Translate().ToString());
            }

            if (!this.CanWorkerApplyRemovalWork(context, worker, out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (record.Status == ShuttleModuleRemovalStatus.RefundPending)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalRefundPending".Translate().ToString());
            }

            if (record.TargetKind == ShuttleAssemblyRemovalTargetKind.Segment)
            {
                return this.AddSegmentRemovalWork(context, state, record, worker, workTicks);
            }

            return this.AddModuleRemovalWork(context, state, record, worker, workTicks);
        }

        private ShuttleCommandResult AddModuleRemovalWork(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record,
            Pawn worker,
            int workTicks)
        {
            string failureReason;
            ShuttleModule module;
            if (!this.TryResolveInstalledModule(
                context,
                record.SegmentInstanceID,
                record.SlotID,
                record.ModuleInstanceID,
                out module,
                out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!this.CanRemoveModuleNow(context, module, out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            record.AddWork(workTicks, this.GetTicksGameSafe());
            ShuttleAssemblyWorkEffectUtility.TryPlayWorkTickEffect(
                context != null ? context.Host : null,
                worker,
                ShuttleAssemblyWorkEffectKind.Remove,
                this.GetTicksGameSafe(),
                80);
            if (record.WorkTicksDone >= record.WorkTicksRequired)
            {
                bool completed = this.TryCompleteRemoval(context, state, record);
                if (!completed && !string.IsNullOrEmpty(record.LastFailureReason))
                {
                    return ShuttleCommandResult.Failed(record.LastFailureReason);
                }
            }

            return ShuttleCommandResult.Succeeded(null);
        }

        private ShuttleCommandResult AddSegmentRemovalWork(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record,
            Pawn worker,
            int workTicks)
        {
            string failureReason;
            ShuttleSegmentSlot slot;
            ShuttleSegment segment;
            if (!this.TryResolveInstalledSegment(
                context,
                record.SlotID,
                out slot,
                out segment,
                out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!this.CanRemoveSegmentNow(context, slot, segment, out failureReason))
            {
                state.RecordFailure(failureReason);
                return ShuttleCommandResult.Failed(failureReason);
            }

            record.AddWork(workTicks, this.GetTicksGameSafe());
            ShuttleAssemblyWorkEffectUtility.TryPlayWorkTickEffect(
                context != null ? context.Host : null,
                worker,
                ShuttleAssemblyWorkEffectKind.Remove,
                this.GetTicksGameSafe(),
                80);
            if (record.WorkTicksDone >= record.WorkTicksRequired)
            {
                bool completed = this.TryCompleteRemoval(context, state, record);
                if (!completed && !string.IsNullOrEmpty(record.LastFailureReason))
                {
                    return ShuttleCommandResult.Failed(record.LastFailureReason);
                }
            }

            return ShuttleCommandResult.Succeeded(null);
        }

        internal ShuttleCommandResult CancelRemoval(ShuttleCommandContext context, string targetInstanceID)
        {
            ShuttleModuleRemovalState state;
            string failureReason;
            if (!this.TryGetState(context, out state, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleModuleRemovalRecord record = state.ActiveRecord;
            if (record == null ||
                !record.IsActive ||
                (!string.IsNullOrEmpty(targetInstanceID) && record.TargetInstanceID != targetInstanceID && record.SlotID != targetInstanceID))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalNoneActive".Translate().ToString());
            }

            if (state.HasPendingRefunds || record.Status == ShuttleModuleRemovalStatus.RefundPending)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ModuleRemovalCannotCancelRefundPending".Translate().ToString());
            }

            state.ClearRemoval();
            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_ModuleRemovalCancelled".Translate().ToString(), true);
        }

        internal void Tick(ShuttleCommandContext context)
        {
            ShuttleModuleRemovalState state;
            string ignored;
            if (!this.TryGetState(context, out state, out ignored) ||
                !state.HasActiveRemoval ||
                state.ActiveRecord == null)
            {
                return;
            }

            ShuttleModuleRemovalRecord record = state.ActiveRecord;
            if (record.Status == ShuttleModuleRemovalStatus.RefundPending)
            {
                if (this.TryResolvePendingRefund(context, state, record))
                {
                    this.TryClearRemovalAfterRefundResolved(state, record);
                }

                return;
            }

            if (record.WorkTicksDone < record.WorkTicksRequired)
            {
                this.UpdateWaitingForWorkerStatus(record);
                return;
            }

            this.TryCompleteRemoval(context, state, record);
        }

        internal bool IsReadyForRemovalWork(ShuttleRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                return false;
            }

            runtimeState.EnsureInitialized();
            ShuttleModuleRemovalState state = runtimeState.ModuleRemoval;
            ShuttleModuleRemovalRecord record = state != null ? state.ActiveRecord : null;
            return record != null &&
                record.IsActive &&
                record.Status != ShuttleModuleRemovalStatus.RefundPending &&
                record.WorkTicksDone < record.WorkTicksRequired;
        }

        internal bool CanUseAssemblySlotActions(
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID)
        {
            if (runtimeState == null)
            {
                return true;
            }

            runtimeState.EnsureInitialized();
            ShuttleModuleRemovalState state = runtimeState.ModuleRemoval;
            return state == null || !state.IsRemovingModule(moduleInstanceID);
        }

        private Pawn FindAvailableRemovalWorker(Map map, ThingWithComps host)
        {
            if (map == null || host == null || map.mapPawns == null)
            {
                return null;
            }

            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists == null)
            {
                return null;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                string ignored;
                if (this.CanPawnTakeRemovalJob(pawn, host, out ignored))
                {
                    return pawn;
                }
            }

            return null;
        }

        private bool CanPawnTakeRemovalJob(Pawn pawn, ThingWithComps host, out string failureReason)
        {
            failureReason = null;
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                !pawn.Spawned ||
                pawn.Downed ||
                pawn.Drafted ||
                pawn.jobs == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalWorkerUnavailable".Translate("-").ToString();
                return false;
            }

            if (host == null || host.Map == null || !host.Spawned || pawn.Map != host.Map)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            if (!pawn.CanReserveAndReach(
                host,
                PathEndMode.InteractionCell,
                Danger.Deadly,
                ShuttleModuleRemovalWorkUtility.MaxShuttleDeconstructors,
                1,
                null,
                false))
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalWorkerUnavailable".Translate(pawn.LabelShortCap).ToString();
                return false;
            }

            return true;
        }

        private bool CanWorkerApplyRemovalWork(
            ShuttleCommandContext context,
            Pawn worker,
            out string failureReason)
        {
            failureReason = null;
            ThingWithComps host = context != null ? context.Host : null;
            if (worker == null ||
                worker.Destroyed ||
                worker.Dead ||
                !worker.Spawned ||
                worker.Downed ||
                worker.jobs == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalWorkerUnavailable".Translate("-").ToString();
                return false;
            }

            if (host == null || host.Map == null || !host.Spawned || worker.Map != host.Map)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private void UpdateWaitingForWorkerStatus(ShuttleModuleRemovalRecord record)
        {
            if (record == null || record.Status != ShuttleModuleRemovalStatus.Working)
            {
                return;
            }

            int ticksGame = this.GetTicksGameSafe();
            if (record.LastWorkTick < 0 ||
                ticksGame < 0 ||
                ticksGame - record.LastWorkTick >= WaitingForWorkerGraceTicks)
            {
                record.MarkWaitingForWorker();
            }
        }

        private bool TryCompleteRemoval(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record)
        {
            if (record != null && record.TargetKind == ShuttleAssemblyRemovalTargetKind.Segment)
            {
                return this.TryCompleteSegmentRemoval(context, state, record);
            }

            return this.TryCompleteModuleRemoval(context, state, record);
        }

        private bool TryCompleteModuleRemoval(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record)
        {
            string failureReason;
            ShuttleModule module;
            if (!this.TryResolveInstalledModule(
                context,
                record.SegmentInstanceID,
                record.SlotID,
                record.ModuleInstanceID,
                out module,
                out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            if (!this.CanRemoveModuleNow(context, module, out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            if (!this.TryPrepareRefunds(context, state, module, out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            string targetLabel = this.GetModuleLabel(module);
            ShuttleProfile profile;
            if (!this.RemoveModuleNow(context, module, out profile, out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            this.NotifyRemovalCompleted(
                context,
                this.BuildRemovalCompletedMessage(record, targetLabel));
            if (!this.TryResolvePendingRefund(context, state, record))
            {
                return true;
            }

            this.TryClearRemovalAfterRefundResolved(state, record);
            return true;
        }

        private bool TryCompleteSegmentRemoval(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record)
        {
            string failureReason;
            ShuttleSegmentSlot slot;
            ShuttleSegment segment;
            if (!this.TryResolveInstalledSegment(
                context,
                record != null ? record.SlotID : null,
                out slot,
                out segment,
                out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            if (!this.CanRemoveSegmentNow(context, slot, segment, out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            if (!this.TryPrepareRefunds(state, segment, out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            string targetLabel = this.GetSegmentLabel(segment);
            ShuttleProfile profile;
            if (!this.RemoveSegmentNow(context, slot.SlotID, out profile, out failureReason))
            {
                state.RecordFailure(failureReason);
                return false;
            }

            this.NotifyRemovalCompleted(
                context,
                this.BuildRemovalCompletedMessage(record, targetLabel));
            if (!this.TryResolvePendingRefund(context, state, record))
            {
                return true;
            }

            this.TryClearRemovalAfterRefundResolved(state, record);
            return true;
        }

        private bool TryClearRemovalAfterRefundResolved(
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record)
        {
            if (state == null)
            {
                return false;
            }

            if (!state.HasPendingRefunds)
            {
                state.ClearRemoval();
                return true;
            }

            if (record != null)
            {
                record.MarkRefundPending("CT_Shuttle_Command_ModuleRemovalRefundPending".Translate().ToString());
            }

            return false;
        }

        private bool RemoveModuleNow(
            ShuttleCommandContext context,
            ShuttleModule module,
            out ShuttleProfile profile,
            out string failureReason)
        {
            profile = null;
            failureReason = null;
            if (context == null ||
                context.AssemblyMutationController == null ||
                context.AssemblyState == null ||
                module == null)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            string segmentInstanceID = module.ParentSegmentInstanceID;
            string slotID = module.ParentSlotID;
            if (!this.mutationGuard.CanRemoveModule(
                context,
                module,
                this.GetTicksGameSafe(),
                false,
                out failureReason))
            {
                return false;
            }

            if (!context.AssemblyMutationController.RemoveModule(
                context.AssemblyState,
                segmentInstanceID,
                slotID))
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalFailed".Translate().ToString();
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            profile = context.ReconcileProfileToHost();

            if (context.ModuleRuntimeCoordinator != null)
            {
                context.ModuleRuntimeCoordinator.NotifyModuleRemoved(
                    context.Host,
                    context.AssemblyState,
                    profile,
                    context.GetRuntimeState(),
                    module,
                    context.StoredEnergySink,
                    this.GetTicksGameSafe());
            }

            return true;
        }

        private bool RemoveSegmentNow(
            ShuttleCommandContext context,
            string segmentSlotID,
            out ShuttleProfile profile,
            out string failureReason)
        {
            profile = null;
            failureReason = null;
            if (context == null ||
                context.AssemblyMutationController == null ||
                context.AssemblyState == null ||
                string.IsNullOrEmpty(segmentSlotID))
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            ShuttleSegmentSlot slot = context.AssemblyState.GetSegmentSlot(segmentSlotID);
            ShuttleSegment segment = slot != null && !string.IsNullOrEmpty(slot.InstalledSegmentInstanceID)
                ? context.AssemblyState.GetSegment(slot.InstalledSegmentInstanceID)
                : null;
            if (!this.mutationGuard.CanRemoveSegment(
                context,
                slot,
                segment,
                out failureReason))
            {
                return false;
            }

            if (!context.AssemblyMutationController.RemoveSegment(
                context.AssemblyState,
                segmentSlotID))
            {
                failureReason = "CT_Shuttle_Command_SegmentRemovalFailed".Translate().ToString();
                return false;
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            profile = context.ReconcileProfileToHost();
            return true;
        }

        private bool TryPrepareRefunds(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModule module,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || module == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalFailed".Translate().ToString();
                return false;
            }

            this.DestroyPendingRefunds(state.PendingRefunds);
            List<ThingDefCountClass> refunds =
                ShuttleAssemblyRefundUtility.BuildModuleRefundList(module);
            if (context != null && context.ModuleRuntimeCoordinator != null)
            {
                List<ThingDefCountClass> runtimeRefunds;
                if (!context.ModuleRuntimeCoordinator.TryCollectModuleRemovalRefunds(
                        context.Host,
                        context.AssemblyState,
                        context.GetProfileForRead(),
                        context.GetRuntimeState(),
                        module,
                        context.StoredEnergySink,
                        this.GetTicksGameSafe(),
                        out runtimeRefunds,
                        out failureReason))
                {
                    return false;
                }

                if (runtimeRefunds != null && runtimeRefunds.Count > 0)
                {
                    refunds.AddRange(runtimeRefunds);
                }
            }

            return ShuttleAssemblyRefundUtility.TryCreateRefundThings(
                state.PendingRefunds,
                refunds,
                "CT_Shuttle_Command_ModuleRemovalRefundFailed",
                out failureReason);
        }

        private bool TryPrepareRefunds(
            ShuttleModuleRemovalState state,
            ShuttleSegment segment,
            out string failureReason)
        {
            failureReason = null;
            if (state == null || segment == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemovalFailed".Translate().ToString();
                return false;
            }

            this.DestroyPendingRefunds(state.PendingRefunds);
            return ShuttleAssemblyRefundUtility.TryCreateRefundThings(
                state.PendingRefunds,
                ShuttleAssemblyRefundUtility.BuildSegmentRefundList(segment),
                "CT_Shuttle_Command_ModuleRemovalRefundFailed",
                out failureReason);
        }

        private string BuildRemovalCompletedMessage(
            ShuttleModuleRemovalRecord record,
            string targetLabel)
        {
            string label = !string.IsNullOrEmpty(targetLabel) ? targetLabel : "-";
            if (record != null && record.TargetKind == ShuttleAssemblyRemovalTargetKind.Segment)
            {
                return "CT_Shuttle_AssemblyRemoval_CompleteSegment".Translate(label).ToString();
            }

            return "CT_Shuttle_AssemblyRemoval_CompleteModule".Translate(label).ToString();
        }

        private void NotifyRemovalCompleted(
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
                this.TryPlayRemovalCompleteSound(target);
                return;
            }

            Messages.Message(
                message,
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private void TryPlayRemovalCompleteSound(TargetInfo target)
        {
            SoundDef sound = SoundDefOf.Building_Deconstructed;
            if (sound == null || sound.sustain)
            {
                return;
            }

            sound.PlayOneShot(target);
        }

        private bool TryResolvePendingRefund(
            ShuttleCommandContext context,
            ShuttleModuleRemovalState state,
            ShuttleModuleRemovalRecord record)
        {
            if (state == null || !state.HasPendingRefunds)
            {
                return true;
            }

            string failureReason = null;
            int depositedCount;
            IShuttleCargoResourceBroker broker = this.BuildCargoBroker(context);
            if (broker != null &&
                broker.TryDepositFrom(
                    state.PendingRefunds,
                    "module removal refund",
                    out depositedCount,
                    out failureReason))
            {
                return true;
            }

            if (this.TryDropRefundsNearShuttle(context, state.PendingRefunds, out failureReason))
            {
                return true;
            }

            string reason = failureReason ?? "CT_Shuttle_Command_ModuleRemovalRefundPending".Translate().ToString();
            if (record != null)
            {
                record.MarkRefundPending(reason);
            }

            return false;
        }

        private IShuttleCargoResourceBroker BuildCargoBroker(ShuttleCommandContext context)
        {
            if (context == null || context.CargoBackend == null)
            {
                return null;
            }

            return new ShuttleCargoResourceBroker(
                context.Host,
                context.GetProfileForRead(),
                context.GetRuntimeState(),
                context.CargoBackend);
        }

        private bool TryDropRefundsNearShuttle(
            ShuttleCommandContext context,
            ThingOwner<Thing> refunds,
            out string failureReason)
        {
            failureReason = null;
            if (refunds == null || refunds.Count == 0)
            {
                return true;
            }

            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (map == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalRefundPending".Translate().ToString();
                return false;
            }

            IntVec3 cell = host != null && host.Spawned ? host.Position : map.Center;
            bool result = refunds.TryDropAll(cell, map, ThingPlaceMode.Near);
            if (!result)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalRefundPending".Translate().ToString();
            }

            return result;
        }

        private void DestroyPendingRefunds(ThingOwner<Thing> refunds)
        {
            if (refunds == null)
            {
                return;
            }

            while (refunds.Count > 0)
            {
                Thing thing = refunds[0];
                refunds.Remove(thing);
                if (thing != null && !thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private bool CanRemoveModuleNow(
            ShuttleCommandContext context,
            ShuttleModule module,
            out string failureReason)
        {
            return this.mutationGuard.CanRemoveModule(
                context,
                module,
                this.GetTicksGameSafe(),
                false,
                out failureReason);
        }

        private bool CanRemoveSegmentNow(
            ShuttleCommandContext context,
            ShuttleSegmentSlot slot,
            ShuttleSegment segment,
            out string failureReason)
        {
            return this.mutationGuard.CanRemoveSegment(
                context,
                slot,
                segment,
                out failureReason);
        }

        private bool HasActiveConstructionOrder(ShuttleCommandContext context)
        {
            ShuttleRuntimeState runtimeState = context != null ? context.GetRuntimeState() : null;
            if (runtimeState == null)
            {
                return false;
            }

            runtimeState.EnsureInitialized();
            return runtimeState.AssemblyConstruction != null &&
                runtimeState.AssemblyConstruction.HasActiveOrder;
        }

        private bool TryResolveInstalledModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            string expectedModuleInstanceID,
            out ShuttleModule module,
            out string failureReason)
        {
            return ShuttleModuleRemovalTargetResolver.TryResolveInstalledModule(
                context,
                segmentInstanceID,
                moduleSlotID,
                expectedModuleInstanceID,
                out module,
                out failureReason);
        }

        private bool TryResolveInstalledSegment(
            ShuttleCommandContext context,
            string segmentSlotID,
            out ShuttleSegmentSlot slot,
            out ShuttleSegment segment,
            out string failureReason)
        {
            return ShuttleModuleRemovalTargetResolver.TryResolveInstalledSegment(
                context,
                segmentSlotID,
                out slot,
                out segment,
                out failureReason);
        }

        private bool TryGetState(
            ShuttleCommandContext context,
            out ShuttleModuleRemovalState state,
            out string failureReason)
        {
            state = null;
            failureReason = null;
            ShuttleRuntimeState runtimeState = context != null ? context.GetRuntimeState() : null;
            if (runtimeState == null)
            {
                failureReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            runtimeState.EnsureInitialized();
            state = runtimeState.ModuleRemoval;
            if (state == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemovalStateUnavailable".Translate().ToString();
                return false;
            }

            state.EnsureInitialized();
            return true;
        }

        private string GetModuleLabel(ShuttleModule module)
        {
            if (module == null || module.ModuleDef == null)
            {
                return "-";
            }

            return !string.IsNullOrEmpty(module.ModuleDef.label)
                ? module.ModuleDef.LabelCap.ToString()
                : module.ModuleDef.defName;
        }

        private string GetSegmentLabel(ShuttleSegment segment)
        {
            if (segment == null || segment.SegmentDef == null)
            {
                return "-";
            }

            return !string.IsNullOrEmpty(segment.SegmentDef.label)
                ? segment.SegmentDef.LabelCap.ToString()
                : segment.SegmentDef.defName;
        }

        private int GetSegmentRemovalWorkTicks(ShuttleSegment segment)
        {
            int constructionWork = ShuttleConstructionCostUtility.GetSegmentConstructionWorkTicks(
                segment != null ? segment.SegmentDef : null);
            return Mathf.Max(
                ShuttleConstructionWorkTuning.ApplyToAuthoredWork(
                    MinimumSegmentRemovalWorkTicks),
                Mathf.RoundToInt(constructionWork * SegmentRemovalWorkFactor));
        }

        private int GetTicksGameSafe()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
        }
    }
}
