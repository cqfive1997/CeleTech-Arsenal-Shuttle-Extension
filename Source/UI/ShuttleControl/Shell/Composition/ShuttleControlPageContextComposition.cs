using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition
{
    internal sealed class ShuttleControlPageContextComposition
    {
        internal void Bind(ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            ShuttlePageActions actions = context.Actions;
            ShuttlePageServices services = context.Services;
            IShuttleIconService icons = services != null ? services.Icons : null;
            IShuttleTutorialTargetService tutorialTargets =
                services != null ? services.TutorialTargets : null;

            context.CargoPageContext = new V3CargoPageContext(
                actions.CargoBayActions,
                actions.CargoBayUnloadActions,
                actions.CargoStackTransferActions,
                actions.CargoStackUnloadActions,
                icons,
                tutorialTargets);
            context.CrewPageContext = new V3CrewPageContext(
                actions.CrewLoadedCrewActions,
                actions.CrewHabitatActions,
                actions.CrewJoyActions,
                actions.CrewMedicalPatientActions,
                actions.CrewMechChargerActions,
                actions.CrewUnloadDialogActions,
                tutorialTargets);
            context.DefensePageContext = new V3DefensePageContext(
                actions.DefenseShieldActions,
                actions.DefenseWeaponTargetingActions,
                actions.DefenseWeaponFireControlActions,
                actions.DefenseWeaponAmmoActions,
                actions.DefenseWeaponLogisticsActions,
                actions.DefenseWeaponGroupActions,
                icons,
                services != null ? services.PaintBrowserPreviewProvider : null,
                tutorialTargets);
            context.ExternalModulesPageContext = new V3ExternalModulesPageContext(
                services != null ? services.ExternalModulePanelModelProvider : null,
                actions.ExternalPanelCommandActions,
                actions.ExternalRuntimeEnablementActions,
                actions.ExternalModuleDetailsActions,
                actions.MarkDirty,
                actions.MarkExternalModuleCommandCompleted);
            context.MainPageContext = new V3MainPageContext(
                actions.MainSegmentInstallActions,
                actions.MainModuleInstallActions,
                actions.MainModuleReplaceActions,
                actions.MainRemovalActions,
                actions.MainRemovalWorkerActions,
                actions.MainModuleEnablementActions,
                actions.MainSegmentModulesEnablementActions,
                actions.MainAssemblyConstructionActions,
                GetOpenExternalRuntime(services),
                tutorialTargets);
            context.MedicalPageContext = new V3MedicalPageContext(
                actions.MedicalAdmissionActions,
                actions.MedicalOccupantActions,
                actions.MedicalProcedureActions,
                actions.MedicalTreatmentActions,
                actions.MedicalSurgeryActions,
                tutorialTargets);
            context.PrisonCellPageContext = new V3PrisonCellPageContext(
                actions.PrisonCellActions,
                tutorialTargets);
            context.ProcessingPageContext = new V3ProcessingPageContext(
                actions.ProcessingActions,
                icons,
                tutorialTargets);
            context.SettingsPageContext = new V3SettingsPageContext(
                actions.SettingsActions,
                tutorialTargets);
        }

        private static Action<string, string> GetOpenExternalRuntime(
            ShuttlePageServices services)
        {
            return services != null &&
                services.ModalService != null
                ? new Action<string, string>(services.ModalService.OpenExternalRuntime)
                : null;
        }
    }
}
