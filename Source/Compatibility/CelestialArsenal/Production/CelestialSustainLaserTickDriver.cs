using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Ammunition-free cycle coordinator for the sustained laser. Target policy and cast entry
    /// remain delegated to their focused framework/backend collaborators.
    /// </summary>
    internal sealed class CelestialSustainLaserTickDriver : IShuttleWeaponTickDriver
    {
        private readonly CelestialSustainLaserHost host;
        private readonly CelestialSustainLaserLifecycleDriver lifecycle;
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponTargetingService targetingService;
        private readonly IShuttleWeaponBurstStarter burstStarter;

        internal CelestialSustainLaserTickDriver(
            CelestialSustainLaserHost host,
            CelestialSustainLaserLifecycleDriver lifecycle,
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponTargetingService targetingService,
            IShuttleWeaponBurstStarter burstStarter)
        {
            this.host = host;
            this.lifecycle = lifecycle;
            this.fireControlPolicy = fireControlPolicy;
            this.targetingService = targetingService;
            this.burstStarter = burstStarter;
        }

        public void Tick(ShuttleModuleRuntimeContext context)
        {
            CelestialSustainLaserTickDiagnostics diagnostics =
                CelestialSustainLaserTickDiagnostics.Create(context);
            diagnostics.Begin();
            string finalSection =
                ShuttleWeaponRuntimeProfiler.SectionParticleLanceLifecycle;
            try
            {
                ShuttleWeaponModuleDef weaponDef = context != null
                    ? context.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                ShuttleWeaponRuntimeState state = context != null
                    ? context.State as ShuttleWeaponRuntimeState
                    : null;
                Verb_ShuttleCelestialSustainLaser verb;
                if (weaponDef == null || state == null || this.host == null ||
                    this.lifecycle == null ||
                    !this.lifecycle.TryGetReadyVerb(context, out verb))
                {
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    return;
                }

                diagnostics.CompleteAndStart(
                    ShuttleWeaponRuntimeProfiler.SectionParticleLanceLifecycle);
                finalSection = ShuttleWeaponRuntimeProfiler.SectionParticleLanceCycle;

                if (verb == null)
                {
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    state.SetActivePowerTicksForRuntimeOnly(0);
                    return;
                }

                if (this.fireControlPolicy.IsFireControlHardStopped(context, state))
                {
                    this.host.AbortActiveCast(context);
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    state.SetActivePowerTicksForRuntimeOnly(0);
                    return;
                }

                if (verb.state == VerbState.Bursting)
                {
                    this.host.TickVerbs(context);
                    diagnostics.CompleteAndStart(
                        ShuttleWeaponRuntimeProfiler.SectionParticleLanceVerbTick);
                    finalSection = ShuttleWeaponRuntimeProfiler.SectionParticleLanceCycle;
                }

                ShuttleWeaponCycleStateTransitions.DecrementActivePower(state);
                if (verb.state == VerbState.Bursting)
                {
                    if (verb.CurrentTarget.IsValid)
                    {
                        state.SetCurrentTargetForRuntimeOnly(verb.CurrentTarget);
                    }

                    state.SetActivePowerTicksForRuntimeOnly(
                        Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
                    return;
                }

                if (!this.fireControlPolicy.CanOperate(context, weaponDef, state, verb) ||
                    !this.targetingService.CanContinueCurrentTarget(
                        context,
                        weaponDef,
                        state,
                        verb))
                {
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    state.SetActivePowerTicksForRuntimeOnly(0);
                    return;
                }

                int warmupTicks = state.GetWarmupTicksForRuntimeOnly();
                if (warmupTicks > 0)
                {
                    state.SetWarmupTicksForRuntimeOnly(warmupTicks - 1);
                    if (state.GetWarmupTicksForRuntimeOnly() <= 0)
                    {
                        this.burstStarter.BeginBurst(context, weaponDef, state, verb);
                    }

                    return;
                }

                int cooldownTicks = state.GetCooldownTicksForRuntimeOnly();
                if (cooldownTicks > 0)
                {
                    state.SetCooldownTicksForRuntimeOnly(cooldownTicks - 1);
                    return;
                }

                if (!state.GetForcedTargetForRuntimeOnly().IsValid &&
                    !this.ShouldRunScan(context, weaponDef, state))
                {
                    return;
                }

                this.targetingService.TryStartShootSomething(
                    context,
                    weaponDef,
                    state,
                    verb);
            }
            finally
            {
                diagnostics.Finish(finalSection);
            }
        }

        private bool ShouldRunScan(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            int interval = weaponDef != null
                ? Math.Max(1, weaponDef.scanIntervalTicks)
                : 1;
            int phase = state.GetIdleTargetScanPhaseForRuntimeOnly(
                context != null ? context.ModuleInstanceID : null,
                interval);
            int tick = context != null ? context.TicksGame : 0;
            int remainder = tick % interval;
            if (remainder < 0)
            {
                remainder += interval;
            }

            return remainder == phase;
        }
    }
}
