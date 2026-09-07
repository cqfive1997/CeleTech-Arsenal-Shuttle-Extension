using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Runs one CE-owned firing cycle through existing shuttle target and timing policies.
    /// Reload, cargo and backend selection remain outside this driver.
    /// </summary>
    internal sealed class CeWeaponCycleDriver
    {
        private const int FailedStartCooldownTicks = 15;

        private readonly CeWeaponShotLifecycle shotLifecycle;
        private readonly CeWeaponVerbTrackerDriver verbTrackerDriver;
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponTargetingService targetingService;
        private readonly ShuttleWeaponEngagementEvaluator engagementEvaluator;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal CeWeaponCycleDriver(
            CeWeaponShotLifecycle shotLifecycle,
            CeWeaponVerbTrackerDriver verbTrackerDriver,
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponTargetingService targetingService,
            ShuttleWeaponEngagementEvaluator engagementEvaluator,
            ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.shotLifecycle = shotLifecycle;
            this.verbTrackerDriver = verbTrackerDriver;
            this.fireControlPolicy = fireControlPolicy;
            this.targetingService = targetingService;
            this.engagementEvaluator = engagementEvaluator;
            this.cyclePolicy = cyclePolicy;
        }

        internal bool TryBind(
            ShuttleModuleRuntimeContext context,
            Action completionObserver,
            out CeWeaponMuzzleVerb verb,
            out CeWeaponAmmoOwnerAdapter owner,
            out int shotsPerBurst,
            out string failureReason)
        {
            verb = null;
            owner = null;
            shotsPerBurst = 0;
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Thing gun = state != null ? state.GunForRuntimeOnly : null;
            if (context == null || weaponDef == null || state == null || gun == null)
            {
                failureReason = "ce-cycle-bind-context-missing";
                return false;
            }

            CeWeaponMuzzleVerb boundVerb = null;
            if (!this.shotLifecycle.TryBind(
                    gun,
                    context.Host,
                    context.ModuleInstanceID,
                    context.ParentSlotID,
                    weaponDef,
                    state,
                    delegate
                    {
                        this.HandleCastComplete(
                            state,
                            weaponDef,
                            boundVerb,
                            completionObserver);
                    },
                    out boundVerb,
                    out owner,
                    out failureReason))
            {
                verb = boundVerb;
                return false;
            }

            verb = boundVerb;
            if (!this.shotLifecycle.TrySetFullBurstMode(
                    gun,
                    verb,
                    global::CombatExtended.AimMode.Snapshot,
                    out shotsPerBurst,
                    out failureReason))
            {
                return false;
            }

            state.MarkVerbsBoundForRuntimeOnly(context.Host, gun);
            return true;
        }

        internal ShuttleWeaponEngagementResult EvaluateForcedTarget(
            ShuttleModuleRuntimeContext context,
            CeWeaponMuzzleVerb verb,
            LocalTargetInfo target)
        {
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (this.engagementEvaluator == null || weaponDef == null ||
                state == null || verb == null)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            return this.engagementEvaluator.EvaluateForcedTarget(
                context,
                weaponDef,
                state,
                verb,
                target);
        }

        internal bool Tick(
            ShuttleModuleRuntimeContext context,
            CeWeaponMuzzleVerb verb,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Thing gun = state != null ? state.GunForRuntimeOnly : null;
            if (context == null || weaponDef == null || state == null ||
                gun == null || verb == null)
            {
                failureReason = "ce-cycle-tick-context-missing";
                return false;
            }

            if (this.fireControlPolicy.IsFireControlHardStopped(context, state))
            {
                this.ResetTargetAndWarmup(state);
                state.SetActivePowerTicksForRuntimeOnly(0);
                return true;
            }

            if (!this.verbTrackerDriver.TryTick(gun, out failureReason))
            {
                return false;
            }

            this.DecrementActivePowerTicks(state);
            if (verb.state == VerbState.Bursting)
            {
                if (verb.CurrentTarget.IsValid)
                {
                    state.SetCurrentTargetForRuntimeOnly(verb.CurrentTarget);
                }

                return true;
            }

            int cooldownTicks = state.GetCooldownTicksForRuntimeOnly();
            if (cooldownTicks > 0)
            {
                state.SetCooldownTicksForRuntimeOnly(cooldownTicks - 1);
                return true;
            }

            if (!this.fireControlPolicy.CanOperate(context, weaponDef, state, verb))
            {
                this.ResetTargetAndWarmup(state);
                state.SetActivePowerTicksForRuntimeOnly(0);
                return true;
            }

            if (!this.targetingService.CanContinueCurrentTarget(
                    context,
                    weaponDef,
                    state,
                    verb))
            {
                this.ResetTargetAndWarmup(state);
                return true;
            }

            int warmupTicks = state.GetWarmupTicksForRuntimeOnly();
            if (warmupTicks > 0)
            {
                state.SetWarmupTicksForRuntimeOnly(warmupTicks - 1);
                if (state.GetWarmupTicksForRuntimeOnly() <= 0)
                {
                    this.TryBeginBurst(context, weaponDef, state, verb);
                }

                return true;
            }

            this.targetingService.TryStartShootSomething(
                context,
                weaponDef,
                state,
                verb);
            return true;
        }

        private void TryBeginBurst(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            CeWeaponMuzzleVerb verb)
        {
            LocalTargetInfo target = state.GetCurrentTargetForRuntimeOnly();
            string failureReason;
            if (!target.IsValid ||
                !this.fireControlPolicy.CanOperate(context, weaponDef, state, verb) ||
                !this.targetingService.CanContinueCurrentTarget(
                    context,
                    weaponDef,
                    state,
                    verb) ||
                !this.shotLifecycle.TryStart(verb, target, out failureReason))
            {
                this.ResetTargetAndWarmup(state);
                state.SetCooldownTicksForRuntimeOnly(FailedStartCooldownTicks);
                return;
            }

            state.SetActivePowerTicksForRuntimeOnly(
                Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
        }

        private void HandleCastComplete(
            ShuttleWeaponRuntimeState state,
            ShuttleWeaponModuleDef weaponDef,
            CeWeaponMuzzleVerb verb,
            Action completionObserver)
        {
            if (state == null)
            {
                return;
            }

            state.SetCooldownTicksForRuntimeOnly(
                this.cyclePolicy.GetCooldownTicks(weaponDef, verb));
            state.SetActivePowerTicksForRuntimeOnly(
                Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
            if (verb != null && verb.CurrentTarget.IsValid)
            {
                state.SetCurrentTargetForRuntimeOnly(verb.CurrentTarget);
            }
            else
            {
                state.ResetCurrentTargetForRuntimeOnly();
            }

            if (completionObserver == null)
            {
                return;
            }

            try
            {
                completionObserver();
            }
            catch (Exception exception)
            {
                Log.Error("[CeleTech Shuttle][CE Backend] completion observer threw " +
                    exception.GetType().Name + ".");
            }
        }

        private void DecrementActivePowerTicks(ShuttleWeaponRuntimeState state)
        {
            int activeTicks = state.GetActivePowerTicksForRuntimeOnly();
            if (activeTicks > 0)
            {
                state.SetActivePowerTicksForRuntimeOnly(activeTicks - 1);
            }
        }

        private void ResetTargetAndWarmup(ShuttleWeaponRuntimeState state)
        {
            state.ResetCurrentTargetForRuntimeOnly();
            state.SetWarmupTicksForRuntimeOnly(0);
        }
    }
}
