using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Advances one ready, non-bursting weapon through operation checks, warmup/cooldown and target
    /// acquisition. Hidden-gun lifecycle and reload progression remain outside this class.
    /// </summary>
    internal sealed class ShuttleWeaponCycleController
    {
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponReloadRequestProcessor reloadRequestProcessor;
        private readonly ShuttleWeaponTargetingService targetingService;
        private readonly IShuttleWeaponBurstStarter burstStarter;
        private readonly ShuttleWeaponTargetScanScheduler scanScheduler;

        internal ShuttleWeaponCycleController(
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponReloadRequestProcessor reloadRequestProcessor,
            ShuttleWeaponTargetingService targetingService,
            IShuttleWeaponBurstStarter burstStarter,
            ShuttleWeaponTargetScanScheduler scanScheduler)
        {
            this.fireControlPolicy = fireControlPolicy;
            this.reloadRequestProcessor = reloadRequestProcessor;
            this.targetingService = targetingService;
            this.burstStarter = burstStarter;
            this.scanScheduler = scanScheduler;
        }

        internal void Advance(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            ShuttleWeaponAmmoState ammoState,
            bool idleScanExecutedThisTick,
            ShuttleWeaponTickDiagnostics diagnostics)
        {
            diagnostics.RecordFullPipeline(
                this.scanScheduler.GetFullPipelineReason(
                    weaponDef,
                    state,
                    attackVerb,
                    ammoState));
            if (!this.fireControlPolicy.CanOperate(context, weaponDef, state, attackVerb))
            {
                ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                state.SetActivePowerTicksForRuntimeOnly(0);
                diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionTargetValidation);
                return;
            }

            diagnostics.CompleteAndStart(ShuttleWeaponRuntimeProfiler.SectionTargetValidation);
            if (!this.reloadRequestProcessor.TryPrepareToFireOrStartReload(
                    context,
                    weaponDef,
                    ammoState))
            {
                ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                state.SetActivePowerTicksForRuntimeOnly(0);
                diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionPrepareFireStartReload);
                return;
            }

            diagnostics.CompleteAndStart(
                ShuttleWeaponRuntimeProfiler.SectionPrepareFireStartReload);
            if (!this.targetingService.CanContinueCurrentTarget(
                    context,
                    weaponDef,
                    state,
                    attackVerb))
            {
                ShuttleWeaponCycleStateTransitions.ResetCurrentTargetAndWarmup(state);
                diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionTargetValidation);
                return;
            }

            int warmupTicks = state.GetWarmupTicksForRuntimeOnly();
            if (warmupTicks > 0)
            {
                state.SetWarmupTicksForRuntimeOnly(warmupTicks - 1);
                if (state.GetWarmupTicksForRuntimeOnly() <= 0 && this.burstStarter != null)
                {
                    this.burstStarter.BeginBurst(context, weaponDef, state, attackVerb);
                }

                diagnostics.Complete(
                    ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
                return;
            }

            int cooldownTicks = state.GetCooldownTicksForRuntimeOnly();
            if (cooldownTicks > 0)
            {
                state.SetCooldownTicksForRuntimeOnly(cooldownTicks - 1);
                diagnostics.Complete(
                    ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
                return;
            }

            diagnostics.CompleteAndStart(
                ShuttleWeaponRuntimeProfiler.SectionFireExecutionCooldown);
            bool hasForcedTarget = state.GetForcedTargetForRuntimeOnly().IsValid;
            if (!hasForcedTarget &&
                !idleScanExecutedThisTick &&
                !this.scanScheduler.ShouldRunRegularScan(
                    context.Host,
                    weaponDef,
                    context.TicksGame))
            {
                diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionTargetValidation);
                return;
            }

            this.targetingService.TryStartShootSomething(
                context,
                weaponDef,
                state,
                attackVerb);
            diagnostics.Complete(ShuttleWeaponRuntimeProfiler.SectionTargetValidation);
        }
    }
}
