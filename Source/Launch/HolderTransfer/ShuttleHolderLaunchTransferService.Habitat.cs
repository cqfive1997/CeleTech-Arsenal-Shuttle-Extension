using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static bool NeedsHabitatMixedLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsHabitatMixedLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsHabitatJoyLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsHabitatJoyLaunchTransfer(shuttleHost);
        }

        internal static bool NeedsAnyHabitatLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsAnyHabitatLaunchTransfer(shuttleHost);
        }

        internal static bool TryExportHabitatLivingToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportHabitatLivingToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportHabitatLivingToLaunchHandoffCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.TryExportLivingToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportHabitatJoyToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportHabitatJoyToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportHabitatJoyToLaunchHandoffCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.TryExportJoyToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportHabitatMixedToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportHabitatMixedToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportHabitatMixedToLaunchHandoffCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HabitatHolderTransferHandler.TryExportMixedToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryRollbackHabitatLivingLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackHabitatLivingLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackHabitatLivingLaunchExportCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HabitatHolderTransferHandler.TryRollbackLivingLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackHabitatJoyLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackHabitatJoyLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackHabitatJoyLaunchExportCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HabitatHolderTransferHandler.TryRollbackJoyLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackHabitatMixedLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackHabitatMixedLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackHabitatMixedLaunchExportCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HabitatHolderTransferHandler.TryRollbackMixedLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        private static bool TryRestoreHabitatBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            CompShuttleHolderLaunchTransferState state,
            ThingOwner cargoStaging,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out HabitatIncomingRestoreResult result)
        {
            return HabitatHolderTransferHandler.TryRestoreBeforeIncomingImpact(
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
