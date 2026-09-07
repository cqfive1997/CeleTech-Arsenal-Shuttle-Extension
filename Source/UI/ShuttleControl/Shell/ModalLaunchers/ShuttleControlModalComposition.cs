using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers
{
    internal sealed class ShuttleControlModalComposition
    {
        private readonly ShuttleLaunchModalLauncher launchModalLauncher;
        private readonly ShuttleCargoLoadModalLauncher cargoLoadModalLauncher;
        private readonly ShuttleCargoUnloadModalLauncher cargoUnloadModalLauncher;

        internal ShuttleControlModalComposition(
            ShuttleUIModalService modalService,
            IShuttleCommandExecutor commandExecutor,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCargoReadPort cargoReadPort,
            Action markDirty,
            Action<bool> setChildModalOpen,
            Action closeWindow)
        {
            this.launchModalLauncher = new ShuttleLaunchModalLauncher(
                commandExecutor,
                markDirty,
                closeWindow);
            this.cargoLoadModalLauncher = new ShuttleCargoLoadModalLauncher(
                loadCargoReadPort,
                cargoReadPort,
                markDirty,
                setChildModalOpen);
            this.cargoUnloadModalLauncher = new ShuttleCargoUnloadModalLauncher(
                cargoReadPort,
                cargoReadPort as IShuttleCargoUnloadReadPort,
                commandExecutor,
                markDirty,
                setChildModalOpen);

            if (modalService != null)
            {
                modalService.OpenLoadCargoAction =
                    this.cargoLoadModalLauncher.OpenLoadCargoWindow;
                modalService.OpenCargoUnloadAction =
                    this.cargoUnloadModalLauncher.Open;
                modalService.OpenLaunchAction =
                    this.launchModalLauncher.OpenLaunchTargeting;
            }
        }
    }
}
