using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttleCargoUnloadCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleCargoUnloadPlanValidator validator =
            new ShuttleCargoUnloadPlanValidator();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is BeginCargoUnloadCommand ||
                command is CancelCargoUnloadCommand;
        }

        public ShuttleCommandResult Execute(
            IShuttleCommand command,
            ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            BeginCargoUnloadCommand begin = command as BeginCargoUnloadCommand;
            if (begin != null)
            {
                return this.ExecuteBegin(context, begin);
            }

            if (command is CancelCargoUnloadCommand)
            {
                return this.ExecuteCancel(context);
            }

            return ShuttleCommandResult.Failed(
                "CT_Shuttle_Command_UnsupportedCargo".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteBegin(
            ShuttleCommandContext context,
            BeginCargoUnloadCommand command)
        {
            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (runtimeState == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Launch_Failed_RuntimeUnavailable".Translate().ToString());
            }

            runtimeState.EnsureInitialized();
            if (runtimeState.CargoUnload.IsActive)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_UnloadAlreadyActive".Translate().ToString());
            }

            if (context.Host == null || !context.Host.Spawned || context.Host.Map == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ShuttleMapUnavailable".Translate().ToString());
            }

            if (context.CargoBackend == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString());
            }

            if (context.CargoBackend.HasPendingLoadQueue(context.Host))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_UnloadBlockedByLoading".Translate().ToString());
            }

            ShuttleCargoSnapshot snapshot = context.BuildCargoSnapshot();
            List<ShuttleCargoUnloadRecord> records;
            string failureReason;
            if (!this.validator.TryBuildRecords(
                command != null ? command.Entries : null,
                snapshot,
                out records,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            int ticksGame = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : 0;
            if (!runtimeState.CargoUnload.Start(records, ticksGame))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_UnloadCouldNotStart".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Cargo_UnloadStarted".Translate(
                    runtimeState.CargoUnload.TotalStackCount,
                    runtimeState.CargoUnload.TotalThingCount).ToString());
        }

        private ShuttleCommandResult ExecuteCancel(ShuttleCommandContext context)
        {
            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (runtimeState == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Launch_Failed_RuntimeUnavailable".Translate().ToString());
            }

            int ticksGame = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : 0;
            if (!runtimeState.CargoUnload.Cancel(ticksGame))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_UnloadNotActive".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Cargo_UnloadCanceled".Translate().ToString());
        }
    }
}
