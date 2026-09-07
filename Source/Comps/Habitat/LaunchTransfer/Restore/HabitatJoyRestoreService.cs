using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatJoyRestoreService
    {
            internal static bool TryRestoreDevJoyFromLocalStaging(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner source,
                out string failureReason)
            {
                failureReason = null;
                if (!Prefs.DevMode)
                {
                    failureReason = "Dev Habitat joy local restore test is DevMode-only.";
                    return false;
                }

                access.EnsureInitialized();
                if (manifest == null)
                {
                    failureReason = "No Habitat joy manifest exists for restore.";
                    return false;
                }

                if (source == null)
                {
                    failureReason = "Habitat joy local staging holder is missing.";
                    return false;
                }

                manifest.EnsureInitialized();
                List<ShuttleHolderLaunchManifestEntry> joyEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind);
                if (joyEntries.Count == 0)
                {
                    failureReason = "No Habitat joy entries exist in the active manifest.";
                    return false;
                }

                List<HabitatJoyRestorePlanEntry> restorePlan = new List<HabitatJoyRestorePlanEntry>();
                HashSet<int> plannedPawnIDs = new HashSet<int>();
                HashSet<string> plannedActivityIDs = new HashSet<string>();
                for (int i = 0; i < joyEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = joyEntries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(entry.ActivityID) && !plannedActivityIDs.Add(entry.ActivityID))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat joy restore found duplicate ActivityID. activityID=" + entry.ActivityID;
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    Pawn pawn = access.FindThingForHabitatRollback(entry.ThingID, source) as Pawn;
                    JoyKindDef joyKind = !entry.JoyKindDefName.NullOrEmpty()
                        ? DefDatabase<JoyKindDef>.GetNamedSilentFail(entry.JoyKindDefName)
                        : null;
                    if (pawn == null || pawn.Destroyed || joyKind == null)
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to find Habitat joy pawn or joy kind during local staging restore. thingID=" +
                            entry.ThingID +
                            " label=" +
                            (entry.Label ?? "null") +
                            " activityID=" +
                            (entry.ActivityID ?? "null") +
                            " joyKind=" +
                            (entry.JoyKindDefName ?? "null");
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat joy restore found duplicate pawn entry. thingID=" + pawn.thingIDNumber;
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat joy restore found conflicting live record for pawn. thingID=" +
                            entry.ThingID +
                            " activityID=" +
                            (entry.ActivityID ?? "null");
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    restorePlan.Add(new HabitatJoyRestorePlanEntry
                    {
                        Entry = entry,
                        Pawn = pawn,
                        JoyKind = joyKind
                    });
                }

                if (restorePlan.Count == 0)
                {
                    failureReason = "No usable Habitat joy entries exist in the active manifest.";
                    return false;
                }

                HabitatRestoreTransaction transaction =
                    new HabitatRestoreTransaction(access, "DevJoyLocal");
                for (int i = 0; i < restorePlan.Count; i++)
                {
                    HabitatJoyRestorePlanEntry planEntry = restorePlan[i];
                    if (access.IsHeldThing(planEntry.Pawn))
                    {
                        continue;
                    }

                    string returnFailure;
                    if (!transaction.TryMoveJoyPawnToHabitat(planEntry, out returnFailure))
                    {
                        planEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = returnFailure;
                        access.LogHabitatRestoreFailure(planEntry.Entry, failureReason);
                        transaction.RollbackMovedJoyPawnsToSource(source);
                        return false;
                    }
                }

                if (source.Count != 0)
                {
                    failureReason = "Habitat joy local staging restore moved joy pawns but staging is not empty. count=" +
                        source.Count;
                    Log.Warning("[CeleTech Shuttle] " + failureReason + " manifest=" + manifest.DumpForDebug());
                    transaction.RollbackMovedJoyPawnsToSource(source);
                    return false;
                }

                for (int i = 0; i < restorePlan.Count; i++)
                {
                    HabitatJoyRestorePlanEntry planEntry = restorePlan[i];
                    access.AddJoyRecord(
                        planEntry.Pawn,
                        planEntry.JoyKind,
                        planEntry.Entry.JoyStartTick,
                        planEntry.Entry.JoyTicks,
                        planEntry.Entry.JoyGainRate,
                        planEntry.Entry.MaxJoyTicks,
                        planEntry.Entry.IsJoying);

                    planEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    planEntry.Entry.DebugNotes = "Restored from Dev Habitat joy local staging holder.";
                }

                manifest.RemoveHabitatJoyEntries();
                transaction.MarkCompleted();
                failureReason = null;
                return true;
            }

            internal static bool TryRestoreJoyFromLaunchStaging(
                HabitatRestoreAccess access,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner primarySource,
                ThingOwner secondarySource,
                Map map,
                IntVec3 fallbackCell,
                out string failureReason)
            {
                failureReason = null;

                access.EnsureInitialized();
                if (manifest == null)
                {
                    failureReason = "No Habitat joy manifest exists for launch restore.";
                    return false;
                }

                manifest.EnsureInitialized();
                List<ShuttleHolderLaunchManifestEntry> joyEntries =
                    manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind);
                if (joyEntries.Count == 0)
                {
                    return true;
                }

                List<HabitatJoyRestorePlanEntry> restorePlan = new List<HabitatJoyRestorePlanEntry>();
                HashSet<int> plannedPawnIDs = new HashSet<int>();
                HashSet<string> plannedActivityIDs = new HashSet<string>();
                for (int i = 0; i < joyEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = joyEntries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(entry.ActivityID) && !plannedActivityIDs.Add(entry.ActivityID))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat joy launch restore found duplicate ActivityID. activityID=" + entry.ActivityID;
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    Pawn pawn = access.FindThingForHabitatTransfer(entry.ThingID, primarySource, secondarySource) as Pawn;
                    JoyKindDef joyKind = !entry.JoyKindDefName.NullOrEmpty()
                        ? DefDatabase<JoyKindDef>.GetNamedSilentFail(entry.JoyKindDefName)
                        : null;
                    if (pawn == null || pawn.Destroyed || joyKind == null)
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Failed to preflight Habitat joy pawn from launch staging. thingID=" +
                            entry.ThingID +
                            " label=" +
                            (entry.Label ?? "null") +
                            " activityID=" +
                            (entry.ActivityID ?? "null") +
                            " joyKind=" +
                            (entry.JoyKindDefName ?? "null") +
                            " pawnFound=" +
                            (pawn != null) +
                            " pawnDestroyed=" +
                            (pawn != null && pawn.Destroyed);
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    if (!plannedPawnIDs.Add(pawn.thingIDNumber))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat joy launch restore found duplicate pawn entry. thingID=" +
                            pawn.thingIDNumber;
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    if (access.HasRecordFor(pawn) || access.HasDiningRecordFor(pawn) || access.HasJoyRecordFor(pawn))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = "Habitat joy launch restore found conflicting live record for pawn. thingID=" +
                            entry.ThingID +
                            " activityID=" +
                            (entry.ActivityID ?? "null");
                        access.LogHabitatRestoreFailure(entry, failureReason);
                        return false;
                    }

                    restorePlan.Add(new HabitatJoyRestorePlanEntry
                    {
                        Entry = entry,
                        Pawn = pawn,
                        JoyKind = joyKind,
                        SourceOwner = access.FindThingOwnerForHabitatTransfer(pawn, primarySource, secondarySource)
                    });
                }

                if (restorePlan.Count == 0)
                {
                    return true;
                }

                HabitatRestoreTransaction transaction =
                    new HabitatRestoreTransaction(access, "JoyLaunch");
                for (int i = 0; i < restorePlan.Count; i++)
                {
                    HabitatJoyRestorePlanEntry planEntry = restorePlan[i];
                    if (access.IsHeldThing(planEntry.Pawn))
                    {
                        continue;
                    }

                    string returnFailure;
                    if (!transaction.TryMoveJoyPawnToHabitat(planEntry, out returnFailure))
                    {
                        planEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        failureReason = returnFailure;
                        access.LogHabitatRestoreFailure(planEntry.Entry, failureReason);
                        transaction.RollbackMovedJoyPawnsToOriginalOwners(
                            map,
                            fallbackCell,
                            ref failureReason);
                        return false;
                    }
                }

                for (int i = 0; i < restorePlan.Count; i++)
                {
                    HabitatJoyRestorePlanEntry planEntry = restorePlan[i];
                    access.AddJoyRecord(
                        planEntry.Pawn,
                        planEntry.JoyKind,
                        planEntry.Entry.JoyStartTick,
                        planEntry.Entry.JoyTicks,
                        planEntry.Entry.JoyGainRate,
                        planEntry.Entry.MaxJoyTicks,
                        planEntry.Entry.IsJoying);

                    planEntry.Entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    planEntry.Entry.DebugNotes = "Restored from Dev Habitat joy real-launch staging before incoming base.Impact.";
                }

                manifest.RemoveHabitatJoyEntries();
                transaction.MarkCompleted();
                failureReason = null;
                return true;
            }
    }
}
