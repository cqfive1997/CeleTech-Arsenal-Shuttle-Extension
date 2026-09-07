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
        internal static bool TryExportDevTestHolderForLaunch(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                return true;
            }

            if (state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Dev holder transfer test refused because a manifest is already active. " +
                    state.DumpManifestForDebug();
                Log.Error(failureReason);
                return false;
            }

            if (!state.HasDevTestHeldThings)
            {
                return true;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Dev holder transfer test export failed: active transporter container unavailable.";
                return false;
            }

            try
            {
                int exportTick = ShuttleTickUtility.TicksGameOrMinusOne();
                ThingOwner<Thing> source = state.DevTestHeldThings;
                while (source.Count > 0)
                {
                    Thing thing = source[source.Count - 1];
                    ShuttleHolderLaunchManifestEntry entry = state.Manifest.AddDevTestEntry(
                        thing,
                        exportTick,
                        ShuttleHolderLaunchManifestConstants.PhaseExported,
                        "Exported after cargo commit and before leaving skyfaller spawn.");

                    if (!destination.TryAddOrTransfer(thing, true))
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                        entry.DebugNotes = "TryAddOrTransfer to ActiveTransporterInfo.innerContainer failed.";
                        failureReason = "[CeleTech Shuttle] Dev holder transfer test export failed while moving thing. " + entry.DumpForDebug();
                        TryRollbackDevTestExport(shuttleHost, handoffs, shuttleHost != null ? shuttleHost.Map : null, out string rollbackNotice);
                        Log.Error(failureReason + " rollback=" + rollbackNotice);
                        return false;
                    }

                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseInFlight;
                }

                Log.Message("[CeleTech Shuttle] Dev holder transfer test export succeeded. " + state.DumpManifestForDebug());
                return true;
            }
            catch (Exception exception)
            {
                failureReason = "[CeleTech Shuttle] Dev holder transfer test export exception: " + exception;
                state.MarkManifestFailed(exception.Message);
                TryRollbackDevTestExport(shuttleHost, handoffs, shuttleHost != null ? shuttleHost.Map : null, out string rollbackNotice);
                Log.Error(failureReason + " rollback=" + rollbackNotice);
                return false;
            }
        }

        internal static bool TryRollbackDevTestExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            Map map,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null || !state.HasActiveManifest)
            {
                notice = "No Dev holder transfer test manifest to roll back.";
                return true;
            }

            List<ShuttleHolderLaunchManifestEntry> devTestEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.DevTestHolderKind);
            if (devTestEntries.Count == 0)
            {
                notice = "No DevTest entries exist in the active holder transfer manifest.";
                return true;
            }

            bool allRestored = true;
            for (int i = 0; i < devTestEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = devTestEntries[i];
                if (entry == null)
                {
                    continue;
                }

                Thing thing = ShuttleHolderTransferLookupUtility.FindThingInHandoffs(handoffs, entry.ThingID);
                if (thing == null)
                {
                    if (ShuttleHolderTransferLookupUtility.FindThingInOwner(state.DevTestHeldThings, entry.ThingID) != null)
                    {
                        entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                        entry.DebugNotes = "Thing was already in dev holder during rollback.";
                        continue;
                    }

                    allRestored = false;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Rollback could not find exported thing.";
                    Log.Error("[CeleTech Shuttle] Dev holder transfer test rollback could not find exported thing. " + entry.DumpForDebug());
                    continue;
                }

                if (state.DevTestHeldThings.TryAddOrTransfer(thing, true))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRolledBack;
                    entry.DebugNotes = "Rolled back to dev test holder.";
                    continue;
                }

                allRestored = false;
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Rollback could not return thing to dev holder.";
                if (!TryDropThingNear(thing, shuttleHost, map))
                {
                    Log.Error("[CeleTech Shuttle] Dev holder transfer test rollback failed to return or drop thing. " + entry.DumpForDebug());
                }
            }

            notice = state.DumpManifestForDebug();
            if (allRestored)
            {
                if (state.Manifest.Entries.Count == devTestEntries.Count)
                {
                    string clearFailureReason;
                    if (!TryClearManifest(shuttleHost, out clearFailureReason))
                    {
                        Log.Warning("[CeleTech Shuttle] Dev holder transfer test export rollback restored all dev test Things but left the manifest active. " +
                            clearFailureReason);
                    }
                }

                notice = "Dev holder transfer test export rolled back successfully.";
            }

            return allRestored;
        }

        internal static bool TryExportDevHabitatLivingForLaunch(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat living export test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat living export test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat living export test refused because a holder transfer manifest is already active. " +
                    state.DumpManifestForDebug();
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat living export test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ThingOwner destination = state.DevHabitatLivingExportContainer;
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat living export test has no ActiveTransporterInfo.innerContainer staging holder.";
                return false;
            }

            if (destination.Count > 0)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat living export test refused because local staging already contains Things. Roll back or clean the staging holder before exporting again.";
                return false;
            }

            if (!habitat.TryExportDevLivingForLaunch(state.Manifest, destination, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                return false;
            }

            Log.Message("[CeleTech Shuttle] Dev Habitat living export test manifest: " + state.DumpManifestForDebug());
            return true;
        }

        internal static bool TryExportDevHabitatJoyForLaunch(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy export test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy export test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy export test refused because a holder transfer manifest is already active. " +
                    state.DumpManifestForDebug();
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy export test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ThingOwner destination = state.DevHabitatLivingExportContainer;
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy export test has no local staging holder.";
                return false;
            }

            if (destination.Count > 0)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy export test refused because local staging already contains Things. Roll back or clean the staging holder before exporting again.";
                return false;
            }

            if (!habitat.TryExportDevJoyForLaunch(state.Manifest, destination, out failureReason))
            {
                return false;
            }

            Log.Message("[CeleTech Shuttle] Dev Habitat joy export test manifest: " + state.DumpManifestForDebug());
            return true;
        }

        internal static bool TryExportDevHabitatMixedForLaunch(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed export test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed export test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed export test refused because a holder transfer manifest is already active. " +
                    state.DumpManifestForDebug();
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed export test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ThingOwner destination = state.DevHabitatLivingExportContainer;
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed export test has no local staging holder.";
                return false;
            }

            if (destination.Count > 0)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed export test refused because local staging already contains Things. Roll back or restore the staging holder before exporting again.";
                return false;
            }

            if (!habitat.TryExportDevMixedForLaunch(state.Manifest, destination, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                return false;
            }

            Log.Message("[CeleTech Shuttle] Dev Habitat mixed export test manifest: " + state.DumpManifestForDebug());
            return true;
        }

        internal static bool TryExportDevMedicalBayPatientsForLaunch(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test refused because a holder transfer manifest is already active. " +
                    state.DumpManifestForDebug();
                return false;
            }

            if (state.HasMedicalBayPatientStagingThings)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test refused because local patient staging is not empty.";
                return false;
            }

            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test requires CompShuttleMedicalBayOccupancy.";
                return false;
            }

            if (!medicalBay.HasPatients)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test no-op: no patients are held.";
                return true;
            }

            if (!medicalBay.CanTransferPatientsForLaunch(out notice))
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient export test prevalidation failed: " + notice;
                return false;
            }

            if (!medicalBay.TryExportPatientsForLaunch(
                state.Manifest,
                state.MedicalBayPatientStagingThings,
                true,
                out notice))
            {
                if (!notice.NullOrEmpty())
                {
                    state.MarkManifestFailed(notice);
                }

                return false;
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay patient export test succeeded. " + state.DumpManifestForDebug());
            return true;
        }

        internal static bool TryRollbackDevMedicalBayPatientsExport(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient local rollback test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient local rollback test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.HasMedicalBayPatientManifestEntries)
            {
                notice = "[CeleTech Shuttle] No MedicalBay patient manifest entries exist for the Dev MedicalBay patient local rollback test.";
                return true;
            }

            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient local rollback test requires CompShuttleMedicalBayOccupancy.";
                return false;
            }

            bool result = medicalBay.TryRollbackPatientLaunchExport(
                state.Manifest,
                state.MedicalBayPatientStagingThings,
                out notice);
            if (result)
            {
                Log.Message("[CeleTech Shuttle] Dev MedicalBay patient local rollback test result: " + notice);
            }

            return result;
        }

        internal static bool TryRestoreDevMedicalBayPatientsFromLocalStaging(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient local restore test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient local restore test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.HasMedicalBayPatientManifestEntries)
            {
                notice = "[CeleTech Shuttle] No MedicalBay patient manifest entries exist for the Dev MedicalBay patient local restore test.";
                return true;
            }

            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            if (medicalBay == null)
            {
                notice = "[CeleTech Shuttle] Dev MedicalBay patient local restore test requires CompShuttleMedicalBayOccupancy.";
                return false;
            }

            bool result = medicalBay.TryRestorePatientsFromLaunchStaging(
                state.Manifest,
                state.MedicalBayPatientStagingThings,
                out notice);
            if (result)
            {
                Log.Message("[CeleTech Shuttle] Dev MedicalBay patient local restore test result: " + notice);
            }

            return result;
        }

        internal static bool IsDevMedicalBayRealLaunchRollbackSpikeEnabled(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.IsDevMedicalBayRealLaunchRollbackSpikeEnabled(shuttleHost);
        }

        internal static bool IsDevMedicalBayRealLaunchRestoreSpikeEnabled(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.IsDevMedicalBayRealLaunchRestoreSpikeEnabled(shuttleHost);
        }

        internal static void ClearDevMedicalBayRealLaunchRollbackSpike(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state != null)
            {
                state.ClearDevMedicalBayRealLaunchRollbackSpike();
            }
        }

        internal static void ClearDevMedicalBayRealLaunchRestoreSpike(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state != null)
            {
                state.ClearDevMedicalBayRealLaunchRestoreSpike();
            }
        }

        internal static bool NeedsDevMedicalBayPatientRealLaunchRollbackSpike(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsDevMedicalBayPatientRealLaunchRollbackSpike(shuttleHost);
        }

        internal static bool NeedsDevMedicalBayPatientRealLaunchRestoreSpike(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsDevMedicalBayPatientRealLaunchRestoreSpike(shuttleHost);
        }

        internal static bool CanUseDevMedicalBayRealLaunchRollbackSpike(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test is DevMode-only.";
                return false;
            }

            if (!IsDevMedicalBayRealLaunchRollbackSpikeEnabled(shuttleHost))
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patients still require the Dev MedicalBay real-launch rollback one-shot flag.";
                return false;
            }

            return CanUseMedicalBayPatientLaunchTransfer(shuttleHost, out failureReason);
        }

        internal static bool CanUseDevMedicalBayRealLaunchRestoreSpike(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev MedicalBay real-launch restore test is DevMode-only.";
                return false;
            }

            if (!IsDevMedicalBayRealLaunchRestoreSpikeEnabled(shuttleHost))
            {
                failureReason = "[CeleTech Shuttle] MedicalBay patients still require the Dev MedicalBay real-launch restore one-shot flag.";
                return false;
            }

            if (IsDevMedicalBayRealLaunchRollbackSpikeEnabled(shuttleHost))
            {
                failureReason = "[CeleTech Shuttle] MedicalBay restore test refused because the rollback test one-shot flag is also active.";
                return false;
            }

            return CanUseMedicalBayPatientLaunchTransfer(shuttleHost, out failureReason);
        }

        internal static bool CanUseDevMedicalBayRealLaunchRestoreSpike(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseDevMedicalBayRealLaunchRestoreSpike(shuttleHost, out failureReason))
            {
                return false;
            }

            return CanUseMedicalBayPatientLaunchTransfer(shuttleHost, arrivalAction, out failureReason);
        }

        internal static bool IsDevHabitatJoyRealLaunchSpikeEnabled(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.IsDevHabitatJoyRealLaunchSpikeEnabled(shuttleHost);
        }

        internal static bool IsDevHabitatMixedRealLaunchSpikeEnabled(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.IsDevHabitatMixedRealLaunchSpikeEnabled(shuttleHost);
        }

        internal static void ClearDevHabitatJoyRealLaunchSpike(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state != null)
            {
                state.ClearDevHabitatJoyRealLaunchSpike();
            }
        }

        internal static void ClearDevHabitatMixedRealLaunchSpike(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state != null)
            {
                state.ClearDevHabitatMixedRealLaunchSpike();
            }
        }

        internal static bool NeedsDevHabitatJoyRealLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsDevHabitatJoyRealLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsDevHabitatMixedRealLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsDevHabitatMixedRealLaunchTransfer(shuttleHost);
        }

        internal static bool CanUseDevHabitatJoyRealLaunchSpike(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy real-launch transfer test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy real-launch transfer test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.DevHabitatJoyRealLaunchSpikeEnabled)
            {
                failureReason = "[CeleTech Shuttle] Habitat joy occupants still require the Dev Habitat joy real-launch one-shot flag.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy real-launch transfer test refused because a holder transfer manifest is already active.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat joy real-launch transfer test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            return habitat.CanTransferJoyOnlyForLaunch(out failureReason);
        }

        internal static bool CanUseDevHabitatJoyRealLaunchSpike(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseDevHabitatJoyRealLaunchSpike(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!IsSupportedHabitatLivingArrivalAction(arrivalAction, out failureReason))
            {
                return false;
            }

            return true;
        }

        internal static bool CanUseDevHabitatMixedRealLaunchSpike(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            if (!Prefs.DevMode)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed real-launch transfer test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Dev Habitat mixed real-launch transfer test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.DevHabitatMixedRealLaunchSpikeEnabled)
            {
                failureReason = "[CeleTech Shuttle] Mixed Habitat occupants require the Dev Habitat mixed real-launch one-shot flag.";
                return false;
            }

            return CanUseHabitatMixedLaunchTransfer(shuttleHost, out failureReason);
        }

        internal static bool CanUseDevHabitatMixedRealLaunchSpike(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseDevHabitatMixedRealLaunchSpike(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!IsSupportedHabitatLivingArrivalAction(arrivalAction, out failureReason))
            {
                return false;
            }

            return true;
        }

        internal static bool TryExportDevHabitatJoyToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return TryExportHabitatJoyToLaunchHandoff(shuttleHost, handoffs, out failureReason);
        }

        internal static bool TryExportDevHabitatMixedToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            failureReason = null;
            if (!NeedsDevHabitatMixedRealLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (!CanUseDevHabitatMixedRealLaunchSpike(shuttleHost, out failureReason))
            {
                return false;
            }

            return TryExportHabitatMixedToLaunchHandoff(shuttleHost, handoffs, out failureReason);
        }

        internal static bool TryRollbackDevHabitatJoyLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return TryRollbackHabitatJoyLaunchExport(shuttleHost, handoffs, out notice);
        }

        internal static bool TryRollbackDevHabitatMixedLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return TryRollbackHabitatMixedLaunchExport(shuttleHost, handoffs, out notice);
        }

        internal static bool TryRollbackDevHabitatLivingExport(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local rollback test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local rollback test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasHabitatJoyManifestEntries)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local rollback test refused because the active manifest also contains Habitat joy entries. Use the Dev Habitat mixed local rollback or restore test.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local rollback test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            bool result = habitat.TryRollbackLivingExport(
                state.Manifest,
                state.DevHabitatLivingExportContainer,
                out notice);
            if (result)
            {
                Log.Message("[CeleTech Shuttle] Dev Habitat living local rollback test result: " + notice);
            }

            return result;
        }

        internal static bool TryRestoreDevHabitatLivingFromLocalStaging(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local restore test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local restore test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.HasHabitatLivingManifestEntries)
            {
                notice = "[CeleTech Shuttle] No Habitat living manifest entries exist for the Dev Habitat living local restore test.";
                return false;
            }

            if (state.HasHabitatJoyManifestEntries)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local restore test refused because the active manifest also contains Habitat joy entries. Use the Dev Habitat mixed local restore or rollback test.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local restore test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ThingOwner source = state.DevHabitatLivingExportContainer;
            if (source == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat living local restore test has no local staging holder.";
                return false;
            }

            string failureReason;
            if (!habitat.TryRestoreDevLivingFromLocalStaging(state.Manifest, source, out failureReason))
            {
                notice = failureReason;
                Log.Warning("[CeleTech Shuttle] Dev Habitat living local restore test failed: " + failureReason + " manifest=" + state.DumpManifestForDebug());
                return false;
            }

            notice = "[CeleTech Shuttle] Dev Habitat living local restore test succeeded.";
            Log.Message(notice + " manifest=" + state.DumpManifestForDebug());
            return true;
        }

        internal static bool TryRollbackDevHabitatJoyExport(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local rollback test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local rollback test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasHabitatLivingManifestEntries)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local rollback test refused because the active manifest also contains Habitat living entries. Use the Dev Habitat mixed local rollback or restore test.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local rollback test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            bool result = habitat.TryRollbackDevJoyExport(
                state.Manifest,
                state.DevHabitatLivingExportContainer,
                out notice);
            if (result)
            {
                Log.Message("[CeleTech Shuttle] Dev Habitat joy local rollback test result: " + notice);
            }

            return result;
        }

        internal static bool TryRestoreDevHabitatJoyFromLocalStaging(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local restore test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local restore test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.HasHabitatJoyManifestEntries)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local restore test found no HabitatJoy manifest entries.";
                return false;
            }

            if (state.HasHabitatLivingManifestEntries)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local restore test refused because the active manifest also contains Habitat living entries. Use the Dev Habitat mixed local restore or rollback test.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local restore test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ThingOwner source = state.DevHabitatLivingExportContainer;
            if (source == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat joy local restore test has no local staging holder.";
                return false;
            }

            string failureReason;
            if (!habitat.TryRestoreDevJoyFromLocalStaging(state.Manifest, source, out failureReason))
            {
                notice = failureReason;
                Log.Warning("[CeleTech Shuttle] Dev Habitat joy local restore test failed: " +
                    failureReason +
                    " manifest=" +
                    state.DumpManifestForDebug());
                return false;
            }

            notice = "[CeleTech Shuttle] Dev Habitat joy local restore test succeeded.";
            Log.Message(notice + " manifest=" + state.DumpManifestForDebug());
            return true;
        }

        internal static bool TryRollbackDevHabitatMixedExport(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local rollback test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local rollback test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local rollback test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            bool result = habitat.TryRollbackDevMixedExport(
                state.Manifest,
                state.DevHabitatLivingExportContainer,
                out notice);
            if (result)
            {
                Log.Message("[CeleTech Shuttle] Dev Habitat mixed local rollback test result: " + notice);
            }

            return result;
        }

        internal static bool TryRestoreDevHabitatMixedFromLocalStaging(
            ThingWithComps shuttleHost,
            out string notice)
        {
            notice = null;
            if (!Prefs.DevMode)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local restore test is DevMode-only.";
                return false;
            }

            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local restore test requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (!state.HasHabitatLivingManifestEntries || !state.HasHabitatJoyManifestEntries)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local restore test requires both Habitat living and Habitat joy manifest entries.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local restore test requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ThingOwner source = state.DevHabitatLivingExportContainer;
            if (source == null)
            {
                notice = "[CeleTech Shuttle] Dev Habitat mixed local restore test has no local staging holder.";
                return false;
            }

            string failureReason;
            if (!habitat.TryRestoreDevMixedFromLocalStaging(state.Manifest, source, out failureReason))
            {
                notice = failureReason;
                Log.Warning("[CeleTech Shuttle] Dev Habitat mixed local restore test failed: " +
                    failureReason +
                    " manifest=" +
                    state.DumpManifestForDebug());
                return false;
            }

            notice = "[CeleTech Shuttle] Dev Habitat mixed local restore test succeeded.";
            Log.Message(notice + " manifest=" + state.DumpManifestForDebug());
            return true;
        }
    }
}
