using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Core-owned adapter from the controller root to narrow UI read/write ports.
    /// UI and command handlers receive only these interfaces, not the concrete controller.
    /// </summary>
    internal sealed class ShuttleControllerUIPort :
        IShuttleCommandExecutor,
        IShuttleControlReadPort,
        IShuttleCachedControlReadPort,
        IShuttleModuleInstallEligibilityReadPort,
        IShuttleMedicalBayActionPort,
        IShuttleWeaponBayReadPort,
        IShuttleAutoWorkTableReadPort,
        IShuttleCargoReadPort,
        IShuttleLoadCargoReadPort,
        IShuttleCargoUnloadReadPort,
        IShuttleAssemblyEventReadPort,
        IShuttleShieldReadPort,
        IShuttleShieldCommandPort,
        IShuttleExternalModuleUIReadPort,
        IShuttleProfileInvalidationPort,
        IShuttlePerformanceCaptureControlPort,
        IShuttlePerformanceDashboardReadPort
    {
        private readonly ShuttleController controller;
        private readonly ShuttleControlReadModelBuilder controlReadModelBuilder;
        private readonly ExternalModuleUIReadModelBuilder externalModuleUIReadModelBuilder;
        private readonly ShuttleCargoRegionSettingsSnapshotBuilder cargoRegionSettingsSnapshotBuilder;
        private ShuttleCargoSnapshot cachedUIReadinessCargoSnapshot;
        private IReadOnlyList<ProfileBuildIssue> cachedUIReadinessIssues;
        private int cachedUIReadinessProfileRevision = int.MinValue;

        public ShuttleControllerUIPort(ShuttleController controller)
        {
            this.controller = controller;
            this.controlReadModelBuilder = new ShuttleControlReadModelBuilder();
            this.externalModuleUIReadModelBuilder = new ExternalModuleUIReadModelBuilder();
            this.cargoRegionSettingsSnapshotBuilder = new ShuttleCargoRegionSettingsSnapshotBuilder();
        }

        public ShuttleCommandResult Execute(IShuttleCommand command)
        {
            return this.controller != null
                ? this.controller.Execute(command)
                : ShuttleCommandResult.Failed("CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString());
        }

        public bool CanInstallModuleForUI(
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            failureReason = null;
            if (this.controller == null)
            {
                failureReason = "CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString();
                return false;
            }

            return this.controller.CanInstallModuleForUI(
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out failureReason);
        }

        public ShuttleCommandResult SetShieldRadius(string moduleInstanceID, float selectedRadius)
        {
            return this.Execute(new SetShuttleShieldRadiusCommand(moduleInstanceID, selectedRadius));
        }

        public ShuttleCommandResult SetSurfaceShieldRechargeSpeed(float rechargeSpeedMultiplier)
        {
            return this.Execute(new SetShuttleSurfaceShieldRechargeSpeedCommand(rechargeSpeedMultiplier));
        }

        public bool TryDequeueAssemblyEvent(out ShuttleAssemblyEvent assemblyEvent)
        {
            if (this.controller == null)
            {
                assemblyEvent = null;
                return false;
            }

            return this.controller.TryDequeueAssemblyEvent(out assemblyEvent);
        }

        public void MarkCombatTuningSettingsChanged()
        {
            if (this.controller != null)
            {
                this.controller.MarkProfileDirtyForSettings(ProfileDirtyReason.CombatTuningChanged);
            }
        }

        public void MarkOtherSettingsChanged()
        {
            ShuttleProfileSettingsInvalidationService.MarkLoadedShuttlesDirty(
                this.controller,
                ProfileDirtyReason.OtherSettingsChanged);
        }

        public ThingWithComps GetMedicalBayActionHost()
        {
            return this.controller != null ? this.controller.ShuttleHost : null;
        }

        public ShuttleControlReadModel BuildControlReadModel()
        {
            return this.BuildControlReadModel(
                null,
                false,
                ShuttleControlReadDetail.All);
        }

        ShuttleControlReadModel IShuttleCachedControlReadPort.BuildControlReadModel(
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleControlReadDetail detail)
        {
            return this.BuildControlReadModel(cargoSnapshot, true, detail);
        }

        private ShuttleControlReadModel BuildControlReadModel(
            ShuttleCargoSnapshot cargoSnapshot,
            bool useCachedCargoSnapshot,
            ShuttleControlReadDetail detail)
        {
            if (this.controller == null)
            {
                return new ShuttleControlReadModel();
            }

            ShuttleProfile profile = this.controller.GetProfileForRead();
            string shuttleLabel = this.controller.ShuttleHost != null
                ? this.controller.ShuttleHost.LabelCap
                : "CT_Shuttle_UI_DefaultShuttleLabel".Translate().ToString();
            ShuttleAssemblyReadSnapshot assemblySnapshot = this.controller.BuildAssemblyReadSnapshot();
            IReadOnlyList<ProfileBuildIssue> readinessIssues =
                useCachedCargoSnapshot
                    ? this.GetUIReadinessIssues(profile, cargoSnapshot)
                    : this.controller.BuildAssemblyReadinessIssues();
            IReadOnlyList<ShuttleDeveloperDiagnosticModel> developerDiagnostics =
                this.controller.BuildDeveloperDiagnostics();

            ShuttleControlReadModel model = this.controlReadModelBuilder.Build(
                profile,
                this.controller.IsProfileDirty,
                shuttleLabel,
                assemblySnapshot,
                this.controller.BuildPowerRuntimeSnapshot(),
                this.controller.GetLaunchRuntimeState(),
                this.controller.AssemblyState,
                readinessIssues,
                developerDiagnostics);
            model.Habitat = this.controller.BuildHabitatReadModel();
            model.MedicalBay = this.controller.BuildMedicalBayReadModel(
                (detail & ShuttleControlReadDetail.MedicalActions) != 0);
            model.MechCharger = this.controller.BuildMechChargerReadModel();
            model.PrisonCell = this.controller.BuildPrisonCellReadModel(
                (detail & ShuttleControlReadDetail.PrisonCellActions) != 0);
            model.CargoSupply = this.controller.BuildCargoSupplySnapshotForRead();
            this.ApplyExternalRuntimeMass(model);
            return model;
        }

        private IReadOnlyList<ProfileBuildIssue> GetUIReadinessIssues(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            int profileRevision = profile != null ? profile.Revision : 0;
            if (this.cachedUIReadinessIssues == null ||
                !object.ReferenceEquals(
                    this.cachedUIReadinessCargoSnapshot,
                    cargoSnapshot) ||
                this.cachedUIReadinessProfileRevision != profileRevision)
            {
                this.cachedUIReadinessIssues =
                    this.controller.BuildAssemblyReadinessIssues(profile, cargoSnapshot);
                this.cachedUIReadinessCargoSnapshot = cargoSnapshot;
                this.cachedUIReadinessProfileRevision = profileRevision;
            }

            return this.cachedUIReadinessIssues;
        }

        private void ApplyExternalRuntimeMass(ShuttleControlReadModel model)
        {
            if (model == null || this.controller == null)
            {
                return;
            }

            ShuttleRuntimeMassContributionSnapshot externalMass =
                this.controller.BuildExternalRuntimeMassContributionSnapshotForRead();
            if (externalMass == null || externalMass.TotalMassKg <= 0f)
            {
                return;
            }

            model.ExternalRuntimeMassKg = externalMass.TotalMassKg;
            model.TotalMass += externalMass.TotalMassKg;
            model.RemainingMassCapacity -= externalMass.TotalMassKg;
            if (model.RemainingMassCapacity < 0f)
            {
                model.RemainingMassCapacity = 0f;
            }
        }

        internal IReadOnlyList<ExternalModuleUIReadModel> BuildExternalModuleUIReadModels()
        {
            if (this.controller == null)
            {
                return new List<ExternalModuleUIReadModel>();
            }

            return this.externalModuleUIReadModelBuilder.Build(
                this.controller.AssemblyState,
                this.controller.GetLaunchRuntimeState());
        }

        internal IReadOnlyList<ExternalModuleUIReadModel> BuildExternalModuleUIBadgeReadModels()
        {
            if (this.controller == null)
            {
                return new List<ExternalModuleUIReadModel>();
            }

            return this.externalModuleUIReadModelBuilder.BuildBadgeModels(
                this.controller.AssemblyState,
                this.controller.GetLaunchRuntimeState());
        }

        internal ExternalModuleUIRuntimeSummary BuildExternalModuleUIRuntimeSummary()
        {
            if (this.controller == null)
            {
                return new ExternalModuleUIRuntimeSummary();
            }

            return this.externalModuleUIReadModelBuilder.BuildSummary(
                this.controller.AssemblyState,
                this.controller.GetLaunchRuntimeState());
        }

        IReadOnlyList<ExternalModuleUIReadModel> IShuttleExternalModuleUIReadPort.BuildExternalModuleUIReadModels()
        {
            return this.BuildExternalModuleUIReadModels();
        }

        IReadOnlyList<ExternalModuleUIReadModel> IShuttleExternalModuleUIReadPort.BuildExternalModuleUIBadgeReadModels()
        {
            return this.BuildExternalModuleUIBadgeReadModels();
        }

        ExternalModuleUIRuntimeSummary IShuttleExternalModuleUIReadPort.BuildExternalModuleUIRuntimeSummary()
        {
            return this.BuildExternalModuleUIRuntimeSummary();
        }

        ShuttleExternalModulePanelContext IShuttleExternalModuleUIReadPort.BuildExternalModulePanelContext(
            string moduleInstanceID,
            string runtimeSystemKey,
            string panelKey)
        {
            IShuttleExternalModuleUIReadPort readPort = this.controller as IShuttleExternalModuleUIReadPort;
            return readPort != null
                ? readPort.BuildExternalModulePanelContext(moduleInstanceID, runtimeSystemKey, panelKey)
                : null;
        }

        public ShuttleWeaponBayReadModel BuildWeaponBayReadModel()
        {
            return this.controller != null
                ? this.controller.BuildWeaponBayReadModel()
                : ShuttleWeaponBayReadModel.Empty;
        }

        public ShuttleAutoWorkTableReadModel BuildAutoWorkTableReadModel()
        {
            return this.controller != null
                ? this.controller.BuildAutoWorkTableReadModel()
                : new ShuttleAutoWorkTableReadModel();
        }

        public ShuttleCargoSnapshot BuildCargoSnapshot()
        {
            if (this.controller == null)
            {
                return new ShuttleCargoSnapshot();
            }

            return this.controller.BuildCargoSnapshot();
        }

        public ShuttleCargoUnloadProgressSnapshot BuildCargoUnloadProgressSnapshot()
        {
            return this.controller != null
                ? this.controller.BuildCargoUnloadProgressSnapshot()
                : new ShuttleCargoUnloadProgressSnapshot();
        }

        public ShuttleLoadCargoReadModel BuildLoadCargoReadModel()
        {
            if (this.controller == null)
            {
                return new ShuttleLoadCargoReadModel();
            }

            return this.controller.BuildLoadCargoReadModel();
        }

        public ShuttleLoadCargoReadModel RefreshLoadCargoReadModel()
        {
            if (this.controller == null)
            {
                return new ShuttleLoadCargoReadModel();
            }

            return this.controller.BuildLoadCargoReadModel(true);
        }

        public bool TryGetShieldSettings(
            string moduleInstanceID,
            out ShuttleShieldSettingsSnapshot snapshot)
        {
            snapshot = null;
            return this.controller != null &&
                this.controller.TryGetShieldSettings(moduleInstanceID, out snapshot);
        }

        public bool HasQueuedLoads()
        {
            return this.controller != null && this.controller.HasQueuedLoadsForLoadCargo();
        }

        public float GetAvailableMass()
        {
            return this.controller != null ? this.controller.GetLoadCargoAvailableMass() : 0f;
        }

        public int GetSelectedCount(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            return this.controller != null
                ? this.controller.GetLoadCargoSelectedCount(passengerTransferables, cargoTransferables)
                : 0;
        }

        public float GetSelectedMassKg(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            return this.controller != null
                ? this.controller.GetLoadCargoSelectedMassKg(passengerTransferables, cargoTransferables)
                : 0f;
        }

        public ShuttleCargoRegionReadModel GetCargoRegionSettings(int regionIndex, int regionCount)
        {
            if (regionCount <= 0 || regionIndex < 0 || regionIndex >= regionCount)
            {
                return null;
            }

            if (this.controller == null)
            {
                return null;
            }

            ShuttleCargoRegionConfigState config = this.controller.CargoRegionConfig;
            if (config == null)
            {
                return null;
            }

            ShuttleCargoRegionSettings settings = config.GetRegionOrNull(regionIndex);
            ShuttleCargoRegionSettingsSnapshot snapshot =
                this.cargoRegionSettingsSnapshotBuilder.Build(regionIndex, settings);
            return ShuttleCargoRegionReadModel.FromSnapshot(snapshot);
        }

        int IShuttlePerformanceCaptureControlPort.BeginPerformanceCapture()
        {
            return this.controller != null
                ? this.controller.BeginPerformanceCapture()
                : 0;
        }

        void IShuttlePerformanceCaptureControlPort.EndPerformanceCapture(
            int captureLeaseID)
        {
            if (this.controller != null)
            {
                this.controller.EndPerformanceCapture(captureLeaseID);
            }
        }

        void IShuttlePerformanceCaptureControlPort.ResetPerformanceCapture(
            int captureLeaseID)
        {
            if (this.controller != null)
            {
                this.controller.ResetPerformanceCapture(captureLeaseID);
            }
        }

        ShuttlePerformanceDashboardReadModel
            IShuttlePerformanceDashboardReadPort.GetPerformanceDashboardReadModel()
        {
            return this.controller != null
                ? this.controller.GetPerformanceDashboardReadModel()
                : ShuttlePerformanceDashboardReadModel.Empty;
        }
    }
}
