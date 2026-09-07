using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition
{
    internal interface IShuttleProcessingDialogLauncher
    {
        void OpenProductionPolicy(
            IShuttleProcessingProductionPolicyActions actions,
            ShuttleProcessingOrderActionTarget order);

        void OpenIngredientFilter(
            IShuttleProcessingIngredientFilterActions actions,
            ShuttleProcessingOrderActionTarget order);
    }

    internal interface IShuttleCrewDialogLauncher
    {
        void OpenHabitatJoyConfig(
            IShuttleCrewJoyUIActions actions,
            ShuttleControlReadModel model);
    }

    internal interface IShuttleMedicalDialogLauncher
    {
        void OpenAdmission(
            ShuttleMedicalPageActionContext pageContext,
            IShuttleMedicalAdmissionCandidateUIActions actions);

        void OpenSurgerySelector(
            IShuttleMedicalSurgeryUIActions actions,
            ShuttleMedicalPatientActionTarget patient);
    }

    internal interface IShuttleSettingsDialogLauncher
    {
        void OpenSettingsHub(
            IShuttleSettingsUIActions actions,
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            IShuttleCommandExecutor commandExecutor);
    }
}
