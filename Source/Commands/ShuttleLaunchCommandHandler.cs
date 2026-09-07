using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles launch commands by delegating targeting and execution to launch services.
    /// </summary>
    internal sealed class ShuttleLaunchCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            // CancelLaunchWarmupCommand is future-only; no current player UI constructs it.
            return command is BeginLaunchTargetingCommand ||
                   command is ConfirmLaunchCommand ||
                   command is CancelLaunchWarmupCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            BeginLaunchTargetingCommand beginLaunchTargeting = command as BeginLaunchTargetingCommand;
            if (beginLaunchTargeting != null)
            {
                return this.ExecuteBeginLaunchTargeting(context, beginLaunchTargeting);
            }

            ConfirmLaunchCommand confirmLaunch = command as ConfirmLaunchCommand;
            if (confirmLaunch != null)
            {
                return this.ExecuteConfirmLaunch(context, confirmLaunch);
            }

            CancelLaunchWarmupCommand cancelLaunchWarmup = command as CancelLaunchWarmupCommand;
            if (cancelLaunchWarmup != null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_LaunchWarmupCancelNotImplemented".Translate().ToString());
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedLaunch".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteBeginLaunchTargeting(ShuttleCommandContext context, BeginLaunchTargetingCommand command)
        {
            if (context.WorldTargetingService == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_LaunchTargetingServiceUnavailable".Translate().ToString());
            }

            ShuttleLaunchTargetingContext targetingContext = context.BuildLaunchTargetingContext();
            if (targetingContext == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_LaunchTargetingContextUnavailable".Translate().ToString());
            }

            ShuttleLaunchResult result = context.WorldTargetingService.BeginTargeting(targetingContext);
            return result.Success
                ? ShuttleCommandResult.Succeeded(result.Message)
                : ShuttleCommandResult.Failed(result.Message);
        }

        private ShuttleCommandResult ExecuteConfirmLaunch(ShuttleCommandContext context, ConfirmLaunchCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ConfirmLaunchMissing".Translate().ToString());
            }

            if (context.ElectricLaunchService == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_LaunchServiceUnavailable".Translate().ToString());
            }

            string processingPauseFailure = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TryPauseAutoWorkTablesForLaunch(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    out processingPauseFailure))
            {
                return ShuttleCommandResult.Failed(
                    processingPauseFailure ??
                        "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleLaunchExecutionInput launchInput = context.BuildLaunchExecutionInput(
                command.DestinationTile,
                command.ArrivalAction);
            if (launchInput == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_LaunchInputUnavailable".Translate().ToString());
            }

            ShuttleCommandResult turretIdleReturnResult =
                this.TryQueueLaunchAfterTurretIdleReturn(context, command);
            if (turretIdleReturnResult != null)
            {
                return turretIdleReturnResult;
            }

            ShuttleLaunchResult result = context.ElectricLaunchService.ExecuteLaunch(launchInput);
            return result.Success
                ? ShuttleCommandResult.Succeeded(result.Message)
                : ShuttleCommandResult.Failed(result.Message);
        }

        private ShuttleCommandResult TryQueueLaunchAfterTurretIdleReturn(
            ShuttleCommandContext context,
            ConfirmLaunchCommand command)
        {
            if (context == null || context.Host == null || command == null)
            {
                return null;
            }

            CompModularShuttleWeaponTurretVisual turretVisual =
                context.Host.TryGetComp<CompModularShuttleWeaponTurretVisual>();
            if (turretVisual == null)
            {
                return null;
            }

            string message;
            if (!turretVisual.TryQueueLaunchAfterIdle(
                context.CommandExecutor,
                command.DestinationTile,
                command.ArrivalAction,
                out message))
            {
                return null;
            }

            return ShuttleCommandResult.Succeeded(message);
        }
    }
}
