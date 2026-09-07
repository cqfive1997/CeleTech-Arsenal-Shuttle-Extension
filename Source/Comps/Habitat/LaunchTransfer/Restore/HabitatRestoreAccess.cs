using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal delegate bool HabitatMixedRestoreConflictValidationDelegate(
        HashSet<int> plannedPawnIDs,
        HashSet<int> plannedFoodSourceThingIDs,
        out string failureReason);

    internal delegate void HabitatLivingRestoreRollbackDelegate(
        HabitatLivingRestoreTransaction transaction,
        Map map,
        IntVec3 fallbackCell,
        ref string failureReason);

    internal delegate void HabitatJoyRestoreSourceRollbackDelegate(
        List<HabitatJoyRestorePlanEntry> movedEntries,
        ThingOwner source);

    internal delegate void HabitatJoyRestoreOwnerRollbackDelegate(
        List<HabitatJoyRestorePlanEntry> movedEntries,
        Map map,
        IntVec3 fallbackCell,
        ref string failureReason);

    internal sealed class HabitatRestoreAccess
    {
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly Action ensureInitialized;
        private readonly HabitatMixedRestoreConflictValidationDelegate validateMixedRestoreHolderConflicts;
        private readonly HabitatLivingRestoreRollbackDelegate rollbackLivingRestoreTransaction;
        private readonly HabitatJoyRestoreSourceRollbackDelegate rollbackMovedJoyPawns;
        private readonly HabitatJoyRestoreOwnerRollbackDelegate rollbackMovedJoyPawnsToOriginalOwners;

        internal HabitatRestoreAccess(
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

        internal bool TryValidateHabitatMixedRestoreHolderConflicts(
            HashSet<int> plannedPawnIDs,
            HashSet<int> plannedFoodSourceThingIDs,
            out string failureReason)
        {
            failureReason = null;
            return this.validateMixedRestoreHolderConflicts != null &&
                this.validateMixedRestoreHolderConflicts(
                    plannedPawnIDs,
                    plannedFoodSourceThingIDs,
                    out failureReason);
        }

        internal Thing FindThingForHabitatRollback(int thingID, ThingOwner source)
        {
            Thing heldThing = ShuttleHolderTransferLookupUtility.FindThingInOwner(this.habitatHeldThings, thingID);
            if (heldThing != null)
            {
                return heldThing;
            }

            return ShuttleHolderTransferLookupUtility.FindThingInOwner(source, thingID);
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

        internal ThingOwner FindThingOwnerForHabitatTransfer(
            Thing thing,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (thing == null)
            {
                return null;
            }

            if (this.habitatHeldThings != null && this.habitatHeldThings.Contains(thing))
            {
                return this.habitatHeldThings;
            }

            if (primarySource != null && primarySource.Contains(thing))
            {
                return primarySource;
            }

            if (secondarySource != null && secondarySource.Contains(thing))
            {
                return secondarySource;
            }

            return null;
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

        private bool OwnerContainsThing(ThingOwner owner, Thing thing)
        {
            return ShuttleHolderTransferLookupUtility.OwnerContainsThing(owner, thing);
        }

        internal int GetDiningFoodStackCount(ShuttleHolderLaunchManifestEntry foodEntry)
        {
            return HabitatLaunchTransferManifestHelper.GetDiningFoodStackCount(foodEntry);
        }

        internal bool TryCreateHabitatMixedSourceOwnerRef(
            Thing thing,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out HabitatMixedSourceOwnerRef sourceOwner)
        {
            sourceOwner = null;
            ThingOwner owner = this.FindOwnerForHabitatTransferThing(
                thing,
                primarySource,
                secondarySource);
            if (owner == null)
            {
                return false;
            }

            sourceOwner = this.CreateHabitatMixedSourceOwnerRef(owner, primarySource, secondarySource);
            return true;
        }

        internal HabitatMixedSourceOwnerRef CreateHabitatMixedSourceOwnerRef(
            ThingOwner owner,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            return new HabitatMixedSourceOwnerRef
            {
                Owner = owner,
                Label = this.GetHabitatMixedSourceOwnerLabel(owner, primarySource, secondarySource)
            };
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

        internal bool TryMoveRestoreThingToHabitat(
            Thing thing,
            ShuttleHolderLaunchManifestEntry entry,
            out string failureReason)
        {
            failureReason = null;
            if (thing == null || thing.Destroyed)
            {
                failureReason = "Cannot move missing or destroyed Habitat restore thing.";
                if (entry != null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    this.LogHabitatRestoreFailure(entry, failureReason);
                }

                return false;
            }

            if (this.IsHeldThing(thing))
            {
                return true;
            }

            if (!this.habitatHeldThings.TryAddOrTransfer(thing, false) || !this.IsHeldThing(thing))
            {
                failureReason = "Failed to move Habitat restore thing to habitatHeldThings. thingID=" +
                    thing.thingIDNumber;
                if (entry != null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    this.LogHabitatRestoreFailure(entry, failureReason);
                }

                return false;
            }

            return true;
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

        internal void AddJoyRecord(
            Pawn pawn,
            RimWorld.JoyKindDef joyKind,
            int joyStartTick,
            int joyTicks,
            float joyGainRate,
            int maxJoyTicks,
            bool isJoying)
        {
            this.joyOccupants.Add(new ShuttleHabitatJoyOccupantRecord(
                pawn,
                joyKind,
                joyStartTick,
                joyTicks,
                joyGainRate,
                maxJoyTicks,
                isJoying));
        }

        internal bool TryFindDiningFoodPlanCandidateInHabitatHolder(
            ShuttleHolderLaunchManifestEntry foodEntry,
            int requiredCount,
            Dictionary<int, int> reservedFoodCountsByThingID,
            HashSet<int> allowedThingIDs,
            bool allowAmbiguousPreferredMatches,
            ref Thing candidate,
            ref ThingOwner candidateOwner,
            ref bool ambiguous)
        {
            ShuttleHabitatDiningFoodReservationUtility.TryFindFoodPlanCandidateInOwner(
                foodEntry,
                requiredCount,
                this.habitatHeldThings,
                reservedFoodCountsByThingID,
                allowedThingIDs,
                allowAmbiguousPreferredMatches,
                ref candidate,
                ref candidateOwner,
                ref ambiguous);
            return candidate != null;
        }

        internal void TryRollbackHabitatLivingRestoreTransaction(
            HabitatLivingRestoreTransaction transaction,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            if (this.rollbackLivingRestoreTransaction != null)
            {
                this.rollbackLivingRestoreTransaction(
                    transaction,
                    map,
                    fallbackCell,
                    ref failureReason);
            }
        }

        internal void RollbackMovedJoyRestorePawns(
            List<HabitatJoyRestorePlanEntry> movedEntries,
            ThingOwner source)
        {
            if (this.rollbackMovedJoyPawns != null)
            {
                this.rollbackMovedJoyPawns(movedEntries, source);
            }
        }

        internal void RollbackMovedJoyRestorePawnsToOriginalOwners(
            List<HabitatJoyRestorePlanEntry> movedEntries,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            if (this.rollbackMovedJoyPawnsToOriginalOwners != null)
            {
                this.rollbackMovedJoyPawnsToOriginalOwners(
                    movedEntries,
                    map,
                    fallbackCell,
                    ref failureReason);
            }
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

        private string GetHabitatMixedSourceOwnerLabel(
            ThingOwner owner,
            ThingOwner primarySource,
            ThingOwner secondarySource)
        {
            if (owner == null)
            {
                return "null";
            }

            if (owner == this.habitatHeldThings)
            {
                return "habitatHeldThings";
            }

            if (owner == primarySource)
            {
                return "primarySource";
            }

            if (owner == secondarySource)
            {
                return "secondarySource";
            }

            return owner.GetType().Name;
        }
    }
}
