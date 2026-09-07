using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food
{
    /// <summary>
    /// Food-specific adapter over the shared Cargo withdrawal transaction. It adds only the
    /// food-facing commit/rollback names and diagnostics used by Habitat and Prison Cell.
    /// </summary>
    internal sealed class ShuttleCargoFoodWithdrawal
    {
        private readonly ShuttleCargoWithdrawal transaction;
        private readonly string reason;

        internal ShuttleCargoFoodWithdrawal(
            ShuttleCargoWithdrawal transaction,
            string reason)
        {
            this.transaction = transaction;
            this.reason = reason;
        }

        internal Thing Food
        {
            get { return this.transaction != null ? this.transaction.Thing : null; }
        }

        internal bool TryTransferTo(ThingOwner<Thing> destination, out string failureReason)
        {
            if (this.transaction == null)
            {
                failureReason = "Cargo food withdrawal transaction is unavailable.";
                return false;
            }

            return this.transaction.TryTransferTo(destination, out failureReason);
        }

        internal bool CommitTransferred()
        {
            return this.transaction != null && this.transaction.CommitTransferred();
        }

        internal bool CommitConsumed()
        {
            return this.transaction != null && this.transaction.CommitConsumed();
        }

        internal bool RollBack()
        {
            if (this.transaction == null)
            {
                return false;
            }

            string rollbackFailure;
            if (this.transaction.TryRollBackToSource(out rollbackFailure))
            {
                return true;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string recoveryFailure;
            if (this.transaction.TryRecoverThroughTransferState(
                null,
                null,
                out recoveryStatus,
                out recoveryFailure))
            {
                ShuttleLog.Error(
                    "CargoFood",
                    "Exact-source rollback failed; emergency recovery secured cargo food. status=" +
                        recoveryStatus + " reason=" + (this.reason ?? "null") +
                        " rollback=" + (rollbackFailure ?? "null") +
                        " recovery=" + (recoveryFailure ?? "null"));
                return true;
            }

            string dropFailure = null;
            Thing shuttleHost = this.transaction.ShuttleHost;
            if (shuttleHost != null &&
                this.transaction.TryRecoverNear(shuttleHost, out dropFailure))
            {
                ShuttleLog.Error(
                    "CargoFood",
                    "Cargo food rollback dropped the stack near the shuttle. reason=" +
                        (this.reason ?? "null"));
                return true;
            }

            ShuttleLog.Error(
                "CargoFood",
                "Cargo food rollback could not secure the withdrawn stack. reason=" +
                    (this.reason ?? "null") + " rollback=" +
                    (rollbackFailure ?? "null") + " recovery=" +
                    (recoveryFailure ?? "null") + " drop=" +
                    (dropFailure ?? "null"));
            return false;
        }

    }
}
