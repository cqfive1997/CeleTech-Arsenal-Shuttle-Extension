using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles weapon player-control commands. These mutate only weapon-owned runtime payloads.
    /// </summary>
    internal sealed class ShuttleWeaponCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is SetWeaponForcedTargetCommand ||
                command is ClearWeaponForcedTargetCommand ||
                command is SetWeaponHoldFireCommand ||
                command is SetWeaponFireControlLinkedCommand ||
                command is SetWeaponFireControlModeCommand ||
                command is SetWeaponTargetPriorityCommand ||
                command is SetWeaponAutoFireCommand ||
                command is SetShuttleWeaponAmmoCommand ||
                command is ReloadShuttleWeaponCommand ||
                command is CancelShuttleWeaponReloadCommand ||
                command is SetShuttleWeaponAutoReloadCommand ||
                command is SetShuttleWeaponManualReloadAllowedCommand ||
                command is SetShuttleWeaponLogisticsAutoFeedCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            SetWeaponForcedTargetCommand setForcedTarget = command as SetWeaponForcedTargetCommand;
            if (setForcedTarget != null)
            {
                return this.ExecuteSetForcedTarget(context, setForcedTarget);
            }

            ClearWeaponForcedTargetCommand clearForcedTarget = command as ClearWeaponForcedTargetCommand;
            if (clearForcedTarget != null)
            {
                return this.ExecuteClearForcedTarget(context, clearForcedTarget);
            }

            SetWeaponHoldFireCommand setHoldFire = command as SetWeaponHoldFireCommand;
            if (setHoldFire != null)
            {
                return this.ExecuteSetHoldFire(context, setHoldFire);
            }

            SetWeaponFireControlLinkedCommand setFireControlLinked = command as SetWeaponFireControlLinkedCommand;
            if (setFireControlLinked != null)
            {
                return this.ExecuteSetFireControlLinked(context, setFireControlLinked);
            }

            SetWeaponFireControlModeCommand setFireControlMode = command as SetWeaponFireControlModeCommand;
            if (setFireControlMode != null)
            {
                return this.ExecuteSetFireControlMode(context, setFireControlMode);
            }

            SetWeaponTargetPriorityCommand setTargetPriority = command as SetWeaponTargetPriorityCommand;
            if (setTargetPriority != null)
            {
                return this.ExecuteSetTargetPriority(context, setTargetPriority);
            }

            SetWeaponAutoFireCommand setAutoFire = command as SetWeaponAutoFireCommand;
            if (setAutoFire != null)
            {
                return this.ExecuteSetAutoFire(context, setAutoFire);
            }

            SetShuttleWeaponAmmoCommand setAmmo = command as SetShuttleWeaponAmmoCommand;
            if (setAmmo != null)
            {
                return this.ExecuteSetAmmo(context, setAmmo);
            }

            ReloadShuttleWeaponCommand reload = command as ReloadShuttleWeaponCommand;
            if (reload != null)
            {
                return this.ExecuteReload(context, reload);
            }

            CancelShuttleWeaponReloadCommand cancelReload = command as CancelShuttleWeaponReloadCommand;
            if (cancelReload != null)
            {
                return this.ExecuteCancelReload(context, cancelReload);
            }

            SetShuttleWeaponAutoReloadCommand setAutoReload = command as SetShuttleWeaponAutoReloadCommand;
            if (setAutoReload != null)
            {
                return this.ExecuteSetAutoReload(context, setAutoReload);
            }

            SetShuttleWeaponManualReloadAllowedCommand setManualReload =
                command as SetShuttleWeaponManualReloadAllowedCommand;
            if (setManualReload != null)
            {
                return this.ExecuteSetManualReloadAllowed(context, setManualReload);
            }

            SetShuttleWeaponLogisticsAutoFeedCommand setLogistics = command as SetShuttleWeaponLogisticsAutoFeedCommand;
            if (setLogistics != null)
            {
                return this.ExecuteSetLogisticsAutoFeed(context, setLogistics);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedWeapon".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetForcedTarget(
            ShuttleCommandContext context,
            SetWeaponForcedTargetCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponForcedTarget(
                context.Host,
                context.AssemblyState,
                context.GetProfileForRead(),
                runtimeState,
                command.ModuleInstanceID,
                command.Target,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponForcedTargetSet".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteClearForcedTarget(
            ShuttleCommandContext context,
            ClearWeaponForcedTargetCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TryClearWeaponForcedTarget(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponForcedTargetCleared".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetHoldFire(
            ShuttleCommandContext context,
            SetWeaponHoldFireCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponHoldFire(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.HoldFire,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponHoldFireUpdated".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetFireControlLinked(
            ShuttleCommandContext context,
            SetWeaponFireControlLinkedCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponFireControlLinked(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Linked,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponFireControlLinkedChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetFireControlMode(
            ShuttleCommandContext context,
            SetWeaponFireControlModeCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponFireControlMode(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Mode,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponFireControlModeChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetTargetPriority(
            ShuttleCommandContext context,
            SetWeaponTargetPriorityCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponTargetPriority(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Priority,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponTargetPriorityChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetAutoFire(
            ShuttleCommandContext context,
            SetWeaponAutoFireCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponAutoFireEnabled(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Enabled,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponAutoFireChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetAmmo(
            ShuttleCommandContext context,
            SetShuttleWeaponAmmoCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponAmmo(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.AmmoDefName,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponAmmoChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteReload(
            ShuttleCommandContext context,
            ReloadShuttleWeaponCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TryStartWeaponReload(
                context.Host,
                context.AssemblyState,
                context.GetProfileForRead(),
                runtimeState,
                context.CargoBackend,
                context.StoredEnergySink,
                command.ModuleInstanceID,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponReloadStarted".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteCancelReload(
            ShuttleCommandContext context,
            CancelShuttleWeaponReloadCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TryCancelWeaponReload(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponReloadCanceled".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetAutoReload(
            ShuttleCommandContext context,
            SetShuttleWeaponAutoReloadCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponAutoReload(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Enabled,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponAutoReloadChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetManualReloadAllowed(
            ShuttleCommandContext context,
            SetShuttleWeaponManualReloadAllowedCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponManualReloadAllowed(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Enabled,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponManualReloadAllowedChanged".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetLogisticsAutoFeed(
            ShuttleCommandContext context,
            SetShuttleWeaponLogisticsAutoFeedCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponCommandMissing".Translate().ToString());
            }

            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_WeaponRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetWeaponLogisticsAutoFeed(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Enabled,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_WeaponLogisticsAutoFeedChanged".Translate().ToString());
        }
    }
}
