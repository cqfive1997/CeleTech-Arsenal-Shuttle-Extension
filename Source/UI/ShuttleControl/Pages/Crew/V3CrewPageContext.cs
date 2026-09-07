using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPageContext
    {
        internal readonly IShuttleCrewLoadedCrewUIActions LoadedCrewActions;
        internal readonly IShuttleCrewHabitatUIActions HabitatActions;
        internal readonly IShuttleCrewJoyUIActions JoyActions;
        internal readonly IShuttleCrewMedicalPatientUIActions MedicalPatientActions;
        internal readonly IShuttleCrewMechChargerUIActions MechChargerActions;
        internal readonly IShuttleCrewUnloadDialogActions UnloadDialogActions;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3CrewPageContext(
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewJoyUIActions joyActions,
            IShuttleCrewMedicalPatientUIActions medicalPatientActions,
            IShuttleCrewMechChargerUIActions mechChargerActions,
            IShuttleCrewUnloadDialogActions unloadDialogActions,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.LoadedCrewActions = loadedCrewActions;
            this.HabitatActions = habitatActions;
            this.JoyActions = joyActions;
            this.MedicalPatientActions = medicalPatientActions;
            this.MechChargerActions = mechChargerActions;
            this.UnloadDialogActions = unloadDialogActions;
            this.TutorialTargets = tutorialTargets;
        }
    }
}
