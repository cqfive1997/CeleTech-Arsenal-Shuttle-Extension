using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffProvider :
        IShuttleExternalLaunchOccupantHandoffProvider
    {
        private readonly ShuttleController controller;
        private readonly ExternalSDKLaunchOccupantHandoffRequestValidator validator =
            new ExternalSDKLaunchOccupantHandoffRequestValidator();
        private readonly ExternalSDKCargoTransactionStateGate stateGate =
            new ExternalSDKCargoTransactionStateGate();
        private readonly ExternalSDKLaunchOccupantHandoffPlanner planner =
            new ExternalSDKLaunchOccupantHandoffPlanner();
        private readonly ExternalSDKLaunchOccupantHandoffExecutor executor =
            new ExternalSDKLaunchOccupantHandoffExecutor();
        private readonly ExternalSDKLaunchOccupantHandoffResultFactory resultFactory =
            new ExternalSDKLaunchOccupantHandoffResultFactory();

        internal ExternalSDKLaunchOccupantHandoffProvider(ShuttleController controller)
        {
            this.controller = controller;
        }

        public bool TryQuoteHandoff(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            return this.TryRun(
                request,
                true,
                ShuttleExternalLaunchOccupantHandoffKind.Quote,
                out result);
        }

        public bool TryHandoff(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            return this.TryRun(
                request,
                false,
                ShuttleExternalLaunchOccupantHandoffKind.Handoff,
                out result);
        }

        private bool TryRun(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            bool dryRun,
            ShuttleExternalLaunchOccupantHandoffKind kind,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            result = null;
            ExternalSDKLaunchOccupantHandoffValidatedRequest validated;
            ExternalSDKLaunchOccupantHandoffPlan plan = null;
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason;
            string message;

            try
            {
                if (!this.validator.TryValidateRequest(
                        request,
                        out validated,
                        out failureReason,
                        out message))
                {
                    result = this.resultFactory.BuildFailureFromRequest(
                        request,
                        kind,
                        dryRun,
                        true,
                        failureReason,
                        message);
                    return false;
                }

                ShuttleExternalCargoTransactionFailureReason cargoFailureReason;
                if (!this.stateGate.TryPass(
                        this.controller,
                        out cargoFailureReason,
                        out message))
                {
                    failureReason = this.MapStateGateFailure(cargoFailureReason);
                    result = this.resultFactory.BuildFailure(
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
                    result = this.resultFactory.BuildFailure(
                        validated,
                        plan,
                        kind,
                        dryRun,
                        this.IsHandoffSurfaceAvailable(failureReason),
                        failureReason,
                        message);
                    return false;
                }

                if (dryRun)
                {
                    result = this.resultFactory.BuildSuccess(
                        validated,
                        plan,
                        kind,
                        true,
                        plan.AffectedMassKg,
                        "launch occupant handoff quote succeeded");
                    return true;
                }

                float affectedMassKg;
                if (!this.executor.TryHandoff(
                        this.controller,
                        plan,
                        out affectedMassKg,
                        out failureReason,
                        out message))
                {
                    result = this.resultFactory.BuildFailure(
                        validated,
                        plan,
                        kind,
                        false,
                        true,
                        failureReason,
                        message);
                    return false;
                }

                result = this.resultFactory.BuildSuccess(
                    validated,
                    plan,
                    kind,
                    false,
                    affectedMassKg,
                    "launch occupant handoff succeeded");
                return true;
            }
            catch (Exception exception)
            {
                result = this.resultFactory.BuildFailureFromRequest(
                    request,
                    kind,
                    dryRun,
                    false,
                    ShuttleExternalLaunchOccupantHandoffFailureReason.InternalError,
                    "launch occupant handoff failed: " + exception.GetType().Name);
                return false;
            }
        }

        private ShuttleExternalLaunchOccupantHandoffFailureReason MapStateGateFailure(
            ShuttleExternalCargoTransactionFailureReason failureReason)
        {
            switch (failureReason)
            {
                case ShuttleExternalCargoTransactionFailureReason.HostUnavailable:
                    return ShuttleExternalLaunchOccupantHandoffFailureReason.HostUnavailable;
                case ShuttleExternalCargoTransactionFailureReason.RuntimeBlocked:
                    return ShuttleExternalLaunchOccupantHandoffFailureReason.RuntimeBlocked;
                case ShuttleExternalCargoTransactionFailureReason.LaunchTransferActive:
                    return ShuttleExternalLaunchOccupantHandoffFailureReason.LaunchTransferActive;
                case ShuttleExternalCargoTransactionFailureReason.CargoTransferActive:
                    return ShuttleExternalLaunchOccupantHandoffFailureReason.CargoTransferActive;
                default:
                    return ShuttleExternalLaunchOccupantHandoffFailureReason.Unavailable;
            }
        }

        private bool IsHandoffSurfaceAvailable(
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason)
        {
            return failureReason != ShuttleExternalLaunchOccupantHandoffFailureReason.HostUnavailable &&
                failureReason != ShuttleExternalLaunchOccupantHandoffFailureReason.RuntimeBlocked &&
                failureReason != ShuttleExternalLaunchOccupantHandoffFailureReason.LaunchTransferActive &&
                failureReason != ShuttleExternalLaunchOccupantHandoffFailureReason.CargoTransferActive &&
                failureReason != ShuttleExternalLaunchOccupantHandoffFailureReason.CargoUnavailable &&
                failureReason != ShuttleExternalLaunchOccupantHandoffFailureReason.QueuedLoadActive;
        }
    }
}
