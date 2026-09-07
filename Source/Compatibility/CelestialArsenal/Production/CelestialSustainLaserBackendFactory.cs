using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Composition root for the typed shuttle-owned sustained-laser backend.
    /// </summary>
    internal sealed class CelestialSustainLaserBackendFactory : IShuttleWeaponBackendFactory
    {
        internal const string Id = "shuttle-particle-lance";
        internal const string SupportedReason =
            "particle-lance-shuttle-shape";

        private readonly CelestialSustainLaserCompatibilityEvaluator evaluator;
        private readonly ShuttleWeaponBackendBinding binding;

        internal CelestialSustainLaserBackendFactory()
        {
            this.evaluator = new CelestialSustainLaserCompatibilityEvaluator();

            ShuttleWeaponMuzzleResolver muzzleResolver = new ShuttleWeaponMuzzleResolver();
            ShuttleWeaponBallisticTargetValidator ballisticValidator =
                new ShuttleWeaponBallisticTargetValidator(muzzleResolver);
            ShuttleWeaponTargetValidator targetValidator =
                new ShuttleWeaponTargetValidator(ballisticValidator);
            ShuttleWeaponFireControlPolicy fireControlPolicy =
                new ShuttleWeaponFireControlPolicy(ballisticValidator);
            ShuttleWeaponEngagementEvaluator engagementEvaluator =
                new ShuttleWeaponEngagementEvaluator(
                    fireControlPolicy,
                    targetValidator,
                    new ShuttleWeaponTargetRangePolicy(),
                    null);
            ShuttleWeaponTargetSelector targetSelector =
                new ShuttleWeaponTargetSelector(
                    fireControlPolicy,
                    engagementEvaluator,
                    new ShuttleWeaponTargetScorer(),
                    null,
                    new ShuttleWeaponAutomaticTargetScanCache(),
                    ShuttleWeaponAttackTargetCandidateSource.Shared);
            ShuttleWeaponCyclePolicy cyclePolicy = new ShuttleWeaponCyclePolicy();
            ShuttleWeaponTargetingService targetingService =
                new ShuttleWeaponTargetingService(
                    fireControlPolicy,
                    targetValidator,
                    engagementEvaluator,
                    targetSelector,
                    cyclePolicy,
                    null);

            CelestialSustainLaserHost host =
                new CelestialSustainLaserHost(muzzleResolver);
            CelestialSustainLaserFireDriver fireDriver =
                new CelestialSustainLaserFireDriver(host, muzzleResolver, cyclePolicy);
            CelestialSustainLaserLifecycleDriver lifecycle =
                new CelestialSustainLaserLifecycleDriver(host, fireDriver);
            CelestialSustainLaserNoAmmoDriver noAmmo =
                new CelestialSustainLaserNoAmmoDriver();

            this.binding = new ShuttleWeaponBackendBinding(
                Id,
                lifecycle,
                new CelestialSustainLaserTickDriver(
                    host,
                    lifecycle,
                    fireControlPolicy,
                    targetingService,
                    new CelestialSustainLaserBurstStarter(
                        targetValidator,
                        fireDriver)),
                new CelestialSustainLaserPowerDemandDriver(host, cyclePolicy),
                new CelestialSustainLaserForcedTargetEvaluator(
                    host,
                    engagementEvaluator),
                fireDriver,
                new CelestialSustainLaserChannelDriver(host),
                noAmmo,
                noAmmo,
                noAmmo,
                noAmmo);
        }

        public string BackendId
        {
            get { return Id; }
        }

        public int Priority
        {
            get { return 50; }
        }

        public ShuttleWeaponCompatibilityReport Probe(ShuttleWeaponBackendProbeContext context)
        {
            return this.evaluator.Evaluate(context);
        }

        public ShuttleWeaponBackendBinding CreateBinding(
            ShuttleWeaponBackendProbeContext context)
        {
            ShuttleWeaponCompatibilityReport report = this.evaluator.Evaluate(context);
            return report != null && report.IsSupported ? this.binding : null;
        }
    }
}
