using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class HabitatHolderExportHandler
    {
        internal static bool TryExportLivingToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            failureReason = null;
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null || !habitat.HasAnyOccupants)
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Living Habitat launch export requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            if (state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Living Habitat launch export refused because a holder transfer manifest is already active. " +
                    state.DumpManifestForDebug();
                return false;
            }

            if (!habitat.CanTransferLivingForLaunch(out failureReason))
            {
                failureReason = "[CeleTech Shuttle] Living Habitat launch export prevalidation failed: " + failureReason;
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Living Habitat launch export failed: active transporter container unavailable.";
                return false;
            }

            if (!habitat.TryExportLivingForLaunch(state.Manifest, destination, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                string rollbackNotice;
                habitat.TryRollbackLivingExport(state.Manifest, destination, out rollbackNotice);
                failureReason = "[CeleTech Shuttle] Living Habitat launch export failed: " +
                    failureReason +
                    " rollback=" +
                    rollbackNotice;
                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] Living Habitat exported to real launch handoff. " + state.DumpManifestForDebug());
            }

            return true;
        }

        internal static bool TryExportJoyToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            failureReason = null;
            if (!HolderTransferPreflightService.NeedsHabitatJoyLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (!HolderTransferPreflightService.CanUseHabitatJoyLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (state == null || habitat == null)
            {
                failureReason = "[CeleTech Shuttle] Habitat joy launch export requires holder transfer state and Habitat occupancy comps.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Habitat joy launch export failed: active transporter container unavailable.";
                return false;
            }

            if (!habitat.TryExportJoyForLaunch(state.Manifest, destination, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                string rollbackNotice;
                habitat.TryRollbackJoyExport(state.Manifest, destination, out rollbackNotice);
                failureReason = "[CeleTech Shuttle] Habitat joy launch export failed: " +
                    failureReason +
                    " rollback=" +
                    rollbackNotice;
                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] Habitat joy exported to real launch handoff. " + state.DumpManifestForDebug());
            }

            return true;
        }

        internal static bool TryExportMixedToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            // Formal mixed export uses the real launch handoff. It must run after
            // cargo commit and before leaving skyfaller spawn.
            failureReason = null;
            if (!HolderTransferPreflightService.NeedsHabitatMixedLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (!HolderTransferPreflightService.CanUseHabitatMixedLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (state == null || habitat == null)
            {
                failureReason = "[CeleTech Shuttle] Habitat mixed launch export requires holder transfer state and Habitat occupancy comps.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner destination = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (destination == null)
            {
                failureReason = "[CeleTech Shuttle] Habitat mixed launch export failed: active transporter container unavailable.";
                return false;
            }

            if (!habitat.TryExportMixedForLaunch(state.Manifest, destination, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    state.MarkManifestFailed(failureReason);
                }

                string rollbackNotice;
                habitat.TryRollbackMixedExport(state.Manifest, destination, out rollbackNotice);
                failureReason = "[CeleTech Shuttle] Habitat mixed launch export failed: " +
                    failureReason +
                    " rollback=" +
                    rollbackNotice;
                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] Habitat mixed exported to real launch handoff. transferMode=mixed " + state.DumpManifestForDebug());
            }

            return true;
        }
    }
}
