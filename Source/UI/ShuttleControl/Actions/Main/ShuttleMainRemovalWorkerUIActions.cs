using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainRemovalWorkerUIActions :
        IShuttleMainRemovalWorkerUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMainRemovalWorkerUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanAssignSegmentRemovalWorker(ShuttleControlSegmentSlotModel segment)
        {
            return ShuttleMainRemovalPolicy.CanAssignSegmentRemovalWorker(
                this.commandExecutor != null,
                segment);
        }

        public bool AssignSegmentRemovalWorker(ShuttleControlSegmentSlotModel segment)
        {
            if (!this.CanAssignSegmentRemovalWorker(segment))
            {
                this.ShowReject(ShuttleMainRemovalText.GetNoActiveRemovalTooltip(
                    segment != null ? segment.RemovalTooltip : null));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new AssignSegmentRemovalWorkerCommand(segment.SlotID));
            return this.ShowResult(result);
        }

        public bool CanAssignModuleRemovalWorker(ShuttleControlModuleSlotModel moduleSlot)
        {
            return ShuttleMainRemovalPolicy.CanAssignModuleRemovalWorker(
                this.commandExecutor != null,
                moduleSlot);
        }

        public bool AssignModuleRemovalWorker(ShuttleControlModuleSlotModel moduleSlot)
        {
            if (!this.CanAssignModuleRemovalWorker(moduleSlot))
            {
                this.ShowReject(ShuttleMainRemovalText.GetNoActiveRemovalTooltip(
                    moduleSlot != null ? moduleSlot.RemovalTooltip : null));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new AssignModuleRemovalWorkerCommand(moduleSlot.InstalledModuleInstanceID));
            return this.ShowResult(result);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result, MessageTypeDefOf.PositiveEvent);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message, false);
        }
    }
}
