using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellPageContext
    {
        internal readonly IShuttlePrisonCellUIActions PrisonCellActions;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3PrisonCellPageContext(
            IShuttlePrisonCellUIActions prisonCellActions,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.PrisonCellActions = prisonCellActions;
            this.TutorialTargets = tutorialTargets;
        }
    }
}
