using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class MedicalBayHolderTransferHandler
    {
        internal static bool CanUseMedicalBayPatientLaunchTransferCore(
            ThingWithComps shuttleHost,
            bool allowHabitatOccupants,
            bool allowMechChargerOccupants,
            bool allowActiveHabitatManifest,
            out string failureReason)
        {
            failureReason = null;
            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null || !medicalBay.HasPatients)
            {
                return true;
            }

            Pawn nonDepartingPatient;
            if (MedicalBayPatientAccessPolicy.TryFindNonDepartingPatient(
                medicalBay.HeldPatients,
                out nonDepartingPatient))
            {
                string patientLabel = nonDepartingPatient != null
                    ? nonDepartingPatient.LabelShortCap
                    : "?";
                failureReason = "CT_Shuttle_Launch_Failed_MedicalBayGuestAboard"
                    .Translate(patientLabel)
                    .ToString();
                return false;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient launch transfer requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                string manifestFailureReason = null;
                bool activeManifestAllowed = allowActiveHabitatManifest &&
                    ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyHabitatEntries(state.Manifest, out manifestFailureReason);
                if (!activeManifestAllowed)
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient launch transfer refused because a holder transfer manifest is already active. " +
                        state.DumpManifestForDebug();
                    if (!string.IsNullOrEmpty(manifestFailureReason))
                    {
                        failureReason = failureReason + " " + manifestFailureReason;
                    }

                    return false;
                }
            }

            if (state.HasMedicalBayPatientStagingThings)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient launch transfer refused because MedicalBay patient staging is not empty.";
                return false;
            }

            if (!allowMechChargerOccupants &&
                allowHabitatOccupants &&
                HolderTransferPreflightService.NeedsMechChargerLaunchTransfer(shuttleHost))
            {
                failureReason = "CT_Shuttle_Launch_Failed_HolderTransferAllThreeUnsupported".Translate().ToString();
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (!allowHabitatOccupants && habitat != null && habitat.HasAnyOccupants)
            {
                failureReason = "CT_Shuttle_Launch_Failed_MedicalBayTransferBlockedByHabitat".Translate().ToString();
                return false;
            }

            if (!medicalBay.CanTransferPatientsForLaunch(out failureReason))
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient launch transfer prevalidation failed: " + failureReason;
                return false;
            }

            return true;
        }

        internal static bool TryExportPatientsToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            bool allowHabitatTransactionManifest,
            out string failureReason)
        {
            failureReason = null;
            if (!HolderTransferPreflightService.NeedsMedicalBayPatientLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (!CanUseMedicalBayPatientLaunchTransferCore(
                shuttleHost,
                allowHabitatTransactionManifest,
                allowHabitatTransactionManifest,
                allowHabitatTransactionManifest,
                out failureReason))
            {
                return false;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (state == null || medicalBay == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient launch export requires holder transfer state and MedicalBay occupancy comps.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner<Thing> destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient launch export failed: active transporter container unavailable.";
                return false;
            }

            if (!medicalBay.TryExportPatientsForLaunch(state.Manifest, destination, false, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                string rollbackNotice;
                medicalBay.TryRollbackPatientLaunchExport(state.Manifest, destination, out rollbackNotice);
                failureReason = "[CeleTech Shuttle] MedicalBay patient launch export failed: " +
                    failureReason +
                    " rollback=" +
                    rollbackNotice;
                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] MedicalBay patients exported to real launch handoff. " + state.DumpManifestForDebug());
            }

            return true;
        }

        internal static bool TryRestorePatientsBeforeImpact(
            ThingWithComps shuttleHost,
            ThingOwner primarySource,
            ThingOwner incomingSkyfallerContainer,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            failureReason = null;
            failureStatus = ShuttleHolderIncomingRestoreFailureStatus.None;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasMedicalBayPatientManifestEntries)
            {
                return true;
            }

            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient incoming restore failed: CompShuttleMedicalBayOccupancy missing. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                int missingCompQuarantinedCount;
                bool missingCompQuarantined = ShuttleHolderLaunchTransferService.TryQuarantineMedicalBayPatientManifestPawnsFromIncoming(
                    state,
                    primarySource,
                    incomingSkyfallerContainer,
                    out missingCompQuarantinedCount,
                    out string missingCompQuarantineNotice);
                ShuttleHolderLaunchTransferService.SetMedicalBayIncomingFailureStatusAndLog(
                    missingCompQuarantined,
                    missingCompQuarantinedCount,
                    missingCompQuarantineNotice,
                    "missing MedicalBay occupancy comp",
                    ref failureStatus);

                return false;
            }

            ThingOwner<Thing> primaryThingSource = primarySource as ThingOwner<Thing>;
            ThingOwner<Thing> incomingThingSource = incomingSkyfallerContainer as ThingOwner<Thing>;
            if (primaryThingSource == null && incomingThingSource == null)
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patient incoming restore failed: no typed source owner is available. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                int untypedSourceQuarantinedCount;
                string untypedSourceQuarantineNotice;
                bool untypedSourceQuarantined = ShuttleHolderLaunchTransferService.TryQuarantineMedicalBayPatientManifestPawnsFromIncoming(
                    state,
                    primarySource,
                    incomingSkyfallerContainer,
                    out untypedSourceQuarantinedCount,
                    out untypedSourceQuarantineNotice);
                if (untypedSourceQuarantinedCount > 0)
                {
                    medicalBay.ReleaseAllReservationsForPatients(ShuttleHolderManifestQueryUtility.GetMedicalBayPatientManifestThingIDs(state.Manifest));
                }

                ShuttleHolderLaunchTransferService.SetMedicalBayIncomingFailureStatusAndLog(
                    untypedSourceQuarantined,
                    untypedSourceQuarantinedCount,
                    untypedSourceQuarantineNotice,
                    "no typed source owner",
                    ref failureStatus);
                return false;
            }

            string notice;
            if (medicalBay.TryRestorePatientsFromLaunchStaging(
                state.Manifest,
                primaryThingSource,
                incomingThingSource,
                out notice))
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[CeleTech Shuttle] MedicalBay patient incoming restore succeeded before base.Impact: " +
                        (notice ?? "null"));
                }

                return true;
            }

            failureReason = "[CeleTech Shuttle] MedicalBay patient incoming restore failed before base.Impact: " +
                (notice ?? "null") +
                " " +
                state.DumpManifestForDebug();
            Log.Error(failureReason);

            int postRestoreQuarantinedCount;
            string postRestoreQuarantineNotice;
            bool postRestoreQuarantined = ShuttleHolderLaunchTransferService.TryQuarantineMedicalBayPatientManifestPawnsFromIncoming(
                state,
                primarySource,
                incomingSkyfallerContainer,
                out postRestoreQuarantinedCount,
                out postRestoreQuarantineNotice);
            if (postRestoreQuarantinedCount > 0)
            {
                medicalBay.ReleaseAllReservationsForPatients(ShuttleHolderManifestQueryUtility.GetMedicalBayPatientManifestThingIDs(state.Manifest));
            }

            ShuttleHolderLaunchTransferService.SetMedicalBayIncomingFailureStatusAndLog(
                postRestoreQuarantined,
                postRestoreQuarantinedCount,
                postRestoreQuarantineNotice,
                "restore failure",
                ref failureStatus);

            return false;
        }

        internal static bool TryRollbackPatientLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasMedicalBayPatientManifestEntries)
            {
                notice = "No MedicalBay patient launch manifest to roll back.";
                return true;
            }

            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null)
            {
                notice = "[CeleTech Shuttle] MedicalBay patient launch rollback requires CompShuttleMedicalBayOccupancy.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner<Thing> source = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoff);
            if (source == null)
            {
                notice = "[CeleTech Shuttle] MedicalBay patient launch rollback could not find active transporter container.";
                return false;
            }

            bool result = medicalBay.TryRollbackPatientLaunchExport(state.Manifest, source, out notice);
            if (result && Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] MedicalBay patient launch export rolled back: " + notice);
            }

            return result;
        }

        internal static bool TryRollbackPatientLaunchExportWithRecovery(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool pawnsQuarantined,
            out string notice)
        {
            rollbackSucceeded = false;
            pawnsQuarantined = false;

            if (TryRollbackPatientLaunchExport(shuttleHost, handoffs, out notice))
            {
                rollbackSucceeded = true;
                return true;
            }

            string rollbackFailureNotice = notice;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasMedicalBayPatientManifestEntries)
            {
                notice = "[CeleTech Shuttle] MedicalBay rollback failed but no MedicalBay patient manifest remains active. rollback=" +
                    (rollbackFailureNotice ?? "null");
                return true;
            }

            int remainingBefore = ShuttleHolderTransferLookupUtility.CountMedicalBayPatientManifestThingsInHandoffs(state.Manifest, handoffs);
            if (remainingBefore <= 0)
            {
                notice = "[CeleTech Shuttle] MedicalBay rollback failed, but no MedicalBay manifest pawns remain in committed handoffs. Manifest remains active for diagnostics/recovery. rollback=" +
                    (rollbackFailureNotice ?? "null");
                return true;
            }

            int quarantinedCount;
            string quarantineNotice;
            bool quarantineSucceeded = ShuttleHolderLaunchTransferService.TryQuarantineMedicalBayPatientManifestPawnsFromHandoffs(
                state,
                handoffs,
                out quarantinedCount,
                out quarantineNotice);
            if (quarantinedCount > 0)
            {
                CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                    ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                    : null;
                if (medicalBay != null)
                {
                    medicalBay.ReleaseAllReservationsForPatients(ShuttleHolderManifestQueryUtility.GetMedicalBayPatientManifestThingIDs(state.Manifest));
                }
            }

            int remainingAfter = ShuttleHolderTransferLookupUtility.CountMedicalBayPatientManifestThingsInHandoffs(state.Manifest, handoffs);
            pawnsQuarantined = quarantinedCount > 0 && remainingAfter == 0;
            if (quarantineSucceeded && remainingAfter == 0)
            {
                notice = "[CeleTech Shuttle] MedicalBay rollback failed, but " +
                    quarantinedCount +
                    " pawn(s) were moved from committed handoffs to MedicalBay patient staging. Manifest remains active for recovery. rollback=" +
                    (rollbackFailureNotice ?? "null") +
                    " quarantine=" +
                    (quarantineNotice ?? "null");
                return true;
            }

            notice = "[CeleTech Shuttle] MedicalBay rollback failed and " +
                remainingAfter +
                " MedicalBay manifest pawn(s) remain in committed handoffs. Normal cargo rollback must not proceed. rollback=" +
                (rollbackFailureNotice ?? "null") +
                " quarantine=" +
                (quarantineNotice ?? "null");
            return false;
        }
    }
}
