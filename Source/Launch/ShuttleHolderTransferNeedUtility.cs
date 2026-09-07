using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleHolderTransferNeedUtility
    {
        internal static bool IsDevMedicalBayRealLaunchRollbackSpikeEnabled(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            return state != null && state.DevMedicalBayRealLaunchRollbackSpikeEnabled;
        }

        internal static bool IsDevMedicalBayRealLaunchRestoreSpikeEnabled(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            return state != null && state.DevMedicalBayRealLaunchRestoreSpikeEnabled;
        }

        internal static bool NeedsMedicalBayPatientLaunchTransfer(ThingWithComps shuttleHost)
        {
            CompShuttleMedicalBayOccupancy medicalBay = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>()
                : null;
            return medicalBay != null && medicalBay.HasPatients;
        }

        internal static bool NeedsDevMedicalBayPatientRealLaunchRollbackSpike(ThingWithComps shuttleHost)
        {
            return IsDevMedicalBayRealLaunchRollbackSpikeEnabled(shuttleHost) &&
                NeedsMedicalBayPatientLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsDevMedicalBayPatientRealLaunchRestoreSpike(ThingWithComps shuttleHost)
        {
            return IsDevMedicalBayRealLaunchRestoreSpikeEnabled(shuttleHost) &&
                NeedsMedicalBayPatientLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsMechChargerLaunchTransfer(ThingWithComps shuttleHost)
        {
            CompShuttleMechChargerOccupancy mechCharger = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
            return mechCharger != null && mechCharger.HasChargingMechs;
        }

        internal static bool NeedsPrisonCellPrisonerLaunchTransfer(ThingWithComps shuttleHost)
        {
            CompShuttlePrisonCellOccupancy prisonCell = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            return prisonCell != null && prisonCell.HasPrisoners;
        }

        internal static bool IsDevHabitatJoyRealLaunchSpikeEnabled(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            return state != null && state.DevHabitatJoyRealLaunchSpikeEnabled;
        }

        internal static bool IsDevHabitatMixedRealLaunchSpikeEnabled(ThingWithComps shuttleHost)
        {
            CompShuttleHolderLaunchTransferState state = GetState(shuttleHost);
            return state != null && state.DevHabitatMixedRealLaunchSpikeEnabled;
        }

        internal static bool NeedsDevHabitatJoyRealLaunchTransfer(ThingWithComps shuttleHost)
        {
            if (!IsDevHabitatJoyRealLaunchSpikeEnabled(shuttleHost))
            {
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            return habitat != null && habitat.JoyOccupantCount > 0;
        }

        internal static bool NeedsDevHabitatMixedRealLaunchTransfer(ThingWithComps shuttleHost)
        {
            if (!IsDevHabitatMixedRealLaunchSpikeEnabled(shuttleHost))
            {
                return false;
            }

            return NeedsHabitatMixedLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsHabitatMixedLaunchTransfer(ThingWithComps shuttleHost)
        {
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            return habitat != null &&
                habitat.JoyOccupantCount > 0 &&
                (habitat.SleepingOccupantCount > 0 || habitat.DiningOccupantCount > 0);
        }

        internal static bool NeedsHabitatJoyLaunchTransfer(ThingWithComps shuttleHost)
        {
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            return habitat != null && habitat.JoyOccupantCount > 0;
        }

        internal static bool NeedsAnyHabitatLaunchTransfer(ThingWithComps shuttleHost)
        {
            return NeedsHabitatMixedLaunchTransfer(shuttleHost) ||
                NeedsHabitatJoyLaunchTransfer(shuttleHost) ||
                NeedsHabitatLivingLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsHabitatLivingLaunchTransfer(ThingWithComps shuttleHost)
        {
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            return habitat != null && habitat.HasAnyOccupants;
        }

        private static CompShuttleHolderLaunchTransferState GetState(ThingWithComps shuttleHost)
        {
            return shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }
    }
}
