using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers
{
    internal sealed class ShuttleLaunchModalLauncher
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action markDirty;
        private readonly Action closeWindow;

        internal ShuttleLaunchModalLauncher(
            IShuttleCommandExecutor commandExecutor,
            Action markDirty,
            Action closeWindow)
        {
            this.commandExecutor = commandExecutor;
            this.markDirty = markDirty;
            this.closeWindow = closeWindow;
        }

        internal void OpenLaunchTargeting()
        {
            if (this.commandExecutor == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return;
            }

            ShuttleCommandResult result =
                this.commandExecutor.Execute(new BeginLaunchTargetingCommand());
            if (result == null || !result.Success)
            {
                this.MarkDirty();
                ShuttleUICommandFeedback.ShowReject(
                    result != null
                        ? result.Message
                        : ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            this.CloseWindow();
        }

        private void MarkDirty()
        {
            if (this.markDirty != null)
            {
                this.markDirty();
            }
        }

        private void CloseWindow()
        {
            if (this.closeWindow != null)
            {
                this.closeWindow();
            }
        }
    }
}
