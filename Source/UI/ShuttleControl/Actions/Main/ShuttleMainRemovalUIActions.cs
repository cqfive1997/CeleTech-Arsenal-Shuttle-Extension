using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainRemovalUIActions : IShuttleMainRemovalUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMainRemovalUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanRemoveSegment(
            ShuttleControlSegmentSlotModel segment,
            bool hasActiveConstructionOrder)
        {
            return ShuttleMainRemovalPolicy.CanRequestSegmentRemove(
                this.commandExecutor != null,
                hasActiveConstructionOrder,
                segment);
        }

        public bool RemoveSegment(
            ShuttleControlSegmentSlotModel segment,
            bool hasActiveConstructionOrder)
        {
            if (!this.CanRemoveSegment(segment, hasActiveConstructionOrder))
            {
                this.ShowReject(this.GetSegmentRemoveTooltip(segment, hasActiveConstructionOrder));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new StartSegmentRemovalCommand(segment.SlotID));
            return this.ShowResult(result);
        }

        public string GetSegmentRemoveTooltip(
            ShuttleControlSegmentSlotModel segment,
            bool hasActiveConstructionOrder)
        {
            bool canRemove = this.CanRemoveSegment(segment, hasActiveConstructionOrder);
            return ShuttleMainRemovalText.GetSegmentRemoveTooltip(
                this.commandExecutor != null,
                hasActiveConstructionOrder,
                segment,
                canRemove);
        }

        public bool CanCancelSegmentRemoval(ShuttleControlSegmentSlotModel segment)
        {
            return ShuttleMainRemovalPolicy.CanCancelSegmentRemoval(
                this.commandExecutor != null,
                segment);
        }

        public bool CancelSegmentRemoval(ShuttleControlSegmentSlotModel segment)
        {
            if (!this.CanCancelSegmentRemoval(segment))
            {
                this.ShowReject(ShuttleMainRemovalText.GetNoActiveRemovalTooltip(
                    segment != null ? segment.RemovalTooltip : null));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CancelSegmentRemovalCommand(segment.SlotID));
            return this.ShowResult(result);
        }

        public bool CanRemoveModule(
            ShuttleControlModuleSlotModel moduleSlot,
            bool hasActiveConstructionOrder)
        {
            return ShuttleMainRemovalPolicy.CanRequestModuleRemove(
                this.commandExecutor != null,
                hasActiveConstructionOrder,
                moduleSlot);
        }

        public bool RemoveModule(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            bool hasActiveConstructionOrder)
        {
            if (segment == null ||
                moduleSlot == null ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID))
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return false;
            }

            if (!this.CanRemoveModule(moduleSlot, hasActiveConstructionOrder))
            {
                this.ShowReject(this.GetModuleRemoveTooltip(moduleSlot, hasActiveConstructionOrder));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new StartModuleRemovalCommand(
                    segment.InstalledSegmentInstanceID,
                    moduleSlot.SlotID,
                    moduleSlot.InstalledModuleInstanceID));
            return this.ShowResult(result);
        }

        public string GetModuleRemoveTooltip(
            ShuttleControlModuleSlotModel moduleSlot,
            bool hasActiveConstructionOrder)
        {
            bool canRemove = this.CanRemoveModule(moduleSlot, hasActiveConstructionOrder);
            return ShuttleMainRemovalText.GetModuleRemoveTooltip(
                this.commandExecutor != null,
                hasActiveConstructionOrder,
                moduleSlot,
                canRemove);
        }

        public bool CanCancelModuleRemoval(ShuttleControlModuleSlotModel moduleSlot)
        {
            return ShuttleMainRemovalPolicy.CanCancelModuleRemoval(
                this.commandExecutor != null,
                moduleSlot);
        }

        public bool CancelModuleRemoval(ShuttleControlModuleSlotModel moduleSlot)
        {
            if (!this.CanCancelModuleRemoval(moduleSlot))
            {
                this.ShowReject(ShuttleMainRemovalText.GetNoActiveRemovalTooltip(
                    moduleSlot != null ? moduleSlot.RemovalTooltip : null));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CancelModuleRemovalCommand(moduleSlot.InstalledModuleInstanceID));
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
