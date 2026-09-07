using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition
{
    internal sealed class ShuttleControlInvalidatingCommandExecutor :
        IShuttleCommandExecutor
    {
        private readonly IShuttleCommandExecutor inner;
        private readonly Action onSuccessfulCommand;

        internal ShuttleControlInvalidatingCommandExecutor(
            IShuttleCommandExecutor inner,
            Action onSuccessfulCommand)
        {
            this.inner = inner;
            this.onSuccessfulCommand = onSuccessfulCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command)
        {
            if (this.inner == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString());
            }

            ShuttleCommandResult result = this.inner.Execute(command);
            if (result != null && result.Success && this.onSuccessfulCommand != null)
            {
                this.onSuccessfulCommand();
            }

            return result;
        }
    }
}
