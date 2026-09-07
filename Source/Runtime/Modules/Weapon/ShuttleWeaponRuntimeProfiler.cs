using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum WeaponCooldownFastPathMissReason
    {
        ReloadInProgress,
        ReloadRequested,
        LoadedAmmoInsufficient,
        InvalidAmmoState,
        Other
    }

    internal enum WeaponFullPipelineReason
    {
        ForcedOrCurrentTarget,
        ReloadRelated,
        Other
    }

    /// <summary>
    /// Stable instrumentation facade used by runtime callers. Metric ownership, formatting and
    /// reset behavior live in separate focused stores.
    /// </summary>
    internal static class ShuttleWeaponRuntimeProfiler
    {
        internal const string SectionStateSetup = "weapon state setup";
        internal const string SectionTargetValidation = "target acquisition / validation";
        internal const string SectionReloadAmmoCheck = "reload service / ammo check";
        internal const string SectionPrepareFireStartReload = "prepare fire / start reload";
        internal const string SectionFireExecutionCooldown = "fire execution / cooldown";
        internal const string SectionMisc = "misc";
        internal const string SectionCeLifecycle = "CE lifecycle / binding";
        internal const string SectionCeReload = "CE reload / idle ammo";
        internal const string SectionCeCycle = "CE target scan / firing cycle";
        internal const string SectionParticleLanceLifecycle =
            "particle-lance lifecycle / binding";
        internal const string SectionParticleLanceVerbTick =
            "particle-lance active Verb tick";
        internal const string SectionParticleLanceCycle =
            "particle-lance target / cycle";
        internal const string SectionFacadeBinding =
            "weapon facade binding";
        internal const string SectionFacadeBackendDriver =
            "weapon backend driver";
        internal const string SectionAutomaticTargetScan =
            "automatic target scan [cache miss]";
        internal const string SectionAutomaticTargetCacheHit =
            "automatic target scan [cache hit]";

        internal static bool Enabled
        {
            get { return ShuttleDiagnosticGate.ShouldCollectPerformanceSamples; }
        }

        internal static long StartSection(bool enabled)
        {
            return ShuttleWeaponSectionMetricsStore.Start(enabled);
        }

        internal static void Record(
            bool enabled,
            string sectionName,
            long startTimestamp)
        {
            ShuttleWeaponSectionMetricsStore.Record(
                enabled,
                sectionName,
                startTimestamp);
        }

        internal static void RecordTotalWeaponTick()
        {
            ShuttleWeaponCycleMetricsStore.RecordTotalWeaponTick();
        }

        internal static void RecordBurstingTick()
        {
            ShuttleWeaponCycleMetricsStore.RecordBurstingTick();
        }

        internal static void RecordCooldownTick()
        {
            ShuttleWeaponCycleMetricsStore.RecordCooldownTick();
        }

        internal static void RecordCooldownFastPathHit()
        {
            ShuttleWeaponCycleMetricsStore.RecordCooldownFastPathHit();
        }

        internal static void RecordCooldownFastPathMiss(WeaponCooldownFastPathMissReason reason)
        {
            ShuttleWeaponCycleMetricsStore.RecordCooldownFastPathMiss(reason);
        }

        internal static void RecordReloadStateTick(
            ShuttleWeaponAmmoState ammoState,
            bool canFireLoadedAmmo,
            ShuttleWeaponReloadRequestKind requestKind,
            bool automaticRetryAllowed,
            bool reloadNeeded)
        {
            ShuttleWeaponReloadStateMetricsStore.Record(
                ammoState,
                canFireLoadedAmmo,
                requestKind,
                automaticRetryAllowed,
                reloadNeeded);
        }

        internal static void RecordIdleScanEligibleTick()
        {
            ShuttleWeaponCycleMetricsStore.RecordIdleScanEligibleTick();
        }

        internal static void RecordIdleScanSkippedTick()
        {
            ShuttleWeaponCycleMetricsStore.RecordIdleScanSkippedTick();
        }

        internal static void RecordIdleScanExecutedTick()
        {
            ShuttleWeaponCycleMetricsStore.RecordIdleScanExecutedTick();
        }

        internal static void RecordFullPipelineTick(WeaponFullPipelineReason reason)
        {
            ShuttleWeaponCycleMetricsStore.RecordFullPipelineTick(reason);
        }

        internal static void RecordAutomaticReloadRetryThrottledEarlyReturn()
        {
            ShuttleWeaponCycleMetricsStore.RecordAutomaticReloadRetryThrottled();
        }

        internal static void RecordReloadCompletion(
            bool fullReload,
            bool partialReload,
            bool continuedRequiredForFire,
            bool continuedAutoTopOff,
            bool stoppedManual,
            bool stoppedSourceEmpty)
        {
            ShuttleWeaponReloadCompletionMetricsStore.Record(
                fullReload,
                partialReload,
                continuedRequiredForFire,
                continuedAutoTopOff,
                stoppedManual,
                stoppedSourceEmpty);
        }

        internal static void MaybeLog(int ticksGame)
        {
            ShuttleWeaponMetricsLogCoordinator.MaybeLog(ticksGame);
        }
    }
}
