using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatRestoreTransactionRunner
    {
            internal static bool TryApplyHabitatLivingRestorePlan(
                HabitatRestoreAccess access,
                HabitatLivingRestorePlan plan,
                Map map,
                IntVec3 fallbackCell,
                out HabitatLivingRestoreTransaction transaction,
                out string failureReason)
            {
                HabitatRestoreTransaction restoreTransaction =
                    new HabitatRestoreTransaction(access, "Living");
                transaction = restoreTransaction.Carrier;
                failureReason = null;
                if (plan == null)
                {
                    failureReason = "Cannot apply a null Habitat living restore plan.";
                    return false;
                }

                for (int i = 0; i < plan.SleepEntries.Count; i++)
                {
                    HabitatSleepRestorePlanEntry sleepEntry = plan.SleepEntries[i];
                    if (!restoreTransaction.TryMoveRestoreThingToHabitat(
                        sleepEntry.Pawn,
                        sleepEntry.SourceOwner,
                        sleepEntry.Entry,
                        out failureReason))
                    {
                        restoreTransaction.Rollback(
                            map,
                            fallbackCell,
                            ref failureReason);
                        return false;
                    }
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    if (!restoreTransaction.TryMoveRestoreThingToHabitat(
                        diningEntry.Pawn,
                        diningEntry.PawnSourceOwner,
                        diningEntry.PawnEntry,
                        out failureReason))
                    {
                        restoreTransaction.Rollback(
                            map,
                            fallbackCell,
                            ref failureReason);
                        return false;
                    }

                    if (!restoreTransaction.TryPrepareDiningFoodForRestore(diningEntry, out failureReason) ||
                        !restoreTransaction.TryMoveRestoreThingToHabitat(
                            diningEntry.FoodForRestore,
                            diningEntry.FoodSourceOwner,
                            diningEntry.FoodEntry,
                            out failureReason))
                    {
                        restoreTransaction.Rollback(
                            map,
                            fallbackCell,
                            ref failureReason);
                        return false;
                    }
                }

                restoreTransaction.MarkCompleted();
                return true;
            }

            internal static bool TryPrepareDiningFoodForRestore(
                HabitatRestoreAccess access,
                HabitatDiningRestorePlanEntry diningEntry,
                HabitatLivingRestoreTransaction transaction,
                out string failureReason)
            {
                failureReason = null;
                if (diningEntry == null ||
                    diningEntry.FoodSourceThing == null ||
                    diningEntry.FoodSourceThing.Destroyed)
                {
                    failureReason = "Cannot prepare missing Habitat dining food for restore.";
                    return false;
                }

                if (diningEntry.FoodSourceThing.stackCount < diningEntry.FoodStackCount)
                {
                    diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat dining food stack is smaller than manifest count during apply. activityID=" +
                        (diningEntry.FoodEntry.ActivityID ?? "null") +
                        " foodThingID=" +
                        diningEntry.FoodEntry.ThingID +
                        " candidateThingID=" +
                        diningEntry.FoodSourceThing.thingIDNumber +
                        " candidateStackCount=" +
                        diningEntry.FoodSourceThing.stackCount +
                        " requiredStackCount=" +
                        diningEntry.FoodStackCount;
                    access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                    return false;
                }

                if (diningEntry.FoodSourceThing.stackCount > diningEntry.FoodStackCount)
                {
                    Thing splitFood = diningEntry.FoodSourceThing.SplitOff(diningEntry.FoodStackCount);
                    if (splitFood == null || splitFood.Destroyed)
                    {
                        diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to split Habitat dining food stack during restore apply. activityID=" +
                            (diningEntry.FoodEntry.ActivityID ?? "null") +
                            " foodThingID=" +
                            diningEntry.FoodEntry.ThingID +
                            " candidateThingID=" +
                            diningEntry.FoodSourceThing.thingIDNumber +
                            " requiredStackCount=" +
                            diningEntry.FoodStackCount;
                        access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                        return false;
                    }

                    diningEntry.FoodForRestore = splitFood;
                    diningEntry.FoodWasSplit = true;
                    diningEntry.FoodEntry.DebugNotes = "Recovered dining food by splitting a planned restore stack during transaction apply.";
                    if (transaction != null)
                    {
                        transaction.PreparedSplitFoods.Add(diningEntry);
                    }
                }
                else
                {
                    diningEntry.FoodForRestore = diningEntry.FoodSourceThing;
                    diningEntry.FoodWasSplit = false;
                }

                return true;
            }

            internal static bool TryMoveRestoreThingToHabitat(
                HabitatRestoreAccess access,
                Thing thing,
                ThingOwner originalOwner,
                ShuttleHolderLaunchManifestEntry entry,
                HabitatLivingRestoreTransaction transaction,
                out string failureReason)
            {
                failureReason = null;
                if (thing == null || thing.Destroyed)
                {
                    failureReason = "Cannot move missing or destroyed Habitat restore thing.";
                    if (entry != null)
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        access.LogHabitatRestoreFailure(entry, failureReason);
                    }

                    return false;
                }

                bool wasHeld = access.IsHeldThing(thing);
                if (wasHeld)
                {
                    return true;
                }

                if (!access.TryMoveRestoreThingToHabitat(thing, entry, out failureReason))
                {
                    return false;
                }

                if (transaction != null && !wasHeld)
                {
                    transaction.MovedThings.Add(new HabitatLivingMovedThing
                    {
                        Thing = thing,
                        OriginalOwner = originalOwner,
                        Entry = entry
                    });
                }

                return true;
            }

            internal static bool TryApplyHabitatMixedRestorePlan(
                HabitatRestoreAccess access,
                HabitatMixedRestorePlan plan,
                Map map,
                IntVec3 fallbackCell,
                bool allowFoodSplits,
                out HabitatLivingRestoreTransaction transaction,
                out string failureReason)
            {
                HabitatRestoreTransaction restoreTransaction =
                    new HabitatRestoreTransaction(access, "Mixed");
                transaction = restoreTransaction.Carrier;
                failureReason = null;
                if (plan == null)
                {
                    failureReason = "Cannot apply a null Habitat mixed restore plan.";
                    return false;
                }

                for (int i = 0; i < plan.SleepEntries.Count; i++)
                {
                    HabitatMixedSleepRestorePlanEntry sleepEntry = plan.SleepEntries[i];
                    ThingOwner sourceOwner = sleepEntry.SourceOwner != null ? sleepEntry.SourceOwner.Owner : null;
                    if (!restoreTransaction.TryMoveRestoreThingToHabitat(
                        sleepEntry.Pawn,
                        sourceOwner,
                        sleepEntry.Entry,
                        out failureReason))
                    {
                        restoreTransaction.Rollback(
                            null,
                            IntVec3.Invalid,
                            ref failureReason);
                        return false;
                    }
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatMixedDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    ThingOwner pawnOwner = diningEntry.PawnSourceOwner != null ? diningEntry.PawnSourceOwner.Owner : null;
                    if (!restoreTransaction.TryMoveRestoreThingToHabitat(
                        diningEntry.Pawn,
                        pawnOwner,
                        diningEntry.PawnEntry,
                        out failureReason))
                    {
                        restoreTransaction.Rollback(
                            null,
                            IntVec3.Invalid,
                            ref failureReason);
                        return false;
                    }

                    ThingOwner foodOwner = diningEntry.FoodAllocation != null &&
                        diningEntry.FoodAllocation.SourceOwner != null
                            ? diningEntry.FoodAllocation.SourceOwner.Owner
                            : null;
                    if (!restoreTransaction.TryPrepareMixedDiningFoodForRestore(
                        diningEntry,
                        allowFoodSplits,
                        out failureReason) ||
                        !restoreTransaction.TryMoveRestoreThingToHabitat(
                            diningEntry.FoodForRestore,
                            foodOwner,
                            diningEntry.FoodEntry,
                            out failureReason))
                    {
                        restoreTransaction.Rollback(
                            null,
                            IntVec3.Invalid,
                            ref failureReason);
                        return false;
                    }
                }

                for (int i = 0; i < plan.JoyEntries.Count; i++)
                {
                    HabitatMixedJoyRestorePlanEntry joyEntry = plan.JoyEntries[i];
                    ThingOwner sourceOwner = joyEntry.SourceOwner != null ? joyEntry.SourceOwner.Owner : null;
                    if (!restoreTransaction.TryMoveRestoreThingToHabitat(
                        joyEntry.Pawn,
                        sourceOwner,
                        joyEntry.Entry,
                        out failureReason))
                    {
                        restoreTransaction.Rollback(
                            null,
                            IntVec3.Invalid,
                            ref failureReason);
                        return false;
                    }
                }

                restoreTransaction.MarkCompleted();
                return true;
            }

            internal static bool TryPrepareMixedDiningFoodForRestore(
                HabitatRestoreAccess access,
                HabitatMixedDiningRestorePlanEntry diningEntry,
                bool allowFoodSplits,
                HabitatLivingRestoreTransaction transaction,
                out string failureReason)
            {
                failureReason = null;
                HabitatMixedFoodAllocation allocation = diningEntry != null ? diningEntry.FoodAllocation : null;
                if (diningEntry == null ||
                    allocation == null ||
                    allocation.SourceThing == null ||
                    allocation.SourceThing.Destroyed)
                {
                    failureReason = "Cannot prepare missing Habitat mixed dining food for restore.";
                    return false;
                }

                if (allocation.SourceThing.stackCount < allocation.RequiredStackCount)
                {
                    diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat mixed dining food stack is smaller than manifest count during apply. activityID=" +
                        (diningEntry.ActivityID ?? "null") +
                        " foodThingID=" +
                        (diningEntry.FoodEntry != null ? diningEntry.FoodEntry.ThingID : -1) +
                        " candidateThingID=" +
                        allocation.SourceThing.thingIDNumber +
                        " candidateStackCount=" +
                        allocation.SourceThing.stackCount +
                        " requiredStackCount=" +
                        allocation.RequiredStackCount;
                    access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                    return false;
                }

                if (allocation.SourceThing.stackCount > allocation.RequiredStackCount)
                {
                    if (!allowFoodSplits)
                    {
                        diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat mixed local staging restore refuses split food allocation. activityID=" +
                            (diningEntry.ActivityID ?? "null") +
                            " sourceThingID=" +
                            allocation.SourceThing.thingIDNumber;
                        access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                        return false;
                    }

                    Thing splitFood = allocation.SourceThing.SplitOff(allocation.RequiredStackCount);
                    if (splitFood == null || splitFood.Destroyed)
                    {
                        diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to split Habitat mixed dining food stack during restore apply. activityID=" +
                            (diningEntry.ActivityID ?? "null") +
                            " foodThingID=" +
                            (diningEntry.FoodEntry != null ? diningEntry.FoodEntry.ThingID : -1) +
                            " candidateThingID=" +
                            allocation.SourceThing.thingIDNumber +
                            " requiredStackCount=" +
                            allocation.RequiredStackCount;
                        access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                        return false;
                    }

                    diningEntry.FoodForRestore = splitFood;
                    diningEntry.FoodWasSplit = true;
                    diningEntry.FoodEntry.DebugNotes = "Recovered mixed dining food by splitting a planned restore stack during transaction apply.";
                    if (transaction != null)
                    {
                        transaction.PreparedMixedSplitFoods.Add(diningEntry);
                    }
                }
                else
                {
                    diningEntry.FoodForRestore = allocation.SourceThing;
                    diningEntry.FoodWasSplit = false;
                }

                return true;
            }
    }
}
