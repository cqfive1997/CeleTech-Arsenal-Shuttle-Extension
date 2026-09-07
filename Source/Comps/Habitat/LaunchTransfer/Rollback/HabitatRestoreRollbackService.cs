using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatRestoreRollbackService
    {
        internal static void TryRollbackHabitatLivingRestoreTransaction(
            HabitatLaunchRollbackAccess access,
            HabitatLivingRestoreTransaction transaction,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            if (transaction == null)
            {
                return;
            }

            bool rollbackSucceeded = true;
            for (int i = transaction.MovedThings.Count - 1; i >= 0; i--)
            {
                HabitatLivingMovedThing movedThing = transaction.MovedThings[i];
                Thing thing = movedThing != null ? movedThing.Thing : null;
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                bool returned = movedThing.OriginalOwner != null &&
                    movedThing.OriginalOwner.TryAddOrTransfer(thing, false);
                if (returned)
                {
                    continue;
                }

                if (access.TrySafeEjectRestoreThing(thing, map, fallbackCell))
                {
                    continue;
                }

                rollbackSucceeded = false;
                ShuttleHolderLaunchManifestEntry entry = movedThing.Entry;
                if (entry != null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Restore apply rollback failed; manifest kept active for diagnosis.";
                }

                Log.Error("[CeleTech Shuttle] Failed to rollback Habitat living restore transaction thing. thingID=" +
                    thing.thingIDNumber +
                    " entry=" +
                    (entry != null ? entry.DumpForDebug() : "null"));
            }

            for (int i = transaction.PreparedSplitFoods.Count - 1; i >= 0; i--)
            {
                HabitatDiningRestorePlanEntry diningEntry = transaction.PreparedSplitFoods[i];
                Thing food = diningEntry != null ? diningEntry.FoodForRestore : null;
                if (food == null || food.Destroyed)
                {
                    continue;
                }

                if (diningEntry.FoodSourceOwner != null &&
                    access.OwnerContainsThing(diningEntry.FoodSourceOwner, food))
                {
                    continue;
                }

                bool returned = diningEntry.FoodSourceOwner != null &&
                    diningEntry.FoodSourceOwner.TryAddOrTransfer(food, false);
                if (returned)
                {
                    continue;
                }

                if (access.TrySafeEjectRestoreThing(food, map, fallbackCell))
                {
                    continue;
                }

                rollbackSucceeded = false;
                if (diningEntry.FoodEntry != null)
                {
                    diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    diningEntry.FoodEntry.DebugNotes = "Split food rollback failed; manifest kept active for diagnosis.";
                }

                Log.Error("[CeleTech Shuttle] Failed to rollback split Habitat dining food from restore transaction. activityID=" +
                    (diningEntry.FoodEntry != null ? diningEntry.FoodEntry.ActivityID ?? "null" : "null") +
                    " foodThingID=" +
                    food.thingIDNumber);
            }

            for (int i = transaction.PreparedMixedSplitFoods.Count - 1; i >= 0; i--)
            {
                HabitatMixedDiningRestorePlanEntry diningEntry = transaction.PreparedMixedSplitFoods[i];
                Thing food = diningEntry != null ? diningEntry.FoodForRestore : null;
                HabitatMixedFoodAllocation allocation = diningEntry != null ? diningEntry.FoodAllocation : null;
                ThingOwner sourceOwner = allocation != null && allocation.SourceOwner != null
                    ? allocation.SourceOwner.Owner
                    : null;
                if (food == null || food.Destroyed)
                {
                    continue;
                }

                if (sourceOwner != null && access.OwnerContainsThing(sourceOwner, food))
                {
                    continue;
                }

                bool returned = sourceOwner != null && sourceOwner.TryAddOrTransfer(food, false);
                if (returned)
                {
                    continue;
                }

                if (access.TrySafeEjectRestoreThing(food, map, fallbackCell))
                {
                    continue;
                }

                rollbackSucceeded = false;
                if (diningEntry != null && diningEntry.FoodEntry != null)
                {
                    diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    diningEntry.FoodEntry.DebugNotes = "Mixed split food rollback failed; manifest kept active for diagnosis.";
                }

                Log.Error("[CeleTech Shuttle] Failed to rollback split Habitat mixed dining food from restore transaction. activityID=" +
                    (diningEntry != null ? diningEntry.ActivityID ?? "null" : "null") +
                    " foodThingID=" +
                    food.thingIDNumber);
            }

            if (!rollbackSucceeded)
            {
                failureReason = (failureReason ?? "Habitat living restore transaction failed.") +
                    " Rollback could not return or safe-eject every moved thing; manifest remains active.";
            }
        }

        internal static void RollbackMovedJoyRestorePawns(
            HabitatLaunchRollbackAccess access,
            List<HabitatJoyRestorePlanEntry> movedEntries,
            ThingOwner source)
        {
            if (movedEntries == null || source == null)
            {
                return;
            }

            for (int i = movedEntries.Count - 1; i >= 0; i--)
            {
                HabitatJoyRestorePlanEntry movedEntry = movedEntries[i];
                Pawn pawn = movedEntry != null ? movedEntry.Pawn : null;
                if (pawn == null || pawn.Destroyed || !access.IsHeldThing(pawn))
                {
                    continue;
                }

                Map rollbackMap = access.GetParentMap();
                string rollbackFailure;
                if (TryRollbackThingToOwnerOrRecovery(
                    access,
                    pawn,
                    source,
                    rollbackMap,
                    rollbackMap != null ? access.GetEjectCell(rollbackMap) : IntVec3.Invalid,
                    "Habitat joy local staging restore rollback.",
                    out rollbackFailure))
                {
                    continue;
                }

                ShuttleHolderLaunchManifestEntry entry = movedEntry.Entry;
                if (entry != null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Dev Habitat joy local staging restore rollback failed; manifest kept active for diagnosis.";
                }

                Log.Error("[CeleTech Shuttle] Failed to rollback Habitat joy local staging restore pawn. thingID=" +
                    pawn.thingIDNumber +
                    " rollback=" +
                    (rollbackFailure ?? "null") +
                    " entry=" +
                    (entry != null ? entry.DumpForDebug() : "null"));
            }
        }

        internal static void RollbackMovedJoyRestorePawnsToOriginalOwners(
            HabitatLaunchRollbackAccess access,
            List<HabitatJoyRestorePlanEntry> movedEntries,
            Map map,
            IntVec3 fallbackCell,
            ref string failureReason)
        {
            if (movedEntries == null)
            {
                return;
            }

            bool rollbackSucceeded = true;
            for (int i = movedEntries.Count - 1; i >= 0; i--)
            {
                HabitatJoyRestorePlanEntry movedEntry = movedEntries[i];
                Pawn pawn = movedEntry != null ? movedEntry.Pawn : null;
                if (pawn == null || pawn.Destroyed || !access.IsHeldThing(pawn))
                {
                    continue;
                }

                if (movedEntry.SourceOwner != null &&
                    movedEntry.SourceOwner.TryAddOrTransfer(pawn, false))
                {
                    continue;
                }

                if (access.TrySafeEjectRestoreThing(pawn, map, fallbackCell))
                {
                    continue;
                }

                rollbackSucceeded = false;
                ShuttleHolderLaunchManifestEntry entry = movedEntry.Entry;
                if (entry != null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Dev Habitat joy launch restore rollback failed; manifest kept active for diagnosis.";
                }

                Log.Error("[CeleTech Shuttle] Failed to rollback Habitat joy launch restore pawn. thingID=" +
                    pawn.thingIDNumber +
                    " entry=" +
                    (entry != null ? entry.DumpForDebug() : "null"));
            }

            if (!rollbackSucceeded)
            {
                failureReason = (failureReason ?? "Habitat joy launch restore failed.") +
                    " Rollback could not return or safe-eject every moved joy pawn; manifest remains active.";
            }
        }

        internal static bool TryRollbackThingToOwnerOrRecovery(
            HabitatLaunchRollbackAccess access,
            Thing thing,
            ThingOwner preferredOwner,
            Map map,
            IntVec3 fallbackCell,
            string context,
            out string failureReason)
        {
            failureReason = null;
            if (thing == null || thing.Destroyed)
            {
                return true;
            }

            if (preferredOwner != null)
            {
                if (access.OwnerContainsThing(preferredOwner, thing))
                {
                    return true;
                }

                if (preferredOwner.TryAddOrTransfer(thing, false))
                {
                    return true;
                }
            }

            if (access.TrySafeEjectRestoreThing(thing, map, fallbackCell))
            {
                return true;
            }

            string recoveryFailureReason;
            if (access.TryRecoverTransferThing(
                thing,
                context,
                preferredOwner,
                map,
                fallbackCell,
                out recoveryFailureReason))
            {
                return true;
            }

            failureReason = recoveryFailureReason;
            if (string.IsNullOrEmpty(failureReason))
            {
                failureReason = "rollback target, safe eject, and emergency recovery all failed";
            }

            return false;
        }

        internal static void RollbackDiningFoodRestoreAllocations(
            HabitatLaunchRollbackAccess access,
            List<DiningFoodRestoreAllocation> allocations)
        {
            if (allocations == null)
            {
                return;
            }

            for (int i = 0; i < allocations.Count; i++)
            {
                RollbackDiningFoodRestoreAllocation(access, allocations[i]);
            }
        }

        internal static void RollbackUnrestoredDiningFoodRestoreAllocations(
            HabitatLaunchRollbackAccess access,
            Dictionary<string, DiningFoodRestoreAllocation> allocationsByActivityID,
            HashSet<string> restoredActivityIDs)
        {
            if (allocationsByActivityID == null)
            {
                return;
            }

            foreach (KeyValuePair<string, DiningFoodRestoreAllocation> pair in allocationsByActivityID)
            {
                if (restoredActivityIDs != null && restoredActivityIDs.Contains(pair.Key))
                {
                    continue;
                }

                RollbackDiningFoodRestoreAllocation(access, pair.Value);
            }
        }

        private static void RollbackDiningFoodRestoreAllocation(
            HabitatLaunchRollbackAccess access,
            DiningFoodRestoreAllocation allocation)
        {
            if (allocation == null ||
                !allocation.WasSplit ||
                allocation.Food == null ||
                allocation.Food.Destroyed ||
                access.IsHeldThing(allocation.Food))
            {
                return;
            }

            if (allocation.Source != null && allocation.Source.TryAddOrTransfer(allocation.Food, false))
            {
                return;
            }

            string activityID = allocation.Entry != null ? allocation.Entry.ActivityID : null;
            Log.Warning("[CeleTech Shuttle] Failed to rollback split Habitat dining food restore allocation. activityID=" +
                (activityID ?? "null") +
                " foodThingID=" +
                (allocation.Food != null ? allocation.Food.thingIDNumber : -1));
        }
    }
}
