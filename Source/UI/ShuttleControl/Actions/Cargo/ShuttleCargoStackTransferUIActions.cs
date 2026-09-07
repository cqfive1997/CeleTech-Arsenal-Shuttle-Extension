using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal sealed class ShuttleCargoStackTransferUIActions :
        IShuttleCargoStackTransferUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action markDirty;

        internal ShuttleCargoStackTransferUIActions(
            IShuttleCommandExecutor commandExecutor,
            Action markDirty)
        {
            this.commandExecutor = commandExecutor;
            this.markDirty = markDirty;
        }

        public bool CanTransfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext)
        {
            return this.commandExecutor != null &&
                ShuttleCargoTransferPolicy.CanStartTransfer(target, pageContext);
        }

        public void Transfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext)
        {
            this.Transfer(target, pageContext, null);
        }

        public void Transfer(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext,
            Action onSuccess)
        {
            if (!this.CanTransfer(target, pageContext))
            {
                this.ShowReject(this.GetTransferTooltip(target, pageContext));
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleCargoTransferV3(
                target,
                pageContext,
                this,
                onSuccess));
        }

        public bool CanTransfer(ShuttleCargoTransferActionTarget transferTarget)
        {
            return this.commandExecutor != null &&
                ShuttleCargoTransferPolicy.CanConfirmTransfer(transferTarget);
        }

        public bool Transfer(
            ShuttleCargoTransferActionTarget transferTarget,
            Action onSuccess)
        {
            if (!this.CanTransfer(transferTarget))
            {
                return this.ShowReject(this.GetTransferTooltip(transferTarget));
            }

            ShuttleCommandResult result = this.ExecuteTransfer(transferTarget);
            bool success = this.ShowResult(result);
            if (success && onSuccess != null)
            {
                onSuccess();
            }

            return success;
        }

        public string GetTransferTooltip(
            ShuttleCargoTransferActionTarget transferTarget)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            return ShuttleCargoTransferPolicy.GetConfirmTooltip(transferTarget);
        }

        public string GetTransferTooltip(
            ShuttleCargoStackActionTarget target,
            ShuttleCargoPageActionContext pageContext)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            return ShuttleCargoTransferPolicy.GetStartTooltip(target, pageContext);
        }

        private ShuttleCommandResult ExecuteTransfer(
            ShuttleCargoTransferActionTarget transferTarget)
        {
            ShuttleCargoTransferCommandTarget command =
                transferTarget != null ? transferTarget.Command : null;
            ShuttleCargoStackActionTarget stack =
                transferTarget != null && transferTarget.Validation != null
                    ? transferTarget.Validation.Stack
                    : null;
            if (command == null)
            {
                return ShuttleCommandResult.Failed(
                    this.Tr("CT_Shuttle_Command_ContextUnavailable"));
            }

            List<ShuttleCargoStackMemberActionTarget> slices;
            if (!ShuttleCargoStackMemberActionUtility.TryBuildSlices(
                stack,
                command.Count,
                out slices))
            {
                return ShuttleCommandResult.Failed(
                    this.Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable"));
            }

            if (command.Direction ==
                ShuttleCargoTransferDirection.LoadedToRefrigerated)
            {
                if (slices.Count == 1)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[0];
                    return this.commandExecutor.Execute(
                        new TransferLoadedCargoToRefrigeratedCargoCommand(
                            command.DestinationModuleInstanceID,
                            member.TransporterIndex,
                            member.LoadedIndex,
                            member.ThingIDNumber,
                            member.DefName,
                            member.StackCount));
                }

                List<TransferLoadedCargoGroupEntry> entries =
                    new List<TransferLoadedCargoGroupEntry>();
                for (int i = 0; i < slices.Count; i++)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[i];
                    entries.Add(new TransferLoadedCargoGroupEntry(
                        member.TransporterIndex,
                        member.LoadedIndex,
                        member.ThingIDNumber,
                        member.DefName,
                        member.StackCount));
                }

                return this.commandExecutor.Execute(
                    new TransferLoadedCargoGroupToRefrigeratedCargoCommand(
                        command.DestinationModuleInstanceID,
                        entries));
            }

            if (command.Direction ==
                ShuttleCargoTransferDirection.RefrigeratedToLoaded)
            {
                string moduleInstanceID =
                    ShuttleCargoStackMemberActionUtility.ResolveColdModuleInstanceID(
                        stack,
                        slices);
                if (slices.Count == 1)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[0];
                    return this.commandExecutor.Execute(
                        new TransferRefrigeratedCargoToLoadedCargoCommand(
                            moduleInstanceID,
                            member.ColdIndex,
                            member.ThingIDNumber,
                            member.DefName,
                            member.StackCount));
                }

                List<TransferRefrigeratedCargoGroupEntry> entries =
                    new List<TransferRefrigeratedCargoGroupEntry>();
                for (int i = 0; i < slices.Count; i++)
                {
                    ShuttleCargoStackMemberActionTarget member = slices[i];
                    entries.Add(new TransferRefrigeratedCargoGroupEntry(
                        member.ColdIndex,
                        member.ThingIDNumber,
                        member.DefName,
                        member.StackCount));
                }

                return this.commandExecutor.Execute(
                    new TransferRefrigeratedCargoGroupToLoadedCargoCommand(
                        moduleInstanceID,
                        entries));
            }

            return ShuttleCommandResult.Failed(
                this.Tr("CT_Shuttle_Command_ContextUnavailable"));
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            bool success = ShuttleUICommandFeedback.ShowResult(result);
            if (success && this.markDirty != null)
            {
                this.markDirty();
            }

            return success;
        }

        private bool ShowReject(string message)
        {
            return ShuttleUICommandFeedback.ShowReject(message, false);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
