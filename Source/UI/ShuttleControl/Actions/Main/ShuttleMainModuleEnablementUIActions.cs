using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainModuleEnablementUIActions :
        IShuttleMainModuleEnablementUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMainModuleEnablementUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanSetModuleEnabled(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool enabled)
        {
            return ShuttleMainEnablementPolicy.CanRequestModuleEnablement(
                this.commandExecutor != null,
                segment,
                moduleSlot,
                enabled);
        }

        public bool SetModuleEnabled(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool enabled)
        {
            if (!this.CanSetModuleEnabled(segment, moduleSlot, enabled))
            {
                this.ShowReject(this.GetModuleEnablementTooltip(segment, moduleSlot, enabled));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetModuleEnabledCommand(
                    segment.InstalledSegmentInstanceID,
                    moduleSlot.SlotID,
                    moduleSlot.InstalledModuleInstanceID,
                    enabled));
            return this.ShowResult(result);
        }

        public string GetModuleEnablementTooltip(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool enabled)
        {
            return ShuttleMainEnablementText.GetModuleEnablementTooltip(
                this.commandExecutor != null,
                segment,
                moduleSlot,
                enabled);
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
