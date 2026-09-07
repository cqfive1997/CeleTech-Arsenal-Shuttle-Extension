using CombatExtended;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    /// <summary>
    /// Converts one explicit DevMode request into a synchronous main-cargo/CE-magazine transaction.
    /// It persists no grant and performs no work from the ordinary probe tick.
    /// </summary>
    internal static class CeRuntimeProbeCargoReloadAdapter
    {
        internal static bool TryRun(
            CeRuntimeProbeReloadSession session,
            bool injectRollback,
            out CeRuntimeProbeReloadOperation operation,
            out string failure)
        {
            operation = null;
            if (!CeRuntimeProbeReloadSessionRunner.IsReadyForAction(session, out failure))
            {
                return false;
            }

            if (session.StagedCount > 0 || session.StagedAmmo != null)
            {
                failure = "Cancel or commit the detached reload grant before testing real cargo.";
                return false;
            }

            CompAmmoUser ammo = session.GetAmmo();
            AmmoDef selectedAmmo = ammo != null ? ammo.SelectedAmmo : null;
            int missing = ammo != null ? ammo.MagSize - ammo.CurMagCount : 0;
            if (ammo == null || selectedAmmo == null || selectedAmmo.ammoCount != 1 || missing <= 0)
            {
                failure = "The active CE reload probe has no compatible missing magazine count.";
                return false;
            }

            CeRuntimeProbeCargoMagazineMutation mutation =
                new CeRuntimeProbeCargoMagazineMutation(
                    session,
                    selectedAmmo,
                    injectRollback);
            ShuttleWeaponCargoCommitResult cargoResult;
            string transactionFailure;
            bool committed = ShuttleWeaponCargoCommitIntegrationPort.TryCommit(
                session.Host,
                selectedAmmo.defName,
                missing,
                mutation.TryApply,
                mutation.TryRollback,
                out cargoResult,
                out transactionFailure);

            operation = new CeRuntimeProbeReloadOperation
            {
                RequestedCount = missing,
                OfferedCount = cargoResult != null ? cargoResult.OfferedCount : 0,
                LoadedBefore = mutation.LoadedBefore,
                LoadedAfter = ammo.CurMagCount,
                CommittedCount = mutation.CommittedCount,
                RolledBackCount = cargoResult != null && cargoResult.CargoRolledBack
                    ? cargoResult.OfferedCount
                    : 0,
                MagazineMutationRolledBack = mutation.Restored,
                SourceModel = "real-shuttle-cargo",
                CargoStoredBefore = cargoResult != null ? cargoResult.StoredBefore : -1,
                CargoStoredAfter = cargoResult != null ? cargoResult.StoredAfter : -1,
                CargoWithdrawn = cargoResult != null ? cargoResult.OfferedCount : 0,
                CargoSourceThingID = cargoResult != null ? cargoResult.SourceThingID : -1,
                CargoSourceStackBefore = cargoResult != null
                    ? cargoResult.SourceStackBefore
                    : -1,
                CargoSourceStackAfterWithdrawal = cargoResult != null
                    ? cargoResult.SourceStackAfterWithdrawal
                    : -1,
                CargoConsumed = cargoResult != null && cargoResult.CargoConsumed,
                CargoRolledBack = cargoResult != null && cargoResult.CargoRolledBack,
                CargoRecoveryMode = cargoResult != null
                    ? cargoResult.CargoRecoveryMode
                    : null,
                MagazineCommitAccepted = cargoResult != null &&
                    cargoResult.MagazineCommitAccepted,
                MagazineRollbackRequested = cargoResult != null &&
                    cargoResult.MagazineRollbackRequested,
                MagazineRollbackAccepted = cargoResult != null &&
                    cargoResult.MagazineRollbackAccepted
            };

            if (injectRollback)
            {
                if (committed ||
                    cargoResult == null ||
                    !cargoResult.CargoRolledBack ||
                    !cargoResult.MagazineRollbackAccepted ||
                    cargoResult.StoredAfter != cargoResult.StoredBefore ||
                    mutation.LoadedAfter != mutation.LoadedBefore ||
                    !mutation.Restored)
                {
                    failure = BuildFailure(
                        "The injected cargo/CE rollback did not restore both authorities.",
                        transactionFailure,
                        mutation.Failure,
                        selectedAmmo,
                        ammo);
                    return false;
                }

                session.CaptureExpectedState();
                failure = null;
                return true;
            }

            if (!committed || cargoResult == null || !cargoResult.CargoConsumed)
            {
                failure = BuildFailure(
                    "The real cargo-to-CE commit failed.",
                    transactionFailure,
                    mutation.Failure,
                    selectedAmmo,
                    ammo);
                return false;
            }

            int committedCount = cargoResult.OfferedCount;
            if (committedCount <= 0 ||
                ammo.CurMagCount - mutation.LoadedBefore != committedCount ||
                cargoResult.StoredBefore - cargoResult.StoredAfter != committedCount)
            {
                failure = "The real cargo and CE magazine deltas did not match exactly.";
                return false;
            }

            session.CaptureExpectedState();
            failure = null;
            return true;
        }

        private static string BuildFailure(
            string prefix,
            string transactionFailure,
            string mutationFailure,
            AmmoDef selectedAmmo = null,
            CompAmmoUser ammo = null)
        {
            return prefix + " transaction=" +
                (transactionFailure ?? "null") +
                " mutation=" + (mutationFailure ?? "null") +
                " requiredCargoThingDef=" +
                (selectedAmmo != null ? selectedAmmo.defName : "null") +
                " selectedAmmoSet=" +
                (ammo != null && ammo.CurAmmoSet != null
                    ? ammo.CurAmmoSet.defName
                    : "null");
        }
    }
}
