using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers
{
    internal sealed class ShuttleCargoUnloadModalLauncher
    {
        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly IShuttleCargoUnloadReadPort unloadReadPort;
        private readonly ShuttleCargoUnloadUIActions actions;
        private readonly Action markDirty;
        private readonly Action<bool> setChildModalOpen;

        internal ShuttleCargoUnloadModalLauncher(
            IShuttleCargoReadPort cargoReadPort,
            IShuttleCargoUnloadReadPort unloadReadPort,
            IShuttleCommandExecutor commandExecutor,
            Action markDirty,
            Action<bool> setChildModalOpen)
        {
            this.cargoReadPort = cargoReadPort;
            this.unloadReadPort = unloadReadPort;
            this.actions = new ShuttleCargoUnloadUIActions(commandExecutor);
            this.markDirty = markDirty;
            this.setChildModalOpen = setChildModalOpen;
        }

        internal void Open()
        {
            if (this.cargoReadPort == null || this.unloadReadPort == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"),
                    false);
                return;
            }

            try
            {
                this.SetChildModalOpen(true);
                Find.WindowStack.Add(new Dialog_ShuttleCargoUnloadV3(
                    this.cargoReadPort,
                    this.unloadReadPort,
                    this.actions,
                    delegate
                    {
                        this.SetChildModalOpen(false);
                        if (this.markDirty != null)
                        {
                            this.markDirty();
                        }
                    }));
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("OpenCargoUnloadWindow", exception);
                this.SetChildModalOpen(false);
                Messages.Message(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"),
                    MessageTypeDefOf.RejectInput,
                    false);
            }
        }

        private void SetChildModalOpen(bool value)
        {
            if (this.setChildModalOpen != null)
            {
                this.setChildModalOpen(value);
            }
        }
    }
}
