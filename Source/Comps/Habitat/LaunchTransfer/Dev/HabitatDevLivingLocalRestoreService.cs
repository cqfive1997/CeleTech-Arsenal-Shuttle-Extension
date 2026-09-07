using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatDevLivingLocalRestoreService
    {
            internal static bool TryRestoreDevLivingFromLocalStaging(
                HabitatDevRestoreAccess owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner source,
                out string failureReason)
            {
                failureReason = null;
                if (!Prefs.DevMode)
                {
                    failureReason = "Dev Habitat living local restore test is DevMode-only.";
                    return false;
                }

                owner.EnsureInitialized();
                if (manifest == null)
                {
                    failureReason = "No Habitat living manifest exists for restore.";
                    return false;
                }

                if (source == null)
                {
                    failureReason = "Habitat living local staging holder is missing.";
                    return false;
                }

                manifest.EnsureInitialized();
                List<ShuttleHolderLaunchManifestEntry> sleepEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind);
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind);
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind);
                if (sleepEntries.Count == 0 && diningPawnEntries.Count == 0 && diningFoodEntries.Count == 0)
                {
                    failureReason = "No Habitat living entries exist in the active manifest.";
                    return false;
                }

                Dictionary<int, PawnRestoreAllocation> sleepPawnAllocations;
                if (!TryBuildSleepRestoreAllocations(
                    owner,
                    sleepEntries,
                    source,
                    null,
                    out sleepPawnAllocations,
                    out failureReason))
                {
                    return false;
                }

                Dictionary<string, DiningFoodRestoreAllocation> diningFoodAllocations;
                if (!TryBuildDiningFoodRestoreAllocations(
                    owner,
                    diningPawnEntries,
                    diningFoodEntries,
                    source,
                    null,
                    out diningFoodAllocations,
                    out failureReason))
                {
                    return false;
                }

                Dictionary<string, PawnRestoreAllocation> diningPawnAllocations;
                if (!TryBuildDiningPawnRestoreAllocations(
                    owner,
                    diningPawnEntries,
                    source,
                    null,
                    out diningPawnAllocations,
                    out failureReason))
                {
                    owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                        diningFoodAllocations,
                        null);
                    return false;
                }

                for (int i = 0; i < sleepEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = sleepEntries[i];
                    PawnRestoreAllocation sleepAllocation;
                    Pawn pawn = sleepPawnAllocations.TryGetValue(entry.ThingID, out sleepAllocation)
                        ? sleepAllocation.Pawn
                        : null;
                    if (pawn == null || pawn.Destroyed)
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to restore Habitat sleep pawn from local staging. thingID=" +
                            entry.ThingID +
                            " label=" +
                            (entry.Label ?? "null") +
                            " activityID=" +
                            (entry.ActivityID ?? "null");
                        owner.LogHabitatRestoreFailure(entry, failureReason);
                        owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                            diningFoodAllocations,
                            null);
                        return false;
                    }

                    if (owner.HasDiningRecordFor(pawn) || owner.HasJoyRecordFor(pawn))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat sleep restore found conflicting live record for pawn. thingID=" + entry.ThingID;
                        owner.LogHabitatRestoreFailure(entry, failureReason);
                        owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                            diningFoodAllocations,
                            null);
                        return false;
                    }

                    string returnFailure;
                    if (!owner.TryReturnThingToHabitatHolder(pawn, out returnFailure))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = returnFailure;
                        owner.LogHabitatRestoreFailure(entry, failureReason);
                        owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                            diningFoodAllocations,
                            null);
                        return false;
                    }

                    if (!owner.HasRecordFor(pawn))
                    {
                        owner.AddSleepingRecord(
                            pawn,
                            entry.RestStartTick,
                            entry.RestedTicks,
                            entry.IsSleeping);
                    }

                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    entry.DebugNotes = "Restored from Dev Habitat living local staging holder.";
                }

                HashSet<string> restoredDiningActivityIDs = new HashSet<string>();
                for (int i = 0; i < diningPawnEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                    ShuttleHolderLaunchManifestEntry foodEntry = owner.FindDiningFoodEntryForActivity(
                        diningFoodEntries,
                        pawnEntry.ActivityID);
                    if (foodEntry == null)
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to restore Habitat dining pair because food entry is missing. activityID=" +
                            (pawnEntry.ActivityID ?? "null");
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    PawnRestoreAllocation pawnAllocation;
                    Pawn pawn = TryGetPawnRestoreAllocation(
                        diningPawnAllocations,
                        pawnEntry.ActivityID,
                        out pawnAllocation)
                        ? pawnAllocation.Pawn
                        : null;
                    DiningFoodRestoreAllocation foodAllocation;
                    Thing food = TryGetDiningFoodRestoreAllocation(
                        diningFoodAllocations,
                        pawnEntry.ActivityID,
                        out foodAllocation)
                        ? foodAllocation.Food
                        : null;
                    if (pawn == null || pawn.Destroyed || food == null || food.Destroyed)
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to restore Habitat dining pair from local staging. activityID=" +
                            (pawnEntry.ActivityID ?? "null") +
                            " pawnThingID=" +
                            pawnEntry.ThingID +
                            " foodThingID=" +
                            foodEntry.ThingID;
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        owner.LogHabitatRestoreFailure(foodEntry, failureReason);
                        owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                            diningFoodAllocations,
                            restoredDiningActivityIDs);
                        return false;
                    }

                    if (owner.HasRecordFor(pawn) || owner.HasJoyRecordFor(pawn))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found conflicting live record for pawn. activityID=" +
                            (pawnEntry.ActivityID ?? "null");
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        owner.LogHabitatRestoreFailure(foodEntry, failureReason);
                        owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                            diningFoodAllocations,
                            restoredDiningActivityIDs);
                        return false;
                    }

                    bool pawnMoved = false;
                    string returnFailure;
                    if (!owner.IsHeldThing(pawn))
                    {
                        if (!owner.TryReturnThingToHabitatHolder(pawn, out returnFailure))
                        {
                            pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                            foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                            failureReason = returnFailure;
                            owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                            owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                                diningFoodAllocations,
                                restoredDiningActivityIDs);
                            return false;
                        }

                        pawnMoved = true;
                    }

                    if (!owner.IsHeldThing(food) &&
                        !owner.TryReturnThingToHabitatHolder(food, out returnFailure))
                    {
                        if (pawnMoved)
                        {
                            Map rollbackMap = owner.GetParentMap();
                            string rollbackFailure;
                            if (!owner.TryRollbackThingToOwnerOrRecovery(
                                pawn,
                                source,
                                rollbackMap,
                                rollbackMap != null ? owner.GetEjectCell(rollbackMap) : IntVec3.Invalid,
                                "Habitat dining local restore pawn rollback after food restore failure.",
                                out rollbackFailure))
                            {
                                returnFailure = returnFailure + " pawnRollback=" + (rollbackFailure ?? "null");
                            }
                        }

                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = returnFailure;
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        owner.LogHabitatRestoreFailure(foodEntry, failureReason);
                        owner.RollbackUnrestoredDiningFoodRestoreAllocations(
                            diningFoodAllocations,
                            restoredDiningActivityIDs);
                        return false;
                    }

                    if (!owner.HasDiningRecordFor(pawn))
                    {
                        owner.AddDiningRecord(
                            pawn,
                            food,
                            pawnEntry.DiningStartTick,
                            pawnEntry.ChewTicksLeft,
                            pawnEntry.ChewTicksTotal,
                            pawnEntry.IsDining,
                            pawnEntry.Finalized);
                    }

                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    pawnEntry.DebugNotes = "Restored from Dev Habitat living local staging holder.";
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    foodEntry.DebugNotes = "Restored from Dev Habitat living local staging holder.";
                    restoredDiningActivityIDs.Add(pawnEntry.ActivityID ?? string.Empty);
                }

                if (source.Count != 0)
                {
                    failureReason = "Habitat living local staging restore completed records but staging is not empty. count=" + source.Count;
                    Log.Warning("[CeleTech Shuttle] " + failureReason + " manifest=" + manifest.DumpForDebug());
                    return false;
                }

                manifest.RemoveHabitatLivingEntries();
                failureReason = null;
                return true;
            }

            private static bool TryBuildDiningFoodRestoreAllocations(
                HabitatDevRestoreAccess owner,
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                out Dictionary<string, DiningFoodRestoreAllocation> allocationsByActivityID,
                out string failureReason)
            {
                allocationsByActivityID = new Dictionary<string, DiningFoodRestoreAllocation>();
                failureReason = null;

                List<ShuttleHolderLaunchManifestEntry> requiredFoodEntries;
                if (!TryCollectRequiredDiningFoodEntries(
                    owner,
                    diningPawnEntries,
                    diningFoodEntries,
                    out requiredFoodEntries,
                    out failureReason))
                {
                    return false;
                }

                if (requiredFoodEntries.Count == 0)
                {
                    return true;
                }

                List<DiningFoodRestoreAllocation> allocations = new List<DiningFoodRestoreAllocation>();
                HashSet<int> whollyAllocatedThingIDs = new HashSet<int>();
                HashSet<int> recoverableStackThingIDs = new HashSet<int>();
                for (int i = 0; i < requiredFoodEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry foodEntry = requiredFoodEntries[i];
                    Thing exactThing = owner.FindThingForHabitatTransfer(
                        foodEntry.ThingID,
                        primarySource,
                        secondarySource);
                    if (exactThing == null || exactThing.Destroyed || whollyAllocatedThingIDs.Contains(exactThing.thingIDNumber))
                    {
                        continue;
                    }

                    ThingOwner exactSource = owner.FindOwnerForHabitatTransferThing(
                        exactThing,
                        primarySource,
                        secondarySource);
                    DiningFoodRestoreAllocation allocation;
                    if (!TryCreateDiningFoodRestoreAllocation(
                        owner,
                        foodEntry,
                        exactThing,
                        exactSource,
                        true,
                        out allocation,
                        out failureReason))
                    {
                        owner.RollbackDiningFoodRestoreAllocations(allocations);
                        return false;
                    }

                    allocationsByActivityID[foodEntry.ActivityID] = allocation;
                    allocations.Add(allocation);
                    if (!allocation.WasSplit && allocation.Food != null)
                    {
                        whollyAllocatedThingIDs.Add(allocation.Food.thingIDNumber);
                    }
                    else if (allocation.WasSplit && exactThing != null && !exactThing.Destroyed && exactThing.stackCount > 0)
                    {
                        recoverableStackThingIDs.Add(exactThing.thingIDNumber);
                    }
                }

                for (int i = 0; i < requiredFoodEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry foodEntry = requiredFoodEntries[i];
                    if (allocationsByActivityID.ContainsKey(foodEntry.ActivityID))
                    {
                        continue;
                    }

                    Thing stackCandidate;
                    ThingOwner candidateSource;
                    if (!TryFindDiningFoodStackCandidate(
                        owner,
                        foodEntry,
                        primarySource,
                        secondarySource,
                        whollyAllocatedThingIDs,
                        recoverableStackThingIDs,
                        out stackCandidate,
                        out candidateSource))
                    {
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to allocate Habitat dining food from launch staging. activityID=" +
                            (foodEntry.ActivityID ?? "null") +
                            " foodThingID=" +
                            foodEntry.ThingID +
                            " foodDefName=" +
                            (foodEntry.FoodDefName ?? foodEntry.DefName ?? "null") +
                            " foodStackCount=" +
                            foodEntry.FoodStackCount;
                        owner.LogHabitatRestoreFailure(foodEntry, failureReason);
                        owner.RollbackDiningFoodRestoreAllocations(allocations);
                        return false;
                    }

                    DiningFoodRestoreAllocation allocation;
                    if (!TryCreateDiningFoodRestoreAllocation(
                        owner,
                        foodEntry,
                        stackCandidate,
                        candidateSource,
                        false,
                        out allocation,
                        out failureReason))
                    {
                        owner.LogHabitatRestoreFailure(foodEntry, failureReason);
                        owner.RollbackDiningFoodRestoreAllocations(allocations);
                        return false;
                    }

                    allocationsByActivityID[foodEntry.ActivityID] = allocation;
                    allocations.Add(allocation);
                    if (!allocation.WasSplit && allocation.Food != null)
                    {
                        whollyAllocatedThingIDs.Add(allocation.Food.thingIDNumber);
                    }
                }

                return true;
            }

            private static bool TryBuildSleepRestoreAllocations(
                HabitatDevRestoreAccess owner,
                List<ShuttleHolderLaunchManifestEntry> sleepEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                out Dictionary<int, PawnRestoreAllocation> allocationsByThingID,
                out string failureReason)
            {
                allocationsByThingID = new Dictionary<int, PawnRestoreAllocation>();
                failureReason = null;
                if (sleepEntries == null || sleepEntries.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < sleepEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = sleepEntries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    Pawn pawn = owner.FindThingForHabitatTransfer(entry.ThingID, primarySource, secondarySource) as Pawn;
                    if (pawn == null || pawn.Destroyed)
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to preflight Habitat sleep pawn from launch staging. thingID=" +
                            entry.ThingID +
                            " label=" +
                            (entry.Label ?? "null") +
                            " activityID=" +
                            (entry.ActivityID ?? "null");
                        owner.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    if (owner.HasDiningRecordFor(pawn) || owner.HasJoyRecordFor(pawn))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat launch sleep restore found conflicting live record for pawn. thingID=" + entry.ThingID;
                        owner.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    allocationsByThingID[entry.ThingID] = new PawnRestoreAllocation
                    {
                        Entry = entry,
                        Pawn = pawn
                    };
                }

                return true;
            }

            private static bool TryBuildDiningPawnRestoreAllocations(
                HabitatDevRestoreAccess owner,
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                out Dictionary<string, PawnRestoreAllocation> allocationsByActivityID,
                out string failureReason)
            {
                allocationsByActivityID = new Dictionary<string, PawnRestoreAllocation>();
                failureReason = null;
                if (diningPawnEntries == null || diningPawnEntries.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < diningPawnEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                    if (pawnEntry == null)
                    {
                        continue;
                    }

                    Pawn pawn = owner.FindThingForHabitatTransfer(pawnEntry.ThingID, primarySource, secondarySource) as Pawn;
                    if (pawn == null || pawn.Destroyed)
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to preflight Habitat dining pawn from launch staging. activityID=" +
                            (pawnEntry.ActivityID ?? "null") +
                            " pawnThingID=" +
                            pawnEntry.ThingID +
                            " pawnFound=" +
                            (pawn != null) +
                            " pawnDestroyed=" +
                            (pawn != null && pawn.Destroyed);
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    if (owner.HasRecordFor(pawn) || owner.HasJoyRecordFor(pawn))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat launch dining restore found conflicting live record for pawn. activityID=" +
                            (pawnEntry.ActivityID ?? "null");
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    allocationsByActivityID[pawnEntry.ActivityID] = new PawnRestoreAllocation
                    {
                        Entry = pawnEntry,
                        Pawn = pawn
                    };
                }

                return true;
            }

            private static bool TryCollectRequiredDiningFoodEntries(
                HabitatDevRestoreAccess owner,
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries,
                out List<ShuttleHolderLaunchManifestEntry> requiredFoodEntries,
                out string failureReason)
            {
                requiredFoodEntries = new List<ShuttleHolderLaunchManifestEntry>();
                failureReason = null;

                int pawnCount = diningPawnEntries != null ? diningPawnEntries.Count : 0;
                int foodCount = diningFoodEntries != null ? diningFoodEntries.Count : 0;
                if (pawnCount == 0 && foodCount == 0)
                {
                    return true;
                }

                HashSet<string> requiredActivityIDs = new HashSet<string>();
                for (int i = 0; i < pawnCount; i++)
                {
                    ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                    if (pawnEntry == null || string.IsNullOrEmpty(pawnEntry.ActivityID))
                    {
                        failureReason = "Habitat dining restore found pawn entry without ActivityID.";
                        return false;
                    }

                    if (!requiredActivityIDs.Add(pawnEntry.ActivityID))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found duplicate pawn ActivityID. activityID=" +
                            pawnEntry.ActivityID;
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    ShuttleHolderLaunchManifestEntry foodEntry = owner.FindDiningFoodEntryForActivity(
                        diningFoodEntries,
                        pawnEntry.ActivityID);
                    if (foodEntry == null)
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to restore Habitat dining pair because food entry is missing. activityID=" +
                            pawnEntry.ActivityID;
                        owner.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    requiredFoodEntries.Add(foodEntry);
                }

                for (int i = 0; i < foodCount; i++)
                {
                    ShuttleHolderLaunchManifestEntry foodEntry = diningFoodEntries[i];
                    if (foodEntry == null || string.IsNullOrEmpty(foodEntry.ActivityID))
                    {
                        failureReason = "Habitat dining restore found food entry without ActivityID.";
                        return false;
                    }

                    if (!requiredActivityIDs.Contains(foodEntry.ActivityID))
                    {
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found orphan food entry. activityID=" +
                            foodEntry.ActivityID +
                            " foodThingID=" +
                            foodEntry.ThingID;
                        owner.LogHabitatRestoreFailure(foodEntry, failureReason);
                        return false;
                    }
                }

                return true;
            }

            private static bool TryFindDiningFoodStackCandidate(
                HabitatDevRestoreAccess owner,
                ShuttleHolderLaunchManifestEntry foodEntry,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<int> whollyAllocatedThingIDs,
                HashSet<int> recoverableStackThingIDs,
                out Thing candidate,
                out ThingOwner candidateSource)
            {
                if (recoverableStackThingIDs != null &&
                    recoverableStackThingIDs.Count > 0 &&
                    TryFindAllowedDiningFoodStackCandidate(
                        owner,
                        foodEntry,
                        primarySource,
                        secondarySource,
                        whollyAllocatedThingIDs,
                        recoverableStackThingIDs,
                        out candidate,
                        out candidateSource))
                {
                    return true;
                }

                return TryFindUniqueDiningFoodStackCandidate(
                    owner,
                    foodEntry,
                    primarySource,
                    whollyAllocatedThingIDs,
                    secondarySource,
                    out candidate,
                    out candidateSource);
            }

            private static bool TryFindAllowedDiningFoodStackCandidate(
                HabitatDevRestoreAccess owner,
                ShuttleHolderLaunchManifestEntry foodEntry,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<int> whollyAllocatedThingIDs,
                HashSet<int> allowedThingIDs,
                out Thing candidate,
                out ThingOwner candidateSource)
            {
                if (owner.TryFindDiningFoodStackCandidateInHabitatHolder(
                    foodEntry,
                    whollyAllocatedThingIDs,
                    allowedThingIDs,
                    out candidate,
                    out candidateSource))
                {
                    return true;
                }

                if (TryFindDiningFoodStackCandidateInOwner(
                    foodEntry,
                    primarySource,
                    whollyAllocatedThingIDs,
                    allowedThingIDs,
                    out candidate))
                {
                    candidateSource = primarySource;
                    return true;
                }

                if (TryFindDiningFoodStackCandidateInOwner(
                    foodEntry,
                    secondarySource,
                    whollyAllocatedThingIDs,
                    allowedThingIDs,
                    out candidate))
                {
                    candidateSource = secondarySource;
                    return true;
                }

                candidate = null;
                candidateSource = null;
                return false;
            }

            private static bool TryFindUniqueDiningFoodStackCandidate(
                HabitatDevRestoreAccess owner,
                ShuttleHolderLaunchManifestEntry foodEntry,
                ThingOwner primarySource,
                HashSet<int> whollyAllocatedThingIDs,
                ThingOwner secondarySource,
                out Thing candidate,
                out ThingOwner candidateSource)
            {
                candidate = null;
                candidateSource = null;
                bool ambiguous = false;
                owner.TryFindUniqueDiningFoodStackCandidateInHabitatHolder(
                    foodEntry,
                    whollyAllocatedThingIDs,
                    ref candidate,
                    ref candidateSource,
                    ref ambiguous);
                TryFindUniqueDiningFoodStackCandidateInOwner(
                    foodEntry,
                    primarySource,
                    whollyAllocatedThingIDs,
                    ref candidate,
                    ref candidateSource,
                    ref ambiguous);
                TryFindUniqueDiningFoodStackCandidateInOwner(
                    foodEntry,
                    secondarySource,
                    whollyAllocatedThingIDs,
                    ref candidate,
                    ref candidateSource,
                    ref ambiguous);

                if (ambiguous)
                {
                    candidate = null;
                    candidateSource = null;
                    return false;
                }

                return candidate != null;
            }

            private static void TryFindUniqueDiningFoodStackCandidateInOwner(
                ShuttleHolderLaunchManifestEntry foodEntry,
                ThingOwner source,
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
                if (!TryFindDiningFoodStackCandidateInOwner(
                    foodEntry,
                    source,
                    whollyAllocatedThingIDs,
                    null,
                    out ownerCandidate))
                {
                    return;
                }

                if (candidate != null && candidate != ownerCandidate)
                {
                    ambiguous = true;
                    return;
                }

                candidate = ownerCandidate;
                candidateSource = source;
            }

            private static bool TryFindDiningFoodStackCandidateInOwner(
                ShuttleHolderLaunchManifestEntry foodEntry,
                ThingOwner source,
                HashSet<int> whollyAllocatedThingIDs,
                HashSet<int> allowedThingIDs,
                out Thing candidate)
            {
                return ShuttleHabitatDiningFoodReservationUtility.TryFindFoodStackCandidateInOwner(
                    foodEntry,
                    source,
                    whollyAllocatedThingIDs,
                    allowedThingIDs,
                    out candidate);
            }

            private static bool TryCreateDiningFoodRestoreAllocation(
                HabitatDevRestoreAccess owner,
                ShuttleHolderLaunchManifestEntry foodEntry,
                Thing candidate,
                ThingOwner source,
                bool exactMatch,
                out DiningFoodRestoreAllocation allocation,
                out string failureReason)
            {
                allocation = null;
                failureReason = null;
                if (foodEntry == null || candidate == null || candidate.Destroyed)
                {
                    failureReason = "Cannot allocate missing Habitat dining food for restore.";
                    return false;
                }

                int count = owner.GetDiningFoodStackCount(foodEntry);
                if (candidate.stackCount < count)
                {
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat dining food stack is smaller than manifest count. activityID=" +
                        (foodEntry.ActivityID ?? "null") +
                        " foodThingID=" +
                        foodEntry.ThingID +
                        " candidateThingID=" +
                        candidate.thingIDNumber +
                        " candidateStackCount=" +
                        candidate.stackCount +
                        " requiredStackCount=" +
                        count;
                    return false;
                }

                Thing foodForRestore = candidate;
                bool wasSplit = false;
                if (candidate.stackCount > count)
                {
                    foodForRestore = candidate.SplitOff(count);
                    wasSplit = true;
                    if (foodForRestore == null || foodForRestore.Destroyed)
                    {
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to split Habitat dining food stack for restore. activityID=" +
                            (foodEntry.ActivityID ?? "null") +
                            " foodThingID=" +
                            foodEntry.ThingID +
                            " candidateThingID=" +
                            candidate.thingIDNumber +
                            " requiredStackCount=" +
                            count;
                        return false;
                    }
                }

                allocation = new DiningFoodRestoreAllocation
                {
                    Entry = foodEntry,
                    Food = foodForRestore,
                    Source = source,
                    WasSplit = wasSplit
                };

                if (wasSplit)
                {
                    foodEntry.DebugNotes = exactMatch
                        ? "Recovered dining food by splitting exact stack; stack count exceeded the manifest count."
                        : "Recovered dining food by splitting a matching stack after the original food thing ID was not found.";
                }
                else if (!exactMatch)
                {
                    foodEntry.DebugNotes = "Recovered dining food from a matching stack after the original food thing ID was not found.";
                }

                return true;
            }

            private static bool TryGetDiningFoodRestoreAllocation(
                Dictionary<string, DiningFoodRestoreAllocation> allocationsByActivityID,
                string activityID,
                out DiningFoodRestoreAllocation allocation)
            {
                allocation = null;
                if (allocationsByActivityID == null || string.IsNullOrEmpty(activityID))
                {
                    return false;
                }

                return allocationsByActivityID.TryGetValue(activityID, out allocation);
            }

            private static bool TryGetPawnRestoreAllocation(
                Dictionary<string, PawnRestoreAllocation> allocationsByActivityID,
                string activityID,
                out PawnRestoreAllocation allocation)
            {
                allocation = null;
                if (allocationsByActivityID == null || string.IsNullOrEmpty(activityID))
                {
                    return false;
                }

                return allocationsByActivityID.TryGetValue(activityID, out allocation);
            }
    }
}
