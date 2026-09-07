using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.RefrigeratedCargo
{
    /// <summary>
    /// Periodic auto-transfer driver for refrigerated cargo modules.
    /// The actual Thing movement and recovery rules live in the cold-transfer service;
    /// this runtime system only decides when a module is eligible to request a pass and
    /// records throttled success/failure telemetry in its own state.
    /// </summary>
    internal sealed class RefrigeratedCargoRuntimeSystem : ShuttleModuleRuntimeSystemBase
    {
        public static readonly RefrigeratedCargoRuntimeSystem Instance = new RefrigeratedCargoRuntimeSystem();
        internal const string RefrigeratedCargoRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.RefrigeratedCargo;

        private RefrigeratedCargoRuntimeSystem()
        {
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return RefrigeratedCargoRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                return 60;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            return module != null && module.ModuleDef is ShuttleRefrigeratedCargoModuleDef;
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            return moduleRecord != null && moduleRecord.ModuleDef is ShuttleRefrigeratedCargoModuleDef;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new RefrigeratedCargoRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            RefrigeratedCargoRuntimeState state = this.GetState(context);
            ShuttleRefrigeratedCargoModuleDef moduleDef = this.GetModuleDef(context);
            if (state == null || moduleDef == null)
            {
                return;
            }

            state.EnsureInitialized();
            int intervalTicks = this.GetIntervalTicks(moduleDef);
            ShuttleRefrigeratedCargoAutoTransferConfig autoTransferConfig =
                this.ResolveAutoTransferConfig(context, moduleDef);
            if (autoTransferConfig == null || !autoTransferConfig.AutoTransferEnabled || intervalTicks <= 0)
            {
                this.WarnInvalidIntervalIfDevMode(context, moduleDef, autoTransferConfig, intervalTicks);
                state.DisableAutoTransferSchedule("disabled");
                return;
            }

            if (state.NextAutoTransferTick < 0)
            {
                state.ScheduleNext(context.TicksGame, intervalTicks);
            }
        }

        public override void Tick(ShuttleModuleRuntimeContext context)
        {
            RefrigeratedCargoRuntimeState state = this.GetState(context);
            ShuttleRefrigeratedCargoModuleDef moduleDef = this.GetModuleDef(context);
            if (context == null || state == null || moduleDef == null || !context.IsEnabled)
            {
                return;
            }

            state.EnsureInitialized();
            int intervalTicks = this.GetIntervalTicks(moduleDef);
            if (intervalTicks <= 0)
            {
                ShuttleRefrigeratedCargoAutoTransferConfig invalidIntervalConfig =
                    this.ResolveAutoTransferConfig(context, moduleDef);
                this.WarnInvalidIntervalIfDevMode(context, moduleDef, invalidIntervalConfig, intervalTicks);
                state.DisableAutoTransferSchedule("disabled");
                return;
            }

            if (!state.ShouldRun(context.TicksGame))
            {
                return;
            }

            if (moduleDef.maxStacksMovedPerInterval <= 0)
            {
                state.RecordSkipped(context.TicksGame, intervalTicks, "max-stacks-zero");
                return;
            }

            // Cold cargo is now routed from the load plan when a pawn actually delivers
            // the item to the transporter. Keep this runtime state for compatibility and
            // telemetry, but do not run the old background normal-cargo scan by default.
            if (!this.ShouldRunBackgroundAutoTransfer())
            {
                state.RecordSkipped(context.TicksGame, intervalTicks, "load-routing");
                return;
            }

            ShuttleRefrigeratedCargoAutoTransferConfig autoTransferConfig =
                this.ResolveAutoTransferConfig(context, moduleDef);
            if (autoTransferConfig == null || !autoTransferConfig.AutoTransferEnabled)
            {
                state.DisableAutoTransferSchedule("disabled");
                return;
            }

            if (this.HasActiveRefrigeratedLaunchTransfer(context))
            {
                // Launch/arrival transfer owns cold-cargo truth while a manifest is active.
                // Auto-transfer must stand down instead of moving ordinary cargo into cold holders.
                state.RecordSkipped(context.TicksGame, intervalTicks, "active-launch-transfer");
                return;
            }

            string eligibilityFailureReason;
            if (!this.CanAutoTransferNow(context, moduleDef, out eligibilityFailureReason))
            {
                state.RecordFailure(context.TicksGame, eligibilityFailureReason, intervalTicks);
                return;
            }

            IShuttleCargoColdTransferService transferService = context.RefrigeratedCargoTransferService;
            if (transferService == null)
            {
                state.RecordFailure(
                    context.TicksGame,
                    "Refrigerated cargo transfer service is unavailable.",
                    intervalTicks);
                return;
            }

            int movedStackCount;
            int movedThingCount;
            string failureReason;
            if (!transferService.TryAutoTransferLoadedCargoToCold(
                context.ModuleInstanceID,
                moduleDef.maxStacksMovedPerInterval,
                autoTransferConfig.AutoTransferFilter,
                autoTransferConfig.HasCustomAutoTransferFilter,
                "auto-runtime",
                out movedStackCount,
                out movedThingCount,
                out failureReason))
            {
                // A false result means the transfer service saw a real movement/recovery problem.
                // Preserve the failure so later retries remain visible instead of treating partial
                // movement as ordinary success.
                state.RecordFailure(context.TicksGame, failureReason, intervalTicks);
                return;
            }

            state.RecordSuccess(
                context.TicksGame,
                movedStackCount,
                movedThingCount,
                intervalTicks,
                movedStackCount > 0 ? "moved" : "no-eligible-cargo");
        }

        private bool CanAutoTransferNow(
            ShuttleModuleRuntimeContext context,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            out string failureReason)
        {
            failureReason = null;
            if (context == null || moduleDef == null)
            {
                failureReason = "CT_Shuttle_RefrigeratedCargo_RuntimeContextUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if (moduleDef.requiresCargoLogisticsForAutoTransfer &&
                (context.Profile == null ||
                    context.Profile.CargoLogistics == null ||
                    !context.Profile.CargoLogistics.HasCargoLogistics ||
                    !context.Profile.CargoLogistics.SupportsItemTransfer))
            {
                failureReason = "CT_Shuttle_RefrigeratedCargo_AutoTransferLogisticsUnsupported"
                    .Translate()
                    .ToString();
                return false;
            }

            if (!moduleDef.autoTransferOnlyWhenCoolingActive)
            {
                return true;
            }

            CompShuttleRefrigeratedCargoRegistry registry = context.Host != null
                ? context.Host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            if (registry == null)
            {
                failureReason = "CT_Shuttle_RefrigeratedCargo_RegistryUnavailable".Translate().ToString();
                return false;
            }

            RefrigeratedCargoCoolingStatus status;
            if (!registry.TryGetCoolingStatus(context.ModuleInstanceID, out status) ||
                status == null ||
                !status.CoolingActive)
            {
                failureReason = "CT_Shuttle_RefrigeratedCargo_HolderNotCooling".Translate().ToString();
                if (status != null && !string.IsNullOrEmpty(status.InactiveReason))
                {
                    failureReason += " " + status.InactiveReason;
                }

                return false;
            }

            return true;
        }

        private bool HasActiveRefrigeratedLaunchTransfer(ShuttleModuleRuntimeContext context)
        {
            CompShuttleHolderLaunchTransferState transferState = context != null && context.Host != null
                ? context.Host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;

            return transferState != null && transferState.HasRefrigeratedCargoLaunchTransfer;
        }

        private bool ShouldRunBackgroundAutoTransfer()
        {
            return false;
        }

        private ShuttleRefrigeratedCargoAutoTransferConfig ResolveAutoTransferConfig(
            ShuttleModuleRuntimeContext context,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            if (context == null || moduleDef == null)
            {
                return null;
            }

            ShuttleRefrigeratedCargoAutoTransferConfig config;
            if (context.TryGetRefrigeratedCargoAutoTransferConfig(context.ModuleInstanceID, out config))
            {
                return config;
            }

            return ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(
                context.ModuleInstanceID,
                moduleDef);
        }

        private int GetIntervalTicks(ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            return moduleDef != null ? moduleDef.autoTransferIntervalTicks : 0;
        }

        private void WarnInvalidIntervalIfDevMode(
            ShuttleModuleRuntimeContext context,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ShuttleRefrigeratedCargoAutoTransferConfig autoTransferConfig,
            int intervalTicks)
        {
            if (!Prefs.DevMode ||
                autoTransferConfig == null ||
                !autoTransferConfig.AutoTransferEnabled ||
                intervalTicks > 0)
            {
                return;
            }

            string moduleInstanceID = context != null ? context.ModuleInstanceID : null;
            string defName = moduleDef != null ? moduleDef.defName : "<null>";
            int hash = Gen.HashCombineInt(91274100, (moduleInstanceID ?? defName).GetHashCode());
            Log.WarningOnce(
                "[CeleTech Shuttle] Refrigerated cargo auto-transfer is enabled but autoTransferIntervalTicks is " +
                intervalTicks +
                " for module " +
                (moduleInstanceID ?? "<unknown>") +
                " def=" +
                defName +
                ". Auto-transfer will not run until the interval is positive.",
                hash);
        }

        private ShuttleRefrigeratedCargoModuleDef GetModuleDef(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
        }

        private RefrigeratedCargoRuntimeState GetState(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.State as RefrigeratedCargoRuntimeState : null;
        }
    }
}
