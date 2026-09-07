using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands.External
{
    internal sealed class SetExternalRuntimeEnabledCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is SetExternalRuntimeEnabledCommand;
        }

        public ShuttleCommandResult Execute(
            IShuttleCommand command,
            ShuttleCommandContext context)
        {
            SetExternalRuntimeEnabledCommand setEnabledCommand =
                command as SetExternalRuntimeEnabledCommand;
            if (setEnabledCommand == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_UnsupportedExternalRuntimeEnabled".Translate().ToString());
            }

            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            return this.ExecuteSetEnabled(context, setEnabledCommand);
        }

        private ShuttleCommandResult ExecuteSetEnabled(
            ShuttleCommandContext context,
            SetExternalRuntimeEnabledCommand command)
        {
            if (string.IsNullOrEmpty(command.ModuleInstanceID))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalCommandModuleInstanceMissing".Translate().ToString());
            }

            if (string.IsNullOrEmpty(command.RuntimeSystemKey))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalRuntimeSystemKeyMissing".Translate().ToString());
            }

            context.ReconcileProfileToHost();

            ShuttleAssemblyState assemblyState = context.AssemblyState;
            if (assemblyState == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ShuttleAssemblyStateUnavailable".Translate().ToString());
            }

            ShuttleModule module = assemblyState.GetModule(command.ModuleInstanceID);
            if (module == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_TargetShuttleModuleNotInstalled".Translate().ToString());
            }

            if (!ExternalRuntimeBindingUtility.ModuleHasRuntimeKey(
                module,
                command.RuntimeSystemKey))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_TargetModuleNotBoundExternalRuntime"
                        .Translate(command.RuntimeSystemKey)
                        .ToString());
            }

            ExternalRuntimeRegistration registration;
            if (!ExternalShuttleRuntimeRegistry.TryResolve(
                command.RuntimeSystemKey,
                out registration))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_TargetExternalRuntimeNotRegistered"
                        .Translate(command.RuntimeSystemKey)
                        .ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (runtimeState == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString());
            }

            IShuttleModuleRuntimeState state;
            if (!runtimeState.Modules.TryGetState(
                command.ModuleInstanceID,
                command.RuntimeSystemKey,
                out state))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalRuntimeStateUnavailableFor"
                        .Translate(command.RuntimeSystemKey)
                        .ToString());
            }

            ExternalModuleRuntimeState externalState = state as ExternalModuleRuntimeState;
            if (externalState == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalRuntimeStateWrongEnvelope"
                        .Translate(command.RuntimeSystemKey)
                        .ToString());
            }

            ExternalRuntimeStateUtility.SetRuntimeEnabled(
                externalState,
                command.Enabled);

            return ShuttleCommandResult.Succeeded(
                command.Enabled
                    ? "CT_Shuttle_Command_ExternalRuntimeEnabled".Translate().ToString()
                    : "CT_Shuttle_Command_ExternalRuntimeDisabled".Translate().ToString());
        }
    }
}
