using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class MechChargerHolderTransferHandler
    {
        internal static bool TryRestoreBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            ThingOwner primarySource,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            failureReason = null;
            failureStatus = ShuttleHolderIncomingRestoreFailureStatus.None;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasMechChargerManifestEntries)
            {
                return true;
            }

            CompShuttleMechChargerOccupancy mechCharger = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
            if (mechCharger == null)
            {
                failureReason = "[CeleTech Shuttle] MechCharger incoming restore failed: CompShuttleMechChargerOccupancy missing. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                int missingCompQuarantinedCount;
                string missingCompQuarantineNotice;
                bool missingCompQuarantined = ShuttleHolderLaunchTransferService.TryQuarantineMechChargerManifestPawnsFromIncoming(
                    state,
                    primarySource,
                    incomingSkyfallerContainer,
                    out missingCompQuarantinedCount,
                    out missingCompQuarantineNotice);
                ShuttleHolderLaunchTransferService.SetMechChargerIncomingFailureStatus(
                    missingCompQuarantined,
                    missingCompQuarantinedCount,
                    ref failureStatus);
                return false;
            }

            ThingOwner<Thing> primaryThingSource = primarySource as ThingOwner<Thing>;
            ThingOwner<Thing> incomingThingSource = incomingSkyfallerContainer as ThingOwner<Thing>;
            if (primaryThingSource == null && incomingThingSource == null)
            {
                failureReason = "[CeleTech Shuttle] MechCharger incoming restore failed: no typed source owner is available. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                int untypedSourceQuarantinedCount;
                string untypedSourceQuarantineNotice;
                bool untypedSourceQuarantined = ShuttleHolderLaunchTransferService.TryQuarantineMechChargerManifestPawnsFromIncoming(
                    state,
                    primarySource,
                    incomingSkyfallerContainer,
                    out untypedSourceQuarantinedCount,
                    out untypedSourceQuarantineNotice);
                ShuttleHolderLaunchTransferService.SetMechChargerIncomingFailureStatus(
                    untypedSourceQuarantined,
                    untypedSourceQuarantinedCount,
                    ref failureStatus);
                return false;
            }

            string notice;
            if (mechCharger.TryRestoreChargingMechsFromLaunchStaging(
                state.Manifest,
                primaryThingSource,
                incomingThingSource,
                map,
                fallbackCell,
                state.EmergencyRecoveryThings,
                out notice))
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[CeleTech Shuttle] MechCharger incoming restore succeeded before base.Impact: " +
                        (notice ?? "null"));
                }

                return true;
            }

            failureReason = "[CeleTech Shuttle] MechCharger incoming restore failed before base.Impact: " +
                (notice ?? "null") +
                " " +
                state.DumpManifestForDebug();
            Log.Error(failureReason);

            int postRestoreQuarantinedCount;
            string postRestoreQuarantineNotice;
            bool postRestoreQuarantined = ShuttleHolderLaunchTransferService.TryQuarantineMechChargerManifestPawnsFromIncoming(
                state,
                primarySource,
                incomingSkyfallerContainer,
                out postRestoreQuarantinedCount,
                out postRestoreQuarantineNotice);
            if (!postRestoreQuarantined &&
                postRestoreQuarantinedCount == 0 &&
                ShuttleHolderManifestQueryUtility.MechChargerManifestEntriesAreQuarantined(state.Manifest))
            {
                postRestoreQuarantined = true;
                postRestoreQuarantinedCount = state.Manifest.FindEntriesByHolderKind(
                    ShuttleHolderLaunchManifestConstants.MechChargerHolderKind).Count;
            }

            ShuttleHolderLaunchTransferService.SetMechChargerIncomingFailureStatus(
                postRestoreQuarantined,
                postRestoreQuarantinedCount,
                ref failureStatus);
            return false;
        }

        internal static bool CanUseMechChargerLaunchTransferCore(
            ThingWithComps shuttleHost,
            bool allowHabitatOccupants,
            bool allowMedicalPatients,
            bool allowActiveHabitatManifest,
            bool allowActiveMedicalBayManifest,
            bool allowActiveHabitatAndMedicalBayManifest,
            out string failureReason)
        {
            failureReason = null;
            CompShuttleMechChargerOccupancy mechCharger = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
            if (mechCharger == null || !mechCharger.HasChargingMechs)
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_MechChargerTransferUnavailable".Translate("holder transfer state unavailable").ToString();
                return false;
            }

            if (state.HasActiveManifest)
            {
                string manifestFailureReason = null;
                bool activeManifestAllowed = false;
                if (allowActiveHabitatManifest &&
                    ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyHabitatEntries(state.Manifest, out manifestFailureReason))
                {
                    activeManifestAllowed = true;
                }

                if (!activeManifestAllowed &&
                    allowActiveMedicalBayManifest &&
                    ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyMedicalBayPatientEntries(state.Manifest, out manifestFailureReason))
                {
                    activeManifestAllowed = true;
                }

                if (!activeManifestAllowed &&
                    allowActiveHabitatAndMedicalBayManifest &&
                    ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyHabitatAndMedicalBayEntries(state.Manifest, out manifestFailureReason))
                {
                    activeManifestAllowed = true;
                }

                if (!activeManifestAllowed)
                {
                    failureReason = "CT_Shuttle_Launch_Failed_HolderTransferManifestActive".Translate().ToString();
                    if (!string.IsNullOrEmpty(manifestFailureReason))
                    {
                        failureReason = failureReason + " " + manifestFailureReason;
                    }

                    return false;
                }
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (!allowHabitatOccupants && habitat != null && habitat.HasAnyOccupants)
            {
                // Default callers remain fail-closed. Participant transactions
                // provide the only narrow Habitat/Medical concurrency paths.
                failureReason = "CT_Shuttle_Launch_Failed_MechChargerTransferBlockedByHabitat".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (!allowMedicalPatients && medicalBay != null && medicalBay.HasPatients)
            {
                // Default callers remain fail-closed. Phase C allows
                // MedicalBay + MechCharger only through the participant transaction.
                failureReason = "CT_Shuttle_Launch_Failed_MechChargerTransferBlockedByMedicalBay".Translate().ToString();
                return false;
            }

            if (!mechCharger.CanTransferChargingMechsForLaunch(out failureReason))
            {
                failureReason = "CT_Shuttle_Launch_Failed_MechChargerTransferUnavailable".Translate(failureReason ?? "unknown").ToString();
                return false;
            }

            return true;
        }

        internal static bool TryExportToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            bool allowHabitatTransactionManifest,
            bool allowMedicalBayTransactionManifest,
            bool allowHabitatAndMedicalBayTransactionManifest,
            out string failureReason)
        {
            failureReason = null;
            if (!HolderTransferPreflightService.NeedsMechChargerLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (!CanUseMechChargerLaunchTransferCore(
                shuttleHost,
                allowHabitatTransactionManifest,
                allowMedicalBayTransactionManifest,
                allowHabitatTransactionManifest,
                allowMedicalBayTransactionManifest,
                allowHabitatAndMedicalBayTransactionManifest,
                out failureReason))
            {
                return false;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            CompShuttleMechChargerOccupancy mechCharger = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
            if (state == null || mechCharger == null)
            {
                failureReason = "[CeleTech Shuttle] MechCharger launch export requires holder transfer state and MechCharger occupancy comps.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner<Thing> destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] MechCharger launch export failed: active transporter container unavailable.";
                return false;
            }

            if (!mechCharger.TryExportChargingMechsForLaunch(state.Manifest, destination, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                string rollbackNotice;
                mechCharger.TryRollbackChargingMechLaunchExport(state.Manifest, destination, out rollbackNotice);
                failureReason = "[CeleTech Shuttle] MechCharger launch export failed: " +
                    failureReason +
                    " rollback=" +
                    rollbackNotice;
                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] MechCharger occupants exported to real launch handoff. " + state.DumpManifestForDebug());
            }

            return true;
        }

        internal static bool TryRollbackLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasMechChargerManifestEntries)
            {
                notice = "No MechCharger launch manifest to roll back.";
                return true;
            }

            CompShuttleMechChargerOccupancy mechCharger = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
            if (mechCharger == null)
            {
                notice = "[CeleTech Shuttle] MechCharger rollback requires CompShuttleMechChargerOccupancy.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner<Thing> source = ShuttleHolderTransferLookupUtility.GetActiveTransporterThingContainer(handoff);
            if (source == null)
            {
                notice = "[CeleTech Shuttle] MechCharger rollback could not find active transporter container.";
                return false;
            }

            bool result = mechCharger.TryRollbackChargingMechLaunchExport(state.Manifest, source, out notice);
            if (result && Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] MechCharger launch export rolled back: " + notice);
            }

            return result;
        }

        internal static bool TryRollbackLaunchExportWithRecovery(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool mechsQuarantined,
            out string notice)
        {
            rollbackSucceeded = false;
            mechsQuarantined = false;

            if (TryRollbackLaunchExport(shuttleHost, handoffs, out notice))
            {
                rollbackSucceeded = true;
                return true;
            }

            string rollbackFailureNotice = notice;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasMechChargerManifestEntries)
            {
                notice = "[CeleTech Shuttle] MechCharger rollback failed but no MechCharger manifest remains active. rollback=" +
                    (rollbackFailureNotice ?? "null");
                return true;
            }

            int remainingBefore = ShuttleHolderTransferLookupUtility.CountMechChargerManifestThingsInHandoffs(state.Manifest, handoffs);
            if (remainingBefore <= 0)
            {
                notice = "[CeleTech Shuttle] MechCharger rollback failed, but no MechCharger manifest mechs remain in committed handoffs. Manifest remains active for diagnostics/recovery. rollback=" +
                    (rollbackFailureNotice ?? "null");
                return true;
            }

            int quarantinedCount;
            string quarantineNotice;
            bool quarantineSucceeded = ShuttleHolderLaunchTransferService.TryQuarantineMechChargerManifestPawnsFromHandoffs(
                state,
                handoffs,
                out quarantinedCount,
                out quarantineNotice);
            int remainingAfter = ShuttleHolderTransferLookupUtility.CountMechChargerManifestThingsInHandoffs(state.Manifest, handoffs);
            mechsQuarantined = quarantinedCount > 0 && remainingAfter == 0;
            if (quarantineSucceeded && remainingAfter == 0)
            {
                notice = "[CeleTech Shuttle] MechCharger rollback failed, but " +
                    quarantinedCount +
                    " mech(s) were moved from committed handoffs to emergency recovery. Manifest remains active for recovery. rollback=" +
                    (rollbackFailureNotice ?? "null") +
                    " quarantine=" +
                    (quarantineNotice ?? "null");
                return true;
            }

            notice = "[CeleTech Shuttle] MechCharger rollback failed and " +
                remainingAfter +
                " MechCharger manifest mech(s) remain in committed handoffs. Normal cargo rollback must not proceed. rollback=" +
                (rollbackFailureNotice ?? "null") +
                " quarantine=" +
                (quarantineNotice ?? "null");
            return false;
        }
    }
}
