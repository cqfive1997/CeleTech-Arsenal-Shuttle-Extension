using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation.External
{
    internal sealed class ExternalModuleUIReadModelBuilder
    {
        internal List<ExternalModuleUIReadModel> Build(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            return this.Build(assemblyState, runtimeState, true);
        }

        internal List<ExternalModuleUIReadModel> BuildBadgeModels(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            return this.Build(assemblyState, runtimeState, false);
        }

        private List<ExternalModuleUIReadModel> Build(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            bool includePanelCounts)
        {
            List<ExternalModuleUIReadModel> models = new List<ExternalModuleUIReadModel>();
            if (assemblyState == null)
            {
                return models;
            }

            assemblyState.EnsureInitialized();
            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            for (int i = 0; modules != null && i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                if (module == null)
                {
                    continue;
                }

                ShuttleModuleBaseDef moduleDef = module.ModuleDef;
                List<string> runtimeKeys =
                    ExternalRuntimeBindingUtility.GetRuntimeSystemKeys(moduleDef);
                for (int keyIndex = 0; runtimeKeys != null && keyIndex < runtimeKeys.Count; keyIndex++)
                {
                    string runtimeKey = runtimeKeys[keyIndex];
                    if (string.IsNullOrWhiteSpace(runtimeKey))
                    {
                        continue;
                    }

                    string normalizedRuntimeKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeKey);
                    if (string.IsNullOrEmpty(normalizedRuntimeKey))
                    {
                        continue;
                    }

                    models.Add(this.BuildModuleRuntimeModel(
                        module,
                        moduleDef,
                        runtimeState,
                        normalizedRuntimeKey,
                        includePanelCounts));
                }
            }

            return models;
        }

        internal ExternalModuleUIRuntimeSummary BuildSummary(
            IReadOnlyList<ExternalModuleUIReadModel> models)
        {
            ExternalModuleUIRuntimeSummary summary = new ExternalModuleUIRuntimeSummary();
            if (models == null)
            {
                return summary;
            }

            summary.ModuleCount = models.Count;
            for (int i = 0; i < models.Count; i++)
            {
                ExternalModuleUIReadModel model = models[i];
                if (model == null)
                {
                    continue;
                }

                if (model.RuntimeEnabled)
                {
                    summary.EnabledCount++;
                }
                else
                {
                    summary.DisabledCount++;
                }

                if (!model.RuntimeRegistered)
                {
                    summary.MissingRuntimeCount++;
                }

                summary.TotalIdlePowerDrawWatts += model.IdlePowerDrawWatts;
                summary.TotalLastKnownPowerDemandWatts += model.LastKnownPowerDemandWatts;
                summary.TotalAverageTickCostMs += model.AverageTickCostMs;
                summary.TotalStoredEnergyConsumedWd += model.TotalStoredEnergyConsumedWd;
                if (model.PeakTickCostMs > summary.PeakTickCostMs)
                {
                    summary.PeakTickCostMs = model.PeakTickCostMs;
                }
            }

            return summary;
        }

        internal ExternalModuleUIRuntimeSummary BuildSummary(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            return this.BuildSummary(this.Build(assemblyState, runtimeState));
        }

        private ExternalModuleUIReadModel BuildModuleRuntimeModel(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef,
            ShuttleRuntimeState runtimeState,
            string runtimeKey,
            bool includePanelCounts)
        {
            ExternalRuntimeRegistration registration;
            bool runtimeRegistered =
                ExternalShuttleRuntimeRegistry.TryResolve(runtimeKey, out registration);

            IShuttleModuleRuntimeState state = null;
            bool runtimeStateExists = runtimeState != null &&
                runtimeState.Modules != null &&
                runtimeState.Modules.TryGetState(
                    module.ModuleInstanceID,
                    runtimeKey,
                    out state);
            ExternalModuleRuntimeState externalState = state as ExternalModuleRuntimeState;
            bool runtimeStateEnvelopeValid = !runtimeStateExists || externalState != null;
            bool runtimeFailed = externalState != null && externalState.HasRuntimeFailure;
            bool runtimeEnabled = runtimeRegistered &&
                runtimeStateExists &&
                runtimeStateEnvelopeValid &&
                !runtimeFailed &&
                externalState != null &&
                ExternalRuntimeStateUtility.IsRuntimeEnabled(externalState);
            int externalPanelCount =
                includePanelCounts
                    ? ExternalModulePanelRegistry.GetRegistrationsForRuntimeKey(runtimeKey).Count
                    : 0;

            ExternalModuleUIReadModel model = new ExternalModuleUIReadModel();
            model.ModuleInstanceID = module.ModuleInstanceID;
            model.ModuleDefName = module.moduleDefName;
            model.RuntimeSystemKey = runtimeKey;
            model.Label = this.GetLabel(module, moduleDef);
            model.ModuleTypeID = moduleDef != null ? moduleDef.moduleTypeID : null;
            model.Category = this.GetCategory(model.ModuleTypeID);
            model.RuntimeRegistered = runtimeRegistered;
            model.RuntimeStateExists = runtimeStateExists;
            model.RuntimeStateEnvelopeValid = runtimeStateEnvelopeValid;
            model.RuntimeEnabled = runtimeEnabled;
            model.HasExternalPanel = externalPanelCount > 0;
            model.ExternalPanelCount = externalPanelCount;
            model.IdlePowerDrawWatts = moduleDef != null ? moduleDef.idlePowerDrawWatts : 0f;
            this.ApplyMetrics(model);
            this.ApplyStatus(
                model,
                runtimeRegistered,
                runtimeStateExists,
                runtimeStateEnvelopeValid,
                runtimeEnabled,
                externalState);
            return model;
        }

        private void ApplyMetrics(ExternalModuleUIReadModel model)
        {
            if (model == null)
            {
                return;
            }

            ExternalRuntimeMetricsSnapshot snapshot =
                ExternalRuntimeMetricsRegistry.GetSnapshot(
                    model.ModuleInstanceID,
                    model.RuntimeSystemKey);
            if (snapshot == null)
            {
                return;
            }

            model.LastKnownPowerDemandWatts = snapshot.LastKnownPowerDemandWatts;
            model.AverageTickCostMs = snapshot.AverageTickCostMs;
            model.PeakTickCostMs = snapshot.PeakTickCostMs;
            model.TotalStoredEnergyConsumedWd = snapshot.TotalStoredEnergyConsumedWd;
            model.AverageReconcileCostMs = snapshot.AverageReconcileCostMs;
            model.PeakReconcileCostMs = snapshot.PeakReconcileCostMs;
            model.AveragePowerDemandCostMs = snapshot.AveragePowerDemandCostMs;
            model.PeakPowerDemandCostMs = snapshot.PeakPowerDemandCostMs;
        }

        private void ApplyStatus(
            ExternalModuleUIReadModel model,
            bool runtimeRegistered,
            bool runtimeStateExists,
            bool runtimeStateEnvelopeValid,
            bool runtimeEnabled,
            ExternalModuleRuntimeState externalState)
        {
            if (model == null)
            {
                return;
            }

            model.DisabledReason = null;
            if (!runtimeRegistered)
            {
                model.StatusText = Tr("CT_Shuttle_ExternalRuntime_Status_MissingRuntime");
                return;
            }

            if (!runtimeStateExists)
            {
                model.StatusText = Tr("CT_Shuttle_ExternalRuntime_Status_StateMissing");
                return;
            }

            if (!runtimeStateEnvelopeValid)
            {
                model.StatusText = Tr("CT_Shuttle_ExternalRuntime_Status_InvalidState");
                model.DisabledReason = Tr("CT_Shuttle_ExternalRuntime_Reason_InvalidStateEnvelope");
                return;
            }

            if (externalState != null && externalState.HasRuntimeFailure)
            {
                model.StatusText = Tr("CT_Shuttle_ExternalRuntime_Status_RuntimeFailed");
                model.DisabledReason = !string.IsNullOrEmpty(externalState.LastFailureMessage)
                    ? externalState.LastFailureMessage
                    : Tr("CT_Shuttle_ExternalRuntime_Reason_InitMigrationFailed");
                return;
            }

            if (!runtimeEnabled)
            {
                model.StatusText = Tr("CT_Shuttle_ExternalRuntime_Status_Disabled");
                model.DisabledReason = Tr("CT_Shuttle_ExternalRuntime_Reason_DisabledByCommand");
                return;
            }

            model.StatusText = Tr("CT_Shuttle_ExternalRuntime_Status_Running");
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }

        private string GetLabel(ShuttleModule module, ShuttleModuleBaseDef moduleDef)
        {
            string fallback = module != null && !string.IsNullOrEmpty(module.moduleDefName)
                ? module.moduleDefName
                : (module != null ? module.ModuleInstanceID : string.Empty);
            return ExternalRuntimeMetadataResolver.ResolveModuleLabel(moduleDef, fallback);
        }

        private ExternalModuleUICategory GetCategory(string moduleTypeID)
        {
            if (moduleTypeID == "scanner" ||
                moduleTypeID == "navigation" ||
                moduleTypeID == "fire-control")
            {
                return ExternalModuleUICategory.ControlAndSensing;
            }

            if (moduleTypeID == "shield" ||
                moduleTypeID == "weapon" ||
                moduleTypeID == "armor")
            {
                return ExternalModuleUICategory.DefenseAndTactical;
            }

            if (moduleTypeID == "production" ||
                moduleTypeID == "medical-bay" ||
                moduleTypeID == "cargo" ||
                moduleTypeID == "support" ||
                moduleTypeID == "mech-charger")
            {
                return ExternalModuleUICategory.ProductionAndSupport;
            }

            return ExternalModuleUICategory.Other;
        }
    }
}
