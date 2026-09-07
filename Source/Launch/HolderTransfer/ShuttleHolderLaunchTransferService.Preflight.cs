using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static bool CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatJoyLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseHabitatJoyLaunchTransfer(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatJoyLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.CanUseHabitatJoyLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatJoyLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseHabitatJoyLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseAnySupportedHabitatHolderLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason,
            out string transferMode)
        {
            return HolderTransferPreflightService.CanUseAnySupportedHabitatHolderLaunchTransfer(
                shuttleHost,
                out failureReason,
                out transferMode);
        }

        internal static bool CanUseAnySupportedHabitatHolderLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason,
            out string transferMode)
        {
            return HabitatHolderTransferHandler.CanUseAnySupportedHabitatHolderLaunchTransferCore(
                shuttleHost,
                out failureReason,
                out transferMode);
        }

        internal static bool CanUseAnySupportedHabitatHolderLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason,
            out string transferMode)
        {
            return HolderTransferPreflightService.CanUseAnySupportedHabitatHolderLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason,
                out transferMode);
        }

        internal static bool CanUseHabitatMixedLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseHabitatMixedLaunchTransfer(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatMixedLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.CanUseHabitatMixedLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatMixedLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseHabitatMixedLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseHabitatLivingLaunchTransfer(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.CanUseHabitatLivingLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseHabitatLivingLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static string GetHabitatLivingLaunchFailureKey(
            TransportersArrivalAction arrivalAction,
            string failureReason)
        {
            return HolderTransferPreflightService.GetHabitatLivingLaunchFailureKey(
                arrivalAction,
                failureReason);
        }

        internal static string GetHabitatLivingLaunchFailureKeyCore(
            TransportersArrivalAction arrivalAction,
            string failureReason)
        {
            return HabitatHolderTransferHandler.GetHabitatLivingLaunchFailureKeyCore(
                arrivalAction,
                failureReason);
        }

        internal static string GetHabitatLivingReadinessIssueKey(string failureReason)
        {
            return HolderTransferPreflightService.GetHabitatLivingReadinessIssueKey(failureReason);
        }

        internal static string GetHabitatLivingReadinessIssueKeyCore(string failureReason)
        {
            return HabitatHolderTransferHandler.GetHabitatLivingReadinessIssueKeyCore(failureReason);
        }

        internal static bool NeedsHabitatLivingLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsHabitatLivingLaunchTransfer(shuttleHost);
        }

        internal static bool IsSupportedHabitatLivingArrivalAction(TransportersArrivalAction arrivalAction)
        {
            return HolderTransferPreflightService.IsSupportedHabitatLivingArrivalAction(arrivalAction);
        }

        internal static bool IsSupportedHabitatLivingArrivalAction(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.IsSupportedHabitatLivingArrivalAction(
                arrivalAction,
                out failureReason);
        }

        // Internal core retained for the Phase 6 holder transfer handler seam.
        internal static bool IsSupportedMedicalBayRestoreArrivalActionCore(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            failureReason = null;
            if (ShuttleLaunchArrivalReflectionUtility.IsModularShuttleSpecificCellArrival(arrivalAction))
            {
                return true;
            }

            TransportersArrivalAction_LandInSpecificCell landInSpecificCell =
                arrivalAction as TransportersArrivalAction_LandInSpecificCell;
            if (landInSpecificCell != null &&
                ShuttleLaunchArrivalReflectionUtility.TryReadsLandInShuttle(landInSpecificCell))
            {
                return true;
            }

            TransportersArrivalAction_VisitSite visitSite = arrivalAction as TransportersArrivalAction_VisitSite;
            if (visitSite != null && ShuttleLaunchArrivalReflectionUtility.TryReadVisitSite(visitSite) != null)
            {
                return true;
            }

            failureReason = MedicalBayUnsupportedArrivalFailureKey.Translate().ToString();
            return false;
        }

        // Internal core retained for the Phase 6 holder transfer handler seam.
        internal static bool IsSupportedMechChargerRestoreArrivalActionCore(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            failureReason = null;
            if (ShuttleLaunchArrivalReflectionUtility.IsModularShuttleSpecificCellArrival(arrivalAction))
            {
                return true;
            }

            TransportersArrivalAction_LandInSpecificCell landInSpecificCell =
                arrivalAction as TransportersArrivalAction_LandInSpecificCell;
            if (landInSpecificCell != null &&
                ShuttleLaunchArrivalReflectionUtility.TryReadsLandInShuttle(landInSpecificCell))
            {
                return true;
            }

            TransportersArrivalAction_VisitSite visitSite = arrivalAction as TransportersArrivalAction_VisitSite;
            if (visitSite != null && ShuttleLaunchArrivalReflectionUtility.TryReadVisitSite(visitSite) != null)
            {
                return true;
            }

            failureReason = MechChargerUnsupportedArrivalFailureKey.Translate().ToString();
            return false;
        }

        // Internal core retained for the Phase 6 holder transfer handler seam.
        internal static bool IsSupportedPrisonCellRestoreArrivalActionCore(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            failureReason = null;
            if (ShuttleLaunchArrivalReflectionUtility.IsModularShuttleSpecificCellArrival(arrivalAction))
            {
                return true;
            }

            TransportersArrivalAction_LandInSpecificCell landInSpecificCell =
                arrivalAction as TransportersArrivalAction_LandInSpecificCell;
            if (landInSpecificCell != null &&
                ShuttleLaunchArrivalReflectionUtility.TryReadsLandInShuttle(landInSpecificCell))
            {
                return true;
            }

            TransportersArrivalAction_VisitSite visitSite = arrivalAction as TransportersArrivalAction_VisitSite;
            if (visitSite != null && ShuttleLaunchArrivalReflectionUtility.TryReadVisitSite(visitSite) != null)
            {
                return true;
            }

            failureReason = PrisonCellUnsupportedArrivalFailureKey.Translate().ToString();
            return false;
        }

        internal static bool IsSupportedHabitatLivingArrivalActionCore(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.IsSupportedHabitatLivingArrivalActionCore(
                arrivalAction,
                out failureReason);
        }
    }
}
