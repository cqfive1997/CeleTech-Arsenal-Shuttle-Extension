using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.PrisonCell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal sealed class ShuttlePageActions
    {
        internal Action MarkDirty;
        internal IShuttleCargoBayUIActions CargoBayActions;
        internal IShuttleCargoBayUnloadUIActions CargoBayUnloadActions;
        internal IShuttleCargoLoadUIActions CargoLoadActions;
        internal IShuttleCargoStackTransferUIActions CargoStackTransferActions;
        internal IShuttleCargoStackUnloadUIActions CargoStackUnloadActions;
        internal IShuttleExternalRuntimeEnablementUIActions ExternalRuntimeEnablementActions;
        internal IShuttleExternalPanelCommandUIActions ExternalPanelCommandActions;
        internal IShuttleExternalModuleDetailsUIActions ExternalModuleDetailsActions;
        internal IShuttleMainSegmentInstallUIActions MainSegmentInstallActions;
        internal IShuttleMainModuleInstallUIActions MainModuleInstallActions;
        internal IShuttleMainModuleReplaceUIActions MainModuleReplaceActions;
        internal IShuttleMainRemovalUIActions MainRemovalActions;
        internal IShuttleMainRemovalWorkerUIActions MainRemovalWorkerActions;
        internal IShuttleMainModuleEnablementUIActions MainModuleEnablementActions;
        internal IShuttleMainSegmentModulesEnablementUIActions MainSegmentModulesEnablementActions;
        internal IShuttleMainAssemblyConstructionUIActions MainAssemblyConstructionActions;
        internal IShuttleSettingsUIActions SettingsActions;
        internal IShuttleCrewLoadedCrewUIActions CrewLoadedCrewActions;
        internal IShuttleCrewHabitatUIActions CrewHabitatActions;
        internal IShuttleCrewJoyUIActions CrewJoyActions;
        internal IShuttleCrewMedicalPatientUIActions CrewMedicalPatientActions;
        internal IShuttleCrewMechChargerUIActions CrewMechChargerActions;
        internal IShuttleCrewUnloadDialogActions CrewUnloadDialogActions;
        internal IShuttleDefenseShieldUIActions DefenseShieldActions;
        internal IShuttleDefenseWeaponTargetingUIActions DefenseWeaponTargetingActions;
        internal IShuttleDefenseWeaponFireControlUIActions DefenseWeaponFireControlActions;
        internal IShuttleDefenseWeaponAmmoUIActions DefenseWeaponAmmoActions;
        internal IShuttleDefenseWeaponLogisticsUIActions DefenseWeaponLogisticsActions;
        internal IShuttleDefenseWeaponGroupUIActions DefenseWeaponGroupActions;
        internal IShuttleMedicalAdmissionUIActions MedicalAdmissionActions;
        internal IShuttleMedicalOccupantUIActions MedicalOccupantActions;
        internal IShuttleMedicalProcedureUIActions MedicalProcedureActions;
        internal IShuttleMedicalTreatmentUIActions MedicalTreatmentActions;
        internal IShuttleMedicalSurgeryUIActions MedicalSurgeryActions;
        internal IShuttlePrisonCellUIActions PrisonCellActions;
        internal IShuttleProcessingUIActions ProcessingActions;
        internal Action MarkExternalModuleCommandCompleted;
    }
}
