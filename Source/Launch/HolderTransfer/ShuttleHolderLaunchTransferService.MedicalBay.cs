using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static partial class ShuttleHolderLaunchTransferService
    {
        internal static bool NeedsMedicalBayPatientLaunchTransfer(ThingWithComps shuttleHost)
        {
            return HolderTransferPreflightService.NeedsMedicalBayPatientLaunchTransfer(shuttleHost);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransfer(
            ThingWithComps shuttleHost,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMedicalBayPatientLaunchTransfer(
                shuttleHost,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferCore(
            ThingWithComps shuttleHost,
            bool allowHabitatOccupants,
            bool allowMechChargerOccupants,
            bool allowActiveHabitatManifest,
            out string failureReason)
        {
            return MedicalBayHolderTransferHandler.CanUseMedicalBayPatientLaunchTransferCore(
                shuttleHost,
                allowHabitatOccupants,
                allowMechChargerOccupants,
                allowActiveHabitatManifest,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransfer(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMedicalBayPatientLaunchTransfer(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
            ThingWithComps shuttleHost,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            return HolderTransferPreflightService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                shuttleHost,
                arrivalAction,
                out failureReason);
        }

        internal static bool TryExportMedicalBayPatientsToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportMedicalBayPatientsToLaunchHandoff(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportMedicalBayPatientsToLaunchHandoffWithHabitatTransaction(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return HolderLaunchTransferCoordinator.TryExportMedicalBayPatientsToLaunchHandoffWithHabitatTransaction(
                shuttleHost,
                handoffs,
                out failureReason);
        }

        internal static bool TryExportMedicalBayPatientsToLaunchHandoffCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            bool allowHabitatTransactionManifest,
            out string failureReason)
        {
            return MedicalBayHolderTransferHandler.TryExportPatientsToLaunchHandoff(
                shuttleHost,
                handoffs,
                allowHabitatTransactionManifest,
                out failureReason);
        }

        internal static bool TryRestoreMedicalBayPatientsBeforeImpact(
            ThingWithComps shuttleHost,
            ThingOwner primarySource,
            ThingOwner incomingSkyfallerContainer,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            return MedicalBayHolderTransferHandler.TryRestorePatientsBeforeImpact(
                shuttleHost,
                primarySource,
                incomingSkyfallerContainer,
                out failureReason,
                out failureStatus);
        }

        internal static bool TryRollbackMedicalBayPatientLaunchExport(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackMedicalBayPatientLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackMedicalBayPatientLaunchExportCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string notice)
        {
            return MedicalBayHolderTransferHandler.TryRollbackPatientLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryRollbackMedicalBayPatientLaunchExportWithRecovery(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool pawnsQuarantined,
            out string notice)
        {
            return HolderLaunchTransferCoordinator.TryRollbackMedicalBayPatientLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out pawnsQuarantined,
                out notice);
        }

        internal static bool TryRollbackMedicalBayPatientLaunchExportWithRecoveryCore(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out bool rollbackSucceeded,
            out bool pawnsQuarantined,
            out string notice)
        {
            return MedicalBayHolderTransferHandler.TryRollbackPatientLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out pawnsQuarantined,
                out notice);
        }
    }
}
