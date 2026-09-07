using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatRestorePlanBuilder
    {
            internal static bool TryBuildHabitatMixedRestorePlan(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                out HabitatMixedRestorePlan plan,
                out string failureReason)
            {
                // This builder constructs restore plan objects and preserves legacy
                // validation side effects. Some failure paths still mark manifest
                // entries failed and log restore failures to preserve behavior. It must
                // not move Things, mutate ThingOwner contents, split food stacks,
                // commit records, or perform rollback.
                plan = new HabitatMixedRestorePlan();
                failureReason = null;

                access.EnsureInitialized();
                if (manifest == null)
                {
                    failureReason = "Cannot build Habitat mixed restore plan without a manifest.";
                    return false;
                }

                manifest.EnsureInitialized();
                List<ShuttleHolderLaunchManifestEntry> sleepEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind);
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind);
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind);
                List<ShuttleHolderLaunchManifestEntry> joyEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind);

                HashSet<string> plannedActivityIDs = new HashSet<string>();
                HashSet<int> plannedPawnIDs = new HashSet<int>();
                HashSet<int> plannedFoodSourceThingIDs = new HashSet<int>();
                Dictionary<int, int> reservedFoodCountsByThingID = new Dictionary<int, int>();

                if (!TryBuildHabitatMixedSleepRestorePlanEntries(
                    access,
                    sleepEntries,
                    primarySource,
                    secondarySource,
                    plannedActivityIDs,
                    plannedPawnIDs,
                    plan,
                    out failureReason))
                {
                    return false;
                }

                if (!TryBuildHabitatMixedDiningRestorePlanEntries(
                    access,
                    diningPawnEntries,
                    diningFoodEntries,
                    primarySource,
                    secondarySource,
                    plannedActivityIDs,
                    plannedPawnIDs,
                    plannedFoodSourceThingIDs,
                    reservedFoodCountsByThingID,
                    plan,
                    out failureReason))
                {
                    return false;
                }

                if (!TryBuildHabitatMixedJoyRestorePlanEntries(
                    access,
                    joyEntries,
                    primarySource,
                    secondarySource,
                    plannedActivityIDs,
                    plannedPawnIDs,
                    plan,
                    out failureReason))
                {
                    return false;
                }

                if (!access.TryValidateHabitatMixedRestoreHolderConflicts(
                    plannedPawnIDs,
                    plannedFoodSourceThingIDs,
                    out failureReason))
                {
                    return false;
                }

                return true;
            }

            internal static bool TryBuildHabitatLivingRestorePlan(
                HabitatRestoreAccess access,
                List<ShuttleHolderLaunchManifestEntry> sleepEntries,
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                out HabitatLivingRestorePlan plan,
                out string failureReason)
            {
                plan = new HabitatLivingRestorePlan();
                failureReason = null;

                HashSet<string> activityIDs = new HashSet<string>();
                HashSet<int> plannedPawnIDs = new HashSet<int>();
                if (sleepEntries != null)
                {
                    for (int i = 0; i < sleepEntries.Count; i++)
                    {
                        ShuttleHolderLaunchManifestEntry entry = sleepEntries[i];
                        if (entry == null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(entry.ActivityID) && !activityIDs.Add(entry.ActivityID))
                        {
                            entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                            failureReason = "Habitat living restore plan found duplicate ActivityID. activityID=" + entry.ActivityID;
                            access.LogHabitatRestoreFailure(entry, failureReason);
                            return false;
                        }

                        if (!TryAddSleepRestorePlanEntry(
                            access,
                            entry,
                            primarySource,
                            secondarySource,
                            plannedPawnIDs,
                            plan,
                            out failureReason))
                        {
                            return false;
                        }
                    }
                }

                if (!TryBuildDiningRestorePlanEntries(
                    access,
                    diningPawnEntries,
                    diningFoodEntries,
                    primarySource,
                    secondarySource,
                    activityIDs,
                    plannedPawnIDs,
                    plan,
                    out failureReason))
                {
                    return false;
                }

                return true;
            }

            private static bool TryBuildHabitatMixedSleepRestorePlanEntries(
                HabitatRestoreAccess access,
                List<ShuttleHolderLaunchManifestEntry> sleepEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<string> plannedActivityIDs,
                HashSet<int> plannedPawnIDs,
                HabitatMixedRestorePlan plan,
                out string failureReason)
            {
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
                        failureReason = "Habitat mixed restore plan found a null sleep manifest entry.";
                        return false;
                    }

                    if (entry.ActivityID.NullOrEmpty())
                    {
                        failureReason = "Habitat mixed restore plan found sleep entry without ActivityID. thingID=" +
                            entry.ThingID;
                        return false;
                    }

                    if (!plannedActivityIDs.Add(entry.ActivityID))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate ActivityID. activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    Pawn pawn = access.FindThingForHabitatTransfer(entry.ThingID, primarySource, secondarySource) as Pawn;
                    if (pawn == null || pawn.Destroyed)
                    {
                        failureReason = "Habitat mixed restore plan could not find sleep pawn. thingID=" +
                            entry.ThingID +
                            " label=" +
                            (entry.Label ?? "null") +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate pawn thingID. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                    {
                        failureReason = "Habitat mixed restore plan found conflicting live record for sleep pawn. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    HabitatMixedSourceOwnerRef sourceOwner;
                    if (!access.TryCreateHabitatMixedSourceOwnerRef(
                        pawn,
                        primarySource,
                        secondarySource,
                        out sourceOwner))
                    {
                        failureReason = "Habitat mixed restore plan could not identify sleep pawn source access. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    plan.SleepEntries.Add(new HabitatMixedSleepRestorePlanEntry
                    {
                        Entry = entry,
                        Pawn = pawn,
                        SourceOwner = sourceOwner,
                        RestStartTick = entry.RestStartTick,
                        RestedTicks = entry.RestedTicks,
                        IsSleeping = entry.IsSleeping
                    });
                }

                return true;
            }

            private static bool TryBuildHabitatMixedDiningRestorePlanEntries(
                HabitatRestoreAccess access,
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<string> plannedActivityIDs,
                HashSet<int> plannedPawnIDs,
                HashSet<int> plannedFoodSourceThingIDs,
                Dictionary<int, int> reservedFoodCountsByThingID,
                HabitatMixedRestorePlan plan,
                out string failureReason)
            {
                failureReason = null;
                int pawnCount = diningPawnEntries != null ? diningPawnEntries.Count : 0;
                int foodCount = diningFoodEntries != null ? diningFoodEntries.Count : 0;
                if (pawnCount == 0 && foodCount == 0)
                {
                    return true;
                }

                Dictionary<string, ShuttleHolderLaunchManifestEntry> foodEntriesByActivityID =
                    new Dictionary<string, ShuttleHolderLaunchManifestEntry>();
                for (int i = 0; i < foodCount; i++)
                {
                    ShuttleHolderLaunchManifestEntry foodEntry = diningFoodEntries[i];
                    if (foodEntry == null)
                    {
                        failureReason = "Habitat mixed restore plan found a null dining food manifest entry.";
                        return false;
                    }

                    if (foodEntry.ActivityID.NullOrEmpty())
                    {
                        failureReason = "Habitat mixed restore plan found dining food entry without ActivityID. thingID=" +
                            foodEntry.ThingID;
                        return false;
                    }

                    if (foodEntriesByActivityID.ContainsKey(foodEntry.ActivityID))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate dining food ActivityID. activityID=" +
                            foodEntry.ActivityID;
                        return false;
                    }

                    foodEntriesByActivityID.Add(foodEntry.ActivityID, foodEntry);
                }

                HashSet<string> pawnActivityIDs = new HashSet<string>();
                for (int i = 0; i < pawnCount; i++)
                {
                    ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                    if (pawnEntry == null)
                    {
                        failureReason = "Habitat mixed restore plan found a null dining pawn manifest entry.";
                        return false;
                    }

                    if (pawnEntry.ActivityID.NullOrEmpty())
                    {
                        failureReason = "Habitat mixed restore plan found dining pawn entry without ActivityID. thingID=" +
                            pawnEntry.ThingID;
                        return false;
                    }

                    if (!pawnActivityIDs.Add(pawnEntry.ActivityID) ||
                        !plannedActivityIDs.Add(pawnEntry.ActivityID))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate dining ActivityID. activityID=" +
                            pawnEntry.ActivityID;
                        return false;
                    }

                    ShuttleHolderLaunchManifestEntry foodEntry;
                    if (!foodEntriesByActivityID.TryGetValue(pawnEntry.ActivityID, out foodEntry))
                    {
                        failureReason = "Habitat mixed restore plan found dining pawn without paired food entry. activityID=" +
                            pawnEntry.ActivityID +
                            " pawnThingID=" +
                            pawnEntry.ThingID;
                        return false;
                    }

                    Pawn pawn = access.FindThingForHabitatTransfer(pawnEntry.ThingID, primarySource, secondarySource) as Pawn;
                    if (pawn == null || pawn.Destroyed)
                    {
                        failureReason = "Habitat mixed restore plan could not find dining pawn. activityID=" +
                            pawnEntry.ActivityID +
                            " pawnThingID=" +
                            pawnEntry.ThingID +
                            " pawnFound=" +
                            (pawn != null) +
                            " pawnDestroyed=" +
                            (pawn != null && pawn.Destroyed);
                        return false;
                    }

                    if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate pawn thingID. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            pawnEntry.ActivityID;
                        return false;
                    }

                    if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                    {
                        failureReason = "Habitat mixed restore plan found conflicting live record for dining pawn. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            pawnEntry.ActivityID;
                        return false;
                    }

                    HabitatMixedSourceOwnerRef pawnSourceOwner;
                    if (!access.TryCreateHabitatMixedSourceOwnerRef(
                        pawn,
                        primarySource,
                        secondarySource,
                        out pawnSourceOwner))
                    {
                        failureReason = "Habitat mixed restore plan could not identify dining pawn source access. activityID=" +
                            pawnEntry.ActivityID;
                        return false;
                    }

                    HabitatMixedDiningRestorePlanEntry diningEntry = new HabitatMixedDiningRestorePlanEntry
                    {
                        PawnEntry = pawnEntry,
                        FoodEntry = foodEntry,
                        Pawn = pawn,
                        PawnSourceOwner = pawnSourceOwner,
                        FoodStackCount = access.GetDiningFoodStackCount(foodEntry),
                        DiningStartTick = pawnEntry.DiningStartTick,
                        ChewTicksLeft = pawnEntry.ChewTicksLeft,
                        ChewTicksTotal = pawnEntry.ChewTicksTotal,
                        IsDining = pawnEntry.IsDining,
                        Finalized = pawnEntry.Finalized,
                        ActivityID = pawnEntry.ActivityID
                    };
                    plan.DiningEntries.Add(diningEntry);
                }

                foreach (KeyValuePair<string, ShuttleHolderLaunchManifestEntry> pair in foodEntriesByActivityID)
                {
                    if (pawnActivityIDs.Contains(pair.Key))
                    {
                        continue;
                    }

                    failureReason = "Habitat mixed restore plan found orphan dining food entry. activityID=" +
                        pair.Key +
                        " foodThingID=" +
                        pair.Value.ThingID;
                    return false;
                }

                for (int i = 0; i < plan.DiningEntries.Count; i++)
                {
                    HabitatMixedDiningRestorePlanEntry diningEntry = plan.DiningEntries[i];
                    HabitatMixedFoodAllocation allocation;
                    if (!HabitatDiningFoodAllocator.TryAllocateHabitatMixedDiningFood(
                        access,
                        diningEntry.FoodEntry,
                        diningEntry.FoodStackCount,
                        primarySource,
                        secondarySource,
                        reservedFoodCountsByThingID,
                        out allocation,
                        out failureReason))
                    {
                        return false;
                    }

                    diningEntry.FoodAllocation = allocation;
                    plannedFoodSourceThingIDs.Add(allocation.SourceThing.thingIDNumber);
                    plan.FoodAllocations.Add(allocation);
                }

                return true;
            }

            private static bool TryBuildHabitatMixedJoyRestorePlanEntries(
                HabitatRestoreAccess access,
                List<ShuttleHolderLaunchManifestEntry> joyEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<string> plannedActivityIDs,
                HashSet<int> plannedPawnIDs,
                HabitatMixedRestorePlan plan,
                out string failureReason)
            {
                failureReason = null;
                if (joyEntries == null || joyEntries.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < joyEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = joyEntries[i];
                    if (entry == null)
                    {
                        failureReason = "Habitat mixed restore plan found a null joy manifest entry.";
                        return false;
                    }

                    if (entry.ActivityID.NullOrEmpty())
                    {
                        failureReason = "Habitat mixed restore plan found joy entry without ActivityID. thingID=" +
                            entry.ThingID;
                        return false;
                    }

                    if (!plannedActivityIDs.Add(entry.ActivityID))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate joy ActivityID. activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    Pawn pawn = access.FindThingForHabitatTransfer(entry.ThingID, primarySource, secondarySource) as Pawn;
                    JoyKindDef joyKind = !entry.JoyKindDefName.NullOrEmpty()
                        ? DefDatabase<JoyKindDef>.GetNamedSilentFail(entry.JoyKindDefName)
                        : null;
                    if (pawn == null || pawn.Destroyed || joyKind == null)
                    {
                        failureReason = "Habitat mixed restore plan could not find joy pawn or joy kind. thingID=" +
                            entry.ThingID +
                            " label=" +
                            (entry.Label ?? "null") +
                            " activityID=" +
                            entry.ActivityID +
                            " joyKind=" +
                            (entry.JoyKindDefName ?? "null") +
                            " pawnFound=" +
                            (pawn != null) +
                            " pawnDestroyed=" +
                            (pawn != null && pawn.Destroyed);
                        return false;
                    }

                    if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                    {
                        failureReason = "Habitat mixed restore plan found duplicate pawn thingID. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                    {
                        failureReason = "Habitat mixed restore plan found conflicting live record for joy pawn. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    HabitatMixedSourceOwnerRef sourceOwner;
                    if (!access.TryCreateHabitatMixedSourceOwnerRef(
                        pawn,
                        primarySource,
                        secondarySource,
                        out sourceOwner))
                    {
                        failureReason = "Habitat mixed restore plan could not identify joy pawn source access. thingID=" +
                            pawn.thingIDNumber +
                            " activityID=" +
                            entry.ActivityID;
                        return false;
                    }

                    plan.JoyEntries.Add(new HabitatMixedJoyRestorePlanEntry
                    {
                        Entry = entry,
                        Pawn = pawn,
                        SourceOwner = sourceOwner,
                        JoyKind = joyKind,
                        JoyStartTick = entry.JoyStartTick,
                        JoyTicks = entry.JoyTicks,
                        JoyGainRate = entry.JoyGainRate,
                        MaxJoyTicks = entry.MaxJoyTicks,
                        IsJoying = entry.IsJoying,
                        ActivityID = entry.ActivityID
                    });
                }

                return true;
            }

            private static bool TryAddSleepRestorePlanEntry(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifestEntry entry,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<int> plannedPawnIDs,
                HabitatLivingRestorePlan plan,
                out string failureReason)
            {
                failureReason = null;
                Pawn pawn = access.FindThingForHabitatTransfer(entry.ThingID, primarySource, secondarySource) as Pawn;
                if (pawn == null || pawn.Destroyed)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Failed to preflight Habitat sleep pawn from launch staging. thingID=" +
                        entry.ThingID +
                        " label=" +
                        (entry.Label ?? "null") +
                        " activityID=" +
                        (entry.ActivityID ?? "null");
                    access.LogHabitatRestoreFailure(entry, failureReason);
                    return false;
                }

                if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat living restore plan found duplicate pawn allocation. thingID=" +
                        pawn.thingIDNumber;
                    access.LogHabitatRestoreFailure(entry, failureReason);
                    return false;
                }

                if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat launch sleep restore found conflicting live record for pawn. thingID=" +
                        entry.ThingID;
                    access.LogHabitatRestoreFailure(entry, failureReason);
                    return false;
                }

                ThingOwner ownerSource = access.FindOwnerForHabitatTransferThing(pawn, primarySource, secondarySource);
                if (ownerSource == null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat launch sleep restore could not find the pawn access. thingID=" +
                        entry.ThingID;
                    access.LogHabitatRestoreFailure(entry, failureReason);
                    return false;
                }

                plan.SleepEntries.Add(new HabitatSleepRestorePlanEntry
                {
                    Entry = entry,
                    Pawn = pawn,
                    SourceOwner = ownerSource
                });
                return true;
            }

            private static bool TryBuildDiningRestorePlanEntries(
                HabitatRestoreAccess access,
                List<ShuttleHolderLaunchManifestEntry> diningPawnEntries,
                List<ShuttleHolderLaunchManifestEntry> diningFoodEntries,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                HashSet<string> activityIDs,
                HashSet<int> plannedPawnIDs,
                HabitatLivingRestorePlan plan,
                out string failureReason)
            {
                failureReason = null;
                int pawnCount = diningPawnEntries != null ? diningPawnEntries.Count : 0;
                int foodCount = diningFoodEntries != null ? diningFoodEntries.Count : 0;
                if (pawnCount == 0 && foodCount == 0)
                {
                    return true;
                }

                Dictionary<string, ShuttleHolderLaunchManifestEntry> foodEntriesByActivityID =
                    new Dictionary<string, ShuttleHolderLaunchManifestEntry>();
                for (int i = 0; i < foodCount; i++)
                {
                    ShuttleHolderLaunchManifestEntry foodEntry = diningFoodEntries[i];
                    if (foodEntry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(foodEntry.ActivityID))
                    {
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found food entry without ActivityID.";
                        access.LogHabitatRestoreFailure(foodEntry, failureReason);
                        return false;
                    }

                    if (foodEntriesByActivityID.ContainsKey(foodEntry.ActivityID))
                    {
                        foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found duplicate food ActivityID. activityID=" +
                            foodEntry.ActivityID;
                        access.LogHabitatRestoreFailure(foodEntry, failureReason);
                        return false;
                    }

                    foodEntriesByActivityID.Add(foodEntry.ActivityID, foodEntry);
                }

                HashSet<string> pawnActivityIDs = new HashSet<string>();
                for (int i = 0; i < pawnCount; i++)
                {
                    ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                    if (pawnEntry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(pawnEntry.ActivityID))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found pawn entry without ActivityID.";
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    if (!pawnActivityIDs.Add(pawnEntry.ActivityID) || !activityIDs.Add(pawnEntry.ActivityID))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat dining restore found duplicate ActivityID. activityID=" +
                            pawnEntry.ActivityID;
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    ShuttleHolderLaunchManifestEntry foodEntry;
                    if (!foodEntriesByActivityID.TryGetValue(pawnEntry.ActivityID, out foodEntry))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to restore Habitat dining pair because food entry is missing. activityID=" +
                            pawnEntry.ActivityID;
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    Pawn pawn = access.FindThingForHabitatTransfer(pawnEntry.ThingID, primarySource, secondarySource) as Pawn;
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
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat living restore plan found duplicate pawn allocation. thingID=" +
                            pawn.thingIDNumber;
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat launch dining restore found conflicting live record for pawn. activityID=" +
                            (pawnEntry.ActivityID ?? "null");
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    ThingOwner pawnOwner = access.FindOwnerForHabitatTransferThing(pawn, primarySource, secondarySource);
                    if (pawnOwner == null)
                    {
                        pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat launch dining restore could not find the pawn access. activityID=" +
                            (pawnEntry.ActivityID ?? "null");
                        access.LogHabitatRestoreFailure(pawnEntry, failureReason);
                        return false;
                    }

                    plan.DiningEntries.Add(new HabitatDiningRestorePlanEntry
                    {
                        PawnEntry = pawnEntry,
                        FoodEntry = foodEntry,
                        Pawn = pawn,
                        PawnSourceOwner = pawnOwner,
                        FoodStackCount = access.GetDiningFoodStackCount(foodEntry)
                    });
                }

                foreach (KeyValuePair<string, ShuttleHolderLaunchManifestEntry> pair in foodEntriesByActivityID)
                {
                    if (pawnActivityIDs.Contains(pair.Key))
                    {
                        continue;
                    }

                    pair.Value.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    failureReason = "Habitat dining restore found orphan food entry. activityID=" +
                        pair.Key +
                        " foodThingID=" +
                        pair.Value.ThingID;
                    access.LogHabitatRestoreFailure(pair.Value, failureReason);
                    return false;
                }

                return HabitatDiningFoodAllocator.TryAssignDiningFoodPlanSources(
                    access,
                    plan,
                    primarySource,
                    secondarySource,
                    out failureReason);
            }
    }
}
