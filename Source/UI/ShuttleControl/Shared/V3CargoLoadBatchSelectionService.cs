using System.Collections.Generic;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadBatchResult
    {
        internal bool Success = true;
        internal bool Changed;
        internal bool RolledBack;
        internal int TargetEntryCount;
        internal int ChangedEntryCount;
        internal int LimitedEntryCount;
    }

    /// <summary>
    /// Owns multi-row edits to the dialog's transient TransferableOneWay selection intent.
    /// It never writes cargo holders, transporter queues, or durable shuttle state.
    /// </summary>
    internal sealed class V3CargoLoadBatchSelectionService
    {
        private readonly V3CargoLoadSelectionModel selectionModel;
        private Dictionary<TransferableOneWay, int> undoCounts;

        internal V3CargoLoadBatchSelectionService(
            V3CargoLoadSelectionModel selectionModel)
        {
            this.selectionModel = selectionModel;
        }

        internal bool CanUndo
        {
            get { return this.undoCounts != null && this.undoCounts.Count > 0; }
        }

        internal bool CanClearAll
        {
            get { return this.selectionModel != null && this.selectionModel.SelectedCount() > 0; }
        }

        internal void ClearUndo()
        {
            this.undoCounts = null;
        }

        internal V3CargoLoadBatchResult SelectAll(
            IList<TransferableOneWay> targets)
        {
            List<TransferableOneWay> scope = NormalizeTargets(targets);
            V3CargoLoadBatchResult result = NewResult(scope.Count);
            if (this.selectionModel == null || scope.Count == 0)
            {
                return result;
            }

            Dictionary<TransferableOneWay, int> before =
                this.selectionModel.CaptureSelectionCounts();
            for (int i = 0; i < scope.Count; i++)
            {
                TransferableOneWay transferable = scope[i];
                int current = transferable.CountToTransfer;
                int applied;
                if (!this.selectionModel.TrySetSelectionCountExact(
                        transferable,
                        this.selectionModel.GetMaxCount(transferable),
                        out applied))
                {
                    this.selectionModel.RestoreSelectionCounts(before);
                    result.Success = false;
                    result.RolledBack = true;
                    return result;
                }

                if (applied != current)
                {
                    result.ChangedEntryCount++;
                }
            }

            this.CompleteSuccessfulEdit(result, before);
            return result;
        }

        internal V3CargoLoadBatchResult FitToCapacity(
            IList<TransferableOneWay> targets)
        {
            List<TransferableOneWay> scope = NormalizeTargets(targets);
            V3CargoLoadBatchResult result = NewResult(scope.Count);
            if (this.selectionModel == null || scope.Count == 0)
            {
                return result;
            }

            Dictionary<TransferableOneWay, int> before =
                this.selectionModel.CaptureSelectionCounts();
            for (int i = 0; i < scope.Count; i++)
            {
                TransferableOneWay transferable = scope[i];
                int current = transferable.CountToTransfer;
                int maximum = this.selectionModel.GetMaxCount(transferable);
                int applied = this.selectionModel.SelectMaximumFit(transferable);
                if (applied != current)
                {
                    result.ChangedEntryCount++;
                }

                if (applied < maximum)
                {
                    result.LimitedEntryCount++;
                }
            }

            this.CompleteSuccessfulEdit(result, before);
            return result;
        }

        internal V3CargoLoadBatchResult SetEach(
            IList<TransferableOneWay> targets,
            int requestedCount)
        {
            List<TransferableOneWay> scope = NormalizeTargets(targets);
            V3CargoLoadBatchResult result = NewResult(scope.Count);
            if (this.selectionModel == null || scope.Count == 0)
            {
                return result;
            }

            int safeRequestedCount = requestedCount > 0 ? requestedCount : 1;
            Dictionary<TransferableOneWay, int> before =
                this.selectionModel.CaptureSelectionCounts();

            // Release capacity first so list order does not prevent earlier rows from
            // benefiting when other targets are being reduced to the requested count.
            for (int i = 0; i < scope.Count; i++)
            {
                TransferableOneWay transferable = scope[i];
                int desired = safeRequestedCount < this.selectionModel.GetMaxCount(transferable)
                    ? safeRequestedCount
                    : this.selectionModel.GetMaxCount(transferable);
                if (transferable.CountToTransfer > desired)
                {
                    int ignored;
                    this.selectionModel.TrySetSelectionCountExact(
                        transferable,
                        desired,
                        out ignored);
                }
            }

            for (int i = 0; i < scope.Count; i++)
            {
                TransferableOneWay transferable = scope[i];
                int original = 0;
                before.TryGetValue(transferable, out original);
                int desired = safeRequestedCount < this.selectionModel.GetMaxCount(transferable)
                    ? safeRequestedCount
                    : this.selectionModel.GetMaxCount(transferable);
                int applied = this.selectionModel.SetSelectionCountToFit(
                    transferable,
                    desired);
                if (applied != original)
                {
                    result.ChangedEntryCount++;
                }

                if (applied < desired)
                {
                    result.LimitedEntryCount++;
                }
            }

            this.CompleteSuccessfulEdit(result, before);
            return result;
        }

        internal V3CargoLoadBatchResult ClearCurrent(
            IList<TransferableOneWay> targets)
        {
            List<TransferableOneWay> scope = NormalizeTargets(targets);
            V3CargoLoadBatchResult result = NewResult(scope.Count);
            if (this.selectionModel == null || scope.Count == 0)
            {
                return result;
            }

            Dictionary<TransferableOneWay, int> before =
                this.selectionModel.CaptureSelectionCounts();
            for (int i = 0; i < scope.Count; i++)
            {
                TransferableOneWay transferable = scope[i];
                if (transferable.CountToTransfer <= 0)
                {
                    continue;
                }

                int ignored;
                this.selectionModel.TrySetSelectionCountExact(
                    transferable,
                    0,
                    out ignored);
                result.ChangedEntryCount++;
            }

            this.CompleteSuccessfulEdit(result, before);
            return result;
        }

        internal V3CargoLoadBatchResult ClearAll()
        {
            V3CargoLoadBatchResult result = NewResult(
                this.selectionModel != null
                    ? this.selectionModel.SelectedEntryCount()
                    : 0);
            if (this.selectionModel == null || this.selectionModel.SelectedCount() <= 0)
            {
                return result;
            }

            Dictionary<TransferableOneWay, int> before =
                this.selectionModel.CaptureSelectionCounts();
            result.ChangedEntryCount = this.selectionModel.SelectedEntryCount();
            this.selectionModel.ClearAllSelections();
            this.CompleteSuccessfulEdit(result, before);
            return result;
        }

        internal V3CargoLoadBatchResult Undo()
        {
            V3CargoLoadBatchResult result = NewResult(0);
            if (this.selectionModel == null || !this.CanUndo)
            {
                return result;
            }

            Dictionary<TransferableOneWay, int> current =
                this.selectionModel.CaptureSelectionCounts();
            result.TargetEntryCount = this.undoCounts.Count;
            result.ChangedEntryCount = CountDifferences(current, this.undoCounts);
            this.selectionModel.RestoreSelectionCounts(this.undoCounts);
            this.undoCounts = null;
            result.Changed = result.ChangedEntryCount > 0;
            return result;
        }

        private void CompleteSuccessfulEdit(
            V3CargoLoadBatchResult result,
            Dictionary<TransferableOneWay, int> before)
        {
            if (result == null)
            {
                return;
            }

            result.Changed = result.ChangedEntryCount > 0;
            if (result.Changed)
            {
                this.undoCounts = before;
            }
        }

        private static V3CargoLoadBatchResult NewResult(int targetCount)
        {
            return new V3CargoLoadBatchResult
            {
                TargetEntryCount = targetCount > 0 ? targetCount : 0
            };
        }

        private static List<TransferableOneWay> NormalizeTargets(
            IList<TransferableOneWay> targets)
        {
            List<TransferableOneWay> normalized = new List<TransferableOneWay>();
            if (targets == null || targets.Count == 0)
            {
                return normalized;
            }

            HashSet<TransferableOneWay> seen = new HashSet<TransferableOneWay>();
            for (int i = 0; i < targets.Count; i++)
            {
                TransferableOneWay transferable = targets[i];
                if (transferable != null && seen.Add(transferable))
                {
                    normalized.Add(transferable);
                }
            }

            return normalized;
        }

        private static int CountDifferences(
            Dictionary<TransferableOneWay, int> left,
            Dictionary<TransferableOneWay, int> right)
        {
            if (left == null || right == null)
            {
                return 0;
            }

            int count = 0;
            foreach (KeyValuePair<TransferableOneWay, int> pair in right)
            {
                int current;
                if (!left.TryGetValue(pair.Key, out current) || current != pair.Value)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
