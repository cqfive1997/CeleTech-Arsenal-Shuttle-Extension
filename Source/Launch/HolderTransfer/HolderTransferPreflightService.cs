using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    // Phase 6 preflight query seam. Core methods still live on
    // ShuttleHolderLaunchTransferService during the transition; this layer must
    // preserve failure reasons, readiness keys, and arrival-action support.
    internal static class HolderTransferPreflightService
    {
        internal static bool NeedsHabitatMixedLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsHabitatMixedLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsHabitatJoyLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsHabitatJoyLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsAnyHabitatLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsAnyHabitatLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsHabitatLivingLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsHabitatLivingLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsMedicalBayPatientLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsMedicalBayPatientLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsMechChargerLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsMechChargerLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsPrisonCellPrisonerLaunchTransfer(ThingWithComps shuttleHost)
        {
            return ShuttleHolderTransferNeedUtility.NeedsPrisonCellPrisonerLaunchTransfer(shuttleHost);
        }

        internal static bool CanUseHabitatJoyLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseHabitatJoyLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatJoyLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseHabitatJoyLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsHabitatJoyLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return IsSupportedHabitatLivingArrivalAction(arrivalAction, out failureReason);
        }

        internal static bool CanUseAnySupportedHabitatHolderLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason,
            out string transferMode)
        {
            return ShuttleHolderLaunchTransferService.CanUseAnySupportedHabitatHolderLaunchTransferCore(
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
            if (!CanUseAnySupportedHabitatHolderLaunchTransfer(shuttleHost, out failureReason, out transferMode))
            {
                return false;
            }

            if (transferMode == "none")
            {
                return true;
            }

            return IsSupportedHabitatLivingArrivalAction(arrivalAction, out failureReason);
        }

        internal static bool CanUseHabitatMixedLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseHabitatMixedLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatMixedLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseHabitatMixedLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsHabitatMixedLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return IsSupportedHabitatLivingArrivalAction(arrivalAction, out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseHabitatLivingLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseHabitatLivingLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsHabitatLivingLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return IsSupportedHabitatLivingArrivalAction(arrivalAction, out failureReason);
        }

        internal static string GetHabitatLivingLaunchFailureKey(
            TransportersArrivalAction arrivalAction,
            string failureReason)
        {
            return ShuttleHolderLaunchTransferService.GetHabitatLivingLaunchFailureKeyCore(
                arrivalAction,
                failureReason);
        }

        internal static string GetHabitatLivingReadinessIssueKey(string failureReason)
        {
            return ShuttleHolderLaunchTransferService.GetHabitatLivingReadinessIssueKeyCore(failureReason);
        }

        internal static bool IsSupportedHabitatLivingArrivalAction(TransportersArrivalAction arrivalAction)
        {
            string ignoredFailureReason;
            return IsSupportedHabitatLivingArrivalAction(arrivalAction, out ignoredFailureReason);
        }

        internal static bool IsSupportedHabitatLivingArrivalAction(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.IsSupportedHabitatLivingArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferCore(
                shuttleHost,
                false,
                false,
                false,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferCore(
                shuttleHost,
                true,
                false,
                false,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferCore(
                shuttleHost,
                true,
                true,
                false,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMedicalBayPatientLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMedicalBayRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(shuttleHost, out failureReason))
            {
                return false;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMedicalBayRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(shuttleHost, out failureReason))
            {
                return false;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMedicalBayRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferCore(
                shuttleHost,
                false,
                false,
                false,
                false,
                false,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferCore(
                shuttleHost,
                true,
                false,
                false,
                false,
                false,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithMedicalConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferCore(
                shuttleHost,
                false,
                true,
                false,
                false,
                false,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferCore(
                shuttleHost,
                true,
                true,
                false,
                false,
                false,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMechChargerLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsMechChargerLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMechChargerRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMechChargerLaunchTransferWithHabitatConcurrency(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsMechChargerLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMechChargerRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithMedicalConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMechChargerLaunchTransferWithMedicalConcurrency(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsMechChargerLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMechChargerRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUseMechChargerLaunchTransferWithFullHolderConcurrency(shuttleHost, out failureReason))
            {
                return false;
            }

            if (!NeedsMechChargerLaunchTransfer(shuttleHost))
            {
                return true;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedMechChargerRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUsePrisonCellPrisonerLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return ShuttleHolderLaunchTransferService.CanUsePrisonCellPrisonerLaunchTransferCore(
                shuttleHost,
                false,
                out failureReason);
        }

        internal static bool CanUsePrisonCellPrisonerLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            if (!CanUsePrisonCellPrisonerLaunchTransfer(shuttleHost, out failureReason))
            {
                return false;
            }

            return ShuttleHolderLaunchTransferService.IsSupportedPrisonCellRestoreArrivalActionCore(
                arrivalAction,
                out failureReason);
        }
    }
}
