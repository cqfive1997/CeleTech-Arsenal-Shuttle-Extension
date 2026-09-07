using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainSegmentModulesEnablementUIActions :
        IShuttleMainSegmentModulesEnablementUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMainSegmentModulesEnablementUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanSetSegmentModulesEnabled(
            ShuttleControlSegmentSlotModel segment,
            bool enabled)
        {
            return ShuttleMainEnablementPolicy.CanRequestSegmentModulesEnablement(
                this.commandExecutor != null,
                segment,
                enabled);
        }

        public void SetSegmentModulesEnabled(
            ShuttleControlSegmentSlotModel segment,
            bool enabled)
        {
            if (!this.CanSetSegmentModulesEnabled(segment, enabled))
            {
                this.ShowReject(this.GetSegmentModulesEnablementTooltip(segment, enabled));
                return;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetSegmentModulesEnabledCommand(segment.SlotID, enabled));
            this.ShowResult(result);
        }

        public string GetSegmentModulesEnablementTooltip(
            ShuttleControlSegmentSlotModel segment,
            bool enabled)
        {
            bool canRequest = this.CanSetSegmentModulesEnabled(segment, enabled);
            return ShuttleMainEnablementText.GetSegmentModulesEnablementTooltip(
                this.commandExecutor != null,
                segment,
                enabled,
                canRequest);
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
