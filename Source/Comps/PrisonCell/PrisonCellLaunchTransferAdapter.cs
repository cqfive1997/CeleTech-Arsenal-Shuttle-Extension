using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttlePrisonCellOccupancy
    {
        internal static class PrisonCellLaunchTransferAdapter
        {
            internal static bool CanTransferPrisonersForLaunch(
                CompShuttlePrisonCellOccupancy owner,
                out string failReason)
            {
                List<PrisonCellPrisonerLaunchTransferPlan> plans;
                return TryBuildPrisonerLaunchTransferPlans(owner, out plans, out failReason);
            }

            internal static bool TryExportPrisonersForLaunch(
                CompShuttlePrisonCellOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> destination,
                bool requireEmptyDestination,
                out string failReason)
            {
                failReason = null;
                owner.EnsureInitialized();

                if (manifest == null)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner export requires a holder launch manifest.";
                    return false;
                }

                if (destination == null)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner export requires a launch handoff holder.";
                    return false;
                }

                manifest.EnsureInitialized();
                if (manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind).Count > 0)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because PrisonCell manifest entries are already active.";
                    return false;
                }

                if (requireEmptyDestination && destination.Count > 0)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because destination holder is not empty.";
                    return false;
                }

                List<PrisonCellPrisonerLaunchTransferPlan> plans;
                if (!TryBuildPrisonerLaunchTransferPlans(owner, out plans, out failReason))
                {
                    return false;
                }

                if (plans.Count == 0)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner export no-op: no prisoners are held.";
                    return true;
                }

                int exportTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
                string successPhase = requireEmptyDestination
                    ? ShuttleHolderLaunchManifestConstants.PhaseExported
                    : ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                List<Pawn> movedPawns = new List<Pawn>();
                for (int i = 0; i < plans.Count; i++)
                {
                    PrisonCellPrisonerLaunchTransferPlan plan = plans[i];
                    ShuttleHolderLaunchManifestEntry entry = manifest.AddPrisonCellPrisonerEntry(
                        plan.PawnThingID,
                        plan.PawnDefName,
                        plan.PawnLabel,
                        plan.OriginalRecordIndex,
                        plan.AdmissionTick,
                        plan.OriginalFaction,
                        plan.HostFaction,
                        plan.InteractionMode,
                        plan.WasPrisonerOnAdmission,
                        plan.Released,
                        plan.PendingRelease,
                        plan.LastFedTick,
                        plan.LastFedNutrition,
                        plan.LastFedFoodLabel,
                        plan.LastTendedTick,
                        plan.LastTendMedicineLabel,
                        plan.LastAutoFeedAttemptTick,
                        plan.LastAutoFeedSuccessTick,
                        plan.LastAutoFeedFailureTick,
                        plan.LastAutoFeedFailureReason,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseNone,
                        "PrisonCell prisoner export transaction entry created before movement commit.");

                    if (!destination.TryAddOrTransfer(plan.Pawn, false))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "TryAddOrTransfer to PrisonCell launch handoff failed.";
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export failed while moving pawn thingID=" +
                            plan.PawnThingID +
                            ". Attempting rollback for already moved prisoners.";
                        bool rollbackSucceeded = TryRollbackMovedPrisonersToPrisonCell(owner, movedPawns, destination);
                        if (rollbackSucceeded)
                        {
                            manifest.RemovePrisonCellPrisonerEntries();
                            failReason = "[CeleTech Shuttle] PrisonCell prisoner export failed while moving pawn thingID=" +
                                plan.PawnThingID +
                                ". Already moved prisoners were returned to the PrisonCell holder and temporary manifest entries were cleared.";
                        }
                        else
                        {
                            failReason = "[CeleTech Shuttle] PrisonCell prisoner export failed while moving pawn thingID=" +
                                plan.PawnThingID +
                                ". Rollback failed, so PrisonCell manifest entries remain active for diagnostics and recovery.";
                        }

                        return false;
                    }

                    if (!destination.Contains(plan.Pawn) || owner.IsHeldPawn(plan.Pawn))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "TryAddOrTransfer reported success but PrisonCell export postcondition failed. destinationContains=" +
                            destination.Contains(plan.Pawn) +
                            " stillInPrisonCell=" +
                            owner.IsHeldPawn(plan.Pawn);
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export failed postcondition for pawn thingID=" +
                            plan.PawnThingID +
                            ". Attempting rollback for already moved prisoners.";
                        movedPawns.Add(plan.Pawn);
                        bool rollbackSucceeded = TryRollbackMovedPrisonersToPrisonCell(owner, movedPawns, destination);
                        if (rollbackSucceeded)
                        {
                            manifest.RemovePrisonCellPrisonerEntries();
                            failReason = "[CeleTech Shuttle] PrisonCell prisoner export failed postcondition for pawn thingID=" +
                                plan.PawnThingID +
                                ". Already moved prisoners were returned to the PrisonCell holder and temporary manifest entries were cleared.";
                        }
                        else
                        {
                            failReason = "[CeleTech Shuttle] PrisonCell prisoner export failed postcondition for pawn thingID=" +
                                plan.PawnThingID +
                                ". Rollback failed, so PrisonCell manifest entries remain active for diagnostics and recovery.";
                        }

                        return false;
                    }

                    entry.TransferPhase = successPhase;
                    entry.DebugNotes = requireEmptyDestination
                        ? "Exported from PrisonCell holder to local staging."
                        : "Exported from PrisonCell holder to launch handoff.";
                    movedPawns.Add(plan.Pawn);
                }

                for (int i = 0; i < plans.Count; i++)
                {
                    owner.RemovePrisonerRecordByThingID(plans[i].PawnThingID);
                }

                failReason = "[CeleTech Shuttle] PrisonCell prisoner export staged " + plans.Count + " prisoner(s).";
                return true;
            }

            internal static bool TryRollbackPrisonerLaunchExport(
                CompShuttlePrisonCellOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> source,
                out string notice)
            {
                return TryReturnPrisonersFromLaunchStaging(
                    owner,
                    manifest,
                    source,
                    null,
                    ShuttleHolderLaunchManifestConstants.PhaseRolledBack,
                    "Rolled back from PrisonCell launch handoff.",
                    out notice);
            }

            internal static bool TryRestorePrisonersFromLaunchStaging(
                CompShuttlePrisonCellOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                out string notice)
            {
                return TryReturnPrisonersFromLaunchStaging(
                    owner,
                    manifest,
                    primarySource,
                    secondarySource,
                    ShuttleHolderLaunchManifestConstants.PhaseRestored,
                    "Restored from incoming launch handoff before base.Impact.",
                    out notice);
            }

            private static bool TryBuildPrisonerLaunchTransferPlans(
                CompShuttlePrisonCellOccupancy owner,
                out List<PrisonCellPrisonerLaunchTransferPlan> plans,
                out string failReason)
            {
                plans = new List<PrisonCellPrisonerLaunchTransferPlan>();
                failReason = null;

                owner.EnsureInitialized();
                if (!TryValidateNoDuplicatePrisonerRecordsForHeldPawns(owner, out failReason))
                {
                    return false;
                }

                owner.ReconcilePrisonerRecords();
                if (owner.prisonCellHeldThings == null || owner.prisonCellHeldThings.Count == 0)
                {
                    return true;
                }

                HashSet<int> seenPawnIDs = new HashSet<int>();
                for (int i = 0; i < owner.prisonCellHeldThings.Count; i++)
                {
                    Thing heldThing = owner.prisonCellHeldThings[i];
                    Pawn pawn = heldThing as Pawn;
                    if (pawn == null)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because the holder contains a non-pawn Thing.";
                        return false;
                    }

                    if (pawn.Destroyed || pawn.Dead)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because a prisoner is invalid: " +
                            pawn.LabelShortCap;
                        return false;
                    }

                    if (!seenPawnIDs.Add(pawn.thingIDNumber))
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because a prisoner appears more than once.";
                        return false;
                    }

                    ShuttlePrisonerRecord record;
                    int recordIndex;
                    if (!owner.TryFindPrisonerRecord(pawn, out record, out recordIndex))
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because no prisoner record exists for " +
                            pawn.LabelShortCap +
                            ".";
                        return false;
                    }

                    int recordCount = owner.CountPrisonerRecordsForThingID(pawn.thingIDNumber);
                    if (recordCount != 1)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused because prisoner record count is invalid. thingID=" +
                            pawn.thingIDNumber +
                            " recordCount=" +
                            recordCount +
                            ".";
                        return false;
                    }

                    record.Sanitize();
                    plans.Add(new PrisonCellPrisonerLaunchTransferPlan(pawn, record, recordIndex));
                }

                return true;
            }

            private static bool TryValidateNoDuplicatePrisonerRecordsForHeldPawns(
                CompShuttlePrisonCellOccupancy owner,
                out string failReason)
            {
                failReason = null;
                if (owner.prisonerRecords == null || owner.prisonerRecords.Count <= 1)
                {
                    return true;
                }

                HashSet<int> seen = new HashSet<int>();
                for (int i = 0; i < owner.prisonerRecords.Count; i++)
                {
                    ShuttlePrisonerRecord record = owner.prisonerRecords[i];
                    if (record == null)
                    {
                        continue;
                    }

                    record.Sanitize();
                    if (record.PawnThingIDNumber <= 0)
                    {
                        continue;
                    }

                    Pawn pawn = record.Prisoner;
                    if (pawn == null || !owner.IsHeldPawn(pawn))
                    {
                        continue;
                    }

                    if (!seen.Add(record.PawnThingIDNumber))
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner export refused duplicate live prisoner record for thingID=" +
                            record.PawnThingIDNumber +
                            ".";
                        return false;
                    }
                }

                return true;
            }

            private static bool TryReturnPrisonersFromLaunchStaging(
                CompShuttlePrisonCellOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                string successPhase,
                string successNotes,
                out string notice)
            {
                notice = null;
                owner.EnsureInitialized();

                if (manifest == null)
                {
                    notice = "[CeleTech Shuttle] PrisonCell prisoner restore requires a holder launch manifest.";
                    return false;
                }

                if (primarySource == null && secondarySource == null)
                {
                    notice = "[CeleTech Shuttle] PrisonCell prisoner restore requires a launch handoff holder.";
                    return false;
                }

                manifest.EnsureInitialized();
                List<ShuttleHolderLaunchManifestEntry> entries;
                if (!TryGetPrisonCellPrisonerManifestEntriesForTransaction(manifest, out entries, out notice))
                {
                    return false;
                }

                if (entries.Count == 0)
                {
                    notice = "[CeleTech Shuttle] No PrisonCell prisoner manifest entries to restore.";
                    return true;
                }

                List<PrisonCellPrisonerReturnPlan> plans;
                List<ShuttleHolderLaunchManifestEntry> deadPrisonerEntries;
                if (!TryBuildPrisonCellPrisonerReturnPlans(
                    owner,
                    entries,
                    primarySource,
                    secondarySource,
                    out plans,
                    out deadPrisonerEntries,
                    out notice))
                {
                    return false;
                }

                List<PrisonCellPrisonerReturnPlan> movedPlans = new List<PrisonCellPrisonerReturnPlan>();
                for (int i = 0; i < plans.Count; i++)
                {
                    PrisonCellPrisonerReturnPlan plan = plans[i];
                    if (plan.AlreadyInPrisonCell)
                    {
                        continue;
                    }

                    if (!owner.prisonCellHeldThings.TryAddOrTransfer(plan.Pawn, false))
                    {
                        notice = "[CeleTech Shuttle] PrisonCell prisoner restore failed while returning pawn thingID=" +
                            plan.Entry.ThingID +
                            ". Rolling back moved prisoners to source.";
                        bool rollbackSucceeded = TryRollbackReturnedPrisonersToSources(owner, movedPlans);
                        if (!rollbackSucceeded)
                        {
                            notice += " Rollback could not return every already-moved prisoner to its original source; manifest remains active for recovery.";
                        }

                        return false;
                    }

                    if (!owner.IsHeldPawn(plan.Pawn) ||
                        (plan.SourceOwner != null && plan.SourceOwner.Contains(plan.Pawn)))
                    {
                        notice = "[CeleTech Shuttle] PrisonCell prisoner restore failed postcondition for pawn thingID=" +
                            plan.Entry.ThingID +
                            ". Rolling back moved prisoners to source. held=" +
                            owner.IsHeldPawn(plan.Pawn) +
                            " sourceStillContains=" +
                            (plan.SourceOwner != null && plan.SourceOwner.Contains(plan.Pawn));
                        movedPlans.Add(plan);
                        bool rollbackSucceeded = TryRollbackReturnedPrisonersToSources(owner, movedPlans);
                        if (!rollbackSucceeded)
                        {
                            notice += " Rollback could not return every already-moved prisoner to its original source; manifest remains active for recovery.";
                        }

                        return false;
                    }

                    movedPlans.Add(plan);
                }

                for (int i = 0; i < plans.Count; i++)
                {
                    PrisonCellPrisonerReturnPlan plan = plans[i];
                    owner.RemovePrisonerRecordByThingID(plan.Entry.ThingID);
                    owner.prisonerRecords.Add(ShuttlePrisonerRecord.FromLaunchManifestEntry(
                        plan.Pawn,
                        plan.Entry));
                    plan.Entry.TransferPhase = successPhase;
                    plan.Entry.DebugNotes = plan.AlreadyInPrisonCell
                        ? successNotes + " Pawn was already back in the PrisonCell holder."
                        : successNotes;
                }

                for (int i = 0; i < deadPrisonerEntries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = deadPrisonerEntries[i];
                    owner.RemovePrisonerRecordByThingID(entry.ThingID);
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseEjected;
                    entry.DebugNotes =
                        "Prisoner died while outside the PrisonCell holder. The containing Corpse remains in the incoming payload for ordinary arrival handling.";
                }

                int restoredCount = plans.Count;
                int deadCount = deadPrisonerEntries.Count;
                manifest.RemovePrisonCellPrisonerEntries();
                owner.ReconcilePrisonerRecords();
                notice = "[CeleTech Shuttle] PrisonCell prisoner staging returned " +
                    restoredCount +
                    " living prisoner(s) and safely resolved " +
                    deadCount +
                    " dead prisoner(s).";
                return true;
            }

            private static bool TryGetPrisonCellPrisonerManifestEntriesForTransaction(
                ShuttleHolderLaunchManifest manifest,
                out List<ShuttleHolderLaunchManifestEntry> entries,
                out string failReason)
            {
                entries = new List<ShuttleHolderLaunchManifestEntry>();
                failReason = null;
                if (manifest == null)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore requires a holder launch manifest.";
                    return false;
                }

                manifest.EnsureInitialized();
                if (manifest.Entries == null)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused because manifest entries list is unavailable.";
                    return false;
                }

                for (int i = 0; i < manifest.Entries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                    if (entry == null)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused because manifest entry index=" +
                            i +
                            " is null.";
                        return false;
                    }

                    entry.EnsureInitialized();
                    if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
                    {
                        entries.Add(entry);
                    }
                }

                return true;
            }

            private static bool TryBuildPrisonCellPrisonerReturnPlans(
                CompShuttlePrisonCellOccupancy owner,
                List<ShuttleHolderLaunchManifestEntry> entries,
                ThingOwner<Thing> primarySource,
                ThingOwner<Thing> secondarySource,
                out List<PrisonCellPrisonerReturnPlan> plans,
                out List<ShuttleHolderLaunchManifestEntry> deadPrisonerEntries,
                out string failReason)
            {
                plans = new List<PrisonCellPrisonerReturnPlan>();
                deadPrisonerEntries = new List<ShuttleHolderLaunchManifestEntry>();
                failReason = null;

                if (entries == null || entries.Count == 0)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused because no manifest entries were provided.";
                    return false;
                }

                int slots = owner.GetPrisonerSlots();
                if (slots <= 0)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused because PrisonCell slots are unavailable.";
                    return false;
                }

                HashSet<int> seenEntryThingIDs = new HashSet<int>();
                for (int i = 0; i < entries.Count; i++)
                {
                    ShuttleHolderLaunchManifestEntry entry = entries[i];
                    if (entry == null)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused because a manifest entry is null.";
                        return false;
                    }

                    entry.EnsureInitialized();
                    if (entry.HolderKind != ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused non-PrisonCell manifest entry. " +
                            entry.DumpForDebug();
                        return false;
                    }

                    if (entry.ThingID <= 0)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused manifest entry with invalid thingID. " +
                            entry.DumpForDebug();
                        return false;
                    }

                    if (!seenEntryThingIDs.Add(entry.ThingID))
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused duplicate manifest entry for prisoner thingID=" +
                            entry.ThingID +
                            ".";
                        return false;
                    }

                    int recordCount = owner.CountPrisonerRecordsForThingID(entry.ThingID);
                    if (recordCount > 1)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused duplicate prisoner records for thingID=" +
                            entry.ThingID +
                            " recordCount=" +
                            recordCount +
                            ".";
                        return false;
                    }

                    PrisonCellManifestPawnResolution resolution =
                        PrisonCellManifestPawnResolver.ResolveInOwners(
                        entry,
                        primarySource,
                        secondarySource);
                    if (resolution.Kind == PrisonCellManifestPawnResolutionKind.Invalid ||
                        resolution.Kind == PrisonCellManifestPawnResolutionKind.Ambiguous)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused invalid source identity. " +
                            (resolution.FailureReason ?? "No resolver detail.");
                        return false;
                    }

                    Pawn heldPawn = owner.FindHeldPrisonerByThingIDNoReconcile(entry.ThingID);
                    if (resolution.Kind == PrisonCellManifestPawnResolutionKind.DeadPawnCorpse)
                    {
                        if (heldPawn != null)
                        {
                            failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused duplicate live/dead owner state. thingID=" +
                                entry.ThingID +
                                " heldPawn=" +
                                heldPawn +
                                " corpse=" +
                                resolution.ContainerThing +
                                ".";
                            return false;
                        }

                        deadPrisonerEntries.Add(entry);
                        continue;
                    }

                    Pawn sourcePawn = resolution.Kind == PrisonCellManifestPawnResolutionKind.LivePawn
                        ? resolution.Pawn
                        : null;
                    ThingOwner<Thing> sourceOwner = resolution.SourceOwner as ThingOwner<Thing>;
                    if (sourcePawn == null)
                    {
                        if (heldPawn == null)
                        {
                            failReason = "[CeleTech Shuttle] PrisonCell prisoner restore could not find staged pawn thingID=" +
                                entry.ThingID +
                                ".";
                            return false;
                        }

                        if (!ValidatePrisonCellPrisonerReturnPawn(entry, heldPawn, out failReason))
                        {
                            return false;
                        }

                        plans.Add(new PrisonCellPrisonerReturnPlan(entry, heldPawn, null, true));
                        continue;
                    }

                    if (!ValidatePrisonCellPrisonerReturnPawn(entry, sourcePawn, out failReason))
                    {
                        return false;
                    }

                    if (heldPawn != null && heldPawn != sourcePawn)
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused duplicate prisoner owner state. thingID=" +
                            entry.ThingID +
                            " sourcePawn=" +
                            sourcePawn +
                            " heldPawn=" +
                            heldPawn +
                            ".";
                        return false;
                    }

                    if (heldPawn == sourcePawn || owner.IsHeldPawn(sourcePawn))
                    {
                        plans.Add(new PrisonCellPrisonerReturnPlan(entry, sourcePawn, null, true));
                        continue;
                    }

                    if (sourceOwner == null || !sourceOwner.Contains(sourcePawn))
                    {
                        failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused inconsistent source owner for thingID=" +
                            entry.ThingID +
                            ".";
                        return false;
                    }

                    plans.Add(new PrisonCellPrisonerReturnPlan(entry, sourcePawn, sourceOwner, false));
                }

                int movingCount = 0;
                for (int i = 0; i < plans.Count; i++)
                {
                    if (!plans[i].AlreadyInPrisonCell)
                    {
                        movingCount++;
                    }
                }

                int heldCount = owner.CountHeldPrisoners();
                if (heldCount + movingCount > slots)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused because PrisonCell slots are unavailable. slots=" +
                        slots +
                        " held=" +
                        heldCount +
                        " moving=" +
                        movingCount +
                        ".";
                    return false;
                }

                return true;
            }

            private static bool ValidatePrisonCellPrisonerReturnPawn(
                ShuttleHolderLaunchManifestEntry entry,
                Pawn pawn,
                out string failReason)
            {
                failReason = null;
                if (entry == null || pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused invalid pawn for manifest entry.";
                    return false;
                }

                if (entry.ThingID > 0 && pawn.thingIDNumber != entry.ThingID)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused pawn thingID mismatch. entryThingID=" +
                        entry.ThingID +
                        " pawnThingID=" +
                        pawn.thingIDNumber +
                        ".";
                    return false;
                }

                if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                {
                    failReason = "[CeleTech Shuttle] PrisonCell prisoner restore refused non-humanlike pawn thingID=" +
                        pawn.thingIDNumber +
                        ".";
                    return false;
                }

                return true;
            }

            private static bool TryRollbackMovedPrisonersToPrisonCell(
                CompShuttlePrisonCellOccupancy owner,
                List<Pawn> movedPawns,
                ThingOwner<Thing> source)
            {
                bool allReturned = true;
                if (movedPawns == null)
                {
                    return true;
                }

                for (int i = movedPawns.Count - 1; i >= 0; i--)
                {
                    Pawn pawn = movedPawns[i];
                    if (pawn == null || pawn.Destroyed || owner.IsHeldPawn(pawn))
                    {
                        continue;
                    }

                    if (!owner.prisonCellHeldThings.TryAddOrTransfer(pawn, false) ||
                        !owner.IsHeldPawn(pawn) ||
                        (source != null && source.Contains(pawn)))
                    {
                        allReturned = false;
                    }
                }

                owner.ReconcilePrisonerRecords();
                return allReturned;
            }

            private static bool TryRollbackReturnedPrisonersToSources(
                CompShuttlePrisonCellOccupancy owner,
                List<PrisonCellPrisonerReturnPlan> movedPlans)
            {
                bool allReturned = true;
                if (movedPlans == null)
                {
                    return true;
                }

                for (int i = movedPlans.Count - 1; i >= 0; i--)
                {
                    PrisonCellPrisonerReturnPlan plan = movedPlans[i];
                    if (plan == null || plan.AlreadyInPrisonCell || plan.Pawn == null || plan.Pawn.Destroyed)
                    {
                        continue;
                    }

                    if (plan.SourceOwner == null ||
                        !plan.SourceOwner.TryAddOrTransfer(plan.Pawn, false) ||
                        !plan.SourceOwner.Contains(plan.Pawn) ||
                        owner.IsHeldPawn(plan.Pawn))
                    {
                        allReturned = false;
                    }
                }

                owner.ReconcilePrisonerRecords();
                return allReturned;
            }

            private sealed class PrisonCellPrisonerLaunchTransferPlan
            {
                internal PrisonCellPrisonerLaunchTransferPlan(
                    Pawn pawn,
                    ShuttlePrisonerRecord record,
                    int originalRecordIndex)
                {
                    this.Pawn = pawn;
                    this.PawnThingID = pawn != null ? pawn.thingIDNumber : -1;
                    this.PawnDefName = pawn != null && pawn.def != null ? pawn.def.defName : null;
                    this.PawnLabel = pawn != null ? pawn.LabelShort : null;
                    this.OriginalRecordIndex = originalRecordIndex;
                    this.AdmissionTick = record != null ? record.AdmissionTick : -1;
                    this.OriginalFaction = record != null ? record.OriginalFaction : null;
                    this.HostFaction = record != null ? record.HostFaction : null;
                    this.InteractionMode = record != null ? record.InteractionMode : null;
                    this.WasPrisonerOnAdmission = record != null && record.WasPrisonerOnAdmission;
                    this.Released = record != null && record.Released;
                    this.PendingRelease = record != null && record.PendingRelease;
                    this.LastFedTick = record != null ? record.LastFedTick : -1;
                    this.LastFedNutrition = record != null ? record.LastFedNutrition : 0f;
                    this.LastFedFoodLabel = record != null ? record.LastFedFoodLabel : string.Empty;
                    this.LastTendedTick = record != null ? record.LastTendedTick : -1;
                    this.LastTendMedicineLabel = record != null ? record.LastTendMedicineLabel : string.Empty;
                    this.LastAutoFeedAttemptTick = record != null ? record.LastAutoFeedAttemptTick : -1;
                    this.LastAutoFeedSuccessTick = record != null ? record.LastAutoFeedSuccessTick : -1;
                    this.LastAutoFeedFailureTick = record != null ? record.LastAutoFeedFailureTick : -1;
                    this.LastAutoFeedFailureReason = record != null ? record.LastAutoFeedFailureReason : string.Empty;
                }

                internal Pawn Pawn { get; private set; }

                internal int PawnThingID { get; private set; }

                internal string PawnDefName { get; private set; }

                internal string PawnLabel { get; private set; }

                internal int OriginalRecordIndex { get; private set; }

                internal int AdmissionTick { get; private set; }

                internal Faction OriginalFaction { get; private set; }

                internal Faction HostFaction { get; private set; }

                internal PrisonerInteractionModeDef InteractionMode { get; private set; }

                internal bool WasPrisonerOnAdmission { get; private set; }

                internal bool Released { get; private set; }

                internal bool PendingRelease { get; private set; }

                internal int LastFedTick { get; private set; }

                internal float LastFedNutrition { get; private set; }

                internal string LastFedFoodLabel { get; private set; }

                internal int LastTendedTick { get; private set; }

                internal string LastTendMedicineLabel { get; private set; }

                internal int LastAutoFeedAttemptTick { get; private set; }

                internal int LastAutoFeedSuccessTick { get; private set; }

                internal int LastAutoFeedFailureTick { get; private set; }

                internal string LastAutoFeedFailureReason { get; private set; }
            }

            private sealed class PrisonCellPrisonerReturnPlan
            {
                internal PrisonCellPrisonerReturnPlan(
                    ShuttleHolderLaunchManifestEntry entry,
                    Pawn pawn,
                    ThingOwner<Thing> sourceOwner,
                    bool alreadyInPrisonCell)
                {
                    this.Entry = entry;
                    this.Pawn = pawn;
                    this.SourceOwner = sourceOwner;
                    this.AlreadyInPrisonCell = alreadyInPrisonCell;
                }

                internal ShuttleHolderLaunchManifestEntry Entry { get; private set; }

                internal Pawn Pawn { get; private set; }

                internal ThingOwner<Thing> SourceOwner { get; private set; }

                internal bool AlreadyInPrisonCell { get; private set; }
            }
        }
    }
}
