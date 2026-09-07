using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Synchronous transaction seam for backend-owned magazines. Cargo owns the real Thing
    /// withdrawal/recovery while the caller owns its magazine mutation and rollback.
    /// </summary>
    internal static class ShuttleWeaponCargoCommitIntegrationPort
    {
        internal static bool TryCommit(
            Thing host,
            string ammoDefName,
            int requestedCount,
            Func<int, bool> tryCommitMagazine,
            Func<bool> tryRollbackMagazine,
            out ShuttleWeaponCargoCommitResult result,
            out string failure)
        {
            result = new ShuttleWeaponCargoCommitResult
            {
                AmmoDefName = ammoDefName,
                RequestedCount = requestedCount
            };
            failure = null;

            ShuttleController controller;
            ThingWithComps typedHost;
            if (!TryResolveController(host, out typedHost, out controller, out failure) ||
                string.IsNullOrEmpty(ammoDefName) ||
                requestedCount <= 0 ||
                tryCommitMagazine == null ||
                tryRollbackMagazine == null)
            {
                if (string.IsNullOrEmpty(failure))
                {
                    failure = "cargo-commit-request-invalid";
                }

                return false;
            }

            IShuttleCargoResourceBroker broker =
                controller.GetCargoResourceBrokerForInternalTransactions();
            return TryCommit(
                typedHost,
                broker,
                ammoDefName,
                requestedCount,
                "CE weapon cargo reload probe",
                tryCommitMagazine,
                tryRollbackMagazine,
                out result,
                out failure);
        }

        internal static bool TryCommit(
            Thing host,
            IShuttleCargoResourceBroker broker,
            string ammoDefName,
            int requestedCount,
            string reason,
            Func<int, bool> tryCommitMagazine,
            Func<bool> tryRollbackMagazine,
            out ShuttleWeaponCargoCommitResult result,
            out string failure)
        {
            result = new ShuttleWeaponCargoCommitResult
            {
                AmmoDefName = ammoDefName,
                RequestedCount = requestedCount
            };
            failure = null;
            ThingWithComps typedHost = host as ThingWithComps;
            if (typedHost == null || !typedHost.Spawned || typedHost.Map == null ||
                broker == null ||
                string.IsNullOrEmpty(ammoDefName) ||
                requestedCount <= 0 ||
                tryCommitMagazine == null ||
                tryRollbackMagazine == null)
            {
                failure = "cargo-commit-request-invalid";
                return false;
            }

            ShuttleCargoInventorySnapshot before = broker != null
                ? broker.GetInventorySnapshot()
                : null;
            if (broker == null || before == null || !before.IsAvailable)
            {
                failure = "cargo-unavailable";
                return false;
            }

            result.StoredBefore = before.Count(ammoDefName);
            CargoStackRef stackRef = FindFirstAvailableStack(
                before.GetStackRefs(ammoDefName));
            if (stackRef == null)
            {
                result.StoredAfter = result.StoredBefore;
                failure = "compatible-cargo-ammo-missing";
                return false;
            }

            int offered = Math.Min(requestedCount, stackRef.Count);
            if (offered <= 0)
            {
                result.StoredAfter = result.StoredBefore;
                failure = "compatible-cargo-ammo-empty";
                return false;
            }

            result.OfferedCount = offered;
            result.SourceThingID = stackRef.ThingIDNumber;
            result.SourceStackBefore = stackRef.Count;

            ShuttleCargoWithdrawal withdrawal;
            if (!broker.TryBeginExactWithdrawal(
                stackRef,
                offered,
                ShuttleCargoAccessRequirement.ItemConsumption,
                !string.IsNullOrEmpty(reason) ? reason : "shuttle weapon reload",
                out withdrawal,
                out failure))
            {
                result.StoredAfter = ReadStoredCount(broker, ammoDefName);
                return false;
            }

            result.SourceStackAfterWithdrawal = withdrawal.CountOriginalSourceStack();

            bool commitAccepted = ShuttleWeaponCargoCommitCallbackInvoker.TryCommit(
                tryCommitMagazine,
                offered,
                out failure);
            result.MagazineCommitAccepted = commitAccepted;
            if (!commitAccepted)
            {
                return FailAfterWithdrawal(
                    typedHost,
                    broker,
                    withdrawal,
                    ammoDefName,
                    tryRollbackMagazine,
                    result,
                    failure ?? "magazine-commit-rejected",
                    out failure);
            }

            if (!withdrawal.CommitConsumed())
            {
                return FailAfterWithdrawal(
                    typedHost,
                    broker,
                    withdrawal,
                    ammoDefName,
                    tryRollbackMagazine,
                    result,
                    "cargo-consume-finalize-failed",
                    out failure);
            }

            result.CargoConsumed = true;
            result.StoredAfter = ReadStoredCount(broker, ammoDefName);
            if (result.StoredBefore - result.StoredAfter != offered)
            {
                failure = "cargo-count-delta-mismatch";
                return false;
            }

            return true;
        }

        private static bool FailAfterWithdrawal(
            ThingWithComps host,
            IShuttleCargoResourceBroker broker,
            ShuttleCargoWithdrawal withdrawal,
            string ammoDefName,
            Func<bool> tryRollbackMagazine,
            ShuttleWeaponCargoCommitResult result,
            string primaryFailure,
            out string failure)
        {
            result.MagazineRollbackRequested = true;
            string magazineRollbackFailure;
            result.MagazineRollbackAccepted = ShuttleWeaponCargoCommitCallbackInvoker.TryRollback(
                tryRollbackMagazine,
                out magazineRollbackFailure);

            string recoveryFailure;
            string recoveryMode;
            result.CargoRolledBack =
                ShuttleWeaponCargoWithdrawalRecovery.TryRecover(
                    withdrawal,
                    host,
                    out recoveryMode,
                    out recoveryFailure);
            result.CargoRecoveryMode = recoveryMode;
            result.StoredAfter = ReadStoredCount(broker, ammoDefName);

            failure = primaryFailure;
            if (!result.MagazineRollbackAccepted)
            {
                failure += "; magazine-rollback=" +
                    (magazineRollbackFailure ?? "failed");
            }

            if (!result.CargoRolledBack)
            {
                failure += "; cargo-recovery=" +
                    (recoveryFailure ?? "failed");
            }

            return false;
        }

        private static CargoStackRef FindFirstAvailableStack(
            IReadOnlyList<CargoStackRef> stackRefs)
        {
            if (stackRefs == null)
            {
                return null;
            }

            for (int i = 0; i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                if (stackRef != null && stackRef.Count > 0)
                {
                    return stackRef;
                }
            }

            return null;
        }

        private static int ReadStoredCount(
            IShuttleCargoResourceBroker broker,
            string ammoDefName)
        {
            ShuttleCargoInventorySnapshot snapshot = broker != null
                ? broker.GetInventorySnapshot()
                : null;
            return snapshot != null ? snapshot.Count(ammoDefName) : -1;
        }

        private static bool TryResolveController(
            Thing host,
            out ThingWithComps typedHost,
            out ShuttleController controller,
            out string failure)
        {
            typedHost = host as ThingWithComps;
            controller = null;
            failure = null;
            if (typedHost == null || !typedHost.Spawned || typedHost.Map == null)
            {
                failure = "host-not-spawned";
                return false;
            }

            CompModularShuttleCore core =
                typedHost.GetComp<CompModularShuttleCore>();
            controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                failure = "controller-unavailable";
                return false;
            }

            return true;
        }
    }
}
