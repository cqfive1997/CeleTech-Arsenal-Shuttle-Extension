using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsPageContext
    {
        internal readonly IShuttleSettingsUIActions SettingsActions;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3SettingsPageContext(
            IShuttleSettingsUIActions settingsActions,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.SettingsActions = settingsActions;
            this.TutorialTargets = tutorialTargets;
        }
    }
}
