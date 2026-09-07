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
        internal static bool TryRestoreBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            return HolderIncomingRestoreCoordinator.TryRestoreBeforeIncomingImpact(
                shuttleHost,
                incomingSkyfallerContainer,
                map,
                fallbackCell,
                out failureReason,
                out failureStatus);
        }

        // Internal core retained for the Phase 6 holder transfer handler seam.
        internal static bool TryRestoreBeforeIncomingImpactCore(
            ThingWithComps shuttleHost,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            // This is the last safe seam before vanilla impact spawns remaining cargo.
            // Holder manifest Things must be removed from staging here or they can
            // appear as ordinary cargo instead of returning to their shuttle holder.
            failureReason = null;
            failureStatus = ShuttleHolderIncomingRestoreFailureStatus.None;
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            if (state == null || !state.HasActiveManifest)
            {
                return true;
            }

            List<ShuttleHolderLaunchManifestEntry> devTestEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.DevTestHolderKind);
            List<ShuttleHolderLaunchManifestEntry> habitatSleepEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind);
            List<ShuttleHolderLaunchManifestEntry> habitatDiningPawnEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind);
            List<ShuttleHolderLaunchManifestEntry> habitatDiningFoodEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind);
            List<ShuttleHolderLaunchManifestEntry> habitatJoyEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind);
            List<ShuttleHolderLaunchManifestEntry> medicalBayPatientEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind);
            List<ShuttleHolderLaunchManifestEntry> mechChargerEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.MechChargerHolderKind);
            List<ShuttleHolderLaunchManifestEntry> prisonCellPrisonerEntries =
                state.Manifest.FindEntriesByHolderKind(ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind);
            bool hasHabitatLivingEntries = habitatSleepEntries.Count > 0 ||
                habitatDiningPawnEntries.Count > 0 ||
                habitatDiningFoodEntries.Count > 0;
            bool hasHabitatJoyEntries = habitatJoyEntries.Count > 0;
            bool hasMedicalBayPatientEntries = medicalBayPatientEntries.Count > 0;
            bool hasMechChargerEntries = mechChargerEntries.Count > 0;
            bool hasPrisonCellPrisonerEntries = prisonCellPrisonerEntries.Count > 0;
            if (devTestEntries.Count == 0 &&
                !hasMedicalBayPatientEntries &&
                !hasMechChargerEntries &&
                !hasPrisonCellPrisonerEntries &&
                !hasHabitatLivingEntries &&
                !hasHabitatJoyEntries)
            {
                return true;
            }

            bool allRestored = true;
            ThingOwner cargoStaging = ShuttleHolderTransferLookupUtility.GetShuttleCargoContainer(shuttleHost);
            if (hasMedicalBayPatientEntries)
            {
                string medicalBayRestoreFailureReason;
                ShuttleHolderIncomingRestoreFailureStatus medicalBayRestoreFailureStatus;
                if (!TryRestoreMedicalBayPatientsBeforeImpact(
                    shuttleHost,
                    cargoStaging,
                    incomingSkyfallerContainer,
                    out medicalBayRestoreFailureReason,
                    out medicalBayRestoreFailureStatus))
                {
                    allRestored = false;
                    KeepMostSevereIncomingRestoreFailure(
                        medicalBayRestoreFailureReason,
                        medicalBayRestoreFailureStatus,
                        ref failureReason,
                        ref failureStatus);
                }
            }

            if (hasMechChargerEntries)
            {
                string mechChargerRestoreFailureReason;
                ShuttleHolderIncomingRestoreFailureStatus mechChargerRestoreFailureStatus;
                if (!TryRestoreMechChargerBeforeIncomingImpact(
                    shuttleHost,
                    cargoStaging,
                    incomingSkyfallerContainer,
                    map,
                    fallbackCell,
                    out mechChargerRestoreFailureReason,
                    out mechChargerRestoreFailureStatus))
                {
                    allRestored = false;
                    KeepMostSevereIncomingRestoreFailure(
                        mechChargerRestoreFailureReason,
                        mechChargerRestoreFailureStatus,
                        ref failureReason,
                        ref failureStatus);
                }
            }

            if (hasPrisonCellPrisonerEntries)
            {
                string prisonCellRestoreFailureReason;
                ShuttleHolderIncomingRestoreFailureStatus prisonCellRestoreFailureStatus;
                if (!TryRestorePrisonCellPrisonersBeforeImpact(
                    shuttleHost,
                    cargoStaging,
                    incomingSkyfallerContainer,
                    out prisonCellRestoreFailureReason,
                    out prisonCellRestoreFailureStatus))
                {
                    allRestored = false;
                    KeepMostSevereIncomingRestoreFailure(
                        prisonCellRestoreFailureReason,
                        prisonCellRestoreFailureStatus,
                        ref failureReason,
                        ref failureStatus);
                }
            }

            for (int i = 0; i < devTestEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = devTestEntries[i];
                if (entry == null)
                {
                    continue;
                }

                Thing thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(cargoStaging, entry.ThingID);
                if (thing == null)
                {
                    thing = ShuttleHolderTransferLookupUtility.FindThingInOwner(incomingSkyfallerContainer, entry.ThingID);
                }

                if (thing == null)
                {
                    allRestored = false;
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                    entry.DebugNotes = "Restore could not find thing in shuttle cargo staging or incoming skyfaller.";
                    Log.Error("[CeleTech Shuttle] Dev holder transfer test restore could not find manifest thing before impact. " + entry.DumpForDebug());
                    continue;
                }

                if (state.DevTestHeldThings.TryAddOrTransfer(thing, true))
                {
                    entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseRestored;
                    entry.DebugNotes = "Restored before ModularShuttleIncoming.base.Impact.";
                    continue;
                }

                allRestored = false;
                entry.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseFailed;
                entry.DebugNotes = "Restore could not return thing to dev holder.";
                if (map != null && fallbackCell.IsValid && GenPlace.TryPlaceThing(thing, fallbackCell, map, ThingPlaceMode.Near))
                {
                    entry.DebugNotes = "Restore holder add failed; thing dropped near incoming shuttle.";
                }
                else
                {
                    Log.Error("[CeleTech Shuttle] Dev holder transfer test restore failed to return or drop thing. " + entry.DumpForDebug());
                }
            }

            if (hasHabitatLivingEntries || hasHabitatJoyEntries)
            {
                HabitatIncomingRestoreResult habitatRestoreResult;
                bool habitatFullyRestored = TryRestoreHabitatBeforeIncomingImpact(
                    shuttleHost,
                    state,
                    cargoStaging,
                    incomingSkyfallerContainer,
                    map,
                    fallbackCell,
                    out habitatRestoreResult);
                if (!habitatFullyRestored)
                {
                    allRestored = false;
                    string habitatFailureReason = habitatRestoreResult != null
                        ? habitatRestoreResult.FailureReason
                        : "[CeleTech Shuttle] Habitat incoming restore failed with no structured result.";
                    ShuttleHolderIncomingRestoreFailureStatus habitatFailureStatus =
                        habitatRestoreResult != null
                            ? ShuttleHolderIncomingRestoreStatusUtility.GetHabitatIncomingFailureStatus(habitatRestoreResult.Status)
                            : ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFailedQuarantined;
                    KeepMostSevereIncomingRestoreFailure(
                        habitatFailureReason,
                        habitatFailureStatus,
                        ref failureReason,
                        ref failureStatus);
                }
            }

            if (allRestored)
            {
                if (Prefs.DevMode &&
                    ShuttleLogThrottle.Global.ShouldLog(
                        "HolderTransfer:incoming-restore-success:" +
                            (shuttleHost != null ? shuttleHost.thingIDNumber : -1)))
                {
                    Log.Message("[CeleTech Shuttle] holder transfer restore succeeded before incoming base.Impact. " +
                        state.DumpManifestForDebug());
                }

                if (state.HasActiveManifest)
                {
                    if (state.HasRefrigeratedCargoLaunchTransfer ||
                        state.HasMedicalBayPatientStagingThings ||
                        state.HasDevHabitatLivingStagingThings ||
                        state.HasEmergencyRecoveryThings)
                    {
                        Log.Warning("[CeleTech Shuttle] Holder transfer restore completed but manifest was retained because an external recovery/staging transfer is still active. " +
                            state.DescribeActiveOrRecoveryTransferBlocker());
                    }
                    else if (!ShuttleHolderManifestClassificationUtility.ManifestContainsOnlyKnownIncomingRestoreHolderKinds(state.Manifest, out string clearBlocker))
                    {
                        Log.Warning("[CeleTech Shuttle] Holder transfer restore completed but manifest was retained because it contains an unknown holder kind. " +
                            clearBlocker +
                            " " +
                            state.DescribeActiveOrRecoveryTransferBlocker());
                    }
                    else
                    {
                        state.ClearManifest();
                    }
                }

                return true;
            }

            if (string.IsNullOrEmpty(failureReason))
            {
                failureReason = "[CeleTech Shuttle] holder transfer restore failed before incoming base.Impact. " + state.DumpManifestForDebug();
            }

            if (ShuttleHolderIncomingRestoreStatusUtility.IsQuarantinedFailureStatus(failureStatus))
            {
                Log.Warning(failureReason);
            }
            else
            {
                Log.Error(failureReason);
            }

            return false;
        }
    }
}
