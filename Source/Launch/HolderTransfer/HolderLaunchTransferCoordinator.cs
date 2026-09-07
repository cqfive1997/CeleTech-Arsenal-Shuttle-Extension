using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    // Phase 6 low-risk holder transfer dispatch seam. The coordinator only
    // dispatches by holder kind and operation; handlers are currently thin
    // wrappers, and complex transactions remain in ShuttleHolderLaunchTransferService
    // Core methods. Future migrations should move one holder and one transaction
    // path at a time.
    internal static class HolderLaunchTransferCoordinator
    {
        internal static bool TryExportHabitatLivingToLaunchHandoff(
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
            return HabitatHolderTransferHandler.TryRollbackMixedLaunchExport(
                shuttleHost,
                handoffs,
                out notice);
        }

        internal static bool TryExportMedicalBayPatientsToLaunchHandoff(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return MedicalBayHolderTransferHandler.TryExportPatientsToLaunchHandoff(
                shuttleHost,
                handoffs,
                false,
                out failureReason);
        }

        internal static bool TryExportMedicalBayPatientsToLaunchHandoffWithHabitatTransaction(
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return MedicalBayHolderTransferHandler.TryExportPatientsToLaunchHandoff(
                shuttleHost,
                handoffs,
                true,
                out failureReason);
        }

        internal static bool TryRollbackMedicalBayPatientLaunchExport(
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
            return MedicalBayHolderTransferHandler.TryRollbackPatientLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out pawnsQuarantined,
                out notice);
        }

        internal static bool TryExportMechChargerToLaunchHandoff(
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
            return MechChargerHolderTransferHandler.TryRollbackLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out mechsQuarantined,
                out notice);
        }

        internal static bool TryExportPrisonCellPrisonersToLaunchHandoff(
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
            return PrisonCellHolderTransferHandler.TryRollbackPrisonerLaunchExportWithRecovery(
                shuttleHost,
                handoffs,
                out rollbackSucceeded,
                out prisonersQuarantined,
                out notice);
        }
    }
}
