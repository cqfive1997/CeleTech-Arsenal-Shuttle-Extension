using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal sealed class ShuttleRuntimeTickDispatchPlan
    {
        private readonly ThingWithComps host;
        private readonly ShuttleAssemblyState assemblyState;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleProfile profile;
        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchRevisionSnapshot revisionSnapshot;
        private readonly List<ShuttleRuntimeTickBinding> tickBindings;

        private ShuttleRuntimeTickDispatchPlan(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchRevisionSnapshot revisionSnapshot,
            List<ShuttleRuntimeTickBinding> tickBindings)
        {
            this.host = host;
            this.assemblyState = assemblyState;
            this.runtimeState = runtimeState;
            this.profile = profile;
            this.registry = registry;
            this.revisionSnapshot = revisionSnapshot;
            this.tickBindings = tickBindings ?? new List<ShuttleRuntimeTickBinding>();
        }

        internal IReadOnlyList<ShuttleRuntimeTickBinding> TickBindings
        {
            get
            {
                return this.tickBindings;
            }
        }

        internal static ShuttleRuntimeTickDispatchPlan Build(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            ShuttleRuntimeDispatchRevisionSnapshot revisionSnapshot =
                ShuttleRuntimeDispatchRevisionSnapshot.Capture(profile, registry, assemblyState, runtimeState);
            List<ShuttleRuntimeTickBinding> bindings = new List<ShuttleRuntimeTickBinding>();

            if (assemblyState != null &&
                runtimeState != null &&
                runtimeState.Modules != null &&
                registry != null &&
                support != null)
            {
                IReadOnlyList<IShuttleModuleRuntimeSystem> systems = registry.SystemsForRead;
                for (int i = 0; systems != null && i < systems.Count; i++)
                {
                    IShuttleModuleRuntimeSystem system = systems[i];
                    if (system == null || system.TickInterval <= 0)
                    {
                        continue;
                    }

                    IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
                    for (int j = 0; modules != null && j < modules.Count; j++)
                    {
                        ShuttleModule module = modules[j];
                        if (module == null || !module.IsEnabled)
                        {
                            continue;
                        }

                        string appliesFailureReason;
                        if (!support.AppliesToSystem(system, module, "tick plan build", out appliesFailureReason))
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

                        bindings.Add(new ShuttleRuntimeTickBinding(
                            system,
                            module,
                            module.ModuleDef,
                            state));
                    }
                }
            }

            return new ShuttleRuntimeTickDispatchPlan(
                host,
                assemblyState,
                runtimeState,
                profile,
                registry,
                revisionSnapshot,
                bindings);
        }

        internal bool CanReuse(
            ThingWithComps currentHost,
            ShuttleAssemblyState currentAssemblyState,
            ShuttleRuntimeState currentRuntimeState,
            ShuttleProfile currentProfile,
            BuiltInShuttleModuleRuntimeRegistry currentRegistry)
        {
            // The tick dispatch plan is intentionally identity + revision gated so Tick can
            // reuse bindings until module topology, profile/runtime state, or registry content
            // changes. Reconcile can still null the cache after materializing new state.
            return object.ReferenceEquals(this.host, currentHost) &&
                object.ReferenceEquals(this.assemblyState, currentAssemblyState) &&
                object.ReferenceEquals(this.runtimeState, currentRuntimeState) &&
                object.ReferenceEquals(this.profile, currentProfile) &&
                object.ReferenceEquals(this.registry, currentRegistry) &&
                this.revisionSnapshot.Matches(
                    currentProfile,
                    currentRegistry,
                    currentAssemblyState,
                    currentRuntimeState);
        }

        internal static bool IsBindingStillValid(
            ShuttleRuntimeTickBinding binding,
            ShuttleRuntimeState currentRuntimeState)
        {
            if (binding == null ||
                binding.System == null ||
                binding.Module == null ||
                binding.ModuleRuntimeState == null ||
                !binding.Module.IsEnabled ||
                !object.ReferenceEquals(binding.Module.ModuleDef, binding.ModuleDef))
            {
                return false;
            }

            if (currentRuntimeState == null || currentRuntimeState.Modules == null)
            {
                return false;
            }

            IShuttleModuleRuntimeState state;
            return currentRuntimeState.Modules.TryGetState(
                    binding.Module.ModuleInstanceID,
                    binding.System.RuntimeSystemKey,
                    out state) &&
                object.ReferenceEquals(state, binding.ModuleRuntimeState);
        }

    }

    internal sealed class ShuttleRuntimeTickBinding
    {
        private ShuttleModuleRuntimeContext cachedTickContext;

        internal ShuttleRuntimeTickBinding(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef,
            IShuttleModuleRuntimeState moduleRuntimeState)
        {
            this.System = system;
            this.Module = module;
            this.ModuleDef = moduleDef;
            this.ModuleRuntimeState = moduleRuntimeState;
            this.RuntimeSystemProfileKey = ResolveProfileKey(system);
            this.ModuleInstanceID = module != null
                ? module.ModuleInstanceID
                : string.Empty;
            this.ModuleLabel = ResolveModuleLabel(module, moduleDef);
            this.TickInterval = system != null ? system.TickInterval : 0;
        }

        internal IShuttleModuleRuntimeSystem System { get; private set; }
        internal ShuttleModule Module { get; private set; }
        internal ShuttleModuleBaseDef ModuleDef { get; private set; }
        internal IShuttleModuleRuntimeState ModuleRuntimeState { get; private set; }
        internal string RuntimeSystemProfileKey { get; private set; }
        internal string ModuleInstanceID { get; private set; }
        internal string ModuleLabel { get; private set; }
        internal int TickInterval { get; private set; }

        internal ShuttleModuleRuntimeContext AcquireTickContext(
            ShuttleRuntimeDispatchSupport support,
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            IShuttleCargoResourceBroker cargoResourceBroker,
            int ticksGame,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig>
                refrigeratedAutoTransferConfigResolver,
            IShuttleCargoPostDepositRouter postDepositCargoRouter)
        {
            if (support == null)
            {
                return null;
            }

            if (this.cachedTickContext == null)
            {
                this.cachedTickContext = support.CreateRuntimeContext(
                    host,
                    assemblyState,
                    profile,
                    runtimeState,
                    this.Module,
                    this.ModuleRuntimeState,
                    storedEnergySink,
                    null,
                    cargoResourceBroker,
                    ticksGame,
                    refrigeratedCargoTransferService,
                    refrigeratedAutoTransferConfigResolver,
                    postDepositCargoRouter);
                return this.cachedTickContext;
            }

            this.cachedTickContext.RefreshTickFrame(
                runtimeState,
                storedEnergySink,
                cargoResourceBroker,
                ticksGame,
                refrigeratedCargoTransferService,
                refrigeratedAutoTransferConfigResolver,
                postDepositCargoRouter);
            return this.cachedTickContext;
        }

        private static string ResolveProfileKey(IShuttleModuleRuntimeSystem system)
        {
            string systemKey = system != null
                ? ShuttleRuntimeSystemKeyUtility.Normalize(system.RuntimeSystemKey)
                : null;
            if (string.IsNullOrEmpty(systemKey) && system != null)
            {
                systemKey = system.GetType().Name;
            }

            return string.IsNullOrEmpty(systemKey)
                ? "unknown-runtime-system"
                : systemKey;
        }

        private static string ResolveModuleLabel(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef)
        {
            if (moduleDef != null)
            {
                string label = moduleDef.LabelCap.ToString();
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }

            return module != null && !string.IsNullOrEmpty(module.ModuleInstanceID)
                ? module.ModuleInstanceID
                : "unknown-module";
        }
    }
}
