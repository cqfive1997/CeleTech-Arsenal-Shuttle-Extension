using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Supplies the Native-selected attack Verb to the shared engagement evaluator.
    /// </summary>
    internal sealed class NativeVerbWeaponForcedTargetEvaluator :
        IShuttleWeaponForcedTargetEvaluator
    {
        private readonly IShuttleWeaponCycleHost cycleHost;
        private readonly ShuttleWeaponEngagementEvaluator engagementEvaluator;

        internal NativeVerbWeaponForcedTargetEvaluator(
            IShuttleWeaponCycleHost cycleHost,
            ShuttleWeaponEngagementEvaluator engagementEvaluator)
        {
            this.cycleHost = cycleHost;
            this.engagementEvaluator = engagementEvaluator;
        }

        public ShuttleWeaponEngagementResult Evaluate(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target)
        {
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Verb attackVerb = this.cycleHost != null
                ? this.cycleHost.GetAttackVerb(context)
                : null;
            if (this.engagementEvaluator == null || attackVerb == null)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            return this.engagementEvaluator.EvaluateForcedTarget(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
        }
    }
}
