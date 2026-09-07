using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal sealed class ShuttleCargoLoadUIActions : IShuttleCargoLoadUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleCargoLoadUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool BeginLoadCargo(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            bool replaceExistingQueue)
        {
            if (this.commandExecutor == null)
            {
                return this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(new BeginLoadCargoCommand(
                passengerTransferables,
                cargoTransferables,
                replaceExistingQueue));
            return this.ShowResult(result);
        }

        public bool ClearQueuedLoad()
        {
            if (this.commandExecutor == null)
            {
                return this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(new ClearQueuedLoadCommand());
            return this.ShowResult(result);
        }

        public bool CancelQueuedLoadEntry(
            int transporterIndex,
            int queueIndex,
            int count)
        {
            if (this.commandExecutor == null)
            {
                return this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(new CancelQueuedLoadEntryCommand(
                transporterIndex,
                queueIndex,
                count));
            return this.ShowResult(result);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
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
