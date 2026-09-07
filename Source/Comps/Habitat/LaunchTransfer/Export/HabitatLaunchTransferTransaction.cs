using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatLaunchTransferTransaction
    {
        private readonly HabitatLaunchExportAccess access;
        private readonly ShuttleHolderLaunchManifest manifest;
        private readonly string transactionKind;
        private readonly List<ShuttleHolderLaunchManifestEntry> touchedEntries =
            new List<ShuttleHolderLaunchManifestEntry>();
        private readonly List<Thing> movedThings = new List<Thing>();
        private bool completed;

        internal HabitatLaunchTransferTransaction(
            HabitatLaunchExportAccess access,
            ShuttleHolderLaunchManifest manifest,
            string transactionKind)
        {
            this.access = access;
            this.manifest = manifest;
            this.transactionKind = transactionKind;
        }

        internal string TransactionKind
        {
            get { return this.transactionKind; }
        }

        internal int TouchedEntryCount
        {
            get { return this.touchedEntries.Count; }
        }

        internal int MovedThingCount
        {
            get { return this.movedThings.Count; }
        }

        internal void MarkCompleted()
        {
            this.completed = true;
        }

        internal bool TryMoveThingToDestination(
            Thing thing,
            ShuttleHolderLaunchManifestEntry entry,
            string failureMessage,
            out string failureReason)
        {
            failureReason = null;
            this.RecordTouchedEntry(entry);
            if (!this.access.TryAddOrTransferToDestination(thing))
            {
                if (entry != null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                }

                failureReason = failureMessage;
                return false;
            }

            this.RecordMovedThing(thing);
            return true;
        }

        internal bool TryMoveDiningPairToDestination(
            Pawn pawn,
            Thing food,
            ShuttleHolderLaunchManifestEntry pawnEntry,
            ShuttleHolderLaunchManifestEntry foodEntry,
            string pawnFailureMessage,
            string foodFailureMessage,
            out string failureReason)
        {
            failureReason = null;
            this.RecordTouchedEntry(pawnEntry);
            this.RecordTouchedEntry(foodEntry);
            if (!this.access.TryAddOrTransferToDestination(pawn))
            {
                this.MarkDiningPairFailed(pawnEntry, foodEntry);
                failureReason = pawnFailureMessage;
                return false;
            }

            this.RecordMovedThing(pawn);
            if (!this.access.TryAddOrTransferToDestination(food))
            {
                this.MarkDiningPairFailed(pawnEntry, foodEntry);
                failureReason = foodFailureMessage;
                return false;
            }

            this.RecordMovedThing(food);
            return true;
        }

        internal void RollbackLivingIfIncomplete(ref string failureReason)
        {
            if (this.completed)
            {
                return;
            }

            string rollbackNotice;
            this.RollbackLiving(out rollbackNotice);
            this.AppendRollbackNotice(ref failureReason, rollbackNotice);
        }

        internal void RollbackJoyIfIncomplete(ref string failureReason)
        {
            if (this.completed)
            {
                return;
            }

            string rollbackNotice;
            this.RollbackJoy(out rollbackNotice);
            this.AppendRollbackNotice(ref failureReason, rollbackNotice);
        }

        internal void RollbackMixedIfIncomplete(ref string failureReason)
        {
            if (this.completed)
            {
                return;
            }

            string rollbackNotice;
            this.RollbackMixed(out rollbackNotice);
            this.AppendRollbackNotice(ref failureReason, rollbackNotice);
        }

        internal bool RollbackLiving(out string notice)
        {
            return this.access.TryRollbackLivingExport(this.manifest, out notice);
        }

        internal bool RollbackJoy(out string notice)
        {
            return this.access.TryRollbackJoyExport(this.manifest, out notice);
        }

        internal bool RollbackMixed(out string notice)
        {
            return this.access.TryRollbackMixedExport(this.manifest, out notice);
        }

        internal void AppendRollbackNotice(ref string failureReason, string rollbackNotice)
        {
            if (rollbackNotice.NullOrEmpty())
            {
                return;
            }

            failureReason = failureReason.NullOrEmpty()
                ? rollbackNotice
                : failureReason + " Rollback: " + rollbackNotice;
        }

        private void MarkDiningPairFailed(
            ShuttleHolderLaunchManifestEntry pawnEntry,
            ShuttleHolderLaunchManifestEntry foodEntry)
        {
            if (pawnEntry != null)
            {
                pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
            }

            if (foodEntry != null)
            {
                foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
            }
        }

        private void RecordTouchedEntry(ShuttleHolderLaunchManifestEntry entry)
        {
            if (entry != null)
            {
                this.touchedEntries.Add(entry);
            }
        }

        private void RecordMovedThing(Thing thing)
        {
            if (thing != null)
            {
                this.movedThings.Add(thing);
            }
        }
    }
}
