using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class HabitatIncomingRestoreOuterHandler
    {
        private enum HabitatManifestThingResolution
        {
            Restored,
            Ejected,
            Quarantined,
            Fatal
        }

        internal static bool TryRestoreBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            ThingOwner cargoStaging,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out HabitatIncomingRestoreResult result)
        {
            result = null;
            List<ShuttleHolderLaunchManifestEntry> habitatEntries =
                ShuttleHolderManifestQueryUtility.GetHabitatManifestEntries(state != null ? state.Manifest : null);
            if (habitatEntries.Count == 0)
            {
                result = new HabitatIncomingRestoreResult(
                    HabitatIncomingRestoreStatus.None,
                    null,
                    state != null ? state.DumpManifestForDebug() : "holder transfer state unavailable",
                    0,
                    0,
                    0,
                    0);
                return true;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            string directRestoreFailureReason = null;
            bool directRestoreSucceeded = false;
            if (habitat == null)
            {
                directRestoreFailureReason = "[CeleTech Shuttle] Habitat holder restore failed before incoming base.Impact: Habitat occupancy comp missing.";
            }
            else
            {
                bool hasLivingEntries =
                    state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind).Count > 0 ||
                    state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind).Count > 0 ||
                    state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind).Count > 0;
                bool hasJoyEntries =
                    state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind).Count > 0;

                if (hasLivingEntries && hasJoyEntries)
                {
                    directRestoreSucceeded = habitat.TryRestoreMixedFromLaunchStaging(
                        state.Manifest,
                        cargoStaging,
                        incomingSkyfallerContainer,
                        map,
                        fallbackCell,
                        out directRestoreFailureReason);
                }
                else if (hasLivingEntries)
                {
                    directRestoreSucceeded = habitat.TryRestoreLivingFromLaunchStaging(
                        state.Manifest,
                        cargoStaging,
                        incomingSkyfallerContainer,
                        map,
                        fallbackCell,
                        out directRestoreFailureReason);
                }
                else if (hasJoyEntries)
                {
                    directRestoreSucceeded = habitat.TryRestoreJoyFromLaunchStaging(
                        state.Manifest,
                        cargoStaging,
                        incomingSkyfallerContainer,
                        map,
                        fallbackCell,
                        out directRestoreFailureReason);
                }
            }

            if (directRestoreSucceeded)
            {
                result = new HabitatIncomingRestoreResult(
                    HabitatIncomingRestoreStatus.AllRestored,
                    null,
                    state.DumpManifestForDebug(),
                    habitatEntries.Count,
                    0,
                    0,
                    0);
                return true;
            }

            result = TrySafeEjectOrQuarantineFailedHabitatManifestThings(
                shuttleHost,
                state,
                cargoStaging,
                incomingSkyfallerContainer,
                map,
                fallbackCell,
                directRestoreFailureReason);
            if (result.Status == HabitatIncomingRestoreStatus.AllRestored)
            {
                state.Manifest.RemoveHabitatLivingEntries();
                state.Manifest.RemoveHabitatJoyEntries();
                return true;
            }

            if (result.Status == HabitatIncomingRestoreStatus.FatalUnresolved)
            {
                Log.Error("[CeleTech Shuttle] Habitat incoming restore is fatal. " +
                    (result.FailureReason ?? "null") +
                    " debug=" +
                    (result.DebugDump ?? "null"));
            }
            else
            {
                Log.Warning("[CeleTech Shuttle] Habitat incoming restore did not fully restore to holder, but all manifest Things are safe before base.Impact. status=" +
                    result.Status +
                    " reason=" +
                    (result.FailureReason ?? "null") +
                    " debug=" +
                    (result.DebugDump ?? "null"));
            }

            return false;
        }

        private static HabitatIncomingRestoreResult TrySafeEjectOrQuarantineFailedHabitatManifestThings(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            string initialFailureReason)
        {
            string notes = initialFailureReason;
            List<ShuttleHolderLaunchManifestEntry> entries =
                ShuttleHolderManifestQueryUtility.GetHabitatManifestEntries(state != null ? state.Manifest : null);
            if (state == null || entries.Count == 0)
            {
                return new HabitatIncomingRestoreResult(
                    HabitatIncomingRestoreStatus.None,
                    notes,
                    state != null ? state.DumpManifestForDebug() : "holder transfer state unavailable",
                    0,
                    0,
                    0,
                    0);
            }

            string diningPairFailure;
            if (!ValidateHabitatDiningManifestPairs(entries, out diningPairFailure))
            {
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, diningPairFailure);
                return new HabitatIncomingRestoreResult(
                    HabitatIncomingRestoreStatus.FatalUnresolved,
                    notes,
                    state.DumpManifestForDebug(),
                    0,
                    0,
                    0,
                    entries.Count);
            }

            Dictionary<ShuttleHolderLaunchManifestEntry, HabitatManifestThingResolution> resolutions =
                new Dictionary<ShuttleHolderLaunchManifestEntry, HabitatManifestThingResolution>();
            int restoredCount = 0;
            int ejectedCount = 0;
            int quarantinedCount = 0;
            int fatalCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                string entryNote;
                HabitatManifestThingResolution resolution = ResolveHabitatManifestThingBeforeImpact(
                    shuttleHost,
                    state,
                    entries,
                    entry,
                    primarySource,
                    secondarySource,
                    map,
                    fallbackCell,
                    out entryNote);
                resolutions[entry] = resolution;
                if (!string.IsNullOrEmpty(entryNote))
                {
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, entryNote);
                }

                if (resolution == HabitatManifestThingResolution.Restored)
                {
                    restoredCount++;
                }
                else if (resolution == HabitatManifestThingResolution.Ejected)
                {
                    ejectedCount++;
                }
                else if (resolution == HabitatManifestThingResolution.Quarantined)
                {
                    quarantinedCount++;
                }
                else
                {
                    fatalCount++;
                }
            }

            string pairResolutionFailure;
            if (!ValidateHabitatDiningPairResolutions(entries, resolutions, out pairResolutionFailure))
            {
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, pairResolutionFailure);
                fatalCount++;
            }

            HabitatIncomingRestoreStatus status = ShuttleHolderIncomingRestoreStatusUtility.BuildHabitatIncomingRestoreStatus(
                entries.Count,
                restoredCount,
                ejectedCount,
                quarantinedCount,
                fatalCount);
            return new HabitatIncomingRestoreResult(
                status,
                notes,
                state.DumpManifestForDebug(),
                restoredCount,
                ejectedCount,
                quarantinedCount,
                fatalCount);
        }

        private static HabitatManifestThingResolution ResolveHabitatManifestThingBeforeImpact(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            List<ShuttleHolderLaunchManifestEntry> allHabitatEntries,
            ShuttleHolderLaunchManifestEntry entry,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string note)
        {
            note = null;
            if (entry == null)
            {
                note = "Habitat manifest entry is null.";
                return HabitatManifestThingResolution.Fatal;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (IsHabitatEntryResolvedInHolder(entry, allHabitatEntries, habitat))
            {
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                entry.DebugNotes = "Verified restored in Habitat holder after incoming restore fallback.";
                return HabitatManifestThingResolution.Restored;
            }

            Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(primarySource, entry.ThingID);
            if (thing == null && !object.ReferenceEquals(primarySource, secondarySource))
            {
                thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(secondarySource, entry.ThingID);
            }

            if (thing != null)
            {
                return MoveHabitatManifestThingOutOfIncomingOwner(
                    state,
                    entry,
                    thing,
                    primarySource,
                    secondarySource,
                    map,
                    fallbackCell,
                    out note);
            }

            ThingOwner quarantineOwner = state != null ? state.DevHabitatLivingExportContainer : null;
            thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(quarantineOwner, entry.ThingID);
            if (thing != null && !thing.Destroyed)
            {
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                entry.DebugNotes = "Verified quarantined in Habitat living staging before incoming base.Impact.";
                return HabitatManifestThingResolution.Quarantined;
            }

            ThingOwner emergencyOwner = state != null ? state.EmergencyRecoveryThings : null;
            thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(emergencyOwner, entry.ThingID);
            if (thing != null && !thing.Destroyed)
            {
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                entry.DebugNotes = "Verified quarantined in emergency recovery before incoming base.Impact.";
                return HabitatManifestThingResolution.Quarantined;
            }

            thing = ShuttleHolderTransferLookupUtility.FindSpawnedThingByID(map, entry.ThingID);
            if (thing != null && !thing.Destroyed)
            {
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseEjected;
                entry.DebugNotes = "Verified spawned on map before incoming base.Impact.";
                return HabitatManifestThingResolution.Ejected;
            }

            if (IsHabitatEntryResolvedInHolder(entry, allHabitatEntries, habitat))
            {
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                entry.DebugNotes = "Verified restored in Habitat holder after source lookup failed.";
                return HabitatManifestThingResolution.Restored;
            }

            note = "Habitat manifest thing could not be found or proven resolved. thingID=" +
                entry.ThingID +
                " holderKind=" +
                (entry.HolderKind ?? "null") +
                " phase=" +
                (entry.TransferPhase ?? "null") +
                ".";
            entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
            entry.DebugNotes = note;
            return HabitatManifestThingResolution.Fatal;
        }

        private static HabitatManifestThingResolution MoveHabitatManifestThingOutOfIncomingOwner(
            CompShuttleHolderLaunchTransferState state,
            ShuttleHolderLaunchManifestEntry entry,
            Thing thing,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            Map map,
            IntVec3 fallbackCell,
            out string note)
        {
            note = null;
            if (thing == null || thing.Destroyed)
            {
                note = "Habitat incoming fallback refused missing/destroyed thing. thingID=" +
                    (entry != null ? entry.ThingID : -1) +
                    ".";
                return HabitatManifestThingResolution.Fatal;
            }

            if (map != null &&
                fallbackCell.IsValid &&
                GenPlace.TryPlaceThing(thing, fallbackCell, map, ThingPlaceMode.Near) &&
                !ShuttleHolderTransferLookupUtility.OwnerContainsThing(primarySource, thing) &&
                !ShuttleHolderTransferLookupUtility.OwnerContainsThing(secondarySource, thing))
            {
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseEjected;
                entry.DebugNotes = "Safe-ejected near incoming shuttle after Habitat restore failure.";
                note = "safe-ejected Habitat manifest thingID=" + entry.ThingID;
                return HabitatManifestThingResolution.Ejected;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string recoveryFailureReason = "transfer state unavailable";
            ThingOwner preferredRecoveryOwner = state != null ? state.DevHabitatLivingExportContainer : null;
            if (state != null &&
                state.TryRecoverTransferThing(
                    thing,
                    "Habitat incoming restore fallback before base.Impact",
                    preferredRecoveryOwner,
                    null,
                    map,
                    fallbackCell,
                    out recoveryStatus,
                    out recoveryFailureReason) &&
                !ShuttleHolderTransferLookupUtility.OwnerContainsThing(primarySource, thing) &&
                !ShuttleHolderTransferLookupUtility.OwnerContainsThing(secondarySource, thing))
            {
                if (recoveryStatus == ShuttleTransferRecoveryStatus.DroppedToMap)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseEjected;
                    entry.DebugNotes = "Safe-ejected through transfer recovery after Habitat restore failure.";
                    note = "safe-ejected via recovery Habitat manifest thingID=" + entry.ThingID;
                    return HabitatManifestThingResolution.Ejected;
                }

                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                entry.DebugNotes = "Quarantined through transfer recovery after Habitat restore failure. status=" +
                    recoveryStatus;
                note = "quarantined Habitat manifest thingID=" +
                    entry.ThingID +
                    " status=" +
                    recoveryStatus;
                return HabitatManifestThingResolution.Quarantined;
            }

            note = "Habitat incoming fallback could not move thing out of incoming/source owner. thingID=" +
                entry.ThingID +
                " recovery=" +
                (recoveryFailureReason ?? "null") +
                ".";
            entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
            entry.DebugNotes = note;
            return HabitatManifestThingResolution.Fatal;
        }

        private static bool IsHabitatEntryResolvedInHolder(
            ShuttleHolderLaunchManifestEntry entry,
            List<ShuttleHolderLaunchManifestEntry> allHabitatEntries,
            CompShuttleHabitatOccupancy habitat)
        {
            if (entry == null || habitat == null)
            {
                return false;
            }

            ThingOwner habitatOwner = habitat.GetDirectlyHeldThings();
            Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(habitatOwner, entry.ThingID);
            if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind)
            {
                Pawn pawn = thing as Pawn;
                return pawn != null && habitat.ContainsSleepingPawn(pawn);
            }

            if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind)
            {
                Pawn pawn = thing as Pawn;
                return pawn != null && habitat.ContainsJoyPawn(pawn);
            }

            if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind)
            {
                Pawn pawn = thing as Pawn;
                Thing food;
                return pawn != null &&
                    habitat.ContainsDiningPawn(pawn) &&
                    habitat.TryGetDiningFoodForPawn(pawn, out food) &&
                    food != null &&
                    !food.Destroyed;
            }

            if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind)
            {
                ShuttleHolderLaunchManifestEntry pawnEntry = FindHabitatDiningPawnEntry(
                    allHabitatEntries,
                    entry.ActivityID);
                if (pawnEntry == null)
                {
                    return false;
                }

                Pawn pawn = ShuttleHolderTransferLookupUtility.FindThingInOwner(habitatOwner, pawnEntry.ThingID) as Pawn;
                Thing food;
                return pawn != null &&
                    habitat.ContainsDiningPawn(pawn) &&
                    habitat.TryGetDiningFoodForPawn(pawn, out food) &&
                    food != null &&
                    !food.Destroyed &&
                    food.thingIDNumber == entry.ThingID;
            }

            return false;
        }

        private static bool ValidateHabitatDiningManifestPairs(
            List<ShuttleHolderLaunchManifestEntry> entries,
            out string failureReason)
        {
            failureReason = null;
            Dictionary<string, ShuttleHolderLaunchManifestEntry> pawnEntries =
                new Dictionary<string, ShuttleHolderLaunchManifestEntry>();
            Dictionary<string, ShuttleHolderLaunchManifestEntry> foodEntries =
                new Dictionary<string, ShuttleHolderLaunchManifestEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    failureReason = "Habitat dining pair validation found null manifest entry.";
                    return false;
                }

                if (entry.HolderKind != ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind &&
                    entry.HolderKind != ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry.ActivityID))
                {
                    failureReason = "Habitat dining pair validation found missing ActivityID. thingID=" +
                        entry.ThingID +
                        ".";
                    return false;
                }

                Dictionary<string, ShuttleHolderLaunchManifestEntry> target =
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind
                        ? pawnEntries
                        : foodEntries;
                if (target.ContainsKey(entry.ActivityID))
                {
                    failureReason = "Habitat dining pair validation found duplicate ActivityID=" +
                        entry.ActivityID +
                        " holderKind=" +
                        entry.HolderKind +
                        ".";
                    return false;
                }

                target.Add(entry.ActivityID, entry);
            }

            foreach (KeyValuePair<string, ShuttleHolderLaunchManifestEntry> pair in pawnEntries)
            {
                if (!foodEntries.ContainsKey(pair.Key))
                {
                    failureReason = "Habitat dining pair validation found pawn entry without food entry. activityID=" +
                        pair.Key +
                        ".";
                    return false;
                }
            }

            foreach (KeyValuePair<string, ShuttleHolderLaunchManifestEntry> pair in foodEntries)
            {
                if (!pawnEntries.ContainsKey(pair.Key))
                {
                    failureReason = "Habitat dining pair validation found food entry without pawn entry. activityID=" +
                        pair.Key +
                        ".";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateHabitatDiningPairResolutions(
            List<ShuttleHolderLaunchManifestEntry> entries,
            Dictionary<ShuttleHolderLaunchManifestEntry, HabitatManifestThingResolution> resolutions,
            out string failureReason)
        {
            failureReason = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry pawnEntry = entries[i];
                if (pawnEntry == null ||
                    pawnEntry.HolderKind != ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind)
                {
                    continue;
                }

                ShuttleHolderLaunchManifestEntry foodEntry = FindHabitatDiningFoodEntry(
                    entries,
                    pawnEntry.ActivityID);
                if (foodEntry == null)
                {
                    failureReason = "Habitat dining pair resolution missing food entry. activityID=" +
                        (pawnEntry.ActivityID ?? "null") +
                        ".";
                    return false;
                }

                HabitatManifestThingResolution pawnResolution;
                HabitatManifestThingResolution foodResolution;
                if (!resolutions.TryGetValue(pawnEntry, out pawnResolution) ||
                    !resolutions.TryGetValue(foodEntry, out foodResolution) ||
                    pawnResolution == HabitatManifestThingResolution.Fatal ||
                    foodResolution == HabitatManifestThingResolution.Fatal)
                {
                    failureReason = "Habitat dining pair resolution has fatal/untracked component. activityID=" +
                        (pawnEntry.ActivityID ?? "null") +
                        ".";
                    return false;
                }

                bool pawnRestored = pawnResolution == HabitatManifestThingResolution.Restored;
                bool foodRestored = foodResolution == HabitatManifestThingResolution.Restored;
                if (pawnRestored != foodRestored)
                {
                    failureReason = "Habitat dining pair resolution split holder restore from fallback recovery. activityID=" +
                        (pawnEntry.ActivityID ?? "null") +
                        " pawnResolution=" +
                        pawnResolution +
                        " foodResolution=" +
                        foodResolution +
                        ".";
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    pawnEntry.DebugNotes = failureReason;
                    foodEntry.DebugNotes = failureReason;
                    return false;
                }
            }

            return true;
        }

        private static ShuttleHolderLaunchManifestEntry FindHabitatDiningPawnEntry(
            List<ShuttleHolderLaunchManifestEntry> entries,
            string activityID)
        {
            return FindHabitatDiningEntry(entries, activityID, ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind);
        }

        private static ShuttleHolderLaunchManifestEntry FindHabitatDiningFoodEntry(
            List<ShuttleHolderLaunchManifestEntry> entries,
            string activityID)
        {
            return FindHabitatDiningEntry(entries, activityID, ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind);
        }

        private static ShuttleHolderLaunchManifestEntry FindHabitatDiningEntry(
            List<ShuttleHolderLaunchManifestEntry> entries,
            string activityID,
            string holderKind)
        {
            if (entries == null || string.IsNullOrEmpty(activityID))
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry != null && entry.HolderKind == holderKind && entry.ActivityID == activityID)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
