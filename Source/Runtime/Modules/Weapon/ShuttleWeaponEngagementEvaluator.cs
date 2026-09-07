using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Applies shared forced/automatic engagement rules to one already-selected target.
    /// Candidate enumeration, scoring, state mutation and firing remain outside this class.
    /// </summary>
    internal sealed class ShuttleWeaponEngagementEvaluator
    {
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponTargetValidator targetValidator;
        private readonly ShuttleWeaponTargetRangePolicy targetRangePolicy;
        private readonly ShuttleWeaponPointDefenseEvaluator pointDefenseEvaluator;

        internal ShuttleWeaponEngagementEvaluator(
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponTargetValidator targetValidator,
            ShuttleWeaponTargetRangePolicy targetRangePolicy,
            ShuttleWeaponPointDefenseEvaluator pointDefenseEvaluator)
        {
            this.fireControlPolicy = fireControlPolicy;
            this.targetValidator = targetValidator;
            this.targetRangePolicy = targetRangePolicy;
            this.pointDefenseEvaluator = pointDefenseEvaluator;
        }

        internal ShuttleWeaponEngagementResult EvaluateForcedTarget(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            if (weaponDef == null || !weaponDef.canSetForcedTarget)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.ForcedTargetUnsupported);
            }

            return this.targetValidator.EvaluateForcedTargetNow(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
        }

        internal ShuttleWeaponEngagementResult EvaluateAutomaticTarget(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            if (!this.fireControlPolicy.CanUseAutomaticFireControl(context, weaponDef, state) ||
                state.TargetPriority == ShuttleWeaponTargetPriority.ForcedTargetOnly)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.AutomaticFireUnavailable);
            }

            if (!target.HasThing)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.AutomaticTargetMustBeThing);
            }

            if (state.FireControlMode == ShuttleWeaponFireControlMode.PointDefense)
            {
                return this.pointDefenseEvaluator != null
                    ? this.pointDefenseEvaluator.Evaluate(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        target)
                    : ShuttleWeaponEngagementResult.Rejected(
                        ShuttleWeaponEngagementFailure.ProjectileInterceptionUnsupported);
            }

            if (this.fireControlPolicy.IsUsingFallbackAutoDefense(context, weaponDef, state) &&
                !this.targetRangePolicy.IsWithinFallbackAutoFireRange(
                    context,
                    weaponDef,
                    attackVerb,
                    target.Cell))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetOutsideFallbackRange);
            }

            return this.targetValidator.EvaluateAutomaticTargetNow(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
        }
    }
}
