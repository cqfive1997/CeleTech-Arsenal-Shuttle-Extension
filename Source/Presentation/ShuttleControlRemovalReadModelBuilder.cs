using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleControlRemovalReadModelBuilder
    {
        internal void ApplySegmentRemoval(
            ShuttleControlSegmentSlotModel slotModel,
            ShuttleRuntimeState runtimeState)
        {
            if (slotModel == null ||
                runtimeState == null ||
                string.IsNullOrEmpty(slotModel.InstalledSegmentInstanceID))
            {
                return;
            }

            runtimeState.EnsureInitialized();
            ShuttleModuleRemovalState removalState = runtimeState.ModuleRemoval;
            ShuttleModuleRemovalRecord record;
            if (removalState == null ||
                !removalState.TryGetRecordForSegmentSlot(slotModel.SlotID, out record) ||
                record == null)
            {
                return;
            }

            slotModel.IsRemovalInProgress = true;
            slotModel.RemovalProgress01 = record.Progress01;
            slotModel.RemovalStatusLabel = this.GetModuleRemovalStatusLabel(record.Status);
            slotModel.RemovalLastFailureReason = record.LastFailureReason;
            slotModel.CanCancelRemoval =
                record.Status != ShuttleModuleRemovalStatus.RefundPending &&
                !removalState.HasPendingRefunds;
            slotModel.CanAssignRemovalWorker =
                record.Status != ShuttleModuleRemovalStatus.RefundPending &&
                record.WorkTicksDone < record.WorkTicksRequired;
            slotModel.RemovalTooltip =
                "CT_Shuttle_ModuleRemoval_Tooltip".Translate(
                    slotModel.RemovalStatusLabel,
                    Mathf.RoundToInt(slotModel.RemovalProgress01 * 100f).ToString())
                    .ToString();
            if (!string.IsNullOrEmpty(slotModel.RemovalLastFailureReason))
            {
                slotModel.RemovalTooltip += "\n" + slotModel.RemovalLastFailureReason;
            }
        }

        internal void ApplyModuleRemoval(
            ShuttleControlModuleSlotModel slotModel,
            ShuttleRuntimeState runtimeState)
        {
            if (slotModel == null ||
                runtimeState == null ||
                string.IsNullOrEmpty(slotModel.InstalledModuleInstanceID))
            {
                return;
            }

            runtimeState.EnsureInitialized();
            ShuttleModuleRemovalState removalState = runtimeState.ModuleRemoval;
            ShuttleModuleRemovalRecord record;
            if (removalState == null ||
                !removalState.TryGetRecordForModule(slotModel.InstalledModuleInstanceID, out record) ||
                record == null)
            {
                return;
            }

            slotModel.IsRemovalInProgress = true;
            slotModel.RemovalProgress01 = record.Progress01;
            slotModel.RemovalStatusLabel = this.GetModuleRemovalStatusLabel(record.Status);
            slotModel.RemovalLastFailureReason = record.LastFailureReason;
            slotModel.CanCancelRemoval =
                record.Status != ShuttleModuleRemovalStatus.RefundPending &&
                !removalState.HasPendingRefunds;
            slotModel.CanAssignRemovalWorker =
                record.Status != ShuttleModuleRemovalStatus.RefundPending &&
                record.WorkTicksDone < record.WorkTicksRequired;
            slotModel.RemovalTooltip =
                "CT_Shuttle_ModuleRemoval_Tooltip".Translate(
                    slotModel.RemovalStatusLabel,
                    Mathf.RoundToInt(slotModel.RemovalProgress01 * 100f).ToString())
                    .ToString();
            if (!string.IsNullOrEmpty(slotModel.RemovalLastFailureReason))
            {
                slotModel.RemovalTooltip += "\n" + slotModel.RemovalLastFailureReason;
            }
        }

        private string GetModuleRemovalStatusLabel(ShuttleModuleRemovalStatus status)
        {
            if (status == ShuttleModuleRemovalStatus.Blocked)
            {
                return "CT_Shuttle_ModuleRemoval_Status_Blocked".Translate().ToString();
            }

            if (status == ShuttleModuleRemovalStatus.RefundPending)
            {
                return "CT_Shuttle_ModuleRemoval_Status_RefundPending".Translate().ToString();
            }

            if (status == ShuttleModuleRemovalStatus.WaitingForWorker)
            {
                return "CT_Shuttle_ModuleRemoval_Status_WaitingForWorker".Translate().ToString();
            }

            return "CT_Shuttle_ModuleRemoval_Status_Working".Translate().ToString();
        }
    }
}
