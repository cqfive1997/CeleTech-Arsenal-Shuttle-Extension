using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Stateless composition root for the CE firing-only facet. It creates no gun and owns no
    /// installed state, cargo transaction or backend selection.
    /// </summary>
    internal sealed class CeWeaponFiringComposition
    {
        internal CeWeaponFiringComposition()
        {
            ShuttleWeaponMuzzleResolver muzzleResolver =
                new ShuttleWeaponMuzzleResolver();
            ShuttleWeaponBallisticTargetValidator ballisticValidator =
                new ShuttleWeaponBallisticTargetValidator(muzzleResolver);
            ShuttleWeaponTargetValidator targetValidator =
                new ShuttleWeaponTargetValidator(ballisticValidator);
            ShuttleWeaponFireControlPolicy fireControlPolicy =
                new ShuttleWeaponFireControlPolicy(ballisticValidator);
            ShuttleWeaponTargetRangePolicy rangePolicy =
                new ShuttleWeaponTargetRangePolicy();
            ShuttleWeaponCyclePolicy cyclePolicy = new ShuttleWeaponCyclePolicy();
            ShuttlePointDefenseAssignmentService pointDefenseAssignments =
                new ShuttlePointDefenseAssignmentService();
            ShuttleWeaponPointDefenseEvaluator pointDefenseEvaluator =
                new ShuttleWeaponPointDefenseEvaluator(
                    ShuttleProjectileThreatAdapterRegistry.Shared,
                    ballisticValidator,
                    rangePolicy,
                    cyclePolicy);
            ShuttleWeaponEngagementEvaluator engagementEvaluator =
                new ShuttleWeaponEngagementEvaluator(
                    fireControlPolicy,
                    targetValidator,
                    rangePolicy,
                    pointDefenseEvaluator);
            ShuttleWeaponPointDefenseTargetSelector pointDefenseTargetSelector =
                new ShuttleWeaponPointDefenseTargetSelector(
                    pointDefenseEvaluator,
                    pointDefenseAssignments);
            ShuttleWeaponTargetSelector targetSelector =
                new ShuttleWeaponTargetSelector(
                    fireControlPolicy,
                    engagementEvaluator,
                    new ShuttleWeaponTargetScorer(),
                    pointDefenseTargetSelector,
                    null,
                    ShuttleWeaponAttackTargetCandidateSource.Shared);
            ShuttleWeaponTargetingService targetingService =
                new ShuttleWeaponTargetingService(
                    fireControlPolicy,
                    targetValidator,
                    engagementEvaluator,
                    targetSelector,
                    cyclePolicy,
                    pointDefenseAssignments);

            this.CyclePolicy = cyclePolicy;
            this.CycleDriver = new CeWeaponCycleDriver(
                new CeWeaponShotLifecycle(),
                new CeWeaponVerbTrackerDriver(),
                fireControlPolicy,
                targetingService,
                engagementEvaluator,
                cyclePolicy);
            this.PowerProjection = new CeWeaponFiringPowerProjection(cyclePolicy);
        }

        internal ShuttleWeaponCyclePolicy CyclePolicy { get; private set; }

        internal CeWeaponCycleDriver CycleDriver { get; private set; }

        internal CeWeaponFiringPowerProjection PowerProjection { get; private set; }
    }
}
