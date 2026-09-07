using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew.Dialogs
{
    internal sealed class ShuttleCrewDialogLauncher :
        IShuttleCrewDialogLauncher
    {
        public void OpenHabitatJoyConfig(
            IShuttleCrewJoyUIActions actions,
            ShuttleControlReadModel model)
        {
            if (actions == null ||
                model == null ||
                model.Habitat == null ||
                model.Habitat.JoyConfigs == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleHabitatJoyConfigV3(
                actions,
                model.Habitat.JoyConfigs));
        }
    }
}
