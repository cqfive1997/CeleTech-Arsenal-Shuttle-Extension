using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal ShuttleProfile GetProfileForRead()
        {
            if (!this.profileDirty && this.profile != null)
            {
                return this.profile;
            }

            if (this.assemblyState == null)
            {
                this.assemblyState = new ShuttleAssemblyState();
            }

            // The profile is a pure derived snapshot and is always rebuilt from current assembly truth.
            this.assemblyState.EnsureInitialized();
            this.profileRevision++;
            this.profile = SharedProfileBuilder.Build(this.assemblyState, this.profileRevision, this.pendingDirtyReasons);
            if (this.profile != null && this.profile.Issues != null && this.profile.Issues.Count > 0)
            {
                ShuttleLog.Debug(
                    "ShuttleController",
                    "Built shuttle profile revision " + this.profileRevision + " with " + this.profile.Issues.Count + " build issue(s).");
            }
            this.profileDirty = false;
            this.pendingDirtyReasons = ProfileDirtyReason.None;
            this.assemblyState.ClearDirty(ShuttleDirtyFlags.Profile | ShuttleDirtyFlags.AssemblyTopology);
            return this.profile;
        }

        internal ShuttleLoadCargoReadModel BuildLoadCargoReadModel()
        {
            return ShuttleControllerCargoReadModelFacade.BuildLoadCargoReadModel(this, false);
        }

        internal ShuttleLoadCargoReadModel BuildLoadCargoReadModel(bool forceRefresh)
        {
            return ShuttleControllerCargoReadModelFacade.BuildLoadCargoReadModel(this, forceRefresh);
        }

        internal void InvalidateLoadCargoReadModelCache()
        {
            ShuttleControllerCargoReadModelFacade.InvalidateLoadCargoReadModelCache(this);
        }

        internal ShuttlePowerRuntimeSnapshot BuildPowerRuntimeSnapshot()
        {
            this.EnsureRuntimeState();
            return SharedPowerReadModelBuilder.Build(this.runtimeState);
        }

        internal IReadOnlyList<ShuttleDeveloperDiagnosticModel> BuildDeveloperDiagnostics()
        {
            List<ShuttleDeveloperDiagnosticModel> diagnostics =
                new List<ShuttleDeveloperDiagnosticModel>();
            if (this.developerDiagnostics != null)
            {
                for (int i = 0; i < this.developerDiagnostics.Count; i++)
                {
                    if (diagnostics.Count >= ShuttleControlReadModel.DeveloperDiagnosticsMax)
                    {
                        return diagnostics;
                    }

                    diagnostics.Add(this.developerDiagnostics[i]);
                }
            }

            IReadOnlyList<ShuttleDeveloperDiagnosticModel> performanceDiagnostics =
                this.tickProfiler != null ? this.tickProfiler.BuildDiagnosticsSnapshot() : null;
            if (performanceDiagnostics != null)
            {
                for (int i = 0; i < performanceDiagnostics.Count; i++)
                {
                    if (diagnostics.Count >= ShuttleControlReadModel.DeveloperDiagnosticsMax)
                    {
                        break;
                    }

                    diagnostics.Add(performanceDiagnostics[i]);
                }
            }

            this.AppendExternalRuntimeMassDiagnostics(diagnostics);
            return diagnostics;
        }

        private void AppendExternalRuntimeMassDiagnostics(List<ShuttleDeveloperDiagnosticModel> diagnostics)
        {
            if (!Prefs.DevMode ||
                diagnostics == null ||
                diagnostics.Count >= ShuttleControlReadModel.DeveloperDiagnosticsMax)
            {
                return;
            }

            ShuttleRuntimeMassContributionSnapshot snapshot =
                this.BuildExternalRuntimeMassContributionSnapshot(
                    this.GetProfileForRead(),
                    this.GetTicksGameSafe());
            if (snapshot == null ||
                snapshot.Records == null ||
                snapshot.Records.Count == 0)
            {
                return;
            }

            ShuttleDeveloperDiagnosticModel total = new ShuttleDeveloperDiagnosticModel();
            total.Code = "external-runtime-mass-total";
            total.Message = "External runtime mass total: " + FormatMass(snapshot.TotalMassKg);
            total.Kind = "Runtime";
            total.Severity = "Info";
            total.Scope = "ExternalRuntimeMass";
            total.ReferenceID = "external-runtime-mass";
            total.Tick = this.GetTicksGameSafe();
            total.Tooltip =
                "totalExternalRuntimeMassKg = " + snapshot.TotalMassKg +
                "\ncontributionCount = " + snapshot.Records.Count +
                "\nrevision = " + snapshot.Revision;
            diagnostics.Add(total);

            for (int i = 0; i < snapshot.Records.Count; i++)
            {
                if (diagnostics.Count >= ShuttleControlReadModel.DeveloperDiagnosticsMax)
                {
                    break;
                }

                ShuttleRuntimeMassContributionRecord record = snapshot.Records[i];
                if (record == null)
                {
                    continue;
                }

                string label = !string.IsNullOrEmpty(record.Label)
                    ? record.Label
                    : !string.IsNullOrEmpty(record.DebugLabel)
                        ? record.DebugLabel
                        : !string.IsNullOrEmpty(record.LabelKey)
                            ? record.LabelKey
                            : "external runtime mass";
                ShuttleDeveloperDiagnosticModel diagnostic = new ShuttleDeveloperDiagnosticModel();
                diagnostic.Code = "external-runtime-mass";
                diagnostic.Message = label + ": " + FormatMass(record.MassKg);
                diagnostic.Kind = "Runtime";
                diagnostic.Severity = "Info";
                diagnostic.Scope = "ExternalRuntimeMass";
                diagnostic.ReferenceID = (record.RuntimeSystemKey ?? "-") + "/" +
                    (record.ModuleInstanceID ?? "-");
                diagnostic.Tick = record.Tick;
                diagnostic.Tooltip =
                    "runtimeFullKey = " + (record.RuntimeSystemKey ?? "-") +
                    "\nmoduleInstanceID = " + (record.ModuleInstanceID ?? "-") +
                    "\nmoduleDefName = " + (record.ModuleDefName ?? "-") +
                    "\nmassKg = " + record.MassKg +
                    "\nlabelKey = " + (record.LabelKey ?? "-") +
                    "\nlabel = " + (record.Label ?? "-") +
                    "\ndebugLabel = " + (record.DebugLabel ?? "-") +
                    "\ntotalExternalRuntimeMassKg = " + snapshot.TotalMassKg;
                diagnostics.Add(diagnostic);
            }
        }

        private static string FormatMass(float massKg)
        {
            return massKg.ToString("0.##") + " kg";
        }

        private void AddDeveloperRuntimeDiagnostic(
            string code,
            string message,
            string kind,
            string severity,
            string scope,
            string referenceID,
            float? elapsedMs)
        {
            if (!Prefs.DevMode || this.developerDiagnostics == null)
            {
                return;
            }

            ShuttleDeveloperDiagnosticModel diagnostic = new ShuttleDeveloperDiagnosticModel();
            diagnostic.Code = code;
            diagnostic.Message = message;
            diagnostic.Kind = kind;
            diagnostic.Severity = severity;
            diagnostic.Scope = scope;
            diagnostic.ReferenceID = referenceID;
            diagnostic.Tick = ShuttleTickUtility.TicksGameOrMinusOne();
            diagnostic.ElapsedMs = elapsedMs;
            diagnostic.Tooltip =
                "Code: " + (code ?? "-") +
                "\nScope: " + (scope ?? "-") +
                "\nReference: " + (referenceID ?? "-");
            this.developerDiagnostics.Add(diagnostic);
            while (this.developerDiagnostics.Count > ShuttleControlReadModel.DeveloperDiagnosticsMax)
            {
                this.developerDiagnostics.RemoveAt(0);
            }
        }

        internal bool TryGetShieldSettings(
            string moduleInstanceID,
            out ShuttleShieldSettingsSnapshot snapshot)
        {
            snapshot = null;
            this.EnsureRuntimeState();
            return SharedWeaponBayReadModelBuilder.TryGetShieldSettings(
                this.assemblyState,
                this.runtimeState,
                moduleInstanceID,
                out snapshot);
        }

        internal bool TryGetActiveProjectileInterceptorShield(
            out ShuttleProjectileInterceptorShieldSnapshot snapshot)
        {
            snapshot = null;
            this.EnsureRuntimeState();
            return SharedWeaponBayReadModelBuilder.TryGetActiveProjectileInterceptorShield(
                this.assemblyState,
                this.runtimeState,
                out snapshot);
        }

        internal ShuttleWeaponBayReadModel BuildWeaponBayReadModel()
        {
            this.EnsureRuntimeState();
            ShuttleProfile currentProfile = this.GetProfileForRead();
            IShuttleCargoResourceBroker broker = this.BuildCargoResourceBroker(currentProfile);
            return SharedWeaponBayReadModelBuilder.Build(
                this.assemblyState,
                this.runtimeState,
                this.shuttleHost,
                currentProfile,
                broker);
        }

        internal ShuttleHabitatReadModel BuildHabitatReadModel()
        {
            ShuttleProfile currentProfile = this.GetProfileForRead();
            return SharedHabitatReadModelBuilder.Build(currentProfile, this.assemblyState, this.shuttleHost);
        }

        internal ShuttleMedicalBayReadModel BuildMedicalBayReadModel()
        {
            return this.BuildMedicalBayReadModel(true);
        }

        internal ShuttleMedicalBayReadModel BuildMedicalBayReadModel(bool includeActionDetails)
        {
            this.EnsureRuntimeState();
            ShuttleProfile currentProfile = this.GetProfileForRead();
            return SharedMedicalBayReadModelBuilder.Build(
                currentProfile,
                this.shuttleHost,
                this.runtimeState,
                includeActionDetails);
        }

        internal ShuttleMechChargerReadModel BuildMechChargerReadModel()
        {
            ShuttleProfile currentProfile = this.GetProfileForRead();
            return SharedMechChargerReadModelBuilder.Build(currentProfile, this.shuttleHost);
        }

        internal ShuttlePrisonCellReadModel BuildPrisonCellReadModel()
        {
            return this.BuildPrisonCellReadModel(true);
        }

        internal ShuttlePrisonCellReadModel BuildPrisonCellReadModel(bool includeActionDetails)
        {
            ShuttleProfile currentProfile = this.GetProfileForRead();
            return SharedPrisonCellReadModelBuilder.Build(
                currentProfile,
                this.shuttleHost,
                this.assemblyState != null
                    ? this.assemblyState.PrisonCellSupplyConfig
                    : null,
                includeActionDetails);
        }

        internal ShuttleAutoWorkTableReadModel BuildAutoWorkTableReadModel()
        {
            this.EnsureRuntimeState();
            ShuttleProfile currentProfile = this.GetProfileForRead();
            ShuttleCargoInventorySnapshot inventorySnapshot =
                this.GetAutoWorkTableInventorySnapshotForRead(currentProfile);
            return SharedAutoWorkTableReadModelBuilder.Build(
                this.assemblyState,
                this.runtimeState,
                inventorySnapshot);
        }

        internal ShuttleAssemblyReadSnapshot BuildAssemblyReadSnapshot()
        {
            return SharedAssemblyReadSnapshotBuilder.Build(this.assemblyState);
        }

        internal IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssues()
        {
            return ShuttleControllerReadinessIssueBuilder.BuildAssemblyReadinessIssues(this);
        }

        internal IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssues(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            return ShuttleControllerReadinessIssueBuilder.BuildAssemblyReadinessIssues(
                this,
                profile,
                cargoSnapshot);
        }

        internal IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssuesForExternalLaunchRead()
        {
            return ShuttleControllerReadinessIssueBuilder
                .BuildAssemblyReadinessIssuesForExternalLaunchRead(this);
        }

        internal IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssuesForExternalLaunchRead(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            return ShuttleControllerReadinessIssueBuilder
                .BuildAssemblyReadinessIssuesForExternalLaunchRead(
                    this,
                    profile,
                    cargoSnapshot);
        }

        private ShuttleCargoInventorySnapshot GetAutoWorkTableInventorySnapshotForRead(
            ShuttleProfile currentProfile)
        {
            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            int currentRevision = currentProfile != null ? currentProfile.Revision : this.profileRevision;
            if (this.cachedAutoWorkTableInventorySnapshot != null &&
                this.cachedAutoWorkTableInventoryTick == ticksGame &&
                this.cachedAutoWorkTableInventoryProfileRevision == currentRevision)
            {
                return this.cachedAutoWorkTableInventorySnapshot;
            }

            IShuttleCargoResourceBroker broker = this.BuildCargoResourceBroker(currentProfile);
            this.cachedAutoWorkTableInventorySnapshot =
                broker != null ? broker.GetInventorySnapshot() : null;
            this.cachedAutoWorkTableInventoryTick = ticksGame;
            this.cachedAutoWorkTableInventoryProfileRevision = currentRevision;
            return this.cachedAutoWorkTableInventorySnapshot;
        }

        private void InvalidateAutoWorkTableInventorySnapshotCache()
        {
            this.cachedAutoWorkTableInventorySnapshot = null;
            this.cachedAutoWorkTableInventoryTick = int.MinValue;
            this.cachedAutoWorkTableInventoryProfileRevision = -1;
        }

        IReadOnlyList<ExternalModuleUIReadModel> IShuttleExternalModuleUIReadPort.BuildExternalModuleUIBadgeReadModels()
        {
            this.EnsureRuntimeState();
            return SharedExternalModuleUIReadModelBuilder.BuildBadgeModels(
                this.assemblyState,
                this.runtimeState);
        }

        IReadOnlyList<ExternalModuleUIReadModel> IShuttleExternalModuleUIReadPort.BuildExternalModuleUIReadModels()
        {
            this.EnsureRuntimeState();
            return SharedExternalModuleUIReadModelBuilder.Build(
                this.assemblyState,
                this.runtimeState);
        }

        ExternalModuleUIRuntimeSummary IShuttleExternalModuleUIReadPort.BuildExternalModuleUIRuntimeSummary()
        {
            this.EnsureRuntimeState();
            IReadOnlyList<ExternalModuleUIReadModel> models =
                SharedExternalModuleUIReadModelBuilder.Build(
                    this.assemblyState,
                    this.runtimeState);
            return SharedExternalModuleUIReadModelBuilder.BuildSummary(models);
        }

        ShuttleExternalModulePanelContext IShuttleExternalModuleUIReadPort.BuildExternalModulePanelContext(
            string moduleInstanceID,
            string runtimeSystemKey,
            string panelKey)
        {
            this.EnsureRuntimeState();
            if (string.IsNullOrWhiteSpace(moduleInstanceID) ||
                string.IsNullOrWhiteSpace(runtimeSystemKey) ||
                this.assemblyState == null ||
                this.runtimeState == null ||
                this.runtimeState.Modules == null)
            {
                return null;
            }

            string normalizedRuntimeKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
            if (string.IsNullOrEmpty(normalizedRuntimeKey))
            {
                return null;
            }

            ShuttleModule module = this.assemblyState.GetModule(moduleInstanceID.Trim());
            if (module == null ||
                !ExternalRuntimeBindingUtility.ModuleHasRuntimeKey(module, normalizedRuntimeKey))
            {
                return null;
            }

            IShuttleModuleRuntimeState state;
            if (!this.runtimeState.Modules.TryGetState(
                module.ModuleInstanceID,
                normalizedRuntimeKey,
                out state))
            {
                return null;
            }

            ExternalModuleRuntimeState externalState = state as ExternalModuleRuntimeState;
            if (externalState == null)
            {
                return null;
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            return ExternalModulePanelContextFactory.Create(
                !string.IsNullOrWhiteSpace(panelKey) ? panelKey.Trim() : null,
                normalizedRuntimeKey,
                module,
                externalState,
                ExternalRuntimeStateUtility.IsRuntimeEnabled(externalState),
                ticksGame);
        }
    }
}
