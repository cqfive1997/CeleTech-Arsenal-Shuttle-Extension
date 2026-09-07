using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Native production tick coordinator. Gameplay decisions remain in focused reload, fast-path,
    /// scan and cycle collaborators; this class owns their ordering around one VerbTracker tick.
    /// </summary>
    internal sealed class NativeVerbWeaponTickDriver : IShuttleWeaponTickDriver
    {
        private readonly ShuttleWeaponTickReloadController reloadController;
        private readonly ShuttleWeaponTickFastPathPolicy fastPathPolicy;
        private readonly ShuttleWeaponTargetScanScheduler scanScheduler;
        private readonly ShuttleWeaponCycleController cycleController;
        private readonly IShuttleWeaponCycleHost cycleHost;
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;

        internal NativeVerbWeaponTickDriver(
            ShuttleWeaponTickReloadController reloadController,
            ShuttleWeaponTickFastPathPolicy fastPathPolicy,
            ShuttleWeaponTargetScanScheduler scanScheduler,
            ShuttleWeaponCycleController cycleController,
            IShuttleWeaponCycleHost cycleHost,
            ShuttleWeaponFireControlPolicy fireControlPolicy)
        {
            this.reloadController = reloadController;
            this.fastPathPolicy = fastPathPolicy;
            this.scanScheduler = scanScheduler;
            this.cycleController = cycleController;
            this.cycleHost = cycleHost;
            this.fireControlPolicy = fireControlPolicy;
        }

        public void Tick(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponTickDiagnostics diagnostics =
                ShuttleWeaponTickDiagnostics.Create(context);
            diagnostics.Begin();
            try
            {
                ShuttleWeaponModuleDef weaponDef = context != null
                    ? context.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                ShuttleWeaponRuntimeState state = context != null
                    ? context.State as ShuttleWeaponRuntimeState
                    : null;
                if (weaponDef == null || state == null)
                {
                    diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionStateSetup);
                    return;
                }

                ShuttleWeaponAmmoState ammoState =
                    this.reloadController.AdvancePendingWork(context, weaponDef, state);
                if (this.cycleHost == null || !this.cycleHost.EnsureReady(context))
                {
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionStateSetup);
                    return;
                }

                Thing gun = state.GunForRuntimeOnly;
                Verb attackVerb = this.cycleHost.GetAttackVerb(context);
                if (gun == null || gun.Destroyed || attackVerb == null)
                {
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionStateSetup);
                    return;
                }

                if (attackVerb.state != VerbState.Bursting)
                {
                    this.reloadController.ProcessIdleTopOff(context, weaponDef, ammoState);
                }

                if (this.fireControlPolicy.IsFireControlHardStopped(context, state))
                {
                    ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                    state.SetActivePowerTicksForRuntimeOnly(0);
                    diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionStateSetup);
                    return;
                }

                diagnostics.CompleteAndStart(ShuttleWeaponRuntimeProfiler.SectionStateSetup);
                this.cycleHost.TickVerbs(context);
                ShuttleWeaponCycleStateTransitions.DecrementActivePower(state);
                diagnostics.CompleteAndStart(ShuttleWeaponRuntimeProfiler.SectionMisc);
                this.reloadController.RecordDiagnostics(
                    context,
                    weaponDef,
                    ammoState,
                    diagnostics);
                diagnostics.CompleteAndStart(
                    ShuttleWeaponRuntimeProfiler.SectionReloadAmmoCheck);

                if (attackVerb.state == VerbState.Bursting)
                {
                    diagnostics.RecordBursting();
                    if (attackVerb.CurrentTarget.IsValid)
                    {
                        state.SetCurrentTargetForRuntimeOnly(attackVerb.CurrentTarget);
                    }

                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
                    return;
                }

                this.reloadController.ProcessIdleTopOff(context, weaponDef, ammoState);
                int cooldownTicks = state.GetCooldownTicksForRuntimeOnly();
                if (cooldownTicks > 0)
                {
                    diagnostics.RecordCooldown();
                    if (this.fastPathPolicy.CanUseCooldownFastPath(weaponDef, ammoState))
                    {
                        diagnostics.RecordCooldownFastPathHit();
                        state.SetCooldownTicksForRuntimeOnly(cooldownTicks - 1);
                        diagnostics.Complete(
                            ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
                        return;
                    }

                    diagnostics.RecordCooldownFastPathMiss(
                        this.fastPathPolicy.GetCooldownMissReason(weaponDef, ammoState));
                }

                if (this.fastPathPolicy.CanThrottleAutomaticReloadRetry(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        ammoState,
                        cooldownTicks))
                {
                    diagnostics.RecordAutomaticReloadRetryThrottled();
                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
                    return;
                }

                bool idleScanExecutedThisTick = false;
                if (this.scanScheduler.IsIdleAutoScanEligible(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        ammoState,
                        cooldownTicks))
                {
                    diagnostics.RecordIdleScanEligible();
                    if (!this.scanScheduler.ShouldRunIdleScanThisTick(
                            state,
                            context.ModuleInstanceID,
                            context.TicksGame,
                            weaponDef))
                    {
                        diagnostics.RecordIdleScanSkipped();
                        diagnostics.Complete(
                            ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
                        return;
                    }

                    diagnostics.RecordIdleScanExecuted();
                    idleScanExecutedThisTick = true;
                }

                this.cycleController.Advance(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    ammoState,
                    idleScanExecutedThisTick,
                    diagnostics);
            }
            finally
            {
                diagnostics.Finish();
            }
        }
    }
}
