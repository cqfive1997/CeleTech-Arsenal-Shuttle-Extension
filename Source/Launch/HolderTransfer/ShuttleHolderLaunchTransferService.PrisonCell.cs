using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static bool TryRestorePrisonCellPrisonersBeforeImpact(
            ThingWithComps shuttleHost,
            ThingOwner primarySource,
            ThingOwner incomingSkyfallerContainer,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            return PrisonCellHolderTransferHandler.TryRestorePrisonersBeforeImpact(
                shuttleHost,
                primarySource,
                incomingSkyfallerContainer,
                out failureReason,
                out failureStatus);
        }

        internal static bool NeedsPrisonCellPrisonerLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsPrisonCellPrisonerLaunchTransfer(shuttleHost);
        }

        internal static bool CanUsePrisonCellPrisonerLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUsePrisonCellPrisonerLaunchTransfer(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUsePrisonCellPrisonerLaunchTransferCore(
            ThingWithComps shuttleHost,
            bool allowActiveKnownTransactionManifest,
            out string failureReason)
        {
            return PrisonCellHolderTransferHandler.CanUsePrisonCellPrisonerLaunchTransferCore(
                shuttleHost,
                allowActiveKnownTransactionManifest,
                out failureReason);
        }

        internal static bool CanUsePrisonCellPrisonerLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUsePrisonCellPrisonerLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool TryExportPrisonCellPrisonersToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportPrisonCellPrisonersToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportPrisonCellPrisonersToLaunchHandoffCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return PrisonCellHolderTransferHandler.TryExportPrisonersToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryRollbackPrisonCellPrisonerLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackPrisonCellPrisonerLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackPrisonCellPrisonerLaunchExportCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return PrisonCellHolderTransferHandler.TryRollbackPrisonerLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackPrisonCellPrisonerLaunchExportWithRecovery(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool prisonersQuarantined,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackPrisonCellPrisonerLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out prisonersQuarantined,
                out notice);
        }

        internal static bool TryRollbackPrisonCellPrisonerLaunchExportWithRecoveryCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool prisonersQuarantined,
            out string notice)
        {
            return PrisonCellHolderTransferHandler.TryRollbackPrisonerLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out prisonersQuarantined,
                out notice);
        }
    }
}
