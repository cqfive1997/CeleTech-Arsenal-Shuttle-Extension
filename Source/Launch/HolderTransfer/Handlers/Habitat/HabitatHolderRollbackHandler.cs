using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class HabitatHolderRollbackHandler
    {
        internal static bool TryRollbackLivingLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasHabitatLivingManifestEntries)
            {
                notice = "No Habitat living launch manifest to roll back.";
                return true;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Habitat living launch rollback requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner source = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (source == null)
            {
                notice = "[CeleTech Shuttle] Habitat living launch rollback could not find active transporter container.";
                return false;
            }

            bool result = habitat.TryRollbackLivingExport(state.Manifest, source, out notice);
            if (result && Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] Habitat living launch export rolled back: " + notice);
            }

            return result;
        }

        internal static bool TryRollbackJoyLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasHabitatJoyManifestEntries)
            {
                notice = "No Habitat joy launch manifest to roll back.";
                return true;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Habitat joy launch rollback requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner source = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (source == null)
            {
                notice = "[CeleTech Shuttle] Habitat joy launch rollback could not find active transporter container.";
                return false;
            }

            bool result = habitat.TryRollbackJoyExport(state.Manifest, source, out notice);
            if (result && Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] Habitat joy launch export rolled back: " + notice);
            }

            return result;
        }

        internal static bool TryRollbackMixedLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            notice = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state == null || !state.HasHabitatLivingManifestEntries || !state.HasHabitatJoyManifestEntries)
            {
                notice = "No Habitat mixed launch manifest to roll back.";
                return true;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null)
            {
                notice = "[CeleTech Shuttle] Habitat mixed launch rollback requires CompShuttleHabitatOccupancy.";
                return false;
            }

            ShuttleLaunchCargoHandoff handoff = ShuttleHolderTransferLookupUtility.FindShuttleHandoff(handoffs);
            ThingOwner source = ShuttleHolderTransferLookupUtility.GetActiveTransporterContainer(handoff);
            if (source == null)
            {
                notice = "[CeleTech Shuttle] Habitat mixed launch rollback could not find active transporter container.";
                return false;
            }

            bool result = habitat.TryRollbackMixedExport(state.Manifest, source, out notice);
            if (result && Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] Habitat mixed launch export rolled back: " + notice);
            }

            return result;
        }
    }
}
