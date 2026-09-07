using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponTargetingService
    {
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponTargetValidator targetValidator;
        private readonly ShuttleWeaponEngagementEvaluator engagementEvaluator;
        private readonly ShuttleWeaponTargetSelector targetSelector;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;
        private readonly ShuttlePointDefenseAssignmentService pointDefenseAssignments;

        internal ShuttleWeaponTargetingService(
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponTargetValidator targetValidator,
            ShuttleWeaponEngagementEvaluator engagementEvaluator,
            ShuttleWeaponTargetSelector targetSelector,
            ShuttleWeaponCyclePolicy cyclePolicy,
            ShuttlePointDefenseAssignmentService pointDefenseAssignments)
        {
            this.fireControlPolicy = fireControlPolicy;
            this.targetValidator = targetValidator;
            this.engagementEvaluator = engagementEvaluator;
            this.targetSelector = targetSelector;
            this.cyclePolicy = cyclePolicy;
            this.pointDefenseAssignments = pointDefenseAssignments;
        }

        internal bool CanContinueCurrentTarget(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            if (state == null)
            {
                return false;
            }

            LocalTargetInfo currentTarget = state.GetCurrentTargetForRuntimeOnly();
            if (!currentTarget.IsValid)
            {
                return true;
            }

            LocalTargetInfo forcedTarget = state.GetForcedTargetForRuntimeOnly();
            if (forcedTarget.IsValid && this.targetValidator.IsSameTarget(currentTarget, forcedTarget))
            {
                ShuttleWeaponEngagementResult result =
                    this.engagementEvaluator.EvaluateForcedTarget(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        forcedTarget);
                if (result.IsAllowed)
                {
                    return true;
                }

                state.SetLastForcedTargetFailureForRuntimeOnly(result.ReasonCode);
                state.ClearForcedTargetForRuntimeOnly();
                return false;
            }

            ShuttleWeaponEngagementResult automaticResult =
                this.engagementEvaluator.EvaluateAutomaticTarget(
                context,
                weaponDef,
                state,
                attackVerb,
                currentTarget);
            return automaticResult.IsAllowed &&
                (this.pointDefenseAssignments == null ||
                 this.pointDefenseAssignments.TryRefreshIfAssigned(
                    context,
                    weaponDef,
                    state,
                    currentTarget));
        }

        internal void TryStartShootSomething(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            LocalTargetInfo target = LocalTargetInfo.Invalid;
            LocalTargetInfo forcedTarget = state.GetForcedTargetForRuntimeOnly();
            if (forcedTarget.IsValid)
            {
                ShuttleWeaponEngagementResult result =
                    this.engagementEvaluator.EvaluateForcedTarget(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        forcedTarget);
                if (result.IsAllowed)
                {
                    target = forcedTarget;
                }
                else
                {
                    state.SetLastForcedTargetFailureForRuntimeOnly(result.ReasonCode);
                    state.ClearForcedTargetForRuntimeOnly();
                    state.ResetCurrentTargetForRuntimeOnly();
                    target = this.fireControlPolicy.CanUseAutomaticFireControl(context, weaponDef, state)
                        ? this.targetSelector.FindNewTarget(context, weaponDef, state, attackVerb)
                        : LocalTargetInfo.Invalid;
                }
            }
            else
            {
                LocalTargetInfo currentTarget = state.GetCurrentTargetForRuntimeOnly();
                if (!this.fireControlPolicy.CanUseAutomaticFireControl(context, weaponDef, state))
                {
                    state.ResetCurrentTargetForRuntimeOnly();
                    target = LocalTargetInfo.Invalid;
                }
                else
                {
                    target = this.IsValidAutoFireTargetNow(context, weaponDef, state, attackVerb, currentTarget)
                        ? currentTarget
                        : this.targetSelector.FindNewTarget(context, weaponDef, state, attackVerb);
                }
            }

            if (!target.IsValid ||
                !this.EvaluateSelectedTarget(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    target).IsAllowed)
            {
                state.ResetCurrentTargetForRuntimeOnly();
                return;
            }

            if (this.pointDefenseAssignments != null &&
                !this.pointDefenseAssignments.TryRefreshIfAssigned(
                    context,
                    weaponDef,
                    state,
                    target))
            {
                state.ResetCurrentTargetForRuntimeOnly();
                return;
            }

            state.SetCurrentTargetForRuntimeOnly(target);
            state.SetWarmupTicksForRuntimeOnly(this.cyclePolicy.GetWarmupTicks(weaponDef, attackVerb));
            ShuttleWeaponCycleBaselineRecorder.RecordTargetAcquired(
                context,
                weaponDef,
                state);
        }

        internal bool IsValidAutoFireTargetNow(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            return this.engagementEvaluator.EvaluateAutomaticTarget(
                context,
                weaponDef,
                state,
                attackVerb,
                target).IsAllowed;
        }

        private ShuttleWeaponEngagementResult EvaluateSelectedTarget(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            LocalTargetInfo forcedTarget = state != null
                ? state.GetForcedTargetForRuntimeOnly()
                : LocalTargetInfo.Invalid;
            if (forcedTarget.IsValid && this.targetValidator.IsSameTarget(target, forcedTarget))
            {
                return this.engagementEvaluator.EvaluateForcedTarget(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    target);
            }

            return this.engagementEvaluator.EvaluateAutomaticTarget(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
        }

    }
}
