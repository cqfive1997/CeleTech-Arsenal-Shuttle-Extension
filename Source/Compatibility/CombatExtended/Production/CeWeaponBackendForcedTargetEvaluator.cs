using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponBackendForcedTargetEvaluator :
        IShuttleWeaponForcedTargetEvaluator
    {
        private readonly CeWeaponCycleDriver cycleDriver;

        internal CeWeaponBackendForcedTargetEvaluator(CeWeaponCycleDriver cycleDriver)
        {
            this.cycleDriver = cycleDriver;
        }

        public ShuttleWeaponEngagementResult Evaluate(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            CeWeaponMuzzleVerb verb = state != null
                ? CeWeaponRuntimeGunAccess.GetMuzzleVerb(state.GunForRuntimeOnly)
                : null;
            if (state == null || verb == null || this.cycleDriver == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly != CeWeaponBackendFactory.Id)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            return this.cycleDriver.EvaluateForcedTarget(context, verb, target);
        }
    }
}
