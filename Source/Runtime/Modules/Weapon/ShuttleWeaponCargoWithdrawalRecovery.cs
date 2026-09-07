using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Applies the existing cargo recovery chain to one short-lived weapon-ammo withdrawal.
    /// It chooses recovery mechanics only; magazine rollback remains the caller's responsibility.
    /// </summary>
    internal static class ShuttleWeaponCargoWithdrawalRecovery
    {
        internal static bool TryRecover(
            ShuttleCargoWithdrawal withdrawal,
            Thing anchor,
            out string recoveryMode,
            out string failure)
        {
            recoveryMode = null;
            failure = null;
            if (withdrawal == null)
            {
                failure = "withdrawal-missing";
                return false;
            }

            string sourceFailure;
            if (withdrawal.TryRollBackToSource(out sourceFailure))
            {
                recoveryMode = "source";
                return true;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string transferFailure;
            if (withdrawal.TryRecoverThroughTransferState(
                null,
                null,
                out recoveryStatus,
                out transferFailure))
            {
                recoveryMode = "transfer-state-" + recoveryStatus;
                return true;
            }

            string placementFailure;
            if (withdrawal.TryRecoverNear(anchor, out placementFailure))
            {
                recoveryMode = "placed-near-host";
                return true;
            }

            failure = "source=" + (sourceFailure ?? "null") +
                "; transfer=" + (transferFailure ?? "null") +
                "; placement=" + (placementFailure ?? "null");
            return false;
        }
    }
}
