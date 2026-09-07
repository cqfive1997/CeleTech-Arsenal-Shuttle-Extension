using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal sealed class CelestialSustainLaserForcedTargetEvaluator :
        IShuttleWeaponForcedTargetEvaluator
    {
        private readonly CelestialSustainLaserHost host;
        private readonly ShuttleWeaponEngagementEvaluator engagementEvaluator;

        internal CelestialSustainLaserForcedTargetEvaluator(
            CelestialSustainLaserHost host,
            ShuttleWeaponEngagementEvaluator engagementEvaluator)
        {
            this.host = host;
            this.engagementEvaluator = engagementEvaluator;
        }

        public ShuttleWeaponEngagementResult Evaluate(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target)
        {
            Verb verb = this.host != null ? this.host.GetVerb(context) : null;
            if (this.engagementEvaluator == null || verb == null)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            return this.engagementEvaluator.EvaluateForcedTarget(
                context,
                context != null
                    ? context.ModuleDef as ShuttleWeaponModuleDef
                    : null,
                context != null
                    ? context.State as ShuttleWeaponRuntimeState
                    : null,
                verb,
                target);
        }
    }
}
