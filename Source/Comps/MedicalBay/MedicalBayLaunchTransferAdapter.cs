using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal static class MedicalBayLaunchTransferAdapter
        {
        internal static bool CanTransferPatientsForLaunch(
            CompShuttleMedicalBayOccupancy owner,
            out string failureReason)
        {
            List<MedicalBayPatientLaunchTransferPlan> plans;
            return TryBuildMedicalBayPatientExportPlans(owner, out plans, out failureReason);
        }

        internal static bool TryExportPatientsForLaunch(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> destination,
            bool requireEmptyDestination,
            out string failureReason)
        {
            failureReason = null;
            owner.EnsureInitialized();

            if (manifest == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient export requires a holder launch manifest.";
                return false;
            }

            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient export requires a patient staging or launch handoff holder.";
                return false;
            }

            manifest.EnsureInitialized();
            if (manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind).Count > 0)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because MedicalBay patient manifest entries are already active.";
                return false;
            }

            if (requireEmptyDestination && destination.Count > 0)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because the destination staging/handoff holder is not empty.";
                return false;
            }

            List<MedicalBayPatientLaunchTransferPlan> plans;
            if (!TryBuildMedicalBayPatientExportPlans(owner, out plans, out failureReason))
            {
                return false;
            }

            if (plans.Count == 0)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient export no-op: no patients are held.";
                return true;
            }

            int exportTick = ShuttleTickUtility.TicksGameOrMinusOne();
            string successPhase = requireEmptyDestination
                ? ShuttleHolderLaunchManifestConstants.PhaseExported
                : ShuttleHolderLaunchManifestConstants.PhaseInFlight;
            List<Pawn> movedPawns = new List<Pawn>();
            for (int i = 0; i < plans.Count; i++)
            {
                MedicalBayPatientLaunchTransferPlan plan = plans[i];
                ShuttleHolderLaunchManifestEntry entry = manifest.AddMedicalBayPatientEntry(
                    plan.PawnThingID,
                    plan.PawnDefName,
                    plan.PawnLabel,
                    plan.OriginalRecordIndex,
                    plan.AdmissionMode,
                    plan.AdmissionTick,
                    plan.WasDownedOnAdmission,
                    plan.LastKnownReasonLabel,
                    plan.LastKnownSeverityLabel,
                    exportTick,
                    ShuttleHolderLaunchManifestConstants.PhaseNone,
                    "MedicalBay patient export transaction entry created before movement commit.");

                if (!destination.TryAddOrTransfer(plan.Pawn, false))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "TryAddOrTransfer to MedicalBay patient staging or launch handoff failed.";
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export failed while moving pawn thingID=" +
                        plan.PawnThingID +
                        ". Attempting rollback for already moved patients.";
                    bool rollbackSucceeded = TryRollbackMovedPatientsToMedicalBay(owner, movedPawns, destination, failureReason);
                    if (rollbackSucceeded)
                    {
                        manifest.RemoveMedicalBayPatientEntries();
                        failureReason = "[CeleTech Shuttle] MedicalBay patient export failed while moving pawn thingID=" +
                            plan.PawnThingID +
                            ". Already moved patients were returned to the MedicalBay holder and temporary manifest entries were cleared.";
                    }
                    else
                    {
                        failureReason = "[CeleTech Shuttle] MedicalBay patient export failed while moving pawn thingID=" +
                            plan.PawnThingID +
                            ". Rollback failed, so MedicalBay patient manifest entries remain active for diagnostics and recovery.";
                    }

                    return false;
                }

                if (!destination.Contains(plan.Pawn) || owner.IsHeldPawn(plan.Pawn))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "TryAddOrTransfer reported success but export postcondition failed. destinationContains=" +
                        destination.Contains(plan.Pawn) +
                        " stillInMedicalBay=" +
                        owner.IsHeldPawn(plan.Pawn);
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export failed postcondition for pawn thingID=" +
                        plan.PawnThingID +
                        ". Attempting rollback for already moved patients.";
                    movedPawns.Add(plan.Pawn);
                    bool rollbackSucceeded = TryRollbackMovedPatientsToMedicalBay(owner, movedPawns, destination, failureReason);
                    if (rollbackSucceeded)
                    {
                        manifest.RemoveMedicalBayPatientEntries();
                        failureReason = "[CeleTech Shuttle] MedicalBay patient export failed postcondition for pawn thingID=" +
                            plan.PawnThingID +
                            ". Already moved patients were returned to the MedicalBay holder and temporary manifest entries were cleared.";
                    }
                    else
                    {
                        failureReason = "[CeleTech Shuttle] MedicalBay patient export failed postcondition for pawn thingID=" +
                            plan.PawnThingID +
                            ". Rollback failed, so MedicalBay patient manifest entries remain active for diagnostics and recovery.";
                    }

                    return false;
                }

                entry.TransferPhase = successPhase;
                entry.DebugNotes = requireEmptyDestination
                    ? "Exported from MedicalBay holder to patient staging."
                    : "Exported from MedicalBay holder to launch handoff.";
                movedPawns.Add(plan.Pawn);
            }

            for (int i = 0; i < plans.Count; i++)
            {
                MedicalBayPatientLaunchTransferPlan plan = plans[i];
                owner.RemovePatientRecord(plan.Pawn);
                owner.ReleaseAllReservationsForPatient(plan.PawnThingID);
            }

            owner.ClearStaleAdmissionReservations();
            owner.ClearStaleTreatmentReservations();
            failureReason = "[CeleTech Shuttle] MedicalBay patient export staged " + plans.Count + " patient(s).";
            return true;
        }

        internal static bool TryRollbackPatientLaunchExport(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
            ThingOwner<Thing> source,
            out string notice)
        {
            return TryReturnPatientsFromLaunchStaging(
                owner,
                manifest,
                source,
                null,
                ShuttleHolderLaunchManifestConstants.PhaseRolledBack,
                "Rolled back from local MedicalBay patient staging.",
                out notice);
        }

        internal static bool TryRestorePatientsFromLaunchStaging(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> source,
            out string notice)
        {
            return TryReturnPatientsFromLaunchStaging(
                owner,
                manifest,
                source,
                null,
                ShuttleHolderLaunchManifestConstants.PhaseRestored,
                "Restored from local MedicalBay patient staging.",
                out notice);
        }

        internal static bool TryRestorePatientsFromLaunchStaging(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
                ThingOwner<Thing> primarySource,
            ThingOwner<Thing> secondarySource,
            out string notice)
        {
            return TryReturnPatientsFromLaunchStaging(
                owner,
                manifest,
                primarySource,
                secondarySource,
                ShuttleHolderLaunchManifestConstants.PhaseRestored,
                "Restored from incoming launch handoff before base.Impact.",
                out notice);
        }

        private static bool TryBuildMedicalBayPatientExportPlans(
                CompShuttleMedicalBayOccupancy owner,
                out List<MedicalBayPatientLaunchTransferPlan> plans,
            out string failureReason)
        {
            plans = new List<MedicalBayPatientLaunchTransferPlan>();
            failureReason = null;

            owner.EnsureInitialized();
            if (!owner.TryValidateNoDuplicatePatientRecordsForHeldPawns(out failureReason))
            {
                return false;
            }

            owner.ReconcilePatientRecordsToHeldPawns();

            if (owner.medicalHeldThings == null || owner.medicalHeldThings.Count == 0)
            {
                return true;
            }

            HashSet<int> seenPawnIDs = new HashSet<int>();
            for (int i = 0; i < owner.medicalHeldThings.Count; i++)
            {
                Thing heldThing = owner.medicalHeldThings[i];
                Pawn pawn = heldThing as Pawn;
                if (pawn == null)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because the holder contains a non-pawn Thing.";
                    return false;
                }

                if (pawn.Destroyed || pawn.Dead)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because a patient is invalid: " +
                        pawn.LabelShortCap;
                    return false;
                }

                if (!seenPawnIDs.Add(pawn.thingIDNumber))
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because a patient appears more than once.";
                    return false;
                }

                ShuttleMedicalPatientRecord record;
                int recordIndex;
                if (!owner.TryFindPatientRecord(pawn, out record, out recordIndex))
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because no patient record exists for " +
                        pawn.LabelShortCap +
                        ".";
                    return false;
                }

                int recordCount = owner.CountPatientRecordsForThingID(pawn.thingIDNumber);
                if (recordCount != 1)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export refused because patient record count is invalid. thingID=" +
                        pawn.thingIDNumber +
                        " recordCount=" +
                        recordCount +
                        ".";
                    return false;
                }

                record.Sanitize();
                plans.Add(new MedicalBayPatientLaunchTransferPlan(pawn, record, recordIndex));
            }

            return true;
        }

        private static bool TryGetMedicalBayPatientManifestEntriesForTransaction(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifest manifest,
            out List<ShuttleHolderLaunchManifestEntry> entries,
            out string failureReason)
        {
            entries = new List<ShuttleHolderLaunchManifestEntry>();
            failureReason = null;
            if (manifest == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore requires a holder launch manifest.";
                return false;
            }

            manifest.EnsureInitialized();
            if (manifest.Entries == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because manifest entries list is unavailable.";
                return false;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because manifest entry index=" +
                        i +
                        " is null.";
                    return false;
                }

                entry.EnsureInitialized();
                if (entry.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
                {
                    entries.Add(entry);
                }
            }

            return true;
        }

        private static bool TryBuildMedicalBayPatientReturnPlans(
                CompShuttleMedicalBayOccupancy owner,
                List<ShuttleHolderLaunchManifestEntry> entries,
            ThingOwner<Thing> primarySource,
            ThingOwner<Thing> secondarySource,
            out List<MedicalBayPatientReturnPlan> plans,
            out string failureReason)
        {
            plans = new List<MedicalBayPatientReturnPlan>();
            failureReason = null;

            if (entries == null || entries.Count == 0)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because no manifest entries were provided.";
                return false;
            }

            if (!TryValidateMedicalBayHolderForPatientReturn(owner, out failureReason))
            {
                return false;
            }

            HashSet<int> seenEntryThingIDs = new HashSet<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because a manifest entry is null.";
                    return false;
                }

                entry.EnsureInitialized();
                if (entry.HolderKind != ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused non-MedicalBay manifest entry. " +
                        entry.DumpForDebug();
                    return false;
                }

                if (entry.ThingID <= 0)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused manifest entry with invalid thingID. " +
                        entry.DumpForDebug();
                    return false;
                }

                if (!seenEntryThingIDs.Add(entry.ThingID))
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused duplicate manifest entry for patient thingID=" +
                        entry.ThingID +
                        ".";
                    return false;
                }

                int patientRecordCount = owner.CountPatientRecordsForThingID(entry.ThingID);
                if (patientRecordCount > 1)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused duplicate patient records for thingID=" +
                        entry.ThingID +
                        " recordCount=" +
                        patientRecordCount +
                        ".";
                    return false;
                }

                ThingOwner<Thing> sourceOwner;
                string sourceFailure;
                Pawn sourcePawn;
                if (!TryFindUniqueMedicalBayPatientInSources(
                    owner,
                    entry,
                    primarySource,
                    secondarySource,
                    out sourcePawn,
                    out sourceOwner,
                    out sourceFailure))
                {
                    failureReason = sourceFailure;
                    return false;
                }

                Pawn heldPawn = owner.FindHeldPatientByThingID(entry.ThingID);
                if (sourcePawn == null)
                {
                    if (heldPawn == null)
                    {
                        failureReason = "[CeleTech Shuttle] MedicalBay patient restore could not find staged pawn thingID=" +
                            entry.ThingID +
                            ".";
                        return false;
                    }

                    if (!ValidateMedicalBayPatientReturnPawn(owner, entry, heldPawn, out failureReason))
                    {
                        return false;
                    }

                    plans.Add(new MedicalBayPatientReturnPlan(entry, heldPawn, null, true));
                    continue;
                }

                if (!ValidateMedicalBayPatientReturnPawn(owner, entry, sourcePawn, out failureReason))
                {
                    return false;
                }

                if (heldPawn != null && heldPawn != sourcePawn)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused duplicate patient owner state. thingID=" +
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
                    plans.Add(new MedicalBayPatientReturnPlan(entry, sourcePawn, null, true));
                    continue;
                }

                if (sourceOwner == null || !sourceOwner.Contains(sourcePawn))
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused inconsistent source owner for thingID=" +
                        entry.ThingID +
                        ".";
                    return false;
                }

                plans.Add(new MedicalBayPatientReturnPlan(entry, sourcePawn, sourceOwner, false));
            }

            int movingCount = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                if (!plans[i].AlreadyInMedicalBay)
                {
                    movingCount++;
                }
            }

            int slots = owner.GetMedicalPatientSlots();
            int heldCount = CountUniqueHeldPawnsInMedicalBay(owner);
            if (slots <= 0 || heldCount + movingCount > slots)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because MedicalBay slots are unavailable. slots=" +
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

        private static bool TryReturnPatientsFromLaunchStaging(
                CompShuttleMedicalBayOccupancy owner,
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
                notice = "[CeleTech Shuttle] MedicalBay patient restore requires a holder launch manifest.";
                return false;
            }

            if (primarySource == null && secondarySource == null)
            {
                notice = "[CeleTech Shuttle] MedicalBay patient restore requires a patient staging or launch handoff holder.";
                return false;
            }

            manifest.EnsureInitialized();
            List<ShuttleHolderLaunchManifestEntry> entries;
            if (!TryGetMedicalBayPatientManifestEntriesForTransaction(owner, manifest, out entries, out notice))
            {
                return false;
            }

            if (entries.Count == 0)
            {
                notice = "[CeleTech Shuttle] No MedicalBay patient manifest entries to restore.";
                return true;
            }

            List<MedicalBayPatientReturnPlan> plans;
            if (!TryBuildMedicalBayPatientReturnPlans(
                owner,
                entries,
                primarySource,
                secondarySource,
                out plans,
                out notice))
            {
                return false;
            }

            List<MedicalBayPatientReturnPlan> movedPlans = new List<MedicalBayPatientReturnPlan>();
            for (int i = 0; i < plans.Count; i++)
            {
                MedicalBayPatientReturnPlan plan = plans[i];
                if (plan.AlreadyInMedicalBay)
                {
                    continue;
                }

                if (!owner.medicalHeldThings.TryAddOrTransfer(plan.Pawn, false))
                {
                    notice = "[CeleTech Shuttle] MedicalBay patient restore failed while returning pawn thingID=" +
                        plan.Entry.ThingID +
                        ". Rolling back moved patients to local staging.";
                    bool rollbackSucceeded = TryRollbackReturnedPatientsToSources(owner, movedPlans, notice);
                    if (!rollbackSucceeded)
                    {
                        notice += " Rollback could not return every already-moved patient to its original source; manifest remains active for recovery.";
                    }

                    return false;
                }

                if (!owner.IsHeldPawn(plan.Pawn) ||
                    (plan.SourceOwner != null && plan.SourceOwner.Contains(plan.Pawn)))
                {
                    notice = "[CeleTech Shuttle] MedicalBay patient restore failed postcondition for pawn thingID=" +
                        plan.Entry.ThingID +
                        ". Rolling back moved patients to local staging. held=" +
                        owner.IsHeldPawn(plan.Pawn) +
                        " sourceStillContains=" +
                        (plan.SourceOwner != null && plan.SourceOwner.Contains(plan.Pawn));
                    movedPlans.Add(plan);
                    bool rollbackSucceeded = TryRollbackReturnedPatientsToSources(owner, movedPlans, notice);
                    if (!rollbackSucceeded)
                    {
                        notice += " Rollback could not return every already-moved patient to its original source; manifest remains active for recovery.";
                    }

                    return false;
                }

                movedPlans.Add(plan);
            }

            for (int i = 0; i < plans.Count; i++)
            {
                MedicalBayPatientReturnPlan plan = plans[i];
                owner.RemovePatientRecord(plan.Entry.ThingID);
                owner.patientRecords.Add(new ShuttleMedicalPatientRecord(
                    plan.Pawn,
                    plan.Entry.AdmissionMode,
                    plan.Entry.AdmissionTick,
                    plan.Entry.WasDownedOnAdmission,
                    plan.Entry.LastKnownReasonLabel,
                    plan.Entry.LastKnownSeverityLabel));
                owner.ReleaseAllReservationsForPatient(plan.Entry.ThingID);
                plan.Entry.TransferPhase = successPhase;
                plan.Entry.DebugNotes = plan.AlreadyInMedicalBay
                    ? successNotes + " Pawn was already back in the MedicalBay holder."
                    : successNotes;
            }

            int restoredCount = plans.Count;
            manifest.RemoveMedicalBayPatientEntries();
            owner.ReconcilePatientRecordsToHeldPawns();
            owner.ClearStaleAdmissionReservations();
            owner.ClearStaleTreatmentReservations();
            notice = "[CeleTech Shuttle] MedicalBay patient staging returned " + restoredCount + " patient(s).";
            return true;
        }

        private static Pawn FindPawnInOwner(ThingOwner<Thing> owner, int thingID)
        {
            if (owner == null || thingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Pawn pawn = owner[i] as Pawn;
                if (pawn != null && pawn.thingIDNumber == thingID)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static Thing FindThingInOwner(ThingOwner<Thing> owner, int thingID)
        {
            if (owner == null || thingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (thing != null && thing.thingIDNumber == thingID)
                {
                    return thing;
                }
            }

            return null;
        }

        private static bool TryValidateMedicalBayHolderForPatientReturn(
                CompShuttleMedicalBayOccupancy owner,
                out string failureReason)
        {
            failureReason = null;
            if (owner.medicalHeldThings == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because MedicalBay holder is unavailable.";
                return false;
            }

            HashSet<int> seenHeldPawnIDs = new HashSet<int>();
            for (int i = 0; i < owner.medicalHeldThings.Count; i++)
            {
                Thing thing = owner.medicalHeldThings[i];
                Pawn pawn = thing as Pawn;
                if (pawn == null)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because MedicalBay holder contains a non-pawn Thing.";
                    return false;
                }

                if (pawn.Destroyed || pawn.Dead)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused invalid held patient. pawn=" +
                        pawn;
                    return false;
                }

                if (!seenHeldPawnIDs.Add(pawn.thingIDNumber))
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused duplicate held patient thingID=" +
                        pawn.thingIDNumber +
                        ".";
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindUniqueMedicalBayPatientInSources(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifestEntry entry,
            ThingOwner<Thing> primarySource,
            ThingOwner<Thing> secondarySource,
            out Pawn pawn,
            out ThingOwner<Thing> sourceOwner,
            out string failureReason)
        {
            pawn = null;
            sourceOwner = null;
            failureReason = null;
            Thing primaryThing = FindThingInOwner(primarySource, entry != null ? entry.ThingID : -1);
            Thing secondaryThing = !object.ReferenceEquals(primarySource, secondarySource)
                ? FindThingInOwner(secondarySource, entry != null ? entry.ThingID : -1)
                : null;

            if (primaryThing != null && secondaryThing != null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused because manifest thing appears in two source owners. thingID=" +
                    entry.ThingID +
                    ".";
                return false;
            }

            Thing foundThing = primaryThing ?? secondaryThing;
            if (foundThing == null)
            {
                return true;
            }

            pawn = foundThing as Pawn;
            if (pawn == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore found manifest thing but it is not a pawn. thingID=" +
                    entry.ThingID +
                    " defName=" +
                    (foundThing.def != null ? foundThing.def.defName : "null") +
                    ".";
                return false;
            }

            sourceOwner = primaryThing != null ? primarySource : secondarySource;
            return true;
        }

        private static bool ValidateMedicalBayPatientReturnPawn(
                CompShuttleMedicalBayOccupancy owner,
                ShuttleHolderLaunchManifestEntry entry,
            Pawn pawn,
            out string failureReason)
        {
            failureReason = null;
            if (entry == null || pawn == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused null entry or pawn.";
                return false;
            }

            if (pawn.Destroyed || pawn.Dead)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore refused destroyed/dead pawn thingID=" +
                    entry.ThingID +
                    ".";
                return false;
            }

            string pawnDefName = pawn.def != null ? pawn.def.defName : null;
            if (!string.IsNullOrEmpty(entry.DefName) && pawnDefName != entry.DefName)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient restore found def mismatch for thingID=" +
                    entry.ThingID +
                    " manifest=" +
                    entry.DefName +
                    " staged=" +
                    (pawnDefName ?? "null") +
                    ".";
                return false;
            }

            return true;
        }

        private static int CountUniqueHeldPawnsInMedicalBay(CompShuttleMedicalBayOccupancy owner)
        {
            if (owner.medicalHeldThings == null)
            {
                return 0;
            }

            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < owner.medicalHeldThings.Count; i++)
            {
                Pawn pawn = owner.medicalHeldThings[i] as Pawn;
                if (pawn != null)
                {
                    seen.Add(pawn.thingIDNumber);
                }
            }

            return seen.Count;
        }

        private static bool TryRollbackMovedPatientsToMedicalBay(
                CompShuttleMedicalBayOccupancy owner,
                List<Pawn> movedPawns,
            ThingOwner<Thing> source,
            string context)
        {
            if (movedPawns == null || movedPawns.Count == 0)
            {
                return true;
            }

            bool success = true;
            for (int i = movedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = movedPawns[i];
                if (pawn == null || pawn.Destroyed || owner.IsHeldPawn(pawn))
                {
                    continue;
                }

                if (owner.medicalHeldThings.TryAddOrTransfer(pawn, false))
                {
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason = null;
                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                if (transferState != null &&
                    transferState.TryRecoverTransferThing(
                        pawn,
                        "MedicalBay patient export rollback to MedicalBay failed. context=" +
                            (context ?? "null"),
                        owner.medicalHeldThings,
                        source,
                        owner.parent != null ? owner.parent.Map : null,
                        owner.parent != null ? owner.parent.Position : IntVec3.Invalid,
                        out recoveryStatus,
                        out recoveryFailureReason))
                {
                    if (recoveryStatus == ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                    {
                        continue;
                    }

                    Log.Warning("[CeleTech Shuttle] MedicalBay patient export rollback did not return pawn to MedicalBay, but emergency recovery secured it. context=" +
                        (context ?? "null") +
                        " pawn=" +
                        pawn +
                        " status=" +
                        recoveryStatus +
                        " note=" +
                        (recoveryFailureReason ?? "null"));
                    success = false;
                    continue;
                }

                Log.Error("[CeleTech Shuttle] MedicalBay patient export rollback failed and emergency recovery could not secure pawn. context=" +
                    (context ?? "null") +
                    " pawn=" +
                    pawn +
                    " recovery=" +
                    (recoveryFailureReason ?? "holder transfer state unavailable"));
                success = false;
            }

            return success;
        }

        private static void TryRollbackReturnedPatientsToSource(
                CompShuttleMedicalBayOccupancy owner,
                List<Pawn> movedPawns,
            ThingOwner<Thing> source,
            string context)
        {
            if (movedPawns == null || movedPawns.Count == 0)
            {
                return;
            }

            for (int i = movedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = movedPawns[i];
                if (pawn == null || pawn.Destroyed || source == null || source.Contains(pawn))
                {
                    continue;
                }

                if (source.TryAddOrTransfer(pawn, false))
                {
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason = null;
                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                if (transferState != null &&
                    transferState.TryRecoverTransferThing(
                        pawn,
                        "MedicalBay patient restore rollback to source failed. context=" +
                            (context ?? "null"),
                        source,
                        owner.medicalHeldThings,
                        owner.parent != null ? owner.parent.Map : null,
                        owner.parent != null ? owner.parent.Position : IntVec3.Invalid,
                        out recoveryStatus,
                        out recoveryFailureReason))
                {
                    if (recoveryStatus != ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                    {
                        Log.Warning("[CeleTech Shuttle] MedicalBay patient restore rollback used emergency recovery instead of the original source. context=" +
                            (context ?? "null") +
                            " pawn=" +
                            pawn +
                            " status=" +
                            recoveryStatus +
                            " note=" +
                            (recoveryFailureReason ?? "null"));
                    }

                    continue;
                }

                Log.Error("[CeleTech Shuttle] MedicalBay patient restore rollback failed and emergency recovery could not secure pawn. context=" +
                    (context ?? "null") +
                    " pawn=" +
                    pawn +
                    " recovery=" +
                    (recoveryFailureReason ?? "holder transfer state unavailable"));
            }
        }

        private static bool TryRollbackReturnedPatientsToSources(
                CompShuttleMedicalBayOccupancy owner,
                List<MedicalBayPatientReturnPlan> movedPlans,
            string context)
        {
            if (movedPlans == null || movedPlans.Count == 0)
            {
                return true;
            }

            bool success = true;
            for (int i = movedPlans.Count - 1; i >= 0; i--)
            {
                MedicalBayPatientReturnPlan plan = movedPlans[i];
                Pawn pawn = plan != null ? plan.Pawn : null;
                ThingOwner<Thing> source = plan != null ? plan.SourceOwner : null;
                if (pawn == null || pawn.Destroyed || source == null || source.Contains(pawn))
                {
                    continue;
                }

                if (source.TryAddOrTransfer(pawn, false))
                {
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason = null;
                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                if (transferState != null &&
                    transferState.TryRecoverTransferThing(
                        pawn,
                        "MedicalBay patient restore rollback to source failed. context=" +
                            (context ?? "null"),
                        source,
                        owner.medicalHeldThings,
                        owner.parent != null ? owner.parent.Map : null,
                        owner.parent != null ? owner.parent.Position : IntVec3.Invalid,
                        out recoveryStatus,
                        out recoveryFailureReason))
                {
                    if (recoveryStatus != ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                    {
                        Log.Warning("[CeleTech Shuttle] MedicalBay patient restore rollback used emergency recovery instead of the original source. context=" +
                            (context ?? "null") +
                            " pawn=" +
                            pawn +
                            " status=" +
                            recoveryStatus +
                            " note=" +
                            (recoveryFailureReason ?? "null"));
                        success = false;
                    }

                    continue;
                }

                Log.Error("[CeleTech Shuttle] MedicalBay patient restore rollback failed and emergency recovery could not secure pawn. context=" +
                    (context ?? "null") +
                    " pawn=" +
                    pawn +
                    " recovery=" +
                    (recoveryFailureReason ?? "holder transfer state unavailable"));
                success = false;
            }

            return success;
        }

        private sealed class MedicalBayPatientLaunchTransferPlan
        {
            internal MedicalBayPatientLaunchTransferPlan(
                Pawn pawn,
                ShuttleMedicalPatientRecord record,
                int originalRecordIndex)
            {
                this.Pawn = pawn;
                this.PawnThingID = pawn != null ? pawn.thingIDNumber : -1;
                this.PawnDefName = pawn != null && pawn.def != null ? pawn.def.defName : null;
                this.PawnLabel = record != null && !string.IsNullOrEmpty(record.PawnLabel)
                    ? record.PawnLabel
                    : (pawn != null ? pawn.LabelShort : string.Empty);
                this.OriginalRecordIndex = originalRecordIndex;
                this.AdmissionMode = record != null ? record.AdmissionMode : string.Empty;
                this.AdmissionTick = record != null ? record.AdmissionTick : -1;
                this.WasDownedOnAdmission = record != null && record.WasDownedOnAdmission;
                this.LastKnownReasonLabel = record != null ? record.LastKnownReasonLabel : string.Empty;
                this.LastKnownSeverityLabel = record != null ? record.LastKnownSeverityLabel : string.Empty;
            }

            internal readonly Pawn Pawn;
            internal readonly int PawnThingID;
            internal readonly string PawnDefName;
            internal readonly string PawnLabel;
            internal readonly int OriginalRecordIndex;
            internal readonly string AdmissionMode;
            internal readonly int AdmissionTick;
            internal readonly bool WasDownedOnAdmission;
            internal readonly string LastKnownReasonLabel;
            internal readonly string LastKnownSeverityLabel;
        }

        private sealed class MedicalBayPatientReturnPlan
        {
            internal MedicalBayPatientReturnPlan(
                ShuttleHolderLaunchManifestEntry entry,
                Pawn pawn,
                ThingOwner<Thing> sourceOwner,
                bool alreadyInMedicalBay)
            {
                this.Entry = entry;
                this.Pawn = pawn;
                this.SourceOwner = sourceOwner;
                this.AlreadyInMedicalBay = alreadyInMedicalBay;
            }

            internal readonly ShuttleHolderLaunchManifestEntry Entry;
            internal readonly Pawn Pawn;
            internal readonly ThingOwner<Thing> SourceOwner;
            internal readonly bool AlreadyInMedicalBay;
        }
        }
    }
}
