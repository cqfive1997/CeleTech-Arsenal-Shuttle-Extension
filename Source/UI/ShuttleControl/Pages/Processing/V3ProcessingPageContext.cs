using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingPageContext
    {
        internal readonly IShuttleProcessingUIActions ProcessingActions;
        internal readonly IShuttleIconService Icons;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3ProcessingPageContext(
            IShuttleProcessingUIActions processingActions,
            IShuttleIconService icons,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.ProcessingActions = processingActions;
            this.Icons = icons;
            this.TutorialTargets = tutorialTargets;
        }
    }
}
