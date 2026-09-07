using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    /// <summary>
    /// V3 facade over the neutral read-model cache. It fills page draw context
    /// without owning page-specific projection or command behavior.
    /// </summary>
    internal sealed class ShuttleUIReadModelHub :
        IShuttleControlUIReadModelSource,
        IShuttleControlUIReadModelInvalidationPort,
        IShuttlePageReadModelContextBinder
    {
        private readonly ShuttleControlReadModelCache readModelCache;
        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly ShuttleExternalModulesReadModelCache externalModulesReadModelCache;
        private readonly IReadOnlyList<ExternalModuleUIReadModel> emptyExternalModuleModels =
            new List<ExternalModuleUIReadModel>();
        private readonly IReadOnlyList<ShuttleCargoRegionReadModel> emptyCargoRegionSettings =
            new List<ShuttleCargoRegionReadModel>();
        private readonly ShuttleCargoRegionReadModelsBuilder cargoRegionReadModelsBuilder =
            new ShuttleCargoRegionReadModelsBuilder();
        private readonly ShuttlePageInputProviderSet pageInputProviders =
            new ShuttlePageInputProviderSet();
        private readonly ShuttlePawnPresenceSnapshotCache pawnPresenceSnapshotCache =
            new ShuttlePawnPresenceSnapshotCache();
        private readonly ShuttlePawnDynamicStatusSnapshotCache pawnDynamicStatusSnapshotCache =
            new ShuttlePawnDynamicStatusSnapshotCache();
        private readonly ShuttleCurrentIssuesReadModelBuilder currentIssuesReadModelBuilder =
            new ShuttleCurrentIssuesReadModelBuilder();
        private readonly ShuttleLaunchDiagnosticsReadModelBuilder launchDiagnosticsReadModelBuilder =
            new ShuttleLaunchDiagnosticsReadModelBuilder();
        private List<ShuttleIssueReadModel> cachedCurrentIssues;
        private List<ShuttleIssueReadModel> cachedLaunchDiagnostics;
        private ShuttleControlReadModel cachedIssuesControlModel;
        private ShuttleCargoSnapshot cachedIssuesCargoSnapshot;
        private ShuttleWeaponBayReadModel cachedIssuesWeaponBayModel;
        private int cachedIssuesCargoSnapshotRevision = int.MinValue;
        private int cachedIssuesProfileRevision = int.MinValue;
        private bool issuesDirty = true;
        private IReadOnlyList<ShuttleCargoRegionReadModel> cachedCargoRegionSettings;
        private int cachedCargoRegionSettingsCargoRevision = int.MinValue;
        private int cachedCargoRegionSettingsProfileRevision = int.MinValue;
        private int cachedCargoRegionSettingsCount = int.MinValue;
        private bool cargoRegionSettingsDirty = true;

        internal ShuttleUIReadModelHub(
            ShuttleControlReadModelCache readModelCache,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleExternalModuleUIReadPort externalModuleUIReadPort)
        {
            this.readModelCache = readModelCache;
            this.cargoReadPort = cargoReadPort;
            this.externalModulesReadModelCache =
                new ShuttleExternalModulesReadModelCache(externalModuleUIReadPort);
        }

        ShuttleControlReadModel IShuttleControlUIReadModelSource.GetControlModelForUI(
            ShuttleControlPageId page)
        {
            return this.GetControlModelForUI(page);
        }

        void IShuttleControlUIReadModelInvalidationPort.MarkDirty()
        {
            this.MarkDirty();
        }

        void IShuttlePageReadModelContextBinder.FillContext(
            ShuttlePageDrawContext context,
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel)
        {
            this.FillContext(context, page, controlModel);
        }

        internal ShuttleControlReadModel GetControlModelForUI(ShuttleControlPageId page)
        {
            return this.readModelCache != null
                ? this.readModelCache.GetControlModelForUI(this.GetControlReadDetail(page))
                : new ShuttleControlReadModel();
        }

        private ShuttleControlReadDetail GetControlReadDetail(ShuttleControlPageId page)
        {
            if (page == ShuttleControlPageId.Medical)
            {
                return ShuttleControlReadDetail.MedicalActions;
            }

            if (page == ShuttleControlPageId.PrisonCell)
            {
                return ShuttleControlReadDetail.PrisonCellActions;
            }

            return ShuttleControlReadDetail.None;
        }

        internal void MarkDirty()
        {
            if (this.readModelCache != null)
            {
                this.readModelCache.MarkDirty();
            }

            if (this.externalModulesReadModelCache != null)
            {
                this.externalModulesReadModelCache.MarkDirty();
            }

            if (this.pawnPresenceSnapshotCache != null)
            {
                this.pawnPresenceSnapshotCache.MarkDirty();
            }

            if (this.pawnDynamicStatusSnapshotCache != null)
            {
                this.pawnDynamicStatusSnapshotCache.MarkDirty();
            }

            this.issuesDirty = true;
            this.cargoRegionSettingsDirty = true;
        }

        internal void FillContext(
            ShuttlePageDrawContext context,
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel)
        {
            if (context == null)
            {
                return;
            }

            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.HubFillContext))
            {
                // Keep cached read-model construction stable across page renders so
                // repeated draws do not make cargo or weapon reads heavier.
                ShuttleCargoSnapshot cargoSnapshot = this.GetCargoSnapshotForPage(page);
                ShuttleWeaponBayReadModel weaponBayModel = this.GetWeaponBayModelForPage(page);
                ShuttleControlReadModel controlModelForUI = controlModel ?? new ShuttleControlReadModel();
                int cargoSnapshotRevision = this.readModelCache != null
                    ? this.readModelCache.CargoSnapshotRevision
                    : 0;
                ShuttleAutoWorkTableReadModel workTableModel = page == ShuttleControlPageId.Processing
                    ? this.GetAutoWorkTableModelForUI()
                    : null;
                IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings =
                    this.GetCargoRegionSettingsForPage(
                        page,
                        controlModelForUI,
                        cargoSnapshot,
                        cargoSnapshotRevision);
                this.RefreshIssueReadModelCachesIfNeeded(
                    controlModelForUI,
                    cargoSnapshot,
                    weaponBayModel,
                    cargoSnapshotRevision);
                List<ShuttleIssueReadModel> currentIssues =
                    this.cachedCurrentIssues ?? new List<ShuttleIssueReadModel>();
                List<ShuttleIssueReadModel> launchDiagnostics =
                    this.cachedLaunchDiagnostics ?? new List<ShuttleIssueReadModel>();
                IReadOnlyList<ExternalModuleUIReadModel> externalModuleModels =
                    this.GetExternalModuleModelsForPage(page, controlModelForUI, context);
                ShuttlePawnPresenceSnapshot pawnPresenceSnapshot =
                    this.GetPawnPresenceSnapshotForPage(
                        page,
                        controlModelForUI,
                        cargoSnapshot,
                        cargoSnapshotRevision);

                ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot =
                    this.BuildPawnDynamicStatusSnapshot(page, pawnPresenceSnapshot);

                context.ReadModels.ControlModel = controlModelForUI;
                context.ReadModels.CargoSnapshot = cargoSnapshot;
                context.ReadModels.CargoRegionSettings = cargoRegionSettings;
                context.ReadModels.WeaponBayModel = weaponBayModel;
                context.ReadModels.MedicalModel = null;
                context.ReadModels.WorkTableModel = workTableModel;
                context.ReadModels.CurrentIssues = currentIssues;
                context.ReadModels.LaunchDiagnostics = launchDiagnostics;
                context.ReadModels.ExternalModuleModels = externalModuleModels;
                context.ReadModels.PawnPresenceSnapshot = pawnPresenceSnapshot;
                context.ReadModels.PawnDynamicStatusSnapshot = pawnDynamicStatusSnapshot;
                context.ReadModels.CargoSnapshotRevision = cargoSnapshotRevision;
                this.pageInputProviders.FillInputs(
                    context,
                    new ShuttlePageInputBuildContext(
                        controlModelForUI,
                        cargoSnapshot,
                        cargoRegionSettings,
                        weaponBayModel,
                        workTableModel,
                        externalModuleModels,
                        pawnPresenceSnapshot,
                        pawnDynamicStatusSnapshot,
                        cargoSnapshotRevision));
            }

            ShuttleUIProfiler.MaybeLog();
        }

        private ShuttleCargoSnapshot GetCargoSnapshotForPage(ShuttleControlPageId page)
        {
            if (this.readModelCache == null)
            {
                return new ShuttleCargoSnapshot();
            }

            if (page == ShuttleControlPageId.Cargo)
            {
                return this.readModelCache.GetCargoSnapshotForCargoPageUI();
            }

            return this.readModelCache.GetCargoSnapshotForUI(false);
        }

        private IReadOnlyList<ShuttleCargoRegionReadModel> GetCargoRegionSettingsForPage(
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int cargoSnapshotRevision)
        {
            if (page != ShuttleControlPageId.Cargo)
            {
                return this.emptyCargoRegionSettings;
            }

            int profileRevision = controlModel != null ? controlModel.ProfileRevision : 0;
            int regionCount = this.GetCargoRegionCount(controlModel, cargoSnapshot);
            if (!this.cargoRegionSettingsDirty &&
                this.cachedCargoRegionSettings != null &&
                this.cachedCargoRegionSettingsCargoRevision == cargoSnapshotRevision &&
                this.cachedCargoRegionSettingsProfileRevision == profileRevision &&
                this.cachedCargoRegionSettingsCount == regionCount)
            {
                return this.cachedCargoRegionSettings;
            }

            this.cachedCargoRegionSettings =
                this.cargoRegionReadModelsBuilder.Build(
                    controlModel,
                    cargoSnapshot,
                    this.cargoReadPort);
            this.cachedCargoRegionSettingsCargoRevision = cargoSnapshotRevision;
            this.cachedCargoRegionSettingsProfileRevision = profileRevision;
            this.cachedCargoRegionSettingsCount = regionCount;
            this.cargoRegionSettingsDirty = false;
            return this.cachedCargoRegionSettings;
        }

        private int GetCargoRegionCount(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            if (cargoSnapshot != null && cargoSnapshot.CargoRegionCount > 0)
            {
                return cargoSnapshot.CargoRegionCount;
            }

            if (controlModel != null && controlModel.CargoRegionCount > 0u)
            {
                return (int)controlModel.CargoRegionCount;
            }

            return 0;
        }

        private ShuttleWeaponBayReadModel GetWeaponBayModelForPage(ShuttleControlPageId page)
        {
            if (this.readModelCache == null)
            {
                return ShuttleWeaponBayReadModel.Empty;
            }

            bool heavyAllowed = page == ShuttleControlPageId.Defense ||
                page == ShuttleControlPageId.Settings;
            return this.readModelCache.GetWeaponBayModelForUI(heavyAllowed);
        }

        private ShuttleAutoWorkTableReadModel GetAutoWorkTableModelForUI()
        {
            return this.readModelCache != null
                ? this.readModelCache.GetAutoWorkTableModelForUI()
                : new ShuttleAutoWorkTableReadModel();
        }

        private void RefreshIssueReadModelCachesIfNeeded(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleWeaponBayReadModel weaponBayModel,
            int cargoSnapshotRevision)
        {
            int profileRevision = controlModel != null ? controlModel.ProfileRevision : 0;
            if (!this.issuesDirty &&
                this.cachedCurrentIssues != null &&
                this.cachedLaunchDiagnostics != null &&
                object.ReferenceEquals(this.cachedIssuesControlModel, controlModel) &&
                object.ReferenceEquals(this.cachedIssuesCargoSnapshot, cargoSnapshot) &&
                object.ReferenceEquals(this.cachedIssuesWeaponBayModel, weaponBayModel) &&
                this.cachedIssuesCargoSnapshotRevision == cargoSnapshotRevision &&
                this.cachedIssuesProfileRevision == profileRevision)
            {
                return;
            }

            this.cachedCurrentIssues =
                this.currentIssuesReadModelBuilder.Build(
                    controlModel,
                    cargoSnapshot,
                    weaponBayModel);
            this.cachedLaunchDiagnostics =
                this.launchDiagnosticsReadModelBuilder.Build(
                    controlModel,
                    cargoSnapshot,
                    weaponBayModel);
            this.cachedIssuesControlModel = controlModel;
            this.cachedIssuesCargoSnapshot = cargoSnapshot;
            this.cachedIssuesWeaponBayModel = weaponBayModel;
            this.cachedIssuesCargoSnapshotRevision = cargoSnapshotRevision;
            this.cachedIssuesProfileRevision = profileRevision;
            this.issuesDirty = false;
        }

        private IReadOnlyList<ExternalModuleUIReadModel> GetExternalModuleModelsForPage(
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel,
            ShuttlePageDrawContext context)
        {
            if (this.externalModulesReadModelCache == null)
            {
                return this.emptyExternalModuleModels;
            }

            if (page == ShuttleControlPageId.ExternalModules)
            {
                V3ExternalModulesPageState state = GetExternalModulesState(context);
                return this.externalModulesReadModelCache.GetExternalModuleModelsForUI(
                    controlModel,
                    state != null ? state.SelectedModuleInstanceID : null,
                    state != null ? state.SelectedRuntimeSystemKey : null);
            }

            if (page == ShuttleControlPageId.Main)
            {
                return this.externalModulesReadModelCache.GetExternalModuleBadgeModelsForUI(
                    controlModel);
            }

            return this.emptyExternalModuleModels;
        }

        private static V3ExternalModulesPageState GetExternalModulesState(
            ShuttlePageDrawContext context)
        {
            return context != null &&
                context.State != null &&
                context.State.ExternalModules != null
                    ? context.State.ExternalModules
                    : null;
        }

        private ShuttlePawnDynamicStatusSnapshot BuildPawnDynamicStatusSnapshot(
            ShuttleControlPageId page,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            ShuttlePawnPresenceKind includedKinds =
                this.GetPawnDynamicStatusKindsForPage(page);
            if (includedKinds == ShuttlePawnPresenceKind.None)
            {
                return ShuttlePawnDynamicStatusSnapshot.Empty;
            }

            return this.pawnDynamicStatusSnapshotCache.GetSnapshotForUI(
                pawnPresenceSnapshot,
                includedKinds);
        }

        private ShuttlePawnPresenceSnapshot GetPawnPresenceSnapshotForPage(
            ShuttleControlPageId page,
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            int cargoSnapshotRevision)
        {
            if (!ShouldBuildPawnPresenceForPage(page))
            {
                return ShuttlePawnPresenceSnapshot.Empty;
            }

            return this.pawnPresenceSnapshotCache.GetSnapshotForUI(
                controlModel,
                cargoSnapshot,
                cargoSnapshotRevision);
        }

        private static bool ShouldBuildPawnPresenceForPage(
            ShuttleControlPageId page)
        {
            return page == ShuttleControlPageId.Crew ||
                page == ShuttleControlPageId.Medical;
        }

        private ShuttlePawnPresenceKind GetPawnDynamicStatusKindsForPage(
            ShuttleControlPageId page)
        {
            if (page == ShuttleControlPageId.Crew)
            {
                return ShuttlePawnPresenceKind.CockpitOccupant |
                    ShuttlePawnPresenceKind.CargoLoaded |
                    ShuttlePawnPresenceKind.HabitatOccupant |
                    ShuttlePawnPresenceKind.MedicalPatient |
                    ShuttlePawnPresenceKind.PrisonCellOccupant |
                    ShuttlePawnPresenceKind.MechCharging;
            }

            if (page == ShuttleControlPageId.Medical)
            {
                return ShuttlePawnPresenceKind.MedicalPatient |
                    ShuttlePawnPresenceKind.MedicalActiveDoctor;
            }

            return ShuttlePawnPresenceKind.None;
        }
    }
}
