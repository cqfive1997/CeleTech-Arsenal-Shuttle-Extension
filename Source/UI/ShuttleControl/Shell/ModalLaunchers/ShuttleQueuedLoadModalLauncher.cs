using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers
{
    internal sealed class ShuttleQueuedLoadModalLauncher
    {
        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly Action markDirty;
        private readonly Action<bool> setChildModalOpen;

        internal ShuttleQueuedLoadModalLauncher(
            IShuttleCargoReadPort cargoReadPort,
            Action markDirty,
            Action<bool> setChildModalOpen)
        {
            this.cargoReadPort = cargoReadPort;
            this.markDirty = markDirty;
            this.setChildModalOpen = setChildModalOpen;
        }

        internal void OpenQueuedLoadWindow(IShuttleCargoLoadUIActions cargoLoadActions)
        {
            if (this.cargoReadPort == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            try
            {
                this.SetChildModalOpen(true);
                Find.WindowStack.Add(new Dialog_ShuttleQueuedLoadV3(
                    this.cargoReadPort,
                    cargoLoadActions,
                    delegate
                    {
                        this.SetChildModalOpen(false);
                    },
                    this.MarkDirty));
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("OpenQueuedLoadWindow", exception);
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

        private void MarkDirty()
        {
            if (this.markDirty != null)
            {
                this.markDirty();
            }
        }
    }
}
