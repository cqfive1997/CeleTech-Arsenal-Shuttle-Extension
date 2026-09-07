using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoPageContext
    {
        internal readonly IShuttleCargoBayUIActions CargoBayActions;
        internal readonly IShuttleCargoBayUnloadUIActions CargoBayUnloadActions;
        internal readonly IShuttleCargoStackTransferUIActions StackTransferActions;
        internal readonly IShuttleCargoStackUnloadUIActions StackUnloadActions;
        internal readonly IShuttleIconService Icons;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3CargoPageContext(
            IShuttleCargoBayUIActions cargoBayActions,
            IShuttleCargoBayUnloadUIActions cargoBayUnloadActions,
            IShuttleCargoStackTransferUIActions stackTransferActions,
            IShuttleCargoStackUnloadUIActions stackUnloadActions,
            IShuttleIconService icons,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.CargoBayActions = cargoBayActions;
            this.CargoBayUnloadActions = cargoBayUnloadActions;
            this.StackTransferActions = stackTransferActions;
            this.StackUnloadActions = stackUnloadActions;
            this.Icons = icons;
            this.TutorialTargets = tutorialTargets;
        }
    }
}
