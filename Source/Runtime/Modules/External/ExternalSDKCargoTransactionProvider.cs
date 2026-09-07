using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionProvider :
        IShuttleExternalCargoTransactionProvider
    {
        private readonly ShuttleController controller;
        private readonly ExternalSDKCargoTransactionRequestValidator validator =
            new ExternalSDKCargoTransactionRequestValidator();
        private readonly ExternalSDKCargoTransactionStateGate stateGate =
            new ExternalSDKCargoTransactionStateGate();
        private readonly ExternalSDKCargoConsumePlanner planner =
            new ExternalSDKCargoConsumePlanner();
        private readonly ExternalSDKCargoConsumeExecutor executor =
            new ExternalSDKCargoConsumeExecutor();
        private readonly ExternalSDKCargoDepositRequestValidator depositValidator =
            new ExternalSDKCargoDepositRequestValidator();
        private readonly ExternalSDKCargoDepositPlanner depositPlanner =
            new ExternalSDKCargoDepositPlanner();
        private readonly ExternalSDKCargoDepositExecutor depositExecutor =
            new ExternalSDKCargoDepositExecutor();
        private readonly ExternalSDKCargoTransactionDiagnostics diagnostics =
            new ExternalSDKCargoTransactionDiagnostics();
        private readonly ExternalSDKCargoDepositDiagnostics depositDiagnostics =
            new ExternalSDKCargoDepositDiagnostics();

        internal ExternalSDKCargoTransactionProvider(ShuttleController controller)
        {
            this.controller = controller;
        }

        public bool TryQuoteConsume(
            ShuttleExternalCargoConsumeRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            bool success = this.TryRunConsume(
                request,
                true,
                ShuttleExternalCargoTransactionKind.QuoteConsume,
                out result);
            this.RecordDiagnostics(result);
            return success;
        }

        public bool TryConsume(
            ShuttleExternalCargoConsumeRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            bool success = this.TryRunConsume(
                request,
                false,
                ShuttleExternalCargoTransactionKind.Consume,
                out result);
            this.RecordDiagnostics(result);
            return success;
        }

        public bool TryQuoteDeposit(
            ShuttleExternalCargoDepositRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            bool success = this.TryRunDeposit(
                request,
                true,
                ShuttleExternalCargoTransactionKind.QuoteDeposit,
                out result);
            this.RecordDiagnostics(result);
            return success;
        }

        public bool TryDeposit(
            ShuttleExternalCargoDepositRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            bool success = this.TryRunDeposit(
                request,
                false,
                ShuttleExternalCargoTransactionKind.Deposit,
                out result);
            this.RecordDiagnostics(result);
            return success;
        }

        private bool TryRunConsume(
            ShuttleExternalCargoConsumeRequest request,
            bool dryRun,
            ShuttleExternalCargoTransactionKind kind,
            out ShuttleExternalCargoTransactionResult result)
        {
            result = null;

            ExternalSDKCargoValidatedConsumeRequest validated;
            ShuttleExternalCargoTransactionFailureReason failureReason;
            string message;
            ExternalSDKCargoConsumePlan plan = null;

            try
            {
                if (!this.validator.TryValidateConsumeRequest(
                        request,
                        out validated,
                        out failureReason,
                        out message))
                {
                    result = this.diagnostics.BuildFailureFromRequest(
                        request,
                        kind,
                        dryRun,
                        true,
                        failureReason,
                        message);
                    return false;
                }

                if (!this.stateGate.TryPass(
                        this.controller,
                        out failureReason,
                        out message))
                {
                    result = this.diagnostics.BuildFailure(
                        validated,
                        null,
                        kind,
                        dryRun,
                        false,
                        failureReason,
                        message);
                    return false;
                }

                if (!this.planner.TryBuildPlan(
                        this.controller,
                        validated,
                        out plan,
                        out failureReason,
                        out message))
                {
                    result = this.diagnostics.BuildFailure(
                        validated,
                        plan,
                        kind,
                        dryRun,
                        this.IsCargoSurfaceAvailable(failureReason),
                        failureReason,
                        message);
                    return false;
                }

                if (dryRun)
                {
                    result = this.diagnostics.BuildSuccess(
                        validated,
                        plan,
                        kind,
                        true,
                        plan.AffectedCount,
                        plan.AffectedStackCount,
                        plan.AffectedMassKg,
                        "consume quote succeeded");
                    return true;
                }

                int affectedCount;
                int affectedStackCount;
                float affectedMassKg;
                if (!this.executor.TryConsume(
                        this.controller,
                        plan,
                        out affectedCount,
                        out affectedStackCount,
                        out affectedMassKg,
                        out failureReason,
                        out message))
                {
                    if (failureReason == ShuttleExternalCargoTransactionFailureReason.RollbackFailed ||
                        failureReason == ShuttleExternalCargoTransactionFailureReason.PartialApplyRejected ||
                        failureReason == ShuttleExternalCargoTransactionFailureReason.InternalError)
                    {
                        this.diagnostics.LogInternalFailure(validated, kind, message);
                    }

                    result = this.diagnostics.BuildFailure(
                        validated,
                        plan,
                        kind,
                        false,
                        true,
                        failureReason,
                        message);
                    return false;
                }

                result = this.diagnostics.BuildSuccess(
                    validated,
                    plan,
                    kind,
                    false,
                    affectedCount,
                    affectedStackCount,
                    affectedMassKg,
                    "consume succeeded");
                return true;
            }
            catch (Exception exception)
            {
                result = this.diagnostics.BuildFailureFromRequest(
                    request,
                    kind,
                    dryRun,
                    false,
                    ShuttleExternalCargoTransactionFailureReason.InternalError,
                    "cargo transaction failed: " + exception.GetType().Name);
                return false;
            }
        }

        private bool TryRunDeposit(
            ShuttleExternalCargoDepositRequest request,
            bool dryRun,
            ShuttleExternalCargoTransactionKind kind,
            out ShuttleExternalCargoTransactionResult result)
        {
            result = null;

            ExternalSDKCargoValidatedDepositRequest validated;
            ShuttleExternalCargoTransactionFailureReason failureReason;
            string message;
            ExternalSDKCargoDepositPlan plan = null;

            try
            {
                if (!this.depositValidator.TryValidateDepositRequest(
                        request,
                        out validated,
                        out failureReason,
                        out message))
                {
                    result = this.depositDiagnostics.BuildFailureFromRequest(
                        request,
                        kind,
                        dryRun,
                        true,
                        failureReason,
                        message);
                    return false;
                }

                if (!this.stateGate.TryPass(
                        this.controller,
                        out failureReason,
                        out message))
                {
                    result = this.depositDiagnostics.BuildFailure(
                        validated,
                        null,
                        kind,
                        dryRun,
                        false,
                        failureReason,
                        message);
                    return false;
                }

                if (!this.depositPlanner.TryBuildPlan(
                        this.controller,
                        validated,
                        out plan,
                        out failureReason,
                        out message))
                {
                    result = this.depositDiagnostics.BuildFailure(
                        validated,
                        plan,
                        kind,
                        dryRun,
                        this.IsCargoSurfaceAvailable(failureReason),
                        failureReason,
                        message);
                    return false;
                }

                if (dryRun)
                {
                    result = this.depositDiagnostics.BuildSuccess(
                        validated,
                        plan,
                        kind,
                        true,
                        plan.AffectedCount,
                        plan.AffectedStackCount,
                        plan.AffectedMassKg,
                        "deposit quote succeeded");
                    return true;
                }

                int affectedCount;
                int affectedStackCount;
                float affectedMassKg;
                if (!this.depositExecutor.TryDeposit(
                        this.controller,
                        plan,
                        out affectedCount,
                        out affectedStackCount,
                        out affectedMassKg,
                        out failureReason,
                        out message))
                {
                    if (failureReason == ShuttleExternalCargoTransactionFailureReason.CleanupFailed ||
                        failureReason == ShuttleExternalCargoTransactionFailureReason.RollbackFailed ||
                        failureReason == ShuttleExternalCargoTransactionFailureReason.PartialApplyRejected ||
                        failureReason == ShuttleExternalCargoTransactionFailureReason.InternalError)
                    {
                        this.depositDiagnostics.LogInternalFailure(validated, kind, message);
                    }

                    result = this.depositDiagnostics.BuildFailure(
                        validated,
                        plan,
                        kind,
                        false,
                        true,
                        failureReason,
                        message);
                    return false;
                }

                result = this.depositDiagnostics.BuildSuccess(
                    validated,
                    plan,
                    kind,
                    false,
                    affectedCount,
                    affectedStackCount,
                    affectedMassKg,
                    "deposit succeeded");
                return true;
            }
            catch (Exception exception)
            {
                result = this.depositDiagnostics.BuildFailureFromRequest(
                    request,
                    kind,
                    dryRun,
                    false,
                    ShuttleExternalCargoTransactionFailureReason.InternalError,
                    "cargo deposit failed: " + exception.GetType().Name);
                return false;
            }
        }

        private bool IsCargoSurfaceAvailable(
            ShuttleExternalCargoTransactionFailureReason failureReason)
        {
            return failureReason != ShuttleExternalCargoTransactionFailureReason.CargoUnavailable &&
                failureReason != ShuttleExternalCargoTransactionFailureReason.HostUnavailable &&
                failureReason != ShuttleExternalCargoTransactionFailureReason.RuntimeBlocked &&
                failureReason != ShuttleExternalCargoTransactionFailureReason.LaunchTransferActive &&
                failureReason != ShuttleExternalCargoTransactionFailureReason.CargoTransferActive;
        }

        private void RecordDiagnostics(ShuttleExternalCargoTransactionResult result)
        {
            ExternalSDKCargoTransactionDiagnosticsRecorder.Instance.RecordSafe(
                this.controller,
                result);
        }
    }
}
