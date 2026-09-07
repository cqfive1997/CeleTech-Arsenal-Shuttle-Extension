using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoConsumeExecutor
    {
        internal bool TryConsume(
            ShuttleController controller,
            ExternalSDKCargoConsumePlan plan,
            out int affectedCount,
            out int affectedStackCount,
            out float affectedMassKg,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            affectedCount = 0;
            affectedStackCount = 0;
            affectedMassKg = 0f;
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (controller == null || plan == null || plan.Candidates == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "consume plan is unavailable";
                return false;
            }

            if (plan.AffectedCount <= 0 || plan.Candidates.Count == 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InsufficientItems;
                message = "consume plan has no affected cargo";
                return false;
            }

            IShuttleCargoResourceBroker cargoBroker =
                controller.GetCargoResourceBrokerForInternalTransactions();
            if (cargoBroker == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoUnavailable;
                message = "cargo transaction service is unavailable";
                return false;
            }

            List<ShuttleCargoWithdrawal> withdrawals =
                new List<ShuttleCargoWithdrawal>();
            for (int i = 0; i < plan.Candidates.Count; i++)
            {
                ExternalSDKCargoConsumeCandidate candidate = plan.Candidates[i];
                if (candidate == null ||
                    candidate.StackRef == null ||
                    candidate.TakeCount <= 0)
                {
                    return this.FailAndRollBack(
                        controller,
                        withdrawals,
                        "consume candidate became unavailable",
                        out affectedCount,
                        out failureReason,
                        out message);
                }

                ShuttleCargoWithdrawal withdrawal;
                string takeFailure;
                if (!cargoBroker.TryBeginExactWithdrawal(
                        candidate.StackRef,
                        candidate.TakeCount,
                        ShuttleCargoAccessRequirement.None,
                        "external SDK cargo consume",
                        out withdrawal,
                        out takeFailure))
                {
                    return this.FailAndRollBack(
                        controller,
                        withdrawals,
                        "failed to take matching cargo: " + (takeFailure ?? "null"),
                        out affectedCount,
                        out failureReason,
                        out message);
                }

                withdrawals.Add(withdrawal);
                affectedCount += withdrawal != null && withdrawal.Thing != null
                    ? withdrawal.Thing.stackCount
                    : 0;
            }

            if (affectedCount != plan.AffectedCount)
            {
                return this.FailAndRollBack(
                    controller,
                    withdrawals,
                    "taken count did not match consume plan",
                    out affectedCount,
                    out failureReason,
                    out message);
            }

            for (int i = 0; i < withdrawals.Count; i++)
            {
                ShuttleCargoWithdrawal withdrawal = withdrawals[i];
                if (withdrawal == null || !withdrawal.CommitConsumed())
                {
                    failureReason = ShuttleExternalCargoTransactionFailureReason.PartialApplyRejected;
                    message = "cargo consume commit failed after exact withdrawal";
                    return false;
                }
            }

            affectedStackCount = withdrawals.Count;
            affectedMassKg = plan.AffectedMassKg;
            return true;
        }

        private bool FailAndRollBack(
            ShuttleController controller,
            List<ShuttleCargoWithdrawal> withdrawals,
            string failureMessage,
            out int affectedCount,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            affectedCount = 0;
            string rollbackNotice;
            if (!this.RollBackWithdrawals(
                    controller,
                    withdrawals,
                    out rollbackNotice))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.RollbackFailed;
                message = failureMessage + "; rollback failed: " +
                    (rollbackNotice ?? "null");
                return false;
            }

            failureReason = ShuttleExternalCargoTransactionFailureReason.PartialApplyRejected;
            message = failureMessage;
            if (!string.IsNullOrEmpty(rollbackNotice))
            {
                message += "; " + rollbackNotice;
            }

            return false;
        }

        private bool RollBackWithdrawals(
            ShuttleController controller,
            List<ShuttleCargoWithdrawal> withdrawals,
            out string recoveryNotice)
        {
            recoveryNotice = null;
            bool success = true;
            for (int i = withdrawals != null ? withdrawals.Count - 1 : -1;
                i >= 0;
                i--)
            {
                ShuttleCargoWithdrawal withdrawal = withdrawals[i];
                if (withdrawal == null || withdrawal.IsCompleted)
                {
                    continue;
                }

                string rollbackFailure;
                if (withdrawal.TryRollBackToSource(out rollbackFailure))
                {
                    recoveryNotice = AppendNote(
                        recoveryNotice,
                        "returned an exact withdrawal to its source");
                    continue;
                }

                ShuttleTransferRecoveryStatus recoveryStatus;
                string transferRecoveryFailure;
                if (withdrawal.TryRecoverThroughTransferState(
                        null,
                        null,
                        out recoveryStatus,
                        out transferRecoveryFailure))
                {
                    recoveryNotice = AppendNote(
                        recoveryNotice,
                        "recovered a withdrawal through transfer state: " +
                            recoveryStatus);
                    continue;
                }

                string placementFailure;
                if (withdrawal.TryRecoverNear(
                        controller != null ? controller.ShuttleHost : null,
                        out placementFailure))
                {
                    recoveryNotice = AppendNote(
                        recoveryNotice,
                        "placed a withdrawal near the shuttle");
                    continue;
                }

                success = false;
                recoveryNotice = AppendNote(
                    recoveryNotice,
                    "rollback=" + (rollbackFailure ?? "null") +
                    ", transferRecovery=" + (transferRecoveryFailure ?? "null") +
                    ", placement=" + (placementFailure ?? "null"));
            }

            return success;
        }

        private static string AppendNote(string existing, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return existing;
            }

            return string.IsNullOrEmpty(existing)
                ? note
                : existing + "; " + note;
        }
    }
}
