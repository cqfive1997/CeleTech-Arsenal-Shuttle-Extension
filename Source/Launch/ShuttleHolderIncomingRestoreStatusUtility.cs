namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleHolderIncomingRestoreStatusUtility
    {
        internal static ShuttleHolderIncomingRestoreFailureStatus GetMedicalBayIncomingFailureStatus(
            bool quarantined)
        {
            return quarantined
                ? ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFailedQuarantined
                : ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFatalUnresolved;
        }

        internal static ShuttleHolderIncomingRestoreFailureStatus GetMechChargerIncomingFailureStatus(
            bool quarantined)
        {
            return quarantined
                ? ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFailedQuarantined
                : ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFatalUnresolved;
        }

        internal static ShuttleHolderIncomingRestoreFailureStatus GetPrisonCellIncomingFailureStatus(
            bool quarantined)
        {
            return quarantined
                ? ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFailedQuarantined
                : ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFatalUnresolved;
        }

        internal static ShuttleHolderIncomingRestoreFailureStatus GetHabitatIncomingFailureStatus(
            HabitatIncomingRestoreStatus status)
        {
            return status == HabitatIncomingRestoreStatus.FatalUnresolved
                ? ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFatalUnresolved
                : ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFailedQuarantined;
        }

        internal static bool IsQuarantinedFailureStatus(ShuttleHolderIncomingRestoreFailureStatus status)
        {
            return status == ShuttleHolderIncomingRestoreFailureStatus.HabitatRestoreFailedQuarantined ||
                status == ShuttleHolderIncomingRestoreFailureStatus.MedicalBayRestoreFailedQuarantined ||
                status == ShuttleHolderIncomingRestoreFailureStatus.MechChargerRestoreFailedQuarantined ||
                status == ShuttleHolderIncomingRestoreFailureStatus.PrisonCellRestoreFailedQuarantined;
        }

        internal static HabitatIncomingRestoreStatus BuildHabitatIncomingRestoreStatus(
            int total,
            int restoredCount,
            int ejectedCount,
            int quarantinedCount,
            int fatalCount)
        {
            if (total <= 0)
            {
                return HabitatIncomingRestoreStatus.None;
            }

            if (fatalCount > 0)
            {
                return HabitatIncomingRestoreStatus.FatalUnresolved;
            }

            if (restoredCount == total)
            {
                return HabitatIncomingRestoreStatus.AllRestored;
            }

            if (ejectedCount == total)
            {
                return HabitatIncomingRestoreStatus.AllSafelyEjected;
            }

            if (quarantinedCount == total)
            {
                return HabitatIncomingRestoreStatus.AllQuarantined;
            }

            return HabitatIncomingRestoreStatus.MixedResolved;
        }
    }
}
