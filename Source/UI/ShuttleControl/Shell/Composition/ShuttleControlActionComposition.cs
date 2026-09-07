using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition
{
    /// <summary>
    /// Creates ordinary V3 page action implementations for the normal shell path.
    /// </summary>
    internal sealed class ShuttleControlActionComposition
    {
        private readonly IShuttleProcessingDialogLauncher processingDialogLauncher;
        private readonly IShuttleCrewDialogLauncher crewDialogLauncher;
        private readonly IShuttleCrewUnloadDialogActions crewUnloadDialogActions;
        private readonly IShuttleMedicalDialogLauncher medicalDialogLauncher;
        private readonly IShuttleSettingsDialogLauncher settingsDialogLauncher;

        internal ShuttleControlActionComposition(
            IShuttleProcessingDialogLauncher processingDialogLauncher,
            IShuttleCrewDialogLauncher crewDialogLauncher,
            IShuttleCrewUnloadDialogActions crewUnloadDialogActions,
            IShuttleMedicalDialogLauncher medicalDialogLauncher,
            IShuttleSettingsDialogLauncher settingsDialogLauncher)
        {
            this.processingDialogLauncher = processingDialogLauncher;
            this.crewDialogLauncher = crewDialogLauncher;
            this.crewUnloadDialogActions = crewUnloadDialogActions;
            this.medicalDialogLauncher = medicalDialogLauncher;
            this.settingsDialogLauncher = settingsDialogLauncher;
        }

        internal void Bind(
            ShuttlePageDrawContext context,
            IShuttleCommandExecutor commandExecutor,
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityReadPort,
            IShuttleMedicalBayActionPort medicalBayActionPort,
            Func<int> getResearchFingerprint,
            Action closeForTargeting,
            ShuttleUIModalService modalService,
            Action<ShuttleSettingsChangeKind> onSettingsChanged)
        {
            if (context == null)
            {
                return;
            }

            context.Actions.CargoLoadActions =
                new ShuttleCargoLoadUIActions(commandExecutor);
            if (modalService != null)
            {
                modalService.CargoLoadActions = context.Actions.CargoLoadActions;
            }

            IShuttleCargoStackTransferUIActions cargoStackTransferActions =
                new ShuttleCargoStackTransferUIActions(
                    commandExecutor,
                    context.Actions.MarkDirty);
            context.Actions.CargoStackTransferActions = cargoStackTransferActions;
            IShuttleCargoStackUnloadUIActions cargoStackUnloadActions =
                new ShuttleCargoStackUnloadUIActions(
                    commandExecutor,
                    context.Actions.MarkDirty);
            context.Actions.CargoStackUnloadActions = cargoStackUnloadActions;
            IShuttleCargoBayUnloadUIActions cargoBayUnloadActions =
                new ShuttleCargoBayUnloadUIActions(
                    commandExecutor,
                    context.Actions.MarkDirty);
            context.Actions.CargoBayUnloadActions = cargoBayUnloadActions;
            context.Actions.CargoBayActions =
                new ShuttleCargoBayUIActions(
                    commandExecutor,
                    cargoStackTransferActions,
                    cargoStackUnloadActions,
                    cargoBayUnloadActions,
                    context.Actions.MarkDirty);
            context.Actions.ExternalRuntimeEnablementActions =
                new ShuttleExternalRuntimeEnablementUIActions(commandExecutor);
            context.Actions.ExternalPanelCommandActions =
                new ShuttleExternalPanelCommandUIActions(commandExecutor);
            context.Actions.ExternalModuleDetailsActions =
                new ShuttleExternalModuleDetailsUIActions(
                    context.Services.ExternalModulePanelModelProvider,
                    context.Actions.ExternalPanelCommandActions);
            context.Actions.MainSegmentInstallActions =
                new ShuttleMainSegmentInstallUIActions(
                    commandExecutor,
                    delegate { return GetAssemblyConstructionReadModel(context); },
                    getResearchFingerprint);
            context.Actions.MainModuleInstallActions =
                new ShuttleMainModuleInstallUIActions(
                    commandExecutor,
                    moduleInstallEligibilityReadPort,
                    delegate { return GetAssemblyConstructionReadModel(context); },
                    getResearchFingerprint);
            context.Actions.MainModuleReplaceActions =
                new ShuttleMainModuleReplaceUIActions(
                    commandExecutor,
                    moduleInstallEligibilityReadPort,
                    delegate { return GetAssemblyConstructionReadModel(context); },
                    getResearchFingerprint);
            context.Actions.MainRemovalActions =
                new ShuttleMainRemovalUIActions(commandExecutor);
            context.Actions.MainRemovalWorkerActions =
                new ShuttleMainRemovalWorkerUIActions(commandExecutor);
            context.Actions.MainModuleEnablementActions =
                new ShuttleMainModuleEnablementUIActions(commandExecutor);
            context.Actions.MainSegmentModulesEnablementActions =
                new ShuttleMainSegmentModulesEnablementUIActions(commandExecutor);
            context.Actions.MainAssemblyConstructionActions =
                new ShuttleMainAssemblyConstructionUIActions(commandExecutor);
            context.Actions.DefenseShieldActions =
                new ShuttleDefenseShieldUIActions(commandExecutor);
            context.Actions.DefenseWeaponTargetingActions =
                new ShuttleDefenseWeaponTargetingUIActions(
                    commandExecutor,
                    new ShuttleDefenseTargeterService(closeForTargeting),
                    context.Actions.MarkDirty);
            context.Actions.DefenseWeaponFireControlActions =
                new ShuttleDefenseWeaponFireControlUIActions(commandExecutor);
            context.Actions.DefenseWeaponAmmoActions =
                new ShuttleDefenseWeaponAmmoUIActions(commandExecutor);
            context.Actions.DefenseWeaponLogisticsActions =
                new ShuttleDefenseWeaponLogisticsUIActions(commandExecutor);
            context.Actions.DefenseWeaponGroupActions =
                new ShuttleDefenseWeaponGroupUIActions(commandExecutor);
            context.Actions.CrewLoadedCrewActions =
                new ShuttleCrewLoadedCrewUIActions(commandExecutor);
            context.Actions.CrewHabitatActions =
                new ShuttleCrewHabitatUIActions(commandExecutor);
            context.Actions.CrewJoyActions =
                new ShuttleCrewJoyUIActions(
                    commandExecutor,
                    this.OpenHabitatJoyConfigDialog);
            context.Actions.CrewMedicalPatientActions =
                new ShuttleCrewMedicalPatientUIActions(commandExecutor);
            context.Actions.CrewMechChargerActions =
                new ShuttleCrewMechChargerUIActions(commandExecutor);
            context.Actions.CrewUnloadDialogActions =
                this.crewUnloadDialogActions;
            context.Actions.MedicalAdmissionActions =
                new ShuttleMedicalAdmissionUIActions(
                    commandExecutor,
                    this.OpenMedicalAdmissionDialog);
            context.Actions.MedicalOccupantActions =
                new ShuttleMedicalOccupantUIActions(commandExecutor);
            context.Actions.MedicalProcedureActions =
                new ShuttleMedicalProcedureUIActions(commandExecutor);
            context.Actions.MedicalTreatmentActions =
                new ShuttleMedicalTreatmentUIActions(
                    commandExecutor,
                    medicalBayActionPort);
            context.Actions.MedicalSurgeryActions =
                new ShuttleMedicalSurgeryUIActions(
                    commandExecutor,
                    medicalBayActionPort,
                    this.OpenMedicalSurgerySelectorDialog);
            context.Actions.PrisonCellActions =
                new ShuttlePrisonCellUIActions(commandExecutor);
            context.Actions.ProcessingActions =
                new ShuttleProcessingUIActions(
                    commandExecutor,
                    this.OpenProcessingProductionPolicyDialog,
                    this.OpenProcessingIngredientFilterDialog);
            context.Actions.SettingsActions =
                new ShuttleSettingsUIActions(
                    onSettingsChanged,
                    delegate(
                        IShuttleSettingsUIActions actions,
                        ShuttleControlReadModel controlModel,
                        ShuttleWeaponBayReadModel weaponBayModel)
                    {
                        this.OpenSettingsHubDialog(
                            actions,
                            controlModel,
                            weaponBayModel,
                            commandExecutor);
                    });
            new ShuttleControlPageContextComposition().Bind(context);
        }

        private static ShuttleAssemblyConstructionReadModel GetAssemblyConstructionReadModel(
            ShuttlePageDrawContext context)
        {
            return context != null &&
                context.ReadModels != null &&
                context.ReadModels.ControlModel != null
                ? context.ReadModels.ControlModel.AssemblyConstruction
                : null;
        }

        private void OpenProcessingProductionPolicyDialog(
            IShuttleProcessingProductionPolicyActions actions,
            ShuttleProcessingOrderActionTarget order)
        {
            if (this.processingDialogLauncher != null)
            {
                this.processingDialogLauncher.OpenProductionPolicy(actions, order);
            }
        }

        private void OpenProcessingIngredientFilterDialog(
            IShuttleProcessingIngredientFilterActions actions,
            ShuttleProcessingOrderActionTarget order)
        {
            if (this.processingDialogLauncher != null)
            {
                this.processingDialogLauncher.OpenIngredientFilter(actions, order);
            }
        }

        private void OpenHabitatJoyConfigDialog(
            IShuttleCrewJoyUIActions actions,
            ShuttleControlReadModel model)
        {
            if (this.crewDialogLauncher != null)
            {
                this.crewDialogLauncher.OpenHabitatJoyConfig(actions, model);
            }
        }

        private void OpenMedicalAdmissionDialog(
            ShuttleMedicalPageActionContext pageContext,
            IShuttleMedicalAdmissionCandidateUIActions actions)
        {
            if (this.medicalDialogLauncher != null)
            {
                this.medicalDialogLauncher.OpenAdmission(pageContext, actions);
            }
        }

        private void OpenMedicalSurgerySelectorDialog(
            IShuttleMedicalSurgeryUIActions actions,
            ShuttleMedicalPatientActionTarget patient)
        {
            if (this.medicalDialogLauncher != null)
            {
                this.medicalDialogLauncher.OpenSurgerySelector(actions, patient);
            }
        }

        private void OpenSettingsHubDialog(
            IShuttleSettingsUIActions actions,
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            IShuttleCommandExecutor commandExecutor)
        {
            if (this.settingsDialogLauncher != null)
            {
                this.settingsDialogLauncher.OpenSettingsHub(
                    actions,
                    controlModel,
                    weaponBayModel,
                    commandExecutor);
            }
        }
    }
}
