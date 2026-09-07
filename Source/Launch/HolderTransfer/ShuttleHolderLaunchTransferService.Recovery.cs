using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        private static bool TryQuarantineMedicalBayPatientManifestPawnsFromIncoming(
            CompShuttleHolderLaunchTransferState state,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out string notice)
        {
            int quarantinedCount;
            return TryQuarantineMedicalBayPatientManifestPawnsFromIncoming(
                state,
                primarySource,
                secondarySource,
                out quarantinedCount,
                out notice);
        }

        // Internal for Phase 8 non-Habitat incoming restore handler migration;
        // preserves existing incoming restore recovery semantics.
        internal static bool TryQuarantineMedicalBayPatientManifestPawnsFromIncoming(
            CompShuttleHolderLaunchTransferState state,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out int quarantinedCount,
            out string notice)
        {
            quarantinedCount = 0;
            notice = null;
            if (state == null || state.Manifest == null)
            {
                notice = "MedicalBay incoming quarantine failed: holder transfer state or manifest is unavailable.";
                return false;
            }

            ThingOwner<Thing> staging = state.MedicalBayPatientStagingThings;
            if (staging == null)
            {
                notice = "MedicalBay incoming quarantine failed: MedicalBay patient staging holder is unavailable.";
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
            bool success = true;
            string notes = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    success = false;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, "manifest entry null");
                    continue;
                }

                string mismatchReason;
                Pawn pawn = FindMedicalBayPatientManifestPawnInOwners(
                    primarySource,
                    secondarySource,
                    entry,
                    out mismatchReason);
                if (pawn == null)
                {
                    Pawn alreadyStagedPawn = ShuttleHolderTransferLookupUtility.FindThingInOwner(staging, entry.ThingID) as Pawn;
                    if (alreadyStagedPawn != null)
                    {
                        quarantinedCount++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(mismatchReason))
                    {
                        success = false;
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = mismatchReason;
                        notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, mismatchReason);
                    }
                    else
                    {
                        success = false;
                        string missingNote = "MedicalBay incoming quarantine could not find manifest pawn thingID=" +
                            entry.ThingID +
                            " in incoming source owners or MedicalBay patient staging.";
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = missingNote;
                        notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, missingNote);
                    }

                    continue;
                }

                if (pawn.Destroyed)
                {
                    success = false;
                    string destroyedNote = "MedicalBay incoming quarantine refused destroyed pawn thingID=" +
                        entry.ThingID +
                        ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = destroyedNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, destroyedNote);
                    continue;
                }

                if (staging.Contains(pawn))
                {
                    quarantinedCount++;
                    continue;
                }

                if (staging.TryAddOrTransfer(pawn, false))
                {
                    quarantinedCount++;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Incoming MedicalBay restore failed; pawn was quarantined in MedicalBay patient staging before base.Impact.";
                    continue;
                }

                success = false;
                string addFailure = "MedicalBay incoming quarantine failed to move pawn thingID=" +
                    entry.ThingID +
                    " to MedicalBay patient staging.";
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = addFailure;
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, addFailure);
            }

            int remaining = ShuttleHolderTransferLookupUtility.CountMedicalBayPatientManifestThingsInOwners(state.Manifest, primarySource, secondarySource);
            notice = "quarantined=" +
                quarantinedCount +
                " remainingInIncomingSources=" +
                remaining +
                " notes=" +
                (notes ?? "none");
            return success && remaining == 0;
        }

        // Internal for Phase 8 non-Habitat incoming restore handler migration;
        // preserves existing incoming restore recovery semantics.
        internal static void SetMedicalBayIncomingFailureStatusAndLog(
            bool quarantined,
            int quarantinedCount,
            string quarantineNotice,
            string context,
            ref ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            failureStatus = ShuttleHolderIncomingRestoreStatusUtility.GetMedicalBayIncomingFailureStatus(quarantined);
            if (failureStatus == ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFailedQuarantined)
            {
                Log.Warning("[CeleTech Shuttle] MedicalBay restore failed but all remaining manifest pawns were quarantined to MedicalBayPatientStagingThings before base.Impact. context=" +
                    (context ?? "null") +
                    " quarantined=" +
                    quarantinedCount +
                    " " +
                    (quarantineNotice ?? "null"));
                return;
            }

            Log.Error("[CeleTech Shuttle] MedicalBay restore failed and one or more manifest pawns may still remain in incoming/source owners. Ordinary base.Impact must not continue. context=" +
                (context ?? "null") +
                " " +
                (quarantineNotice ?? "null"));
        }

        // Internal for Phase 7 non-Habitat handler migration; preserves the
        // existing handoff quarantine behavior without moving incoming restore.
        internal static bool TryQuarantineMedicalBayPatientManifestPawnsFromHandoffs(
            CompShuttleHolderLaunchTransferState state,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out int quarantinedCount,
            out string notice)
        {
            quarantinedCount = 0;
            notice = null;
            if (state == null || state.Manifest == null)
            {
                notice = "MedicalBay quarantine failed: holder transfer state or manifest is unavailable.";
                return false;
            }

            ThingOwner<Thing> staging = state.MedicalBayPatientStagingThings;
            if (staging == null)
            {
                notice = "MedicalBay quarantine failed: MedicalBay patient staging holder is unavailable.";
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
            bool success = true;
            string notes = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    success = false;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, "manifest entry null");
                    continue;
                }

                ThingOwner<Thing> sourceOwner;
                string mismatchReason;
                Pawn pawn = FindMedicalBayPatientManifestPawnInHandoffs(
                    handoffs,
                    entry,
                    out sourceOwner,
                    out mismatchReason);
                if (pawn == null)
                {
                    Pawn alreadyStagedPawn = ShuttleHolderTransferLookupUtility.FindThingInOwner(staging, entry.ThingID) as Pawn;
                    if (alreadyStagedPawn != null)
                    {
                        quarantinedCount++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(mismatchReason))
                    {
                        success = false;
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = mismatchReason;
                        notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, mismatchReason);
                    }
                    else
                    {
                        success = false;
                        string missingNote = "MedicalBay handoff quarantine could not find manifest pawn thingID=" +
                            entry.ThingID +
                            " in committed handoffs or MedicalBay patient staging.";
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = missingNote;
                        notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, missingNote);
                    }

                    continue;
                }

                if (pawn.Destroyed)
                {
                    success = false;
                    string destroyedNote = "MedicalBay quarantine refused destroyed pawn thingID=" + entry.ThingID + ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = destroyedNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, destroyedNote);
                    continue;
                }

                if (staging.Contains(pawn))
                {
                    quarantinedCount++;
                    continue;
                }

                if (staging.TryAddOrTransfer(pawn, false))
                {
                    quarantinedCount++;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Primary MedicalBay rollback failed; pawn was quarantined in MedicalBay patient staging for recovery.";
                    continue;
                }

                success = false;
                string addFailure = "MedicalBay quarantine failed to move pawn thingID=" +
                    entry.ThingID +
                    " from committed handoff to MedicalBay patient staging.";
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = addFailure;
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, addFailure);
            }

            int remaining = ShuttleHolderTransferLookupUtility.CountMedicalBayPatientManifestThingsInHandoffs(state.Manifest, handoffs);
            notice = "quarantined=" +
                quarantinedCount +
                " remainingInHandoffs=" +
                remaining +
                " notes=" +
                (notes ?? "none");
            return success && remaining == 0;
        }

        private static Pawn FindMedicalBayPatientManifestPawnInHandoffs(
            List<ShuttleLaunchCargoHandoff> handoffs,
            ShuttleHolderLaunchManifestEntry entry,
            out ThingOwner<Thing> sourceOwner,
            out string mismatchReason)
        {
            sourceOwner = null;
            mismatchReason = null;
            if (handoffs == null || entry == null || entry.ThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < handoffs.Count; i++)
            {
                ThingOwner<Thing> owner = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoffs[i]);
                Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(owner, entry.ThingID);
                if (thing == null)
                {
                    continue;
                }

                sourceOwner = owner;
                Pawn pawn = thing as Pawn;
                if (pawn == null)
                {
                    mismatchReason = "MedicalBay manifest thingID=" +
                        entry.ThingID +
                        " is in committed handoff but is not a Pawn.";
                    return null;
                }

                string pawnDefName = pawn.def != null ? pawn.def.defName : null;
                if (!string.IsNullOrEmpty(entry.DefName) && pawnDefName != entry.DefName)
                {
                    mismatchReason = "MedicalBay manifest thingID=" +
                        entry.ThingID +
                        " def mismatch while in committed handoff. manifest=" +
                        entry.DefName +
                        " handoff=" +
                        (pawnDefName ?? "null") +
                        ".";
                    return null;
                }

                return pawn;
            }

            return null;
        }

        private static Pawn FindMedicalBayPatientManifestPawnInOwners(
            ThingOwner primarySource,
            ThingOwner secondarySource,
            ShuttleHolderLaunchManifestEntry entry,
            out string mismatchReason)
        {
            mismatchReason = null;
            if (entry == null || entry.ThingID <= 0)
            {
                return null;
            }

            Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(primarySource, entry.ThingID);
            if (thing == null && !object.ReferenceEquals(primarySource, secondarySource))
            {
                thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(secondarySource, entry.ThingID);
            }

            if (thing == null)
            {
                return null;
            }

            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                mismatchReason = "MedicalBay manifest thingID=" +
                    entry.ThingID +
                    " is in incoming source but is not a Pawn.";
                return null;
            }

            string pawnDefName = pawn.def != null ? pawn.def.defName : null;
            if (!string.IsNullOrEmpty(entry.DefName) && pawnDefName != entry.DefName)
            {
                mismatchReason = "MedicalBay manifest thingID=" +
                    entry.ThingID +
                    " def mismatch while in incoming source. manifest=" +
                    entry.DefName +
                    " source=" +
                    (pawnDefName ?? "null") +
                    ".";
                return null;
            }

            return pawn;
        }

        // Internal for Phase 8 non-Habitat incoming restore handler migration;
        // preserves existing incoming restore recovery semantics.
        internal static bool TryQuarantineMechChargerManifestPawnsFromIncoming(
            CompShuttleHolderLaunchTransferState state,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out int quarantinedCount,
            out string notice)
        {
            quarantinedCount = 0;
            notice = null;
            if (state == null || state.Manifest == null)
            {
                notice = "MechCharger incoming quarantine failed: holder transfer state or manifest is unavailable.";
                return false;
            }

            ThingOwner<Thing> emergency = state.EmergencyRecoveryThings;
            if (emergency == null)
            {
                notice = "MechCharger incoming quarantine failed: emergency recovery holder is unavailable.";
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
            bool success = true;
            string notes = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    success = false;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, "manifest entry null");
                    continue;
                }

                string mismatchReason;
                Pawn pawn = FindMechChargerManifestPawnInOwners(
                    primarySource,
                    secondarySource,
                    entry,
                    out mismatchReason);
                if (pawn == null)
                {
                    Pawn alreadyRecoveredPawn = ShuttleHolderTransferLookupUtility.FindThingInOwner(emergency, entry.ThingID) as Pawn;
                    if (alreadyRecoveredPawn != null)
                    {
                        quarantinedCount++;
                        continue;
                    }

                    success = false;
                    string missingNote = !string.IsNullOrEmpty(mismatchReason)
                        ? mismatchReason
                        : "MechCharger incoming quarantine could not find manifest mech thingID=" +
                            entry.ThingID +
                            " in incoming source owners or emergency recovery.";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = missingNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, missingNote);
                    continue;
                }

                if (pawn.Destroyed)
                {
                    success = false;
                    string destroyedNote = "MechCharger incoming quarantine refused destroyed mech thingID=" +
                        entry.ThingID +
                        ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = destroyedNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, destroyedNote);
                    continue;
                }

                if (emergency.Contains(pawn) || emergency.TryAddOrTransfer(pawn, false))
                {
                    quarantinedCount++;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                    entry.DebugNotes = "Incoming MechCharger restore failed; mech was quarantined in emergency recovery before base.Impact.";
                    continue;
                }

                success = false;
                string addFailure = "MechCharger incoming quarantine failed to move mech thingID=" +
                    entry.ThingID +
                    " to emergency recovery.";
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = addFailure;
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, addFailure);
            }

            int remaining = ShuttleHolderTransferLookupUtility.CountMechChargerManifestThingsInOwners(state.Manifest, primarySource, secondarySource);
            notice = "quarantined=" +
                quarantinedCount +
                " remainingInIncomingSources=" +
                remaining +
                " notes=" +
                (notes ?? "none");
            return success && remaining == 0;
        }

        // Internal for Phase 7 non-Habitat handler migration; preserves the
        // existing handoff quarantine behavior without moving incoming restore.
        internal static bool TryQuarantineMechChargerManifestPawnsFromHandoffs(
            CompShuttleHolderLaunchTransferState state,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out int quarantinedCount,
            out string notice)
        {
            quarantinedCount = 0;
            notice = null;
            if (state == null || state.Manifest == null)
            {
                notice = "MechCharger quarantine failed: holder transfer state or manifest is unavailable.";
                return false;
            }

            ThingOwner<Thing> emergency = state.EmergencyRecoveryThings;
            if (emergency == null)
            {
                notice = "MechCharger quarantine failed: emergency recovery holder is unavailable.";
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
            bool success = true;
            string notes = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    success = false;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, "manifest entry null");
                    continue;
                }

                string mismatchReason;
                Pawn pawn = FindMechChargerManifestPawnInHandoffs(
                    handoffs,
                    entry,
                    out mismatchReason);
                if (pawn == null)
                {
                    Pawn alreadyRecoveredPawn = ShuttleHolderTransferLookupUtility.FindThingInOwner(emergency, entry.ThingID) as Pawn;
                    if (alreadyRecoveredPawn != null)
                    {
                        quarantinedCount++;
                        continue;
                    }

                    success = false;
                    string missingNote = !string.IsNullOrEmpty(mismatchReason)
                        ? mismatchReason
                        : "MechCharger handoff quarantine could not find manifest mech thingID=" +
                            entry.ThingID +
                            " in committed handoffs or emergency recovery.";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = missingNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, missingNote);
                    continue;
                }

                if (pawn.Destroyed)
                {
                    success = false;
                    string destroyedNote = "MechCharger quarantine refused destroyed mech thingID=" + entry.ThingID + ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = destroyedNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, destroyedNote);
                    continue;
                }

                if (emergency.Contains(pawn) || emergency.TryAddOrTransfer(pawn, false))
                {
                    quarantinedCount++;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                    entry.DebugNotes = "Primary MechCharger rollback failed; mech was quarantined in emergency recovery.";
                    continue;
                }

                success = false;
                string addFailure = "MechCharger quarantine failed to move mech thingID=" +
                    entry.ThingID +
                    " from committed handoff to emergency recovery.";
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = addFailure;
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, addFailure);
            }

            int remaining = ShuttleHolderTransferLookupUtility.CountMechChargerManifestThingsInHandoffs(state.Manifest, handoffs);
            notice = "quarantined=" +
                quarantinedCount +
                " remainingInHandoffs=" +
                remaining +
                " notes=" +
                (notes ?? "none");
            return success && remaining == 0;
        }

        private static Pawn FindMechChargerManifestPawnInHandoffs(
            List<ShuttleLaunchCargoHandoff> handoffs,
            ShuttleHolderLaunchManifestEntry entry,
            out string mismatchReason)
        {
            mismatchReason = null;
            if (handoffs == null || entry == null || entry.ThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < handoffs.Count; i++)
            {
                Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(
                    ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoffs[i]),
                    entry.ThingID);
                if (thing == null)
                {
                    continue;
                }

                return ValidateMechChargerManifestThing(entry, thing, "committed handoff", out mismatchReason);
            }

            return null;
        }

        private static Pawn FindMechChargerManifestPawnInOwners(
            ThingOwner primarySource,
            ThingOwner secondarySource,
            ShuttleHolderLaunchManifestEntry entry,
            out string mismatchReason)
        {
            mismatchReason = null;
            if (entry == null || entry.ThingID <= 0)
            {
                return null;
            }

            Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(primarySource, entry.ThingID);
            if (thing == null && !object.ReferenceEquals(primarySource, secondarySource))
            {
                thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(secondarySource, entry.ThingID);
            }

            return thing != null
                ? ValidateMechChargerManifestThing(entry, thing, "incoming source", out mismatchReason)
                : null;
        }

        private static Pawn ValidateMechChargerManifestThing(
            ShuttleHolderLaunchManifestEntry entry,
            Thing thing,
            string sourceLabel,
            out string mismatchReason)
        {
            mismatchReason = null;
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                mismatchReason = "MechCharger manifest thingID=" +
                    entry.ThingID +
                    " is in " +
                    (sourceLabel ?? "source") +
                    " but is not a Pawn.";
                return null;
            }

            string pawnDefName = pawn.def != null ? pawn.def.defName : null;
            if (!string.IsNullOrEmpty(entry.DefName) && pawnDefName != entry.DefName)
            {
                mismatchReason = "MechCharger manifest thingID=" +
                    entry.ThingID +
                    " def mismatch while in " +
                    (sourceLabel ?? "source") +
                    ". manifest=" +
                    entry.DefName +
                    " source=" +
                    (pawnDefName ?? "null") +
                    ".";
                return null;
            }

            if (pawn.RaceProps == null || !pawn.RaceProps.IsMechanoid)
            {
                mismatchReason = "MechCharger manifest thingID=" +
                    entry.ThingID +
                    " is not a mechanoid while in " +
                    (sourceLabel ?? "source") +
                    ".";
                return null;
            }

            return pawn;
        }

        // Internal for Phase 8 non-Habitat incoming restore handler migration;
        // preserves existing incoming restore recovery semantics.
        internal static bool TryQuarantinePrisonCellPrisonerManifestPawnsFromIncoming(
            CompShuttleHolderLaunchTransferState state,
            ThingOwner primarySource,
            ThingOwner secondarySource,
            out int quarantinedCount,
            out string notice)
        {
            quarantinedCount = 0;
            notice = null;
            if (state == null || state.Manifest == null)
            {
                notice = "PrisonCell incoming quarantine failed: holder transfer state or manifest is unavailable.";
                return false;
            }

            ThingOwner<Thing> emergency = state.EmergencyRecoveryThings;
            if (emergency == null)
            {
                notice = "PrisonCell incoming quarantine failed: emergency recovery holder is unavailable.";
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind);
            bool success = true;
            string notes = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    success = false;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, "manifest entry null");
                    continue;
                }

                PrisonCellManifestPawnResolution resolution =
                    PrisonCellManifestPawnResolver.ResolveInOwners(
                        entry,
                        primarySource,
                        secondarySource);
                Thing recoveryThing = resolution.ContainerThing;
                if (resolution.Kind == PrisonCellManifestPawnResolutionKind.Missing)
                {
                    PrisonCellManifestPawnResolution recoveredResolution =
                        PrisonCellManifestPawnResolver.ResolveInOwners(
                            entry,
                            emergency,
                            null);
                    if (recoveredResolution.Kind == PrisonCellManifestPawnResolutionKind.LivePawn ||
                        recoveredResolution.Kind == PrisonCellManifestPawnResolutionKind.DeadPawnCorpse)
                    {
                        quarantinedCount++;
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                        entry.DebugNotes =
                            "Incoming PrisonCell restore failed; manifest identity was already present in emergency recovery.";
                        continue;
                    }

                    success = false;
                    string missingNote = !string.IsNullOrEmpty(recoveredResolution.FailureReason)
                        ? recoveredResolution.FailureReason
                        : "PrisonCell incoming quarantine could not find manifest prisoner thingID=" +
                            entry.ThingID +
                            " in incoming source owners or emergency recovery.";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = missingNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, missingNote);
                    continue;
                }

                if (resolution.Kind == PrisonCellManifestPawnResolutionKind.Invalid ||
                    resolution.Kind == PrisonCellManifestPawnResolutionKind.Ambiguous ||
                    recoveryThing == null)
                {
                    success = false;
                    string invalidNote = !string.IsNullOrEmpty(resolution.FailureReason)
                        ? resolution.FailureReason
                        : "PrisonCell incoming quarantine resolved no movable Thing for manifest thingID=" +
                            entry.ThingID +
                            ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = invalidNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, invalidNote);
                    continue;
                }

                if (recoveryThing.Destroyed)
                {
                    success = false;
                    string destroyedNote = "PrisonCell incoming quarantine refused destroyed prisoner representation thingID=" +
                        entry.ThingID +
                        ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = destroyedNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, destroyedNote);
                    continue;
                }

                if (emergency.Contains(recoveryThing) ||
                    emergency.TryAddOrTransfer(recoveryThing, false))
                {
                    quarantinedCount++;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                    entry.DebugNotes =
                        resolution.Kind == PrisonCellManifestPawnResolutionKind.DeadPawnCorpse
                            ? "Incoming PrisonCell restore failed; the containing Corpse was quarantined in emergency recovery before base.Impact."
                            : "Incoming PrisonCell restore failed; prisoner was quarantined in emergency recovery before base.Impact.";
                    continue;
                }

                success = false;
                string addFailure = "PrisonCell incoming quarantine failed to move prisoner representation thingID=" +
                    entry.ThingID +
                    " to emergency recovery.";
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = addFailure;
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, addFailure);
            }

            int remaining = ShuttleHolderTransferLookupUtility.CountPrisonCellPrisonerManifestThingsInOwners(state.Manifest, primarySource, secondarySource);
            notice = "quarantined=" +
                quarantinedCount +
                " remainingInIncomingSources=" +
                remaining +
                " notes=" +
                (notes ?? "none");
            return success && remaining == 0;
        }

        // Internal for Phase 7 non-Habitat handler migration; preserves the
        // existing handoff quarantine behavior without moving incoming restore.
        internal static bool TryQuarantinePrisonCellPrisonerManifestPawnsFromHandoffs(
            CompShuttleHolderLaunchTransferState state,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out int quarantinedCount,
            out string notice)
        {
            quarantinedCount = 0;
            notice = null;
            if (state == null || state.Manifest == null)
            {
                notice = "PrisonCell quarantine failed: holder transfer state or manifest is unavailable.";
                return false;
            }

            ThingOwner<Thing> emergency = state.EmergencyRecoveryThings;
            if (emergency == null)
            {
                notice = "PrisonCell quarantine failed: emergency recovery holder is unavailable.";
                return false;
            }

            List<ShuttleHolderLaunchManifestEntry> entries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind);
            bool success = true;
            string notes = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = entries[i];
                if (entry == null)
                {
                    success = false;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, "manifest entry null");
                    continue;
                }

                string mismatchReason;
                Pawn pawn = FindPrisonCellPrisonerManifestPawnInHandoffs(
                    handoffs,
                    entry,
                    out mismatchReason);
                if (pawn == null)
                {
                    Pawn alreadyRecoveredPawn = ShuttleHolderTransferLookupUtility.FindThingInOwner(emergency, entry.ThingID) as Pawn;
                    if (alreadyRecoveredPawn != null)
                    {
                        quarantinedCount++;
                        continue;
                    }

                    success = false;
                    string missingNote = !string.IsNullOrEmpty(mismatchReason)
                        ? mismatchReason
                        : "PrisonCell handoff quarantine could not find manifest prisoner thingID=" +
                            entry.ThingID +
                            " in committed handoffs or emergency recovery.";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = missingNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, missingNote);
                    continue;
                }

                if (pawn.Destroyed)
                {
                    success = false;
                    string destroyedNote = "PrisonCell quarantine refused destroyed prisoner thingID=" + entry.ThingID + ".";
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = destroyedNote;
                    notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, destroyedNote);
                    continue;
                }

                if (emergency.Contains(pawn) || emergency.TryAddOrTransfer(pawn, false))
                {
                    quarantinedCount++;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseQuarantined;
                    entry.DebugNotes = "Primary PrisonCell rollback failed; prisoner was quarantined in emergency recovery.";
                    continue;
                }

                success = false;
                string addFailure = "PrisonCell quarantine failed to move prisoner thingID=" +
                    entry.ThingID +
                    " from committed handoff to emergency recovery.";
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = addFailure;
                notes = ShuttleHolderManifestQueryUtility.AppendNote(notes, addFailure);
            }

            int remaining = ShuttleHolderTransferLookupUtility.CountPrisonCellPrisonerManifestThingsInHandoffs(state.Manifest, handoffs);
            notice = "quarantined=" +
                quarantinedCount +
                " remainingInHandoffs=" +
                remaining +
                " notes=" +
                (notes ?? "none");
            return success && remaining == 0;
        }

        private static Pawn FindPrisonCellPrisonerManifestPawnInHandoffs(
            List<ShuttleLaunchCargoHandoff> handoffs,
            ShuttleHolderLaunchManifestEntry entry,
            out string mismatchReason)
        {
            mismatchReason = null;
            if (handoffs == null || entry == null || entry.ThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < handoffs.Count; i++)
            {
                Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(
                    ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoffs[i]),
                    entry.ThingID);
                if (thing == null)
                {
                    continue;
                }

                return ValidatePrisonCellPrisonerManifestThing(entry, thing, "committed handoff", out mismatchReason);
            }

            return null;
        }

        private static Pawn ValidatePrisonCellPrisonerManifestThing(
            ShuttleHolderLaunchManifestEntry entry,
            Thing thing,
            string sourceLabel,
            out string mismatchReason)
        {
            mismatchReason = null;
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                mismatchReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " is in " +
                    (sourceLabel ?? "source") +
                    " but is not a Pawn.";
                return null;
            }

            string pawnDefName = pawn.def != null ? pawn.def.defName : null;
            if (!string.IsNullOrEmpty(entry.DefName) && pawnDefName != entry.DefName)
            {
                mismatchReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " def mismatch while in " +
                    (sourceLabel ?? "source") +
                    ". manifest=" +
                    entry.DefName +
                    " source=" +
                    (pawnDefName ?? "null") +
                    ".";
                return null;
            }

            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                mismatchReason = "PrisonCell manifest thingID=" +
                    entry.ThingID +
                    " is not humanlike while in " +
                    (sourceLabel ?? "source") +
                    ".";
                return null;
            }

            return pawn;
        }

        // Internal for Phase 8 non-Habitat incoming restore handler migration;
        // preserves existing incoming restore recovery semantics.
        internal static void SetPrisonCellIncomingFailureStatus(
            bool quarantined,
            int quarantinedCount,
            ref ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            failureStatus = ShuttleHolderIncomingRestoreStatusUtility.GetPrisonCellIncomingFailureStatus(quarantined);
            if (failureStatus == ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFailedQuarantined)
            {
                Log.Warning("[CeleTech Shuttle] PrisonCell restore failed but remaining manifest prisoners were quarantined to emergency recovery before base.Impact. quarantined=" +
                    quarantinedCount);
                return;
            }

            Log.Error("[CeleTech Shuttle] PrisonCell restore failed and one or more manifest prisoners may still remain in incoming/source owners. Ordinary base.Impact must not continue.");
        }

        // Internal for Phase 8 non-Habitat incoming restore handler migration;
        // preserves existing incoming restore recovery semantics.
        internal static void SetMechChargerIncomingFailureStatus(
            bool quarantined,
            int quarantinedCount,
            ref ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            failureStatus = ShuttleHolderIncomingRestoreStatusUtility.GetMechChargerIncomingFailureStatus(quarantined);
            if (failureStatus == ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFailedQuarantined)
            {
                Log.Warning("[CeleTech Shuttle] MechCharger restore failed but remaining manifest mechs were quarantined to emergency recovery before base.Impact. quarantined=" +
                    quarantinedCount);
                return;
            }

            Log.Error("[CeleTech Shuttle] MechCharger restore failed and one or more manifest mechs may still remain in incoming/source owners. Ordinary base.Impact must not continue.");
        }

        private static bool TryDropThingNear(Thing thing, ThingWithComps shuttleHost, Map map)
        {
            if (thing == null || thing.Destroyed)
            {
                return true;
            }

            Map targetMap = map ?? (shuttleHost != null ? shuttleHost.Map : null);
            IntVec3 cell = shuttleHost != null && shuttleHost.Position.IsValid
                ? shuttleHost.Position
                : IntVec3.Invalid;
            return targetMap != null &&
                cell.IsValid &&
                GenPlace.TryPlaceThing(thing, cell, targetMap, ThingPlaceMode.Near);
        }
    }
}
