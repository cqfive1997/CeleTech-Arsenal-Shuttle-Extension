using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Narrow runtime context passed to built-in module systems.
    /// It exposes module identity, profile inputs, narrow services, and the owned payload only;
    /// it does not expose UI, mutation controller, cargo backend, or direct AssemblyState mutation.
    /// </summary>
    internal sealed class ShuttleModuleRuntimeContext
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyModuleInstanceConfig =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        public ShuttleModuleRuntimeContext(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            ShuttleSegment parentSegment,
            IShuttleModuleRuntimeState state,
            IShuttleStoredEnergySink storedEnergySink,
            IShuttlePowerDemandSink powerDemandSink,
            IShuttleCargoResourceBroker cargoResourceBroker,
            int ticksGame,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService = null,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver = null,
            IShuttleCargoPostDepositRouter postDepositCargoRouter = null)
        {
            this.Initialize(
                host,
                profile,
                runtimeState,
                module != null ? module.ModuleInstanceID : null,
                this.ResolveModuleDefName(module),
                module != null ? module.ModuleDef : null,
                this.CopyModuleInstanceConfig(module),
                module != null ? module.ParentSegmentInstanceID : null,
                this.ResolveSegmentDefName(parentSegment),
                parentSegment != null ? parentSegment.SegmentDef : null,
                module != null ? module.ParentSlotID : null,
                module != null && module.IsEnabled,
                state,
                storedEnergySink,
                powerDemandSink,
                cargoResourceBroker,
                ticksGame,
                refrigeratedCargoTransferService,
                refrigeratedAutoTransferConfigResolver,
                postDepositCargoRouter);
        }

        internal ShuttleModuleRuntimeContext(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string moduleDefName,
            ShuttleModuleBaseDef moduleDef,
            IReadOnlyDictionary<string, string> moduleInstanceConfig,
            string parentSegmentInstanceID,
            string parentSegmentDefName,
            ShuttleSegmentBaseDef parentSegmentDef,
            string parentSlotID,
            bool isEnabled,
            IShuttleModuleRuntimeState state,
            IShuttleStoredEnergySink storedEnergySink,
            IShuttlePowerDemandSink powerDemandSink,
            IShuttleCargoResourceBroker cargoResourceBroker,
            int ticksGame,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService = null,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver = null,
            IShuttleCargoPostDepositRouter postDepositCargoRouter = null)
        {
            this.Initialize(
                host,
                profile,
                runtimeState,
                moduleInstanceID,
                moduleDefName,
                moduleDef,
                moduleInstanceConfig,
                parentSegmentInstanceID,
                parentSegmentDefName,
                parentSegmentDef,
                parentSlotID,
                isEnabled,
                state,
                storedEnergySink,
                powerDemandSink,
                cargoResourceBroker,
                ticksGame,
                refrigeratedCargoTransferService,
                refrigeratedAutoTransferConfigResolver,
                postDepositCargoRouter);
        }

        private ShuttleRuntimeState runtimeState;
        private IShuttlePowerDemandSink powerDemandSink;
        private Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver;

        /// <summary>
        /// Derived read-only capability snapshot. Runtime truth must not be written back into Profile.
        /// </summary>
        public ShuttleProfile Profile { get; private set; }

        /// <summary>
        /// Map-side shuttle Thing for runtime-only integrations such as vanilla verb caster binding.
        /// Runtime systems must not use this as a controller or assembly mutation back door.
        /// </summary>
        public ThingWithComps Host { get; private set; }

        public string ModuleInstanceID { get; private set; }
        public string ModuleDefName { get; private set; }
        public ShuttleModuleBaseDef ModuleDef { get; private set; }
        public IReadOnlyDictionary<string, string> ModuleInstanceConfig { get; private set; }
        public string ParentSegmentInstanceID { get; private set; }
        public string ParentSegmentDefName { get; private set; }
        public ShuttleSegmentBaseDef ParentSegmentDef { get; private set; }
        public string ParentSlotID { get; private set; }
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Durable state payload owned by the active runtime system for this module instance.
        /// </summary>
        public IShuttleModuleRuntimeState State { get; private set; }

        /// <summary>
        /// Narrow service for spending stored shuttle energy, not grid power or reactor output.
        /// </summary>
        public IShuttleStoredEnergySink StoredEnergySink { get; private set; }

        /// <summary>
        /// Optional narrow cargo-service seam for module runtime systems. It is not exposed to UI
        /// and does not expose cargo containers or backend internals.
        /// </summary>
        public IShuttleCargoResourceBroker CargoResourceBroker { get; private set; }

        /// <summary>
        /// Narrow service for refrigerated normal-cargo to cold-holder movement.
        /// Runtime systems must use this instead of touching cargo holders directly.
        /// </summary>
        public IShuttleCargoColdTransferService RefrigeratedCargoTransferService { get; private set; }

        /// <summary>
        /// Optional event-driven router for cargo stacks committed by module production. It
        /// exposes no AssemblyState, transporter, backend, or cold-holder references.
        /// </summary>
        public IShuttleCargoPostDepositRouter PostDepositCargoRouter { get; private set; }

        public int TicksGame { get; private set; }
        public bool InternalBusPowered { get; private set; }

        internal bool TryGetRefrigeratedCargoAutoTransferConfig(
            string moduleInstanceID,
            out ShuttleRefrigeratedCargoAutoTransferConfig config)
        {
            config = null;
            if (this.refrigeratedAutoTransferConfigResolver == null || string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            config = this.refrigeratedAutoTransferConfigResolver(moduleInstanceID);
            return config != null;
        }

        internal void RefreshPowerDemandFrame(
            ShuttleRuntimeState currentRuntimeState,
            IShuttlePowerDemandSink currentPowerDemandSink,
            int ticksGame)
        {
            // Power-demand contexts are binding-local and may be reused only while the dispatch
            // plan identity/revision gates remain valid. Refresh only per-tick inputs; module,
            // profile, parent, and owned-state identity stay fixed until the plan is rebuilt.
            this.runtimeState = currentRuntimeState;
            this.powerDemandSink = currentPowerDemandSink;
            this.TicksGame = ticksGame;
            this.InternalBusPowered = currentRuntimeState != null &&
                currentRuntimeState.Power.InternalBusPowered;
        }

        internal void RefreshTickFrame(
            ShuttleRuntimeState currentRuntimeState,
            IShuttleStoredEnergySink currentStoredEnergySink,
            IShuttleCargoResourceBroker currentCargoResourceBroker,
            int ticksGame,
            IShuttleCargoColdTransferService currentRefrigeratedCargoTransferService,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig>
                currentRefrigeratedAutoTransferConfigResolver,
            IShuttleCargoPostDepositRouter currentPostDepositCargoRouter)
        {
            // Tick contexts are binding-local and reused only while the dispatch plan's identity
            // and revision gates remain valid. Refresh services that may change between frames;
            // module/profile/parent/owned-state identity stays fixed until plan rebuild.
            this.runtimeState = currentRuntimeState;
            this.StoredEnergySink = currentStoredEnergySink;
            this.powerDemandSink = null;
            this.CargoResourceBroker = currentCargoResourceBroker;
            this.TicksGame = ticksGame;
            this.InternalBusPowered = currentRuntimeState != null &&
                currentRuntimeState.Power.InternalBusPowered;
            this.RefrigeratedCargoTransferService =
                currentRefrigeratedCargoTransferService;
            this.refrigeratedAutoTransferConfigResolver =
                currentRefrigeratedAutoTransferConfigResolver;
            this.PostDepositCargoRouter = currentPostDepositCargoRouter;
        }

        internal void AddInternalPowerDemandWatts(float watts)
        {
            if (this.powerDemandSink == null)
            {
                return;
            }

            this.powerDemandSink.AddInternalDemandWatts(this.runtimeState, watts);
        }

        internal bool TryConsumeStoredEnergyWd(float amountWd)
        {
            if (!this.IsFiniteFloat(amountWd))
            {
                return false;
            }

            // Zero or negative costs are treated as already paid. This keeps no-cost module
            // tuning simple while still routing real spending through the stored-energy sink.
            if (amountWd <= 0f)
            {
                return true;
            }

            if (this.StoredEnergySink == null)
            {
                return false;
            }

            return this.StoredEnergySink.TryConsumeStoredEnergyWd(this.runtimeState, amountWd);
        }

        private string ResolveModuleDefName(ShuttleModule module)
        {
            if (module == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(module.moduleDefName))
            {
                return module.moduleDefName;
            }

            return module.ModuleDef != null ? module.ModuleDef.defName : null;
        }

        private IReadOnlyDictionary<string, string> CopyModuleInstanceConfig(ShuttleModule module)
        {
            Dictionary<string, string> config = module != null
                ? module.CopyInstanceConfig()
                : new Dictionary<string, string>();
            return new ReadOnlyDictionary<string, string>(config);
        }

        private void Initialize(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string moduleDefName,
            ShuttleModuleBaseDef moduleDef,
            IReadOnlyDictionary<string, string> moduleInstanceConfig,
            string parentSegmentInstanceID,
            string parentSegmentDefName,
            ShuttleSegmentBaseDef parentSegmentDef,
            string parentSlotID,
            bool isEnabled,
            IShuttleModuleRuntimeState state,
            IShuttleStoredEnergySink storedEnergySink,
            IShuttlePowerDemandSink powerDemandSink,
            IShuttleCargoResourceBroker cargoResourceBroker,
            int ticksGame,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver,
            IShuttleCargoPostDepositRouter postDepositCargoRouter)
        {
            this.Host = host;
            this.Profile = profile;
            this.runtimeState = runtimeState;
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleDefName = moduleDefName;
            this.ModuleDef = moduleDef;
            this.ModuleInstanceConfig = moduleInstanceConfig ?? EmptyModuleInstanceConfig;
            this.ParentSegmentInstanceID = parentSegmentInstanceID;
            this.ParentSegmentDefName = parentSegmentDefName;
            this.ParentSegmentDef = parentSegmentDef;
            this.ParentSlotID = parentSlotID;
            this.IsEnabled = isEnabled;
            this.State = state;
            this.StoredEnergySink = storedEnergySink;
            this.powerDemandSink = powerDemandSink;
            this.CargoResourceBroker = cargoResourceBroker;
            this.TicksGame = ticksGame;
            this.InternalBusPowered = runtimeState != null && runtimeState.Power.InternalBusPowered;
            this.RefrigeratedCargoTransferService = refrigeratedCargoTransferService;
            this.refrigeratedAutoTransferConfigResolver = refrigeratedAutoTransferConfigResolver;
            this.PostDepositCargoRouter = postDepositCargoRouter;
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

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
