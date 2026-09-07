using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatRestoreTransaction
    {
        private readonly HabitatRestoreAccess access;
        private readonly string transactionKind;
        private readonly HabitatLivingRestoreTransaction carrier;
        private readonly List<HabitatJoyRestorePlanEntry> movedJoyEntries =
            new List<HabitatJoyRestorePlanEntry>();
        private bool completed;

        internal HabitatRestoreTransaction(
            HabitatRestoreAccess access,
            string transactionKind)
        {
            this.access = access;
            this.transactionKind = transactionKind;
            this.carrier = new HabitatLivingRestoreTransaction();
        }

        internal HabitatLivingRestoreTransaction Carrier
        {
            get { return this.carrier; }
        }

        internal string TransactionKind
        {
            get { return this.transactionKind; }
        }

        internal int MovedThingCount
        {
            get { return this.carrier.MovedThings.Count; }
        }

        internal int MovedJoyEntryCount
        {
            get { return this.movedJoyEntries.Count; }
        }

        internal void MarkCompleted()
        {
            this.completed = true;
        }

        internal bool TryMoveRestoreThingToHabitat(
            Thing thing,
            ThingOwner originalOwner,
            ShuttleHolderLaunchManifestEntry entry,
            out string failureReason)
        {
            bool wasHeld = this.access.IsHeldThing(thing);
            if (!this.access.TryMoveRestoreThingToHabitat(thing, entry, out failureReason))
            {
                return false;
            }

            if (wasHeld)
            {
                return true;
            }

            this.carrier.MovedThings.Add(new HabitatLivingMovedThing
            {
                Thing = thing,
                OriginalOwner = originalOwner,
                Entry = entry
            });
            return true;
        }

        internal bool TryPrepareDiningFoodForRestore(
            HabitatDiningRestorePlanEntry diningEntry,
            out string failureReason)
        {
            return HabitatRestoreTransactionRunner.TryPrepareDiningFoodForRestore(
                this.access,
                diningEntry,
                this.carrier,
                out failureReason);
        }

        internal bool TryPrepareMixedDiningFoodForRestore(
            HabitatMixedDiningRestorePlanEntry diningEntry,
            bool allowFoodSplits,
            out string failureReason)
        {
            return HabitatRestoreTransactionRunner.TryPrepareMixedDiningFoodForRestore(
                this.access,
                diningEntry,
                allowFoodSplits,
                this.carrier,
                out failureReason);
        }

        internal void Rollback(Map map, IntVec3 fallbackCell, ref string failureReason)
        {
            if (this.completed)
            {
                return;
            }

            this.access.TryRollbackHabitatLivingRestoreTransaction(
                this.carrier,
                map,
                fallbackCell,
                ref failureReason);
        }

        internal bool TryMoveJoyPawnToHabitat(
            HabitatJoyRestorePlanEntry planEntry,
            out string failureReason)
        {
            failureReason = null;
            if (planEntry == null || planEntry.Pawn == null)
            {
                failureReason = "Cannot return missing or destroyed Habitat export thing.";
                return false;
            }

            if (!this.access.TryReturnThingToHabitatHolder(planEntry.Pawn, out failureReason))
            {
                return false;
            }

            this.movedJoyEntries.Add(planEntry);
            return true;
        }

        internal void RollbackMovedJoyPawnsToSource(ThingOwner source)
        {
            this.access.RollbackMovedJoyRestorePawns(this.movedJoyEntries, source);
        }

        internal void RollbackMovedJoyPawnsToOriginalOwners(
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            this.access.RollbackMovedJoyRestorePawnsToOriginalOwners(
                this.movedJoyEntries,
                map,
                fallbackCell,
                ref failureReason);
        }
    }
}
