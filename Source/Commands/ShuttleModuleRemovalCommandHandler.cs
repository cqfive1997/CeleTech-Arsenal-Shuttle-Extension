using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttleModuleRemovalCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleModuleRemovalService service = new ShuttleModuleRemovalService();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is StartSegmentRemovalCommand ||
                command is StartModuleRemovalCommand ||
                command is CancelModuleRemovalCommand ||
                command is AssignModuleRemovalWorkerCommand ||
                command is CancelSegmentRemovalCommand ||
                command is AssignSegmentRemovalWorkerCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            StartModuleRemovalCommand start = command as StartModuleRemovalCommand;
            if (start != null)
            {
                if (this.HasActiveCargoUnload(context))
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_Cargo_RemovalBlockedByUnloading".Translate().ToString());
                }

                return this.service.StartRemoval(
                    context,
                    start.SegmentInstanceID,
                    start.ModuleSlotID,
                    start.ModuleInstanceID);
            }

            StartSegmentRemovalCommand startSegment = command as StartSegmentRemovalCommand;
            if (startSegment != null)
            {
                if (this.HasActiveCargoUnload(context))
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_Cargo_RemovalBlockedByUnloading".Translate().ToString());
                }

                return this.service.StartSegmentRemoval(context, startSegment.SegmentSlotID);
            }

            CancelModuleRemovalCommand cancel = command as CancelModuleRemovalCommand;
            if (cancel != null)
            {
                return this.service.CancelRemoval(context, cancel.ModuleInstanceID);
            }

            AssignModuleRemovalWorkerCommand assign = command as AssignModuleRemovalWorkerCommand;
            if (assign != null)
            {
                return this.service.AssignRemovalWorker(context, assign.ModuleInstanceID);
            }

            CancelSegmentRemovalCommand cancelSegment = command as CancelSegmentRemovalCommand;
            if (cancelSegment != null)
            {
                return this.service.CancelRemoval(context, cancelSegment.SegmentSlotID);
            }

            AssignSegmentRemovalWorkerCommand assignSegment = command as AssignSegmentRemovalWorkerCommand;
            if (assignSegment != null)
            {
                return this.service.AssignRemovalWorker(context, assignSegment.SegmentSlotID);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }

        private bool HasActiveCargoUnload(ShuttleCommandContext context)
        {
            ShuttleRuntimeState runtimeState = context != null
                ? context.GetRuntimeState()
                : null;
            return runtimeState != null &&
                runtimeState.CargoUnload != null &&
                runtimeState.CargoUnload.IsActive;
        }
    }
}
