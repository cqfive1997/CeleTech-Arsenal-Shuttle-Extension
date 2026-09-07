namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Production composition root for simple, single-channel Verse Verb weapons. It keeps the
    /// core magazine/reload state authoritative while Native owns hidden-gun lifecycle and firing.
    /// </summary>
    internal sealed class NativeVerbWeaponBackendFactory : IShuttleWeaponBackendFactory
    {
        internal const string Id = "native";

        private readonly NativeVerbWeaponCompatibilityEvaluator compatibilityEvaluator;
        private readonly ShuttleWeaponBackendBinding binding;

        internal NativeVerbWeaponBackendFactory()
        {
            this.compatibilityEvaluator = new NativeVerbWeaponCompatibilityEvaluator();

            ShuttleWeaponReloadPowerCoordinator reloadPowerCoordinator =
                new ShuttleWeaponReloadPowerCoordinator();
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions =
                new ShuttleWeaponAmmoDefinitionCatalog();
            CoreMagazineDriver coreMagazine = new CoreMagazineDriver(ammoDefinitions);
            ShuttleWeaponReloadCoordinator reloadCoordinator =
                new ShuttleWeaponReloadCoordinator();
            ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver =
                new ShuttleWeaponAmmoSupplyResolver();
            ShuttleWeaponAmmoLoaderAvailability loaderAvailability =
                new ShuttleWeaponAmmoLoaderAvailability();
            ShuttleWeaponReloadCompletionTransaction reloadCompletionTransaction =
                new ShuttleWeaponReloadCompletionTransaction(
                    coreMagazine,
                    new ShuttleWeaponAmmoTransferCommitter());
            ShuttleWeaponReloadCompletionOrchestrator completionOrchestrator =
                new ShuttleWeaponReloadCompletionOrchestrator(
                    ammoDefinitions,
                    coreMagazine,
                    reloadCoordinator,
                    reloadCompletionTransaction);
            ShuttleWeaponAutomaticReloadEligibility automaticReloadEligibility =
                new ShuttleWeaponAutomaticReloadEligibility(
                    loaderAvailability,
                    ammoSupplyResolver,
                    ammoDefinitions,
                    coreMagazine,
                    reloadCompletionTransaction);
            ShuttleWeaponAutomaticReloadExecutor automaticReloadExecutor =
                new ShuttleWeaponAutomaticReloadExecutor(
                    reloadPowerCoordinator,
                    ammoSupplyResolver,
                    ammoDefinitions,
                    coreMagazine,
                    reloadCoordinator,
                    reloadCompletionTransaction,
                    completionOrchestrator);
            ShuttleWeaponReloadRequestProcessor reloadRequestProcessor =
                new ShuttleWeaponReloadRequestProcessor(
                    loaderAvailability,
                    automaticReloadEligibility,
                    automaticReloadExecutor,
                    ammoDefinitions,
                    coreMagazine,
                    reloadCoordinator);
            ShuttleWeaponManualReloadHandler manualReloadHandler =
                new ShuttleWeaponManualReloadHandler(
                    loaderAvailability,
                    automaticReloadEligibility,
                    ammoSupplyResolver,
                    ammoDefinitions,
                    coreMagazine,
                    reloadCoordinator,
                    completionOrchestrator);
            ShuttleWeaponManualReloadClaimReconciler manualReloadClaimReconciler =
                new ShuttleWeaponManualReloadClaimReconciler();
            ShuttleWeaponMuzzleResolver muzzleResolver = new ShuttleWeaponMuzzleResolver();
            ShuttleWeaponBallisticTargetValidator ballisticTargetValidator =
                new ShuttleWeaponBallisticTargetValidator(muzzleResolver);
            ShuttleWeaponTargetValidator targetValidator =
                new ShuttleWeaponTargetValidator(ballisticTargetValidator);
            ShuttleWeaponFireControlPolicy fireControlPolicy =
                new ShuttleWeaponFireControlPolicy(ballisticTargetValidator);
            ShuttleWeaponTargetRangePolicy targetRangePolicy =
                new ShuttleWeaponTargetRangePolicy();
            ShuttleWeaponCyclePolicy cyclePolicy = new ShuttleWeaponCyclePolicy();
            ShuttlePointDefenseAssignmentService pointDefenseAssignments =
                new ShuttlePointDefenseAssignmentService();
            ShuttleWeaponPointDefenseEvaluator pointDefenseEvaluator =
                new ShuttleWeaponPointDefenseEvaluator(
                    ShuttleProjectileThreatAdapterRegistry.Shared,
                    ballisticTargetValidator,
                    targetRangePolicy,
                    cyclePolicy);
            ShuttleWeaponEngagementEvaluator engagementEvaluator =
                new ShuttleWeaponEngagementEvaluator(
                    fireControlPolicy,
                    targetValidator,
                    targetRangePolicy,
                    pointDefenseEvaluator);
            ShuttleWeaponTargetScorer targetScorer = new ShuttleWeaponTargetScorer();
            ShuttleWeaponPointDefenseTargetSelector pointDefenseTargetSelector =
                new ShuttleWeaponPointDefenseTargetSelector(
                    pointDefenseEvaluator,
                    pointDefenseAssignments);
            ShuttleWeaponTargetSelector targetSelector =
                new ShuttleWeaponTargetSelector(
                    fireControlPolicy,
                    engagementEvaluator,
                    targetScorer,
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

            NativeVerbWeaponHost host = new NativeVerbWeaponHost();
            NativeVerbWeaponFireDriver fireDriver = new NativeVerbWeaponFireDriver(
                host,
                muzzleResolver,
                fireControlPolicy,
                cyclePolicy);
            NativeVerbWeaponLifecycleDriver lifecycle =
                new NativeVerbWeaponLifecycleDriver(host, fireDriver);
            NativeVerbWeaponBurstStarter burstStarter =
                new NativeVerbWeaponBurstStarter(
                    reloadRequestProcessor,
                    coreMagazine,
                    targetingService,
                    fireDriver);
            ShuttleWeaponTickReloadController tickReloadController =
                new ShuttleWeaponTickReloadController(
                    automaticReloadExecutor,
                    reloadRequestProcessor,
                    coreMagazine,
                    manualReloadClaimReconciler);
            ShuttleWeaponTickFastPathPolicy tickFastPathPolicy =
                new ShuttleWeaponTickFastPathPolicy(ammoDefinitions, coreMagazine);
            ShuttleWeaponTargetScanScheduler targetScanScheduler =
                new ShuttleWeaponTargetScanScheduler(
                    tickFastPathPolicy,
                    coreMagazine,
                    fireControlPolicy);
            ShuttleWeaponCycleController cycleController =
                new ShuttleWeaponCycleController(
                    fireControlPolicy,
                    reloadRequestProcessor,
                    targetingService,
                    burstStarter,
                    targetScanScheduler);
            NativeVerbWeaponTickDriver tickDriver = new NativeVerbWeaponTickDriver(
                tickReloadController,
                tickFastPathPolicy,
                targetScanScheduler,
                cycleController,
                lifecycle,
                fireControlPolicy);
            NativeVerbWeaponChannelDriver channelDriver =
                new NativeVerbWeaponChannelDriver(host);
            CoreWeaponAmmoReadDriver ammoReader =
                new CoreWeaponAmmoReadDriver(
                    ammoSupplyResolver,
                    ammoDefinitions,
                    coreMagazine);
            CoreWeaponAmmoCommandDriver ammoCommands =
                new CoreWeaponAmmoCommandDriver(
                    ammoDefinitions,
                    coreMagazine,
                    reloadCoordinator);
            CoreWeaponManualReloadDriver manualReload =
                new CoreWeaponManualReloadDriver(manualReloadHandler, coreMagazine);
            ShuttleWeaponRemovalRefundContributor removalRefund =
                new ShuttleWeaponRemovalRefundContributor(coreMagazine);

            this.binding = new ShuttleWeaponBackendBinding(
                Id,
                lifecycle,
                tickDriver,
                new NativeVerbWeaponPowerDemandDriver(
                    ammoDefinitions,
                    coreMagazine,
                    reloadPowerCoordinator,
                    lifecycle,
                    cyclePolicy),
                new NativeVerbWeaponForcedTargetEvaluator(
                    lifecycle,
                    engagementEvaluator),
                fireDriver,
                channelDriver,
                ammoReader,
                ammoCommands,
                manualReload,
                removalRefund);
        }

        public string BackendId
        {
            get { return Id; }
        }

        public int Priority
        {
            get { return 10; }
        }

        internal NativeVerbWeaponCompatibilityEvaluator CompatibilityEvaluator
        {
            get { return this.compatibilityEvaluator; }
        }

        public ShuttleWeaponCompatibilityReport Probe(ShuttleWeaponBackendProbeContext context)
        {
            return this.compatibilityEvaluator.Evaluate(context);
        }

        public ShuttleWeaponBackendBinding CreateBinding(ShuttleWeaponBackendProbeContext context)
        {
            ShuttleWeaponCompatibilityReport report = this.compatibilityEvaluator.Evaluate(context);
            return report != null && report.IsSupported ? this.binding : null;
        }
    }
}
