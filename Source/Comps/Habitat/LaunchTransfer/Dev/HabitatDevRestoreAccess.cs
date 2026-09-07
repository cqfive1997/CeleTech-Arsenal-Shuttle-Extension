using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatDevRestoreAccess
    {
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly Action ensureInitialized;
        private readonly HabitatMixedRestoreConflictValidationDelegate validateMixedRestoreHolderConflicts;
        private readonly HabitatLivingRestoreRollbackDelegate rollbackLivingRestoreTransaction;
        private readonly HabitatJoyRestoreSourceRollbackDelegate rollbackMovedJoyPawns;
        private readonly HabitatJoyRestoreOwnerRollbackDelegate rollbackMovedJoyPawnsToOriginalOwners;

        internal HabitatDevRestoreAccess(
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Action ensureInitialized,
            HabitatMixedRestoreConflictValidationDelegate validateMixedRestoreHolderConflicts,
            HabitatLivingRestoreRollbackDelegate rollbackLivingRestoreTransaction,
            HabitatJoyRestoreSourceRollbackDelegate rollbackMovedJoyPawns,
            HabitatJoyRestoreOwnerRollbackDelegate rollbackMovedJoyPawnsToOriginalOwners)
        {
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
            this.diningOccupants = diningOccupants;
            this.joyOccupants = joyOccupants;
            this.ensureInitialized = ensureInitialized;
            this.validateMixedRestoreHolderConflicts = validateMixedRestoreHolderConflicts;
            this.rollbackLivingRestoreTransaction = rollbackLivingRestoreTransaction;
            this.rollbackMovedJoyPawns = rollbackMovedJoyPawns;
            this.rollbackMovedJoyPawnsToOriginalOwners = rollbackMovedJoyPawnsToOriginalOwners;
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal bool TryBuildHabitatMixedRestorePlan(
            ShuttleHolderLaunchManifest manifest,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out HabitatMixedRestorePlan plan,
            out string failureReason)
        {
            return HabitatRestoreService.TryBuildHabitatMixedRestorePlan(
                this.CreateRestoreAccess(),
                manifest,
                primarySource,
                secondarySource,
                out plan,
                out failureReason);
        }

        internal bool TryValidateHabitatMixedLocalStagingRestorePlan(
            HabitatMixedRestorePlan plan,
            out string failureReason)
        {
            return HabitatLaunchTransferValidationService.TryValidateHabitatMixedLocalStagingRestorePlan(
                plan,
                out failureReason);
        }

        internal bool TryApplyHabitatMixedRestorePlan(
            HabitatMixedRestorePlan plan,
            Map map,
            IntVec3 fallbackCell,
            bool allowFoodSplits,
            out HabitatLivingRestoreTransaction transaction,
            out string failureReason)
        {
            return HabitatRestoreService.TryApplyHabitatMixedRestorePlan(
                this.CreateRestoreAccess(),
                plan,
                map,
                fallbackCell,
                allowFoodSplits,
                out transaction,
                out failureReason);
        }

        internal void TryRollbackHabitatLivingRestoreTransaction(
            HabitatLivingRestoreTransaction transaction,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            HabitatRestoreRollbackService.TryRollbackHabitatLivingRestoreTransaction(
                this.CreateLaunchRollbackAccess(),
                transaction,
                map,
                fallbackCell,
                ref failureReason);
        }

        internal void CommitHabitatMixedRestorePlan(HabitatMixedRestorePlan plan)
        {
            HabitatRestoreService.CommitHabitatMixedRestorePlan(
                this.CreateRestoreAccess(),
                plan);
        }

        internal bool HasRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasRecordFor(this.sleepingOccupants, pawn);
        }

        internal bool HasDiningRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasDiningRecordFor(this.diningOccupants, pawn);
        }

        internal bool HasJoyRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasJoyRecordFor(this.joyOccupants, pawn);
        }

        internal bool IsHeldThing(Thing thing)
        {
            return HabitatOccupancyQueryService.IsHeldThing(this.habitatHeldThings, thing);
        }

        internal void AddSleepingRecord(
            Pawn pawn,
            int restStartTick,
            int restedTicks,
            bool isSleeping)
        {
            this.sleepingOccupants.Add(new ShuttleHabitatOccupantRecord(
                pawn,
                restStartTick,
                restedTicks,
                isSleeping));
        }

        internal void AddDiningRecord(
            Pawn pawn,
            Thing food,
            int diningStartTick,
            int chewTicksLeft,
            int chewTicksTotal,
            bool isDining,
            bool finalized)
        {
            this.diningOccupants.Add(new ShuttleHabitatDiningOccupantRecord(
                pawn,
                food,
                diningStartTick,
                chewTicksLeft,
                chewTicksTotal,
                isDining,
                finalized));
        }

        internal ShuttleHolderLaunchManifestEntry FindDiningFoodEntryForActivity(
            List<ShuttleHolderLaunchManifestEntry> foodEntries,
            string activityID)
        {
            return HabitatLaunchTransferManifestHelper.FindDiningFoodEntryForActivity(
                foodEntries,
                activityID);
        }

        internal Thing FindThingForHabitatTransfer(
            int thingID,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            Thing heldThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(this.habitatHeldThings, thingID);
            if (heldThing != null)
            {
                return heldThing;
            }

            Thing primaryThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(primarySource, thingID);
            if (primaryThing != null)
            {
                return primaryThing;
            }

            return ShuttleHolderTransferLookupUtility.FindThingInOwner(secondarySource, thingID);
        }

        internal ThingOwner FindOwnerForHabitatTransferThing(
            Thing thing,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (this.OwnerContainsThing(this.habitatHeldThings, thing))
            {
                return this.habitatHeldThings;
            }

            if (this.OwnerContainsThing(primarySource, thing))
            {
                return primarySource;
            }

            if (this.OwnerContainsThing(secondarySource, thing))
            {
                return secondarySource;
            }

            return null;
        }

        internal int GetDiningFoodStackCount(ShuttleHolderLaunchManifestEntry foodEntry)
        {
            return HabitatLaunchTransferManifestHelper.GetDiningFoodStackCount(foodEntry);
        }

        internal bool TryReturnThingToHabitatHolder(Thing thing, out string failureReason)
        {
            failureReason = null;
            if (thing == null || thing.Destroyed)
            {
                failureReason = "Cannot return missing or destroyed Habitat export thing.";
                return false;
            }

            if (this.IsHeldThing(thing))
            {
                return true;
            }

            if (!this.habitatHeldThings.TryAddOrTransfer(thing, false) || !this.IsHeldThing(thing))
            {
                failureReason = "Failed to return Habitat export thing to habitatHeldThings. thingID=" +
                    thing.thingIDNumber;
                return false;
            }

            return true;
        }

        internal void LogHabitatRestoreFailure(
            ShuttleHolderLaunchManifestEntry entry,
            string failureReason)
        {
            Log.Warning(
                "[CeleTech Shuttle] Habitat living manifest restore failed: " +
                failureReason +
                " entry=" +
                (entry != null ? entry.DumpForDebug() : "null"));
        }

        internal void RollbackDiningFoodRestoreAllocations(List<DiningFoodRestoreAllocation> allocations)
        {
            HabitatRestoreRollbackService.RollbackDiningFoodRestoreAllocations(
                this.CreateLaunchRollbackAccess(),
                allocations);
        }

        internal void RollbackUnrestoredDiningFoodRestoreAllocations(
            Dictionary<string, DiningFoodRestoreAllocation> allocationsByActivityID,
            HashSet<string> restoredActivityIDs)
        {
            HabitatRestoreRollbackService.RollbackUnrestoredDiningFoodRestoreAllocations(
                this.CreateLaunchRollbackAccess(),
                allocationsByActivityID,
                restoredActivityIDs);
        }

        internal bool TryRollbackThingToOwnerOrRecovery(
            Thing thing,
            ThingOwner preferredOwner,
            Map map,
            IntVec3 fallbackCell,
            string context,
            out string failureReason)
        {
            return HabitatRestoreRollbackService.TryRollbackThingToOwnerOrRecovery(
                this.CreateLaunchRollbackAccess(),
                thing,
                preferredOwner,
                map,
                fallbackCell,
                context,
                out failureReason);
        }

        internal Map GetParentMap()
        {
            return this.host != null ? this.host.Map : null;
        }

        internal IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.host, map);
        }

        internal bool TryFindDiningFoodStackCandidateInHabitatHolder(
            ShuttleHolderLaunchManifestEntry foodEntry,
            HashSet<int> whollyAllocatedThingIDs,
            HashSet<int> allowedThingIDs,
            out Thing candidate,
            out ThingOwner candidateSource)
        {
            if (ShuttleHabitatDiningFoodReservationUtility.TryFindFoodStackCandidateInOwner(
                foodEntry,
                this.habitatHeldThings,
                whollyAllocatedThingIDs,
                allowedThingIDs,
                out candidate))
            {
                candidateSource = this.habitatHeldThings;
                return true;
            }

            candidateSource = null;
            return false;
        }

        internal void TryFindUniqueDiningFoodStackCandidateInHabitatHolder(
            ShuttleHolderLaunchManifestEntry foodEntry,
            HashSet<int> whollyAllocatedThingIDs,
            ref Thing candidate,
            ref ThingOwner candidateSource,
            ref bool ambiguous)
        {
            if (ambiguous)
            {
                return;
            }

            Thing ownerCandidate;
            ThingOwner ownerSource;
            if (!this.TryFindDiningFoodStackCandidateInHabitatHolder(
                foodEntry,
                whollyAllocatedThingIDs,
                null,
                out ownerCandidate,
                out ownerSource))
            {
                return;
            }

            if (candidate != null && candidate != ownerCandidate)
            {
                ambiguous = true;
                return;
            }

            candidate = ownerCandidate;
            candidateSource = ownerSource;
        }

        private bool OwnerContainsThing(ThingOwner owner, Thing thing)
        {
            return ShuttleHolderTransferLookupUtility.OwnerContainsThing(owner, thing);
        }

        private HabitatRestoreAccess CreateRestoreAccess()
        {
            this.EnsureInitialized();
            return new HabitatRestoreAccess(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized,
                this.validateMixedRestoreHolderConflicts,
                this.rollbackLivingRestoreTransaction,
                this.rollbackMovedJoyPawns,
                this.rollbackMovedJoyPawnsToOriginalOwners);
        }

        private HabitatLaunchRollbackAccess CreateLaunchRollbackAccess()
        {
            this.EnsureInitialized();
            return new HabitatLaunchRollbackAccess(
                this.host,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized);
        }
    }
}
