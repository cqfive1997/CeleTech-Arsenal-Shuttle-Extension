using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class HabitatHolderPreflightHandler
    {
        private const string UnsupportedArrivalFailureKey = "CT_Shuttle_Launch_Failed_HabitatTransferUnsupportedArrival";

        private const string InvalidStateFailureKey = "CT_Shuttle_Launch_Failed_HabitatTransferInvalidState";

        private const string JoyOccupiedFailureKey = "CT_Shuttle_Launch_Failed_HabitatJoyOccupied";

        private const string InvalidStateIssueKey = "CT_Shuttle_Issue_HabitatTransferInvalidState";

        private const string JoyOccupiedIssueKey = "CT_Shuttle_Issue_HabitatJoyOccupied";

        internal static bool CanUseHabitatJoyLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state != null && state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Habitat joy launch transfer refused because a holder transfer manifest is already active.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null || habitat.JoyOccupantCount == 0)
            {
                return true;
            }

            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Habitat joy launch transfer requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            return habitat.CanTransferJoyOnlyForLaunch(out failureReason);
        }

        internal static bool CanUseAnySupportedHabitatHolderLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason,
            out string transferMode)
        {
            // Shared prevalidation for world-target menu generation and readiness
            // checks. It classifies the current holder state before arrival-action
            // validation adds the final safety gate.
            transferMode = "none";
            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null || !habitat.HasAnyOccupants)
            {
                failureReason = null;
                return true;
            }

            if (HolderTransferPreflightService.NeedsHabitatMixedLaunchTransfer(shuttleHost))
            {
                transferMode = "mixed";
                return CanUseHabitatMixedLaunchTransferCore(shuttleHost, out failureReason);
            }

            if (HolderTransferPreflightService.NeedsHabitatJoyLaunchTransfer(shuttleHost))
            {
                transferMode = "joy-only";
                return CanUseHabitatJoyLaunchTransferCore(shuttleHost, out failureReason);
            }

            transferMode = "sleep-dining";
            return CanUseHabitatLivingLaunchTransferCore(shuttleHost, out failureReason);
        }

        internal static bool CanUseHabitatMixedLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            // Formal mixed gate: sleep/dining and joy must move as one holder
            // transaction, so do not fall through to the living-only or joy-only gates.
            failureReason = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state != null && state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Habitat mixed launch transfer refused because a holder transfer manifest is already active.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null || !HolderTransferPreflightService.NeedsHabitatMixedLaunchTransfer(shuttleHost))
            {
                return true;
            }

            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Habitat mixed launch transfer requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            return habitat.CanTransferMixedForLaunch(out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            failureReason = null;
            CompShuttleHolderLaunchTransferState state = ShuttleHolderLaunchTransferService.GetState(shuttleHost);
            if (state != null && state.HasActiveManifest)
            {
                failureReason = "[CeleTech Shuttle] Living Habitat launch transfer refused because a holder transfer manifest is already active.";
                return false;
            }

            CompShuttleHabitatOccupancy habitat = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (habitat == null || !habitat.HasAnyOccupants)
            {
                return true;
            }

            if (state == null)
            {
                failureReason = "[CeleTech Shuttle] Living Habitat launch transfer requires CompShuttleHolderLaunchTransferState.";
                return false;
            }

            return habitat.CanTransferLivingForLaunch(out failureReason);
        }

        internal static string GetHabitatLivingLaunchFailureKeyCore(
            TransportersArrivalAction arrivalAction,
            string failureReason)
        {
            if (IsJoyTransferFailure(failureReason))
            {
                return JoyOccupiedFailureKey;
            }

            if (!failureReason.NullOrEmpty() && !IsUnsupportedArrivalFailure(failureReason))
            {
                return InvalidStateFailureKey;
            }

            if (!IsSupportedHabitatLivingArrivalActionCore(arrivalAction, out string ignoredFailureReason))
            {
                return UnsupportedArrivalFailureKey;
            }

            return InvalidStateFailureKey;
        }

        internal static string GetHabitatLivingReadinessIssueKeyCore(string failureReason)
        {
            return IsJoyTransferFailure(failureReason)
                ? JoyOccupiedIssueKey
                : InvalidStateIssueKey;
        }

        internal static bool IsSupportedHabitatLivingArrivalActionCore(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            failureReason = null;
            if (arrivalAction == null)
            {
                failureReason = UnsupportedArrivalFailureKey.Translate().ToString();
                return false;
            }

            if (arrivalAction is TransportersArrivalAction_FormCaravan)
            {
                failureReason = UnsupportedArrivalFailureKey.Translate().ToString();
                return false;
            }

            if (ShuttleLaunchArrivalReflectionUtility.IsModularShuttleSpecificCellArrival(arrivalAction))
            {
                return true;
            }

            TransportersArrivalAction_LandInSpecificCell landInSpecificCell =
                arrivalAction as TransportersArrivalAction_LandInSpecificCell;
            if (landInSpecificCell != null)
            {
                if (ShuttleLaunchArrivalReflectionUtility.TryReadsLandInShuttle(landInSpecificCell))
                {
                    return true;
                }

                failureReason = UnsupportedArrivalFailureKey.Translate().ToString();
                return false;
            }

            TransportersArrivalAction_VisitSite visitSite = arrivalAction as TransportersArrivalAction_VisitSite;
            if (visitSite != null)
            {
                if (ShuttleLaunchArrivalReflectionUtility.TryReadVisitSite(visitSite) != null)
                {
                    return true;
                }

                failureReason = UnsupportedArrivalFailureKey.Translate().ToString();
                return false;
            }

            failureReason = UnsupportedArrivalFailureKey.Translate().ToString();
            return false;
        }

        private static bool IsJoyTransferFailure(string failureReason)
        {
            return !failureReason.NullOrEmpty() &&
                failureReason.IndexOf("joy", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsUnsupportedArrivalFailure(string failureReason)
        {
            return !failureReason.NullOrEmpty() &&
                failureReason == UnsupportedArrivalFailureKey.Translate().ToString();
        }
    }
}
