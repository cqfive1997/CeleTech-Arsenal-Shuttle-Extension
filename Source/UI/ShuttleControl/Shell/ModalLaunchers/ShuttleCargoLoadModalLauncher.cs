using System;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers
{
    internal sealed class ShuttleCargoLoadModalLauncher
    {
        private readonly IShuttleLoadCargoReadPort loadCargoReadPort;
        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly Action markDirty;
        private readonly Action<bool> setChildModalOpen;

        internal ShuttleCargoLoadModalLauncher(
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCargoReadPort cargoReadPort,
            Action markDirty,
            Action<bool> setChildModalOpen)
        {
            this.loadCargoReadPort = loadCargoReadPort;
            this.cargoReadPort = cargoReadPort;
            this.markDirty = markDirty;
            this.setChildModalOpen = setChildModalOpen;
        }

        internal void OpenLoadCargoWindow(IShuttleCargoLoadUIActions cargoLoadActions)
        {
            if (this.loadCargoReadPort == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_LoadCargoReadPortUnavailable"));
                return;
            }

            try
            {
                bool useStubReadModel =
                    ShuttleCargoOpenPathDiagnosticFlags.LoadCargoUseStubReadModel;
                ShuttleLoadCargoReadModel readModel = useStubReadModel
                    ? BuildDiagnosticStubReadModel()
                    : this.loadCargoReadPort.BuildLoadCargoReadModel();
                if (!useStubReadModel && (readModel == null || !readModel.HasTransporter))
                {
                    ShuttleUICommandFeedback.ShowReject(
                        ShuttleUIText.Tr("CT_Shuttle_Cargo_NoTransporterAvailable"));
                    return;
                }

                this.SetChildModalOpen(true);
                Find.WindowStack.Add(new Dialog_ShuttleCargoLoadV3(
                    readModel,
                    this.loadCargoReadPort,
                    this.cargoReadPort,
                    delegate
                    {
                        this.SetChildModalOpen(false);
                        this.MarkDirty();
                    },
                    cargoLoadActions));
            }
            catch (Exception exception)
            {
                ShuttleReadModelFailureLogger.LogOnce("OpenLoadCargoWindow", exception);
                this.SetChildModalOpen(false);
                Messages.Message(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"),
                    MessageTypeDefOf.RejectInput,
                    false);
            }
        }

        private static ShuttleLoadCargoReadModel BuildDiagnosticStubReadModel()
        {
            ShuttleLoadCargoReadModel readModel = new ShuttleLoadCargoReadModel();
            readModel.RefreshSearchCorpusKey();
            return readModel;
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
