using System;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Shared helper for runtime dispatchers. It centralizes context creation and guarded
    /// invocation so lifecycle, power, and launch paths keep the same module identity rules.
    /// </summary>
    internal sealed class ShuttleRuntimeDispatchSupport
    {
        internal ShuttleModuleRuntimeContext CreateRuntimeContext(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleModuleRuntimeState state,
            IShuttleStoredEnergySink storedEnergySink,
            IShuttlePowerDemandSink powerDemandSink,
            IShuttleCargoResourceBroker cargoResourceBroker,
            int ticksGame,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService = null,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver = null,
            IShuttleCargoPostDepositRouter postDepositCargoRouter = null)
        {
            ShuttleSegment parentSegment = assemblyState != null
                ? assemblyState.GetSegment(module.ParentSegmentInstanceID)
                : null;

            return new ShuttleModuleRuntimeContext(
                host,
                profile,
                runtimeState,
                module,
                parentSegment,
                state,
                storedEnergySink,
                powerDemandSink,
                cargoResourceBroker,
                ticksGame,
                refrigeratedCargoTransferService,
                refrigeratedAutoTransferConfigResolver,
                postDepositCargoRouter);
        }

        internal ShuttleModuleRuntimeContext CreatePowerDemandRuntimeContext(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleRuntimePowerDemandBinding binding,
            IShuttlePowerDemandSink powerDemandSink,
            int ticksGame)
        {
            return new ShuttleModuleRuntimeContext(
                host,
                profile,
                runtimeState,
                binding != null ? binding.ModuleInstanceID : null,
                binding != null ? binding.ModuleDefName : null,
                binding != null ? binding.ModuleDef : null,
                binding != null ? binding.ModuleInstanceConfig : null,
                binding != null ? binding.ParentSegmentInstanceID : null,
                binding != null ? binding.ParentSegmentDefName : null,
                binding != null ? binding.ParentSegmentDef : null,
                binding != null ? binding.ParentSlotID : null,
                binding != null && binding.IsEnabled,
                binding != null ? binding.ModuleRuntimeState : null,
                null,
                powerDemandSink,
                null,
                ticksGame);
        }

        internal ShuttleModuleLaunchContext CreateLaunchContext(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleLaunchModuleRecord moduleRecord,
            IShuttleModuleRuntimeState state)
        {
            ShuttleLaunchSegmentRecord parentSegmentRecord = assemblySnapshot != null && moduleRecord != null
                ? assemblySnapshot.GetSegment(moduleRecord.ParentSegmentInstanceID)
                : null;

            return new ShuttleModuleLaunchContext(
                moduleRecord,
                parentSegmentRecord,
                state);
        }

        internal ShuttleModulePreLaunchValidationContext CreatePreLaunchValidationContext(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleLaunchModuleRecord moduleRecord,
            IShuttleModuleRuntimeState state)
        {
            ShuttleLaunchSegmentRecord parentSegmentRecord = assemblySnapshot != null && moduleRecord != null
                ? assemblySnapshot.GetSegment(moduleRecord.ParentSegmentInstanceID)
                : null;

            return new ShuttleModulePreLaunchValidationContext(
                moduleRecord,
                parentSegmentRecord,
                new ShuttleModuleRuntimeStateReadOnlyAdapter(state));
        }

        internal IShuttleModuleRuntimeState GetOrCreateState(
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleModuleRuntimeSystem system)
        {
            // State identity is module instance + runtime system key. Do not use module defName
            // here because multiple installed copies of the same def need independent payloads.
            return runtimeState.Modules.GetOrCreateState(
                module.ModuleInstanceID,
                system.RuntimeSystemKey,
                system.CreateState);
        }

        internal bool ShouldTick(IShuttleModuleRuntimeSystem system, int ticksGame)
        {
            // A non-positive interval disables ticking for systems that are reconcile/event only.
            int interval = system.TickInterval;
            return interval > 0 && ticksGame % interval == 0;
        }

        internal void LogMissingState(
            ShuttleModule module,
            IShuttleModuleRuntimeSystem system,
            string actionName)
        {
            Log.WarningOnce("[CeleTech Shuttle] Missing runtime state for " +
                this.GetModuleDebugName(module) + " / " + system.RuntimeSystemKey +
                " during " + actionName + ". Skipping runtime action.",
                this.MakeRuntimeLogHash(
                    "missing-state-" + actionName,
                    module != null ? module.ModuleInstanceID : null,
                    system != null ? system.RuntimeSystemKey : null,
                    null));
        }

        internal bool AppliesToSystem(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            string actionName,
            out string failureReason)
        {
            // Applicability checks are treated like validation. In fail-closed paths the caller
            // propagates failureReason; in notification paths we log and skip the system.
            failureReason = null;

            if (system == null)
            {
                return false;
            }

            try
            {
                return system.AppliesTo(module);
            }
            catch (Exception exception)
            {
                failureReason = "Module runtime applicability check failed during " + actionName +
                    " for " + this.GetModuleDebugName(module) + " / " + system.RuntimeSystemKey + ".";
                Log.ErrorOnce(
                    "[CeleTech Shuttle] " + failureReason + " Exception: " + exception,
                    this.MakeRuntimeLogHash(
                        "applies-to-" + actionName,
                        module != null ? module.ModuleInstanceID : null,
                        system != null ? system.RuntimeSystemKey : null,
                        exception.GetType().FullName + exception.Message));
                return false;
            }
        }

        internal bool AppliesToSystem(
            IShuttleModuleRuntimeSystem system,
            ShuttleLaunchModuleRecord moduleRecord,
            string actionName,
            out string failureReason)
        {
            failureReason = null;

            if (system == null)
            {
                return false;
            }

            try
            {
                return system.AppliesTo(moduleRecord);
            }
            catch (Exception exception)
            {
                failureReason = "Module runtime applicability check failed during " + actionName +
                    " for " + this.GetModuleDebugName(moduleRecord) + " / " + system.RuntimeSystemKey + ".";
                Log.ErrorOnce(
                    "[CeleTech Shuttle] " + failureReason + " Exception: " + exception,
                    this.MakeRuntimeLogHash(
                        "launch-applies-to-" + actionName,
                        moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                        system != null ? system.RuntimeSystemKey : null,
                        exception.GetType().FullName + exception.Message));
                return false;
            }
        }

        internal void TryRunRuntimeAction(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            string actionName,
            Action action)
        {
            // Runtime action failures are logged with module identity so broken built-in systems
            // do not disappear into silent no-ops.
            try
            {
                action();
            }
            catch (Exception exception)
            {
                this.LogRuntimeActionFailure(system, module, actionName, exception);
            }
        }

        internal void TryRunPowerDemandCollection(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            ShuttleModuleRuntimeContext context)
        {
            try
            {
                system.CollectPowerDemand(context);
            }
            catch (Exception exception)
            {
                this.LogRuntimeActionFailure(system, module, "power demand collection", exception);
            }
        }

        internal void TryRunRuntimeTick(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            ShuttleModuleRuntimeContext context)
        {
            try
            {
                system.Tick(context);
            }
            catch (Exception exception)
            {
                this.LogRuntimeActionFailure(system, module, "tick", exception);
            }
        }

        internal void TryRunRuntimeAction(
            IShuttleModuleRuntimeSystem system,
            ShuttleLaunchModuleRecord moduleRecord,
            string actionName,
            Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Log.ErrorOnce("[CeleTech Shuttle] Module runtime " + actionName + " failed for " +
                    this.GetModuleDebugName(moduleRecord) + " / " + system.RuntimeSystemKey +
                    ". Exception: " + exception,
                    this.MakeRuntimeLogHash(
                        "launch-runtime-action-" + actionName,
                        moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                        system != null ? system.RuntimeSystemKey : null,
                        exception.GetType().FullName + exception.Message));
            }
        }

        private void LogRuntimeActionFailure(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            string actionName,
            Exception exception)
        {
            Log.ErrorOnce("[CeleTech Shuttle] Module runtime " + actionName + " failed for " +
                this.GetModuleDebugName(module) + " / " + system.RuntimeSystemKey +
                ". Exception: " + exception,
                this.MakeRuntimeLogHash(
                    "runtime-action-" + actionName,
                    module != null ? module.ModuleInstanceID : null,
                    system != null ? system.RuntimeSystemKey : null,
                    exception.GetType().FullName + exception.Message));
        }

        internal string GetModuleDebugName(ShuttleModule module)
        {
            if (module == null)
            {
                return "null module";
            }

            string defName = !string.IsNullOrEmpty(module.moduleDefName) ? module.moduleDefName : "missing def";
            return module.ModuleInstanceID + " (" + defName + ")";
        }

        internal string GetModuleDebugName(ShuttleLaunchModuleRecord moduleRecord)
        {
            if (moduleRecord == null)
            {
                return "null module";
            }

            string defName = !string.IsNullOrEmpty(moduleRecord.ModuleDefName)
                ? moduleRecord.ModuleDefName
                : "missing def";
            return moduleRecord.ModuleInstanceID + " (" + defName + ")";
        }

        internal int MakeRuntimeLogHash(
            string actionName,
            string moduleInstanceID,
            string runtimeSystemKey,
            string reason)
        {
            unchecked
            {
                int hash = 31;
                hash = (hash * 37) + (actionName != null ? actionName.GetHashCode() : 0);
                hash = (hash * 37) + (moduleInstanceID != null ? moduleInstanceID.GetHashCode() : 0);
                hash = (hash * 37) + (runtimeSystemKey != null ? runtimeSystemKey.GetHashCode() : 0);
                hash = (hash * 37) + (reason != null ? reason.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
