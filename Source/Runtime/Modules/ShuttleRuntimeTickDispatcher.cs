using System;
using System.Collections.Generic;
using System.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal interface IShuttleRuntimeSystemTickProfileSink
    {
        void RecordRuntimeSystemTick(
            string runtimeSystemKey,
            string moduleInstanceID,
            string moduleLabel,
            int tickInterval,
            long elapsedStopwatchTicks);
        void RecordRuntimeTickDispatchSample(ShuttleRuntimeTickDispatchSample sample);
    }

    internal sealed class ShuttleRuntimeTickDispatcher
    {
        private const int StablePlanBindingValidationIntervalTicks = 500;

        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchSupport support;
        private ShuttleRuntimeTickDispatchPlan cachedTickDispatchPlan;

        internal ShuttleRuntimeTickDispatcher(
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            this.registry = registry;
            this.support = support;
        }

        internal void Reconcile(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            IShuttleCargoResourceBroker cargoResourceBroker = null)
        {
            // Reconcile is the normal place to materialize missing module runtime state.
            // Tick and launch validation intentionally do not create state.
            if (assemblyState == null || runtimeState == null)
            {
                return;
            }

            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                if (module == null)
                {
                    continue;
                }

                if (!module.IsEnabled)
                {
                    continue;
                }

                foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
                {
                    string appliesFailureReason;
                    if (!this.support.AppliesToSystem(system, module, "reconcile", out appliesFailureReason))
                    {
                        continue;
                    }

                    IShuttleModuleRuntimeState state = this.support.GetOrCreateState(runtimeState, module, system);
                    if (state == null)
                    {
                        this.support.LogMissingState(module, system, "reconcile");
                        continue;
                    }

                    ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                        host,
                        assemblyState,
                        profile,
                        runtimeState,
                        module,
                        state,
                        storedEnergySink,
                        null,
                        cargoResourceBroker,
                        ticksGame);

                    this.support.TryRunRuntimeAction(system, module, "reconcile", () => system.Reconcile(context));
                }
            }

            this.cachedTickDispatchPlan = null;
        }

        internal void Tick(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            IShuttleCargoResourceBroker cargoResourceBroker = null,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService = null,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver = null,
            IShuttleCargoPostDepositRouter postDepositCargoRouter = null,
            IShuttleRuntimeSystemTickProfileSink runtimeSystemTickProfileSink = null)
        {
            // Tick uses only existing state so a broken factory cannot spam creation failures.
            if (assemblyState == null || runtimeState == null)
            {
                return;
            }

            ShuttleRuntimeTickDispatchDiagnostics diagnostics =
                ShuttleRuntimeTickDispatchDiagnostics.Create(
                    runtimeSystemTickProfileSink,
                    ticksGame);
            diagnostics.Begin();
            try
            {
                long planStart = diagnostics.StartSection();
                ShuttleRuntimeTickDispatchPlan plan = this.GetOrBuildTickDispatchPlan(
                    host,
                    assemblyState,
                    runtimeState,
                    profile);
                diagnostics.RecordPlan(planStart);
                IReadOnlyList<ShuttleRuntimeTickBinding> bindings = plan.TickBindings;
                bool rebuiltForInvalidBinding = false;
                bool validateBindingsThisTick =
                    ShouldRunStablePlanBindingValidation(ticksGame);
                IShuttleModuleRuntimeSystem lastSystem = null;
                bool lastSystemShouldTick = false;
                for (int i = 0; i < bindings.Count; i++)
                {
                    long bindingStart = diagnostics.StartSection();
                    ShuttleRuntimeTickBinding binding = bindings[i];
                    if (validateBindingsThisTick &&
                        !ShuttleRuntimeTickDispatchPlan.IsBindingStillValid(binding, runtimeState))
                    {
                        if (!rebuiltForInvalidBinding)
                        {
                            this.cachedTickDispatchPlan = ShuttleRuntimeTickDispatchPlan.Build(
                                host,
                                assemblyState,
                                runtimeState,
                                profile,
                                this.registry,
                                this.support);
                            bindings = this.cachedTickDispatchPlan.TickBindings;
                            rebuiltForInvalidBinding = true;
                            lastSystem = null;
                            lastSystemShouldTick = false;
                            i = -1;
                        }

                        diagnostics.RecordBinding(bindingStart);
                        continue;
                    }

                    IShuttleModuleRuntimeSystem system = binding.System;
                    if (!object.ReferenceEquals(system, lastSystem))
                    {
                        lastSystem = system;
                        lastSystemShouldTick = this.support.ShouldTick(system, ticksGame);
                    }

                    if (!lastSystemShouldTick)
                    {
                        diagnostics.RecordBinding(bindingStart);
                        continue;
                    }

                    diagnostics.RecordBinding(bindingStart);
                    long contextStart = diagnostics.StartSection();
                    ShuttleModuleRuntimeContext context = binding.AcquireTickContext(
                        this.support,
                        host,
                        assemblyState,
                        profile,
                        runtimeState,
                        storedEnergySink,
                        cargoResourceBroker,
                        ticksGame,
                        refrigeratedCargoTransferService,
                        refrigeratedAutoTransferConfigResolver,
                        postDepositCargoRouter);
                    diagnostics.RecordContext(contextStart);
                    if (context == null)
                    {
                        continue;
                    }

                    if (runtimeSystemTickProfileSink != null)
                    {
                        long runtimeSystemTickStart = Stopwatch.GetTimestamp();
                        try
                        {
                            this.support.TryRunRuntimeTick(
                                system,
                                binding.Module,
                                context);
                        }
                        finally
                        {
                            long runtimeSystemElapsedTicks =
                                Stopwatch.GetTimestamp() - runtimeSystemTickStart;
                            diagnostics.RecordRuntimeSystem(runtimeSystemElapsedTicks);
                            runtimeSystemTickProfileSink.RecordRuntimeSystemTick(
                                binding.RuntimeSystemProfileKey,
                                binding.ModuleInstanceID,
                                binding.ModuleLabel,
                                binding.TickInterval,
                                runtimeSystemElapsedTicks);
                        }
                    }
                    else
                    {
                        this.support.TryRunRuntimeTick(
                            system,
                            binding.Module,
                            context);
                    }
                }
            }
            finally
            {
                diagnostics.Finish();
            }
        }

        private ShuttleRuntimeTickDispatchPlan GetOrBuildTickDispatchPlan(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile)
        {
            if (this.cachedTickDispatchPlan != null &&
                this.cachedTickDispatchPlan.CanReuse(
                    host,
                    assemblyState,
                    runtimeState,
                    profile,
                    this.registry))
            {
                return this.cachedTickDispatchPlan;
            }

            this.cachedTickDispatchPlan = ShuttleRuntimeTickDispatchPlan.Build(
                host,
                assemblyState,
                runtimeState,
                profile,
                this.registry,
                this.support);
            return this.cachedTickDispatchPlan;
        }

        private static bool ShouldRunStablePlanBindingValidation(int ticksGame)
        {
            return StablePlanBindingValidationIntervalTicks > 0 &&
                ticksGame >= 0 &&
                ticksGame % StablePlanBindingValidationIntervalTicks == 0;
        }

        internal bool CanRemoveModule(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            out string reason)
        {
            // Removal validation fails closed for explicit runtime-system failures, but missing
            // runtime state allows removal for recovery.
            reason = null;

            if (module == null)
            {
                return true;
            }

            foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
            {
                string appliesFailureReason;
                if (!this.support.AppliesToSystem(system, module, "removal validation", out appliesFailureReason))
                {
                    if (!string.IsNullOrEmpty(appliesFailureReason))
                    {
                        reason = appliesFailureReason;
                        return false;
                    }

                    continue;
                }

                IShuttleModuleRuntimeState state = null;
                if (runtimeState != null)
                {
                    runtimeState.Modules.TryGetState(
                        module.ModuleInstanceID,
                        system.RuntimeSystemKey,
                        out state);
                }

                if (state == null)
                {
                    Log.WarningOnce("[CeleTech Shuttle] Missing runtime state for " +
                        this.support.GetModuleDebugName(module) + " / " + system.RuntimeSystemKey +
                        " during removal validation. Allowing removal for recovery.",
                        this.support.MakeRuntimeLogHash(
                            "removal-validation-missing-state",
                            module != null ? module.ModuleInstanceID : null,
                            system != null ? system.RuntimeSystemKey : null,
                            null));
                    continue;
                }

                ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                    host,
                    assemblyState,
                    profile,
                    runtimeState,
                    module,
                    state,
                    storedEnergySink,
                    null,
                    null,
                    ticksGame);

                try
                {
                    if (!system.CanRemove(context, out reason))
                    {
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    reason = "Module runtime removal validation failed for " +
                        this.support.GetModuleDebugName(module) + " / " + system.RuntimeSystemKey + ".";
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] " + reason + " Exception: " + exception,
                        this.support.MakeRuntimeLogHash(
                            "removal-validation-exception",
                            module != null ? module.ModuleInstanceID : null,
                            system != null ? system.RuntimeSystemKey : null,
                            exception.GetType().FullName + exception.Message));
                    return false;
                }
            }

            return true;
        }

        internal void NotifyModuleInstalled(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame)
        {
            // Install notification may create the state immediately for systems that need setup
            // before the next reconcile pass.
            if (module == null || runtimeState == null)
            {
                return;
            }

            this.cachedTickDispatchPlan = null;
            foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
            {
                string appliesFailureReason;
                if (!this.support.AppliesToSystem(system, module, "install notification", out appliesFailureReason))
                {
                    continue;
                }

                IShuttleModuleRuntimeState state = this.support.GetOrCreateState(runtimeState, module, system);
                if (state == null)
                {
                    this.support.LogMissingState(module, system, "install notification");
                    continue;
                }

                ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                    host,
                    assemblyState,
                    profile,
                    runtimeState,
                    module,
                    state,
                    storedEnergySink,
                    null,
                    null,
                    ticksGame);

                this.support.TryRunRuntimeAction(system, module, "install notification", () => system.OnInstalled(context));
            }
        }

        internal void NotifyModuleRemoved(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame)
        {
            // This method must be called only after assembly removal has succeeded.
            // It is the one explicit cleanup path for module-owned runtime payloads.
            if (module == null || runtimeState == null)
            {
                return;
            }

            this.cachedTickDispatchPlan = null;
            foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
            {
                string appliesFailureReason;
                if (!this.support.AppliesToSystem(system, module, "removal notification", out appliesFailureReason))
                {
                    continue;
                }

                IShuttleModuleRuntimeState state;
                runtimeState.Modules.TryGetState(
                    module.ModuleInstanceID,
                    system.RuntimeSystemKey,
                    out state);

                if (state == null)
                {
                    this.support.LogMissingState(module, system, "removal notification");
                    continue;
                }

                ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                    host,
                    assemblyState,
                    profile,
                    runtimeState,
                    module,
                    state,
                    storedEnergySink,
                    null,
                    null,
                    ticksGame);

                this.support.TryRunRuntimeAction(system, module, "removal notification", () => system.OnRemoved(context));
            }

            int removedRecords = runtimeState.Modules.RemoveAllForModule(module.ModuleInstanceID);
            if (removedRecords > 0)
            {
                ShuttleLog.Info(
                    "ModuleRuntime",
                    "Cleaned module runtime records module=" + this.support.GetModuleDebugName(module) +
                    " removedRecords=" + removedRecords + ".");
            }
        }

        internal void NotifyArrived(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            IShuttleCargoResourceBroker cargoResourceBroker = null)
        {
            // Arrival has map-side host/module context again, but systems still receive only
            // their owned payload plus narrow services.
            if (assemblyState == null || runtimeState == null)
            {
                return;
            }

            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                if (module == null)
                {
                    continue;
                }

                if (!module.IsEnabled)
                {
                    continue;
                }

                foreach (IShuttleModuleRuntimeSystem system in this.registry.SystemsForRead)
                {
                    string appliesFailureReason;
                    if (!this.support.AppliesToSystem(system, module, "arrival notification", out appliesFailureReason))
                    {
                        continue;
                    }

                    IShuttleModuleRuntimeState state;
                    if (!runtimeState.Modules.TryGetState(
                        module.ModuleInstanceID,
                        system.RuntimeSystemKey,
                        out state))
                    {
                        continue;
                    }

                    ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                        host,
                        assemblyState,
                        profile,
                        runtimeState,
                        module,
                        state,
                        storedEnergySink,
                        null,
                        cargoResourceBroker,
                        ticksGame);

                    this.support.TryRunRuntimeAction(system, module, "arrival notification", () => system.OnArrived(context));
                }
            }
        }
    }
}
