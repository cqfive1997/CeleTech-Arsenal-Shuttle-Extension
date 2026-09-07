using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands.External
{
    internal sealed class ExternalShuttleCommandHandlerAdapter : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is ShuttleExternalCommand;
        }

        public ShuttleCommandResult Execute(
            IShuttleCommand command,
            ShuttleCommandContext context)
        {
            ShuttleExternalCommand externalCommand = command as ShuttleExternalCommand;
            if (externalCommand == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_UnsupportedExternalCommand".Translate().ToString());
            }

            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            try
            {
                return this.ExecuteExternalCommand(context, externalCommand);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(ExternalShuttleCommandRegistry.LogPrefix +
                    "external command '" + externalCommand.CommandKey +
                    "' failed unexpectedly. Exception: " + exception,
                    MakeCommandFailureHash(externalCommand, exception));
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ExternalCommandFailed".Translate().ToString());
            }
        }

        private static int MakeCommandFailureHash(
            ShuttleExternalCommand command,
            Exception exception)
        {
            unchecked
            {
                int hash = 43;
                hash = (hash * 37) + (command != null && command.CommandKey != null
                    ? command.CommandKey.GetHashCode()
                    : 0);
                hash = (hash * 37) + (command != null && command.RuntimeSystemKey != null
                    ? command.RuntimeSystemKey.GetHashCode()
                    : 0);
                hash = (hash * 37) + (command != null && command.ModuleInstanceID != null
                    ? command.ModuleInstanceID.GetHashCode()
                    : 0);
                hash = (hash * 37) + (exception != null && exception.GetType() != null
                    ? exception.GetType().FullName.GetHashCode()
                    : 0);
                hash = (hash * 37) + (exception != null && exception.Message != null
                    ? exception.Message.GetHashCode()
                    : 0);
                return hash;
            }
        }

        private ShuttleCommandResult ExecuteExternalCommand(
            ShuttleCommandContext context,
            ShuttleExternalCommand command)
        {
            if (string.IsNullOrEmpty(command.CommandKey))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalCommandKeyMissing".Translate().ToString());
            }

            if (string.IsNullOrEmpty(command.RuntimeSystemKey))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalRuntimeSystemKeyMissing".Translate().ToString());
            }

            if (string.IsNullOrEmpty(command.ModuleInstanceID))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalCommandModuleInstanceMissing".Translate().ToString());
            }

            ExternalCommandRegistration commandRegistration;
            if (!ExternalShuttleCommandRegistry.TryResolve(
                command.CommandKey,
                out commandRegistration))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalCommandKeyNotRegistered"
                        .Translate(command.CommandKey)
                        .ToString());
            }

            string ownerPrefix = commandRegistration.OwnerPackageId + "/";
            if (command.RuntimeSystemKey.IndexOf(ownerPrefix, StringComparison.Ordinal) != 0)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalCommandRuntimeOwnerMismatch"
                        .Translate(command.CommandKey, command.RuntimeSystemKey)
                        .ToString());
            }

            ExternalRuntimeRegistration runtimeRegistration;
            if (!ExternalShuttleRuntimeRegistry.TryResolve(
                command.RuntimeSystemKey,
                out runtimeRegistration))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalRuntimeKeyNotRegistered"
                        .Translate(command.RuntimeSystemKey)
                        .ToString());
            }

            if (runtimeRegistration == null ||
                !StringComparer.Ordinal.Equals(
                    runtimeRegistration.OwnerPackageId,
                    commandRegistration.OwnerPackageId))
            {
                string ownerPackageId = runtimeRegistration != null
                    ? runtimeRegistration.OwnerPackageId
                    : "CT_Shuttle_Command_ExternalRuntimeOwnerUnknown".Translate().ToString();
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalRuntimeOwnerMismatch"
                        .Translate(
                            command.CommandKey,
                            ownerPackageId)
                        .ToString());
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

            ShuttleExternalCommandContext externalContext =
                new ShuttleExternalCommandContext(
                    command,
                    ExternalRuntimeInfoFactory.CreateModuleInfo(module),
                    new ExternalRuntimeStateStore(externalState),
                    ShuttleTickUtility.TicksGameOrZero(),
                    runtimeState.Power != null && runtimeState.Power.InternalBusPowered,
                    amountWd => context.StoredEnergySink != null &&
                        context.StoredEnergySink.TryConsumeStoredEnergyWd(runtimeState, amountWd));

            ShuttleExternalCommandResult externalResult =
                commandRegistration.Handler.Execute(externalContext);
            if (externalResult == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExternalCommandNoResult"
                        .Translate(command.CommandKey)
                        .ToString());
            }

            return externalResult.Success
                ? ShuttleCommandResult.Succeeded(externalResult.Message)
                : ShuttleCommandResult.Failed(externalResult.Message);
        }
    }
}
