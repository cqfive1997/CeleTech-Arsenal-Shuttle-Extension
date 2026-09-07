using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    // Assigns dining food sources for Habitat restore plans. This preserves
    // legacy validation side effects: some failure paths still mark manifest
    // entries failed and log restore failures. It must not split food stacks,
    // move Things, mutate ThingOwner contents, commit records, or roll back.
    internal static class HabitatDiningFoodAllocator
    {
            internal static bool TryAssignDiningFoodPlanSources(
                HabitatRestoreAccess access,
                HabitatLivingRestorePlan plan,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                out string failureReason)
            {
                failureReason = null;
                Dictionary<int, int> reservedFoodCountsByThingID = new Dictionary<int, int>();
                HashSet<int> exactSourceThingIDs = new HashSet<int>();

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    ShuttleHolderLaunchManifestEntry foodEntry = diningEntry.FoodEntry;
                    Thing exactThing = access.FindThingForHabitatTransfer(
                        foodEntry.ThingID,
                        primarySource,
                        secondarySource);
                    if (exactThing == null ||
                        exactThing.Destroyed ||
                        !MatchesDiningFoodManifest(foodEntry, exactThing))
                    {
                        continue;
                    }

                    int availableCount = exactThing.stackCount - GetReservedFoodCount(
                        reservedFoodCountsByThingID,
                        exactThing);
                    if (availableCount < diningEntry.FoodStackCount)
                    {
                        continue;
                    }

                    diningEntry.FoodSourceThing = exactThing;
                    diningEntry.FoodSourceOwner = access.FindOwnerForHabitatTransferThing(
                        exactThing,
                        primarySource,
                        secondarySource);
                    ReserveDiningFoodCount(
                        reservedFoodCountsByThingID,
                        exactThing,
                        diningEntry.FoodStackCount);
                    exactSourceThingIDs.Add(exactThing.thingIDNumber);
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    if (diningEntry.FoodSourceThing != null)
                    {
                        continue;
                    }

                    Thing candidate;
                    ThingOwner candidateOwner;
                    if (!TryFindDiningFoodPlanCandidate(
                        access,
                        diningEntry.FoodEntry,
                        diningEntry.FoodStackCount,
                        primarySource,
                        secondarySource,
                        reservedFoodCountsByThingID,
                        exactSourceThingIDs,
                        out candidate,
                        out candidateOwner))
                    {
                        diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to allocate Habitat dining food from launch staging. activityID=" +
                            (diningEntry.FoodEntry.ActivityID ?? "null") +
                            " foodThingID=" +
                            diningEntry.FoodEntry.ThingID +
                            " foodDefName=" +
                            (diningEntry.FoodEntry.FoodDefName ?? diningEntry.FoodEntry.DefName ?? "null") +
                            " foodStackCount=" +
                            diningEntry.FoodStackCount;
                        access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                        return false;
                    }

                    diningEntry.FoodSourceThing = candidate;
                    diningEntry.FoodSourceOwner = candidateOwner;
                    ReserveDiningFoodCount(
                        reservedFoodCountsByThingID,
                        candidate,
                        diningEntry.FoodStackCount);
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    if (diningEntry.FoodSourceThing == null ||
                        diningEntry.FoodSourceOwner == null ||
                        diningEntry.FoodSourceThing.Destroyed)
                    {
                        diningEntry.FoodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining food restore plan lost its source allocation. activityID=" +
                            (diningEntry.FoodEntry.ActivityID ?? "null");
                        access.LogHabitatRestoreFailure(diningEntry.FoodEntry, failureReason);
                        return false;
                    }
                }

                return true;
            }

            internal static bool TryAllocateHabitatMixedDiningFood(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifestEntry foodEntry,
                int requiredCount,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                Dictionary<int, int> reservedFoodCountsByThingID,
                out HabitatMixedFoodAllocation allocation,
                out string failureReason)
            {
                allocation = null;
                failureReason = null;
                if (foodEntry == null)
                {
                    failureReason = "Habitat mixed restore plan cannot allocate null dining food entry.";
                    return false;
                }

                Thing sourceThing = access.FindThingForHabitatTransfer(
                    foodEntry.ThingID,
                    primarySource,
                    secondarySource);
                ThingOwner sourceOwner = null;
                if (sourceThing != null &&
                    !sourceThing.Destroyed &&
                    MatchesDiningFoodManifest(foodEntry, sourceThing))
                {
                    int availableCount = sourceThing.stackCount - GetReservedFoodCount(
                        reservedFoodCountsByThingID,
                        sourceThing);
                    if (availableCount >= requiredCount)
                    {
                        sourceOwner = access.FindOwnerForHabitatTransferThing(
                            sourceThing,
                            primarySource,
                            secondarySource);
                    }
                }

                if (sourceOwner == null)
                {
                    if (!TryFindDiningFoodPlanCandidate(
                        access,
                        foodEntry,
                        requiredCount,
                        primarySource,
                        secondarySource,
                        reservedFoodCountsByThingID,
                        null,
                        out sourceThing,
                        out sourceOwner))
                    {
                        failureReason = "Habitat mixed restore plan could not allocate dining food. activityID=" +
                            (foodEntry.ActivityID ?? "null") +
                            " foodThingID=" +
                            foodEntry.ThingID +
                            " foodDefName=" +
                            (foodEntry.FoodDefName ?? foodEntry.DefName ?? "null") +
                            " requiredStackCount=" +
                            requiredCount;
                        return false;
                    }
                }

                if (sourceThing == null || sourceThing.Destroyed || sourceOwner == null)
                {
                    failureReason = "Habitat mixed restore plan allocated invalid dining food source. activityID=" +
                        (foodEntry.ActivityID ?? "null");
                    return false;
                }

                int reservedBefore = GetReservedFoodCount(reservedFoodCountsByThingID, sourceThing);
                int availableAfterReservation = sourceThing.stackCount - reservedBefore;
                if (availableAfterReservation < requiredCount)
                {
                    failureReason = "Habitat mixed restore plan over-allocated dining food stack. activityID=" +
                        (foodEntry.ActivityID ?? "null") +
                        " sourceThingID=" +
                        sourceThing.thingIDNumber +
                        " sourceStackCount=" +
                        sourceThing.stackCount +
                        " reservedBefore=" +
                        reservedBefore +
                        " requiredStackCount=" +
                        requiredCount;
                    return false;
                }

                ReserveDiningFoodCount(
                    reservedFoodCountsByThingID,
                    sourceThing,
                    requiredCount);
                allocation = new HabitatMixedFoodAllocation
                {
                    Entry = foodEntry,
                    SourceThing = sourceThing,
                    SourceOwner = access.CreateHabitatMixedSourceOwnerRef(sourceOwner, primarySource, secondarySource),
                    RequiredStackCount = requiredCount,
                    SourceStackCountBefore = sourceThing.stackCount,
                    ReservedStackCountBefore = reservedBefore,
                    WouldRequireSplit = sourceThing.stackCount > requiredCount
                };
                return true;
            }

            private static bool TryFindDiningFoodPlanCandidate(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifestEntry foodEntry,
                int requiredCount,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                Dictionary<int, int> reservedFoodCountsByThingID,
                HashSet<int> preferredThingIDs,
                out Thing candidate,
                out ThingOwner candidateOwner)
            {
                if (preferredThingIDs != null &&
                    preferredThingIDs.Count > 0 &&
                    TryFindDiningFoodPlanCandidateInOwners(
                        access,
                        foodEntry,
                        requiredCount,
                        primarySource,
                        secondarySource,
                        reservedFoodCountsByThingID,
                        preferredThingIDs,
                        true,
                        out candidate,
                        out candidateOwner))
                {
                    return true;
                }

                return TryFindDiningFoodPlanCandidateInOwners(
                    access,
                    foodEntry,
                    requiredCount,
                    primarySource,
                    secondarySource,
                    reservedFoodCountsByThingID,
                    null,
                    false,
                    out candidate,
                    out candidateOwner);
            }

            private static bool TryFindDiningFoodPlanCandidateInOwners(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifestEntry foodEntry,
                int requiredCount,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                Dictionary<int, int> reservedFoodCountsByThingID,
                HashSet<int> allowedThingIDs,
                bool allowAmbiguousPreferredMatches,
                out Thing candidate,
                out ThingOwner candidateOwner)
            {
                candidate = null;
                candidateOwner = null;
                bool ambiguous = false;
                access.TryFindDiningFoodPlanCandidateInHabitatHolder(
                    foodEntry,
                    requiredCount,
                    reservedFoodCountsByThingID,
                    allowedThingIDs,
                    allowAmbiguousPreferredMatches,
                    ref candidate,
                    ref candidateOwner,
                    ref ambiguous);
                TryFindDiningFoodPlanCandidateInOwner(
                    foodEntry,
                    requiredCount,
                    primarySource,
                    reservedFoodCountsByThingID,
                    allowedThingIDs,
                    allowAmbiguousPreferredMatches,
                    ref candidate,
                    ref candidateOwner,
                    ref ambiguous);
                TryFindDiningFoodPlanCandidateInOwner(
                    foodEntry,
                    requiredCount,
                    secondarySource,
                    reservedFoodCountsByThingID,
                    allowedThingIDs,
                    allowAmbiguousPreferredMatches,
                    ref candidate,
                    ref candidateOwner,
                    ref ambiguous);

                if (ambiguous)
                {
                    candidate = null;
                    candidateOwner = null;
                    return false;
                }

                return candidate != null;
            }

            private static void TryFindDiningFoodPlanCandidateInOwner(
                ShuttleHolderLaunchManifestEntry foodEntry,
                int requiredCount,
                ThingOwner source,
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
                    source,
                    reservedFoodCountsByThingID,
                    allowedThingIDs,
                    allowAmbiguousPreferredMatches,
                    ref candidate,
                    ref candidateOwner,
                    ref ambiguous);
            }

            private static bool MatchesDiningFoodManifest(ShuttleHolderLaunchManifestEntry foodEntry, Thing thing)
            {
                return ShuttleHabitatDiningFoodReservationUtility.MatchesFoodManifest(foodEntry, thing);
            }

            private static int GetReservedFoodCount(Dictionary<int, int> reservedFoodCountsByThingID, Thing thing)
            {
                return ShuttleHabitatDiningFoodReservationUtility.GetReservedFoodCount(
                    reservedFoodCountsByThingID,
                    thing);
            }

            private static void ReserveDiningFoodCount(
                Dictionary<int, int> reservedFoodCountsByThingID,
                Thing thing,
                int count)
            {
                ShuttleHabitatDiningFoodReservationUtility.AddReservedFoodCount(
                    reservedFoodCountsByThingID,
                    thing,
                    count);
            }
    }
}
