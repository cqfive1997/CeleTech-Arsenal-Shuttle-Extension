using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttleAssemblyConstructionCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleAssemblyConstructionSystem system = new ShuttleAssemblyConstructionSystem();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is BeginSegmentConstructionCommand ||
                command is BeginSegmentReplacementConstructionCommand ||
                command is BeginModuleConstructionCommand ||
                command is BeginModuleReplacementConstructionCommand ||
                command is CancelAssemblyConstructionCommand ||
                command is DebugSetAssemblyConstructionProgressCommand ||
                command is DebugCompleteAssemblyConstructionCommand ||
                command is DebugFillAssemblyConstructionMaterialsCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            BeginSegmentConstructionCommand beginSegment = command as BeginSegmentConstructionCommand;
            if (beginSegment != null)
            {
                return this.system.BeginSegmentConstruction(
                    context,
                    beginSegment.SegmentSlotID,
                    beginSegment.SegmentDefName);
            }

            BeginSegmentReplacementConstructionCommand beginSegmentReplacement =
                command as BeginSegmentReplacementConstructionCommand;
            if (beginSegmentReplacement != null)
            {
                return this.system.BeginSegmentReplacementConstruction(
                    context,
                    beginSegmentReplacement.SegmentSlotID,
                    beginSegmentReplacement.SegmentDefName);
            }

            BeginModuleConstructionCommand beginModule = command as BeginModuleConstructionCommand;
            if (beginModule != null)
            {
                return this.system.BeginModuleConstruction(
                    context,
                    beginModule.SegmentInstanceID,
                    beginModule.ModuleSlotID,
                    beginModule.ModuleDefName,
                    beginModule.SelectedStuffDefName);
            }

            BeginModuleReplacementConstructionCommand beginReplacement =
                command as BeginModuleReplacementConstructionCommand;
            if (beginReplacement != null)
            {
                return this.system.BeginModuleReplacementConstruction(
                    context,
                    beginReplacement.SegmentInstanceID,
                    beginReplacement.ModuleSlotID,
                    beginReplacement.ModuleDefName,
                    beginReplacement.SelectedStuffDefName);
            }

            CancelAssemblyConstructionCommand cancel = command as CancelAssemblyConstructionCommand;
            if (cancel != null)
            {
                return this.system.Cancel(context, cancel.OrderID);
            }

            DebugSetAssemblyConstructionProgressCommand debugProgress =
                command as DebugSetAssemblyConstructionProgressCommand;
            if (debugProgress != null)
            {
                if (!Prefs.DevMode)
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_AssemblyConstruction_DebugDevModeOnly".Translate().ToString());
                }

                return this.system.DebugSetProgress(context, debugProgress.OrderID, debugProgress.Progress01);
            }

            DebugCompleteAssemblyConstructionCommand debugComplete =
                command as DebugCompleteAssemblyConstructionCommand;
            if (debugComplete != null)
            {
                if (!Prefs.DevMode)
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_AssemblyConstruction_DebugDevModeOnly".Translate().ToString());
                }

                return this.system.DebugComplete(context, debugComplete.OrderID);
            }

            DebugFillAssemblyConstructionMaterialsCommand debugFillMaterials =
                command as DebugFillAssemblyConstructionMaterialsCommand;
            if (debugFillMaterials != null)
            {
                if (!Prefs.DevMode)
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_AssemblyConstruction_DebugDevModeOnly".Translate().ToString());
                }

                return this.system.DebugFillMaterials(context, debugFillMaterials.OrderID);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }
    }
}
