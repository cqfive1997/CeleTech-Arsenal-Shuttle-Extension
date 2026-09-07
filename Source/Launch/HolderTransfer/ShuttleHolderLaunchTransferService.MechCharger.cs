using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static bool TryRestoreMechChargerBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            ThingOwner primarySource,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            return MechChargerHolderTransferHandler.TryRestoreBeforeIncomingImpact(
                shuttleHost,
                primarySource,
                incomingSkyfallerContainer,
                map,
                fallbackCell,
                out failureReason,
                out failureStatus);
        }

        internal static bool NeedsMechChargerLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsMechChargerLaunchTransfer(shuttleHost);
        }

        internal static bool CanUseMechChargerLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransfer(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransferWithHabitatConcurrency(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithMedicalConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransferWithMedicalConcurrency(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferCore(
            ThingWithComps shuttleHost,
            bool allowHabitatOccupants,
            bool allowMedicalPatients,
            bool allowActiveHabitatManifest,
            bool allowActiveMedicalBayManifest,
            bool allowActiveHabitatAndMedicalBayManifest,
            out string failureReason)
        {
            return MechChargerHolderTransferHandler.CanUseMechChargerLaunchTransferCore(
                shuttleHost,
                allowHabitatOccupants,
                allowMedicalPatients,
                allowActiveHabitatManifest,
                allowActiveMedicalBayManifest,
                allowActiveHabitatAndMedicalBayManifest,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransferWithHabitatConcurrency(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithMedicalConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransferWithMedicalConcurrency(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool TryExportMechChargerToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportMechChargerToLaunchHandoff(
                shuttleHost,
                handoffs,
                false,
                false,
                false,
                out failureReason);
        }

        internal static bool TryExportMechChargerToLaunchHandoffWithHabitatTransaction(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportMechChargerToLaunchHandoff(
                shuttleHost,
                handoffs,
                true,
                false,
                false,
                out failureReason);
        }

        internal static bool TryExportMechChargerToLaunchHandoffWithMedicalTransaction(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportMechChargerToLaunchHandoff(
                shuttleHost,
                handoffs,
                false,
                true,
                false,
                out failureReason);
        }

        internal static bool TryExportMechChargerToLaunchHandoffWithHabitatAndMedicalTransaction(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportMechChargerToLaunchHandoff(
                shuttleHost,
                handoffs,
                true,
                true,
                true,
                out failureReason);
        }

        internal static bool TryExportMechChargerToLaunchHandoffCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            bool allowHabitatTransactionManifest,
            bool allowMedicalBayTransactionManifest,
            bool allowHabitatAndMedicalBayTransactionManifest,
            out string failureReason)
        {
            return MechChargerHolderTransferHandler.TryExportToLaunchHandoff(
                shuttleHost,
                handoffs,
                allowHabitatTransactionManifest,
                allowMedicalBayTransactionManifest,
                allowHabitatAndMedicalBayTransactionManifest,
                out failureReason);
        }

        internal static bool TryRollbackMechChargerLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackMechChargerLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackMechChargerLaunchExportCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return MechChargerHolderTransferHandler.TryRollbackLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackMechChargerLaunchExportWithRecovery(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool mechsQuarantined,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackMechChargerLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out mechsQuarantined,
                out notice);
        }

        internal static bool TryRollbackMechChargerLaunchExportWithRecoveryCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool mechsQuarantined,
            out string notice)
        {
            return MechChargerHolderTransferHandler.TryRollbackLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out mechsQuarantined,
                out notice);
        }
    }
}
