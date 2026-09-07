using System.Collections.Generic;
using System.Collections.ObjectModel;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Cached dispatch plan for the pre-power demand pass. Reuse is guarded by runtime,
    /// assembly, profile, and registry revision snapshots; stable plans avoid per-binding
    /// validation on most ticks.
    /// </summary>
    internal sealed class ShuttleRuntimePowerDemandDispatchPlan
    {
        private readonly ThingWithComps host;
        private readonly ShuttleAssemblyState assemblyState;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleProfile profile;
        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchRevisionSnapshot revisionSnapshot;
        private readonly List<ShuttleRuntimePowerDemandBinding> powerDemandBindings;

        private ShuttleRuntimePowerDemandDispatchPlan(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchRevisionSnapshot revisionSnapshot,
            List<ShuttleRuntimePowerDemandBinding> powerDemandBindings)
        {
            this.host = host;
            this.assemblyState = assemblyState;
            this.runtimeState = runtimeState;
            this.profile = profile;
            this.registry = registry;
            this.revisionSnapshot = revisionSnapshot;
            this.powerDemandBindings = powerDemandBindings ?? new List<ShuttleRuntimePowerDemandBinding>();
        }

        internal IReadOnlyList<ShuttleRuntimePowerDemandBinding> PowerDemandBindings
        {
            get
            {
                return this.powerDemandBindings;
            }
        }

        internal static ShuttleRuntimePowerDemandDispatchPlan Build(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            ShuttleRuntimeDispatchRevisionSnapshot revisionSnapshot =
                ShuttleRuntimeDispatchRevisionSnapshot.Capture(profile, registry, assemblyState, runtimeState);
            List<ShuttleRuntimePowerDemandBinding> bindings = new List<ShuttleRuntimePowerDemandBinding>();

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
                    if (system == null || !system.ParticipatesInPowerDemand)
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
                        if (!support.AppliesToSystem(system, module, "power demand plan build", out appliesFailureReason))
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

                        ShuttleSegment parentSegment = assemblyState.GetSegment(module.ParentSegmentInstanceID);
                        bindings.Add(new ShuttleRuntimePowerDemandBinding(
                            system,
                            module,
                            module.ModuleDef,
                            parentSegment,
                            state));
                    }
                }
            }

            return new ShuttleRuntimePowerDemandDispatchPlan(
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
            ShuttleRuntimePowerDemandBinding binding,
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

    internal sealed class ShuttleRuntimePowerDemandBinding
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyModuleInstanceConfig =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        private ShuttleModuleRuntimeContext cachedPowerDemandContext;

        internal ShuttleRuntimePowerDemandBinding(
            IShuttleModuleRuntimeSystem system,
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef,
            ShuttleSegment parentSegment,
            IShuttleModuleRuntimeState moduleRuntimeState)
        {
            this.System = system;
            this.Module = module;
            this.ModuleDef = moduleDef;
            this.ModuleRuntimeState = moduleRuntimeState;
            this.ModuleInstanceID = module != null ? module.ModuleInstanceID : null;
            this.ModuleDefName = this.ResolveModuleDefName(module, moduleDef);
            this.ModuleInstanceConfig = this.CopyModuleInstanceConfig(module);
            this.ParentSegmentInstanceID = module != null ? module.ParentSegmentInstanceID : null;
            this.ParentSegmentDefName = this.ResolveSegmentDefName(parentSegment);
            this.ParentSegmentDef = parentSegment != null ? parentSegment.SegmentDef : null;
            this.ParentSlotID = module != null ? module.ParentSlotID : null;
            this.IsEnabled = module != null && module.IsEnabled;
        }

        internal IShuttleModuleRuntimeSystem System { get; private set; }
        internal ShuttleModule Module { get; private set; }
        internal ShuttleModuleBaseDef ModuleDef { get; private set; }
        internal IShuttleModuleRuntimeState ModuleRuntimeState { get; private set; }
        internal string ModuleInstanceID { get; private set; }
        internal string ModuleDefName { get; private set; }
        internal IReadOnlyDictionary<string, string> ModuleInstanceConfig { get; private set; }
        internal string ParentSegmentInstanceID { get; private set; }
        internal string ParentSegmentDefName { get; private set; }
        internal ShuttleSegmentBaseDef ParentSegmentDef { get; private set; }
        internal string ParentSlotID { get; private set; }
        internal bool IsEnabled { get; private set; }

        internal ShuttleModuleRuntimeContext AcquirePowerDemandContext(
            ShuttleRuntimeDispatchSupport support,
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttlePowerDemandSink powerDemandSink,
            int ticksGame,
            out bool allocated)
        {
            allocated = false;
            if (this.cachedPowerDemandContext == null)
            {
                this.cachedPowerDemandContext = support.CreatePowerDemandRuntimeContext(
                    host,
                    profile,
                    runtimeState,
                    this,
                    powerDemandSink,
                    ticksGame);
                allocated = this.cachedPowerDemandContext != null;
                return this.cachedPowerDemandContext;
            }

            this.cachedPowerDemandContext.RefreshPowerDemandFrame(
                runtimeState,
                powerDemandSink,
                ticksGame);
            return this.cachedPowerDemandContext;
        }

        private string ResolveModuleDefName(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef)
        {
            if (module != null && !string.IsNullOrEmpty(module.moduleDefName))
            {
                return module.moduleDefName;
            }

            return moduleDef != null ? moduleDef.defName : null;
        }

        private IReadOnlyDictionary<string, string> CopyModuleInstanceConfig(ShuttleModule module)
        {
            if (module == null)
            {
                return EmptyModuleInstanceConfig;
            }

            Dictionary<string, string> config = module.CopyInstanceConfig();
            return config != null && config.Count > 0
                ? new ReadOnlyDictionary<string, string>(config)
                : EmptyModuleInstanceConfig;
        }

        private string ResolveSegmentDefName(ShuttleSegment segment)
        {
            if (segment == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(segment.segmentDefName))
            {
                return segment.segmentDefName;
            }

            return segment.SegmentDef != null ? segment.SegmentDef.defName : null;
        }
    }
}
