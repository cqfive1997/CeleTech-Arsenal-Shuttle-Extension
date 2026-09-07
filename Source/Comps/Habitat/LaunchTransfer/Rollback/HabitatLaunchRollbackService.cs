using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatLaunchRollbackService
    {
        internal static bool TryRollbackLivingExport(
            HabitatLaunchRollbackAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            notice = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                notice = "No Habitat living manifest exists for rollback.";
                return true;
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
                notice = "No Habitat living entries exist in the active manifest.";
                return true;
            }

            for (int i = 0; i < sleepEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = sleepEntries[i];
                Pawn pawn = access.FindThingForHabitatRollback(entry.ThingID, source) as Pawn;
                if (pawn == null || pawn.Destroyed)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find Habitat sleep pawn during rollback. thingID=" + entry.ThingID;
                    return false;
                }

                string failureReason;
                if (!access.TryReturnThingToHabitatHolder(pawn, out failureReason))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = failureReason;
                    return false;
                }

                if (!access.HasRecordFor(pawn))
                {
                    access.AddSleepingRecord(
                        pawn,
                        entry.RestStartTick,
                        entry.RestedTicks,
                        entry.IsSleeping);
                }

                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
            }

            for (int i = 0; i < diningPawnEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                ShuttleHolderLaunchManifestEntry foodEntry = access.FindDiningFoodEntryForActivity(
                    diningFoodEntries,
                    pawnEntry.ActivityID);
                if (foodEntry == null)
                {
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find paired Habitat dining food manifest entry during rollback. activityID=" + pawnEntry.ActivityID;
                    return false;
                }

                Pawn pawn = access.FindThingForHabitatRollback(pawnEntry.ThingID, source) as Pawn;
                Thing food = access.FindThingForHabitatRollback(foodEntry.ThingID, source);
                if (pawn == null || pawn.Destroyed || food == null || food.Destroyed)
                {
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find Habitat dining pair during rollback. activityID=" + pawnEntry.ActivityID;
                    return false;
                }

                string failureReason;
                if (!access.TryReturnThingToHabitatHolder(pawn, out failureReason) ||
                    !access.TryReturnThingToHabitatHolder(food, out failureReason))
                {
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = failureReason;
                    return false;
                }

                if (!access.HasDiningRecordFor(pawn))
                {
                    access.AddDiningRecord(
                        pawn,
                        food,
                        pawnEntry.DiningStartTick,
                        pawnEntry.ChewTicksLeft,
                        pawnEntry.ChewTicksTotal,
                        pawnEntry.IsDining,
                        pawnEntry.Finalized);
                }

                pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
            }

            manifest.RemoveHabitatLivingEntries();
            notice = "Habitat living export rolled back.";
            return true;
        }

        internal static bool TryRollbackJoyExport(
            HabitatLaunchRollbackAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return TryRollbackJoyExportCore(access, manifest, source, out notice);
        }

        internal static bool TryRollbackDevJoyExport(
            HabitatLaunchRollbackAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            return TryRollbackJoyExportCore(access, manifest, source, out notice);
        }

        internal static bool TryRollbackDevMixedExport(
            HabitatLaunchRollbackAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "Dev Habitat mixed local rollback test is DevMode-only.";
                return false;
            }

            return TryRollbackMixedExport(access, manifest, source, out notice);
        }

        internal static bool TryRollbackMixedExport(
            HabitatLaunchRollbackAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            notice = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                notice = "No Habitat mixed manifest exists for rollback.";
                return true;
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
            if (sleepEntries.Count == 0 &&
                diningPawnEntries.Count == 0 &&
                diningFoodEntries.Count == 0 &&
                joyEntries.Count == 0)
            {
                notice = "No Habitat mixed entries exist in the active manifest.";
                return true;
            }

            for (int i = 0; i < sleepEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = sleepEntries[i];
                Pawn pawn = access.FindThingForHabitatRollback(entry.ThingID, source) as Pawn;
                if (pawn == null || pawn.Destroyed)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find Habitat mixed sleep pawn during rollback. thingID=" + entry.ThingID;
                    return false;
                }

                string failureReason;
                if (!access.TryReturnThingToHabitatHolder(pawn, out failureReason))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = failureReason;
                    return false;
                }

                if (!access.HasRecordFor(pawn))
                {
                    access.AddSleepingRecord(
                        pawn,
                        entry.RestStartTick,
                        entry.RestedTicks,
                        entry.IsSleeping);
                }

                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                entry.DebugNotes = "Rolled back from Habitat mixed launch transfer.";
            }

            HashSet<string> restoredDiningActivityIDs = new HashSet<string>();
            for (int i = 0; i < diningPawnEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry pawnEntry = diningPawnEntries[i];
                ShuttleHolderLaunchManifestEntry foodEntry = access.FindDiningFoodEntryForActivity(
                    diningFoodEntries,
                    pawnEntry.ActivityID);
                if (foodEntry == null)
                {
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find paired Habitat mixed dining food manifest entry during rollback. activityID=" +
                        (pawnEntry.ActivityID ?? "null");
                    return false;
                }

                Pawn pawn = access.FindThingForHabitatRollback(pawnEntry.ThingID, source) as Pawn;
                Thing food = access.FindThingForHabitatRollback(foodEntry.ThingID, source);
                if (pawn == null || pawn.Destroyed || food == null || food.Destroyed)
                {
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find Habitat mixed dining pair during rollback. activityID=" +
                        (pawnEntry.ActivityID ?? "null");
                    return false;
                }

                string failureReason;
                if (!access.TryReturnThingToHabitatHolder(pawn, out failureReason) ||
                    !access.TryReturnThingToHabitatHolder(food, out failureReason))
                {
                    pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = failureReason;
                    return false;
                }

                if (!access.HasDiningRecordFor(pawn))
                {
                    access.AddDiningRecord(
                        pawn,
                        food,
                        pawnEntry.DiningStartTick,
                        pawnEntry.ChewTicksLeft,
                        pawnEntry.ChewTicksTotal,
                        pawnEntry.IsDining,
                        pawnEntry.Finalized);
                }

                pawnEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                pawnEntry.DebugNotes = "Rolled back from Habitat mixed launch transfer.";
                foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                foodEntry.DebugNotes = "Rolled back from Habitat mixed launch transfer.";
                restoredDiningActivityIDs.Add(pawnEntry.ActivityID ?? string.Empty);
            }

            for (int i = 0; i < diningFoodEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry foodEntry = diningFoodEntries[i];
                if (foodEntry == null || restoredDiningActivityIDs.Contains(foodEntry.ActivityID ?? string.Empty))
                {
                    continue;
                }

                foodEntry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                notice = "Habitat mixed rollback found orphan dining food entry. activityID=" +
                    (foodEntry.ActivityID ?? "null");
                return false;
            }

            for (int i = 0; i < joyEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = joyEntries[i];
                Pawn pawn = access.FindThingForHabitatRollback(entry.ThingID, source) as Pawn;
                JoyKindDef joyKind = !entry.JoyKindDefName.NullOrEmpty()
                    ? DefDatabase<JoyKindDef>.GetNamedSilentFail(entry.JoyKindDefName)
                    : null;
                if (pawn == null || pawn.Destroyed || joyKind == null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find Habitat mixed joy pawn or joy kind during rollback. thingID=" +
                        entry.ThingID +
                        " joyKind=" +
                        (entry.JoyKindDefName ?? "null");
                    return false;
                }

                string failureReason;
                if (!access.TryReturnThingToHabitatHolder(pawn, out failureReason))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = failureReason;
                    return false;
                }

                if (!access.HasJoyRecordFor(pawn))
                {
                    access.AddJoyRecord(
                        pawn,
                        joyKind,
                        entry.JoyStartTick,
                        entry.JoyTicks,
                        entry.JoyGainRate,
                        entry.MaxJoyTicks,
                        entry.IsJoying);
                }

                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                entry.DebugNotes = "Rolled back from Habitat mixed launch transfer.";
            }

            manifest.RemoveHabitatLivingEntries();
            manifest.RemoveHabitatJoyEntries();
            notice = "Habitat mixed export rolled back.";
            return true;
        }

        private static bool TryRollbackJoyExportCore(
            HabitatLaunchRollbackAccess access,
            ShuttleHolderLaunchManifest manifest,
            ThingOwner source,
            out string notice)
        {
            notice = null;
            access.EnsureInitialized();
            if (manifest == null)
            {
                notice = "No Habitat joy manifest exists for rollback.";
                return true;
            }

            manifest.EnsureInitialized();
            List<ShuttleHolderLaunchManifestEntry> joyEntries =
                manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind);
            if (joyEntries.Count == 0)
            {
                notice = "No Habitat joy entries exist in the active manifest.";
                return true;
            }

            for (int i = 0; i < joyEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = joyEntries[i];
                Pawn pawn = access.FindThingForHabitatRollback(entry.ThingID, source) as Pawn;
                JoyKindDef joyKind = !entry.JoyKindDefName.NullOrEmpty()
                    ? DefDatabase<JoyKindDef>.GetNamedSilentFail(entry.JoyKindDefName)
                    : null;
                if (pawn == null || pawn.Destroyed || joyKind == null)
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Failed to find Habitat joy pawn or joy kind during rollback. thingID=" +
                        entry.ThingID +
                        " joyKind=" +
                        (entry.JoyKindDefName ?? "null");
                    return false;
                }

                if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = "Habitat joy rollback found conflicting sleep/dining record for pawn. thingID=" +
                        entry.ThingID;
                    return false;
                }

                string failureReason;
                if (!access.TryReturnThingToHabitatHolder(pawn, out failureReason))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    notice = failureReason;
                    return false;
                }

                if (!access.HasJoyRecordFor(pawn))
                {
                    access.AddJoyRecord(
                        pawn,
                        joyKind,
                        entry.JoyStartTick,
                        entry.JoyTicks,
                        entry.JoyGainRate,
                        entry.MaxJoyTicks,
                        entry.IsJoying);
                }

                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
            }

            manifest.RemoveHabitatJoyEntries();
            notice = "Dev Habitat joy export test rolled back.";
            return true;
        }
    }
}
