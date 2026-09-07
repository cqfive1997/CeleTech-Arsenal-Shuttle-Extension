using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class HabitatHolderTransferHandler
    {
        internal static bool CanUseHabitatJoyLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HabitatHolderPreflightHandler.CanUseHabitatJoyLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseAnySupportedHabitatHolderLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason,
            out string transferMode)
        {
            return HabitatHolderPreflightHandler.CanUseAnySupportedHabitatHolderLaunchTransferCore(
                shuttleHost,
                out failureReason,
                out transferMode);
        }

        internal static bool CanUseHabitatMixedLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HabitatHolderPreflightHandler.CanUseHabitatMixedLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseHabitatLivingLaunchTransferCore(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HabitatHolderPreflightHandler.CanUseHabitatLivingLaunchTransferCore(
                shuttleHost,
                out failureReason);
        }

        internal static string GetHabitatLivingLaunchFailureKeyCore(
            TransportersArrivalAction arrivalAction,
            string failureReason)
        {
            return HabitatHolderPreflightHandler.GetHabitatLivingLaunchFailureKeyCore(
                arrivalAction,
                failureReason);
        }

        internal static string GetHabitatLivingReadinessIssueKeyCore(string failureReason)
        {
            return HabitatHolderPreflightHandler.GetHabitatLivingReadinessIssueKeyCore(failureReason);
        }

        internal static bool IsSupportedHabitatLivingArrivalActionCore(
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HabitatHolderPreflightHandler.IsSupportedHabitatLivingArrivalActionCore(
                arrivalAction,
                out failureReason);
        }

        internal static bool TryExportLivingToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HabitatHolderExportHandler.TryExportLivingToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportJoyToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HabitatHolderExportHandler.TryExportJoyToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportMixedToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HabitatHolderExportHandler.TryExportMixedToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryRollbackLivingLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HabitatHolderRollbackHandler.TryRollbackLivingLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackJoyLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HabitatHolderRollbackHandler.TryRollbackJoyLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackMixedLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HabitatHolderRollbackHandler.TryRollbackMixedLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRestoreBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            ThingOwner cargoStaging,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out HabitatIncomingRestoreResult result)
        {
            return HabitatIncomingRestoreOuterHandler.TryRestoreBeforeIncomingImpact(
                shuttleHost,
                state,
                cargoStaging,
                incomingSkyfallerContainer,
                map,
                fallbackCell,
                out result);
        }
    }
}
