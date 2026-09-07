using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal interface IShuttleCrewUnloadDialogActions
    {
        void Open(
            V3CrewPageData pageData,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions);
    }
}
