using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal sealed class ShuttleCargoUnloadUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleCargoUnloadUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        internal bool Begin(IList<ShuttleCargoUnloadIntent> entries)
        {
            if (this.commandExecutor == null)
            {
                return ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable"),
                    false);
            }

            return ShuttleUICommandFeedback.ShowResult(
                this.commandExecutor.Execute(new BeginCargoUnloadCommand(entries)));
        }

        internal bool Cancel()
        {
            if (this.commandExecutor == null)
            {
                return ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable"),
                    false);
            }

            return ShuttleUICommandFeedback.ShowResult(
                this.commandExecutor.Execute(new CancelCargoUnloadCommand()));
        }
    }
}
