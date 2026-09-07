using System.Collections.Generic;
using System.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal interface IShuttlePowerDemandProfileSink
    {
        bool Enabled { get; }

        void RecordPowerDemandCollection(long startTimestamp);

        void RecordPowerDemandContextCreation(long startTimestamp);

        void RecordPowerDemandFastContextCreation();

        void RecordPowerDemandFastContextReuse();

        void RecordPowerDemandFullContextCreation();

        void RecordPowerDemandRuntimeSystem(
            IShuttleModuleRuntimeSystem system,
            long startTimestamp,
            float demandWatts);

        void RecordPowerDemandAggregation(long startTimestamp);

        void RecordPowerDemandPlanGet(long startTimestamp);

        void RecordPowerDemandPlanReuse();

        void RecordPowerDemandPlanBuild(long startTimestamp);

        void RecordPowerDemandBindingValidation(long startTimestamp, bool valid);

        void RecordPowerDemandValidationSkippedDueStablePlan();

        void RecordPowerDemandFullValidationPass();

        void RecordPowerDemandLowFrequencyValidationPass();

        void RecordPowerDemandInvalidRebuild();

        void RecordPowerDemandDispatchLoopIteration();

        void RecordPowerDemandValidBinding();

        void RecordPowerDemandSkippedInvalidBinding();

        void RecordPowerDemandTryRunRuntimeAction(long startTimestamp);

        void RecordPowerDemandDirectDispatch();
    }

    /// <summary>
    /// Runs the module power-demand pass before PowerSystem.Tick().
    /// This service reports transient demand only; stored energy truth remains in RuntimeState.Power.
    /// </summary>
    internal sealed class ShuttleRuntimePowerDemandService
    {
        private const int StablePlanBindingValidationIntervalTicks = 500;

        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchSupport support;
        private ShuttleRuntimePowerDemandDispatchPlan cachedPowerDemandDispatchPlan;

        internal ShuttleRuntimePowerDemandService(
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            this.registry = registry;
            this.support = support;
        }

        internal void CollectPowerDemand(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttlePowerDemandSink powerDemandSink,
            int ticksGame,
            IShuttlePowerDemandProfileSink powerDemandProfileSink = null)
        {
            // Demand collection is a pure pre-power pass: no missing state is created and no
            // gameplay action such as firing or target acquisition should run here.
            if (assemblyState == null || runtimeState == null || powerDemandSink == null)
            {
                return;
            }

            bool profilePowerDemand = powerDemandProfileSink != null && powerDemandProfileSink.Enabled;
            bool profileDetailedPowerDemand =
                profilePowerDemand && ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns;
            bool planReused;
            ShuttleRuntimePowerDemandDispatchPlan plan = this.GetOrBuildPowerDemandDispatchPlan(
                host,
                assemblyState,
                runtimeState,
                profile,
                out planReused,
                profileDetailedPowerDemand ? powerDemandProfileSink : null);
            IReadOnlyList<ShuttleRuntimePowerDemandBinding> bindings = plan.PowerDemandBindings;
            bool rebuiltForInvalidBinding = false;
            bool validateBindingsThisTick = this.ShouldRunStablePlanBindingValidation(ticksGame);
            if (validateBindingsThisTick && profileDetailedPowerDemand)
            {
                powerDemandProfileSink.RecordPowerDemandFullValidationPass();
                powerDemandProfileSink.RecordPowerDemandLowFrequencyValidationPass();
            }

            ProfilingPowerDemandSink profilingPowerDemandSink = profilePowerDemand
                ? new ProfilingPowerDemandSink(powerDemandSink, powerDemandProfileSink)
                : null;
            IShuttlePowerDemandSink activePowerDemandSink = profilingPowerDemandSink != null
                ? profilingPowerDemandSink
                : powerDemandSink;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (profileDetailedPowerDemand)
                {
                    powerDemandProfileSink.RecordPowerDemandDispatchLoopIteration();
                }

                ShuttleRuntimePowerDemandBinding binding = bindings[i];
                bool bindingStillValid = true;
                if (validateBindingsThisTick)
                {
                    if (profileDetailedPowerDemand)
                    {
                        long validationStart = Stopwatch.GetTimestamp();
                        try
                        {
                            bindingStillValid = ShuttleRuntimePowerDemandDispatchPlan.IsBindingStillValid(
                                binding,
                                runtimeState);
                        }
                        finally
                        {
                            powerDemandProfileSink.RecordPowerDemandBindingValidation(
                                validationStart,
                                bindingStillValid);
                        }
                    }
                    else
                    {
                        bindingStillValid = ShuttleRuntimePowerDemandDispatchPlan.IsBindingStillValid(
                            binding,
                            runtimeState);
                    }
                }
                else if (profileDetailedPowerDemand && planReused)
                {
                    powerDemandProfileSink.RecordPowerDemandValidationSkippedDueStablePlan();
                }

                if (!bindingStillValid)
                {
                    if (profileDetailedPowerDemand)
                    {
                        powerDemandProfileSink.RecordPowerDemandSkippedInvalidBinding();
                    }

                    if (!rebuiltForInvalidBinding)
                    {
                        long invalidRebuildStart = profileDetailedPowerDemand ? Stopwatch.GetTimestamp() : 0L;
                        this.cachedPowerDemandDispatchPlan = ShuttleRuntimePowerDemandDispatchPlan.Build(
                            host,
                            assemblyState,
                            runtimeState,
                            profile,
                            this.registry,
                            this.support);
                        if (profileDetailedPowerDemand)
                        {
                            powerDemandProfileSink.RecordPowerDemandInvalidRebuild();
                            powerDemandProfileSink.RecordPowerDemandPlanBuild(invalidRebuildStart);
                        }

                        bindings = this.cachedPowerDemandDispatchPlan.PowerDemandBindings;
                        rebuiltForInvalidBinding = true;
                        planReused = false;
                        i = -1;
                    }

                    continue;
                }

                if (profileDetailedPowerDemand)
                {
                    powerDemandProfileSink.RecordPowerDemandValidBinding();
                }

                IShuttleModuleRuntimeSystem system = binding.System;
                ShuttleModuleRuntimeContext context;
                bool contextAllocated;
                if (profilePowerDemand)
                {
                    long contextCreationStart = Stopwatch.GetTimestamp();
                    try
                    {
                        context = binding.AcquirePowerDemandContext(
                            this.support,
                            host,
                            profile,
                            runtimeState,
                            activePowerDemandSink,
                            ticksGame,
                            out contextAllocated);
                        if (profileDetailedPowerDemand)
                        {
                            if (contextAllocated)
                            {
                                powerDemandProfileSink.RecordPowerDemandFastContextCreation();
                            }
                            else
                            {
                                powerDemandProfileSink.RecordPowerDemandFastContextReuse();
                            }
                        }
                    }
                    finally
                    {
                        powerDemandProfileSink.RecordPowerDemandContextCreation(contextCreationStart);
                    }
                }
                else
                {
                    context = binding.AcquirePowerDemandContext(
                        this.support,
                        host,
                        profile,
                        runtimeState,
                        activePowerDemandSink,
                        ticksGame,
                        out contextAllocated);
                }

                if (context == null)
                {
                    continue;
                }

                if (profilePowerDemand)
                {
                    profilingPowerDemandSink.BeginRuntimeSystem();
                    long tryRunStart = profileDetailedPowerDemand ? Stopwatch.GetTimestamp() : 0L;
                    long runtimeSystemDemandStart = Stopwatch.GetTimestamp();
                    try
                    {
                        if (profileDetailedPowerDemand)
                        {
                            powerDemandProfileSink.RecordPowerDemandDirectDispatch();
                        }

                        this.support.TryRunPowerDemandCollection(
                            system,
                            binding.Module,
                            context);
                    }
                    finally
                    {
                        powerDemandProfileSink.RecordPowerDemandRuntimeSystem(
                            system,
                            runtimeSystemDemandStart,
                            profilingPowerDemandSink.RuntimeSystemDemandWatts);
                        if (profileDetailedPowerDemand)
                        {
                            powerDemandProfileSink.RecordPowerDemandTryRunRuntimeAction(tryRunStart);
                        }
                    }
                }
                else
                {
                    this.support.TryRunPowerDemandCollection(
                        system,
                        binding.Module,
                        context);
                }
            }
        }

        private ShuttleRuntimePowerDemandDispatchPlan GetOrBuildPowerDemandDispatchPlan(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            out bool planReused,
            IShuttlePowerDemandProfileSink powerDemandProfileSink = null)
        {
            planReused = false;
            long planGetStart = powerDemandProfileSink != null && powerDemandProfileSink.Enabled
                ? Stopwatch.GetTimestamp()
                : 0L;
            try
            {
                if (this.cachedPowerDemandDispatchPlan != null &&
                    this.cachedPowerDemandDispatchPlan.CanReuse(
                        host,
                        assemblyState,
                        runtimeState,
                        profile,
                        this.registry))
                {
                    if (powerDemandProfileSink != null && powerDemandProfileSink.Enabled)
                    {
                        powerDemandProfileSink.RecordPowerDemandPlanReuse();
                    }

                    planReused = true;
                    return this.cachedPowerDemandDispatchPlan;
                }

                long planBuildStart = powerDemandProfileSink != null && powerDemandProfileSink.Enabled
                    ? Stopwatch.GetTimestamp()
                    : 0L;
                this.cachedPowerDemandDispatchPlan = ShuttleRuntimePowerDemandDispatchPlan.Build(
                    host,
                    assemblyState,
                    runtimeState,
                    profile,
                    this.registry,
                    this.support);
                if (powerDemandProfileSink != null && powerDemandProfileSink.Enabled)
                {
                    powerDemandProfileSink.RecordPowerDemandPlanBuild(planBuildStart);
                }

                return this.cachedPowerDemandDispatchPlan;
            }
            finally
            {
                if (powerDemandProfileSink != null && powerDemandProfileSink.Enabled)
                {
                    powerDemandProfileSink.RecordPowerDemandPlanGet(planGetStart);
                }
            }
        }

        private bool ShouldRunStablePlanBindingValidation(int ticksGame)
        {
            return StablePlanBindingValidationIntervalTicks > 0 &&
                ticksGame >= 0 &&
                ticksGame % StablePlanBindingValidationIntervalTicks == 0;
        }

        private sealed class ProfilingPowerDemandSink : IShuttlePowerDemandSink
        {
            private readonly IShuttlePowerDemandSink inner;
            private readonly IShuttlePowerDemandProfileSink profileSink;
            private float runtimeSystemDemandWatts;

            internal ProfilingPowerDemandSink(
                IShuttlePowerDemandSink inner,
                IShuttlePowerDemandProfileSink profileSink)
            {
                this.inner = inner;
                this.profileSink = profileSink;
            }

            internal float RuntimeSystemDemandWatts
            {
                get { return this.runtimeSystemDemandWatts; }
            }

            internal void BeginRuntimeSystem()
            {
                this.runtimeSystemDemandWatts = 0f;
            }

            public void AddInternalDemandWatts(ShuttleRuntimeState runtimeState, float watts)
            {
                long aggregationStart = Stopwatch.GetTimestamp();
                try
                {
                    this.inner.AddInternalDemandWatts(runtimeState, watts);
                }
                finally
                {
                    if (!float.IsNaN(watts) && !float.IsInfinity(watts) && watts > 0f)
                    {
                        this.runtimeSystemDemandWatts += watts;
                    }

                    this.profileSink.RecordPowerDemandAggregation(aggregationStart);
                }
            }
        }
    }
}
