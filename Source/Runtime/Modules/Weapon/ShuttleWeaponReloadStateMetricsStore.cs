using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns active reload-state counters. Request/blocker classification is delegated separately.
    /// </summary>
    internal static class ShuttleWeaponReloadStateMetricsStore
    {
        private static long reloadInProgressTicks;
        private static long reloadRequestedTicks;
        private static long manualReloadJobActiveTicks;
        private static long multipleReloadFlagsTicks;
        private static long reloadRequestedButCanFireTicks;
        private static long reloadRequestedAndCannotFireTicks;

        internal static bool HasMetrics
        {
            get
            {
                return reloadInProgressTicks > 0L ||
                    reloadRequestedTicks > 0L ||
                    manualReloadJobActiveTicks > 0L;
            }
        }

        internal static void Record(
            ShuttleWeaponAmmoState ammoState,
            bool canFireLoadedAmmo,
            ShuttleWeaponReloadRequestKind requestKind,
            bool automaticRetryAllowed,
            bool reloadNeeded)
        {
            if (!ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns ||
                ammoState == null)
            {
                return;
            }

            int activeFlags = 0;
            if (ammoState.ReloadInProgress)
            {
                reloadInProgressTicks++;
                activeFlags++;
            }
            else if (ammoState.ReloadRequested)
            {
                reloadRequestedTicks++;
                activeFlags++;
                if (canFireLoadedAmmo)
                {
                    reloadRequestedButCanFireTicks++;
                }
                else
                {
                    reloadRequestedAndCannotFireTicks++;
                }

                ShuttleWeaponReloadBlockerMetricsStore.Record(
                    ammoState,
                    canFireLoadedAmmo,
                    reloadNeeded,
                    requestKind,
                    automaticRetryAllowed);
            }

            if (ammoState.ManualReloadJobActive)
            {
                manualReloadJobActiveTicks++;
                activeFlags++;
            }

            if (activeFlags > 1)
            {
                multipleReloadFlagsTicks++;
            }
        }

        internal static void Log(int ticksGame)
        {
            if (!ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                return;
            }

            Verse.Log.Message(
                "[CeleTech Shuttle] PerfSummary weapon reload state profile" +
                " ticksGame=" + ticksGame +
                "\n - reload-related full pipeline ticks: " +
                ShuttleWeaponCycleMetricsStore.ReloadRelatedFullPipelineTicks +
                "\n - reload in progress ticks: " + reloadInProgressTicks +
                "\n - reload requested ticks: " + reloadRequestedTicks +
                "\n - manual reload job active ticks: " + manualReloadJobActiveTicks +
                "\n - multiple reload flags ticks: " + multipleReloadFlagsTicks +
                "\n - reload requested but can fire ticks: " +
                reloadRequestedButCanFireTicks +
                "\n - reload requested and cannot fire ticks: " +
                reloadRequestedAndCannotFireTicks +
                "\n - automatic reload retry throttled early returns: " +
                ShuttleWeaponCycleMetricsStore.AutomaticRetryThrottledEarlyReturnTicks);
        }

        internal static void Clear()
        {
            reloadInProgressTicks = 0L;
            reloadRequestedTicks = 0L;
            manualReloadJobActiveTicks = 0L;
            multipleReloadFlagsTicks = 0L;
            reloadRequestedButCanFireTicks = 0L;
            reloadRequestedAndCannotFireTicks = 0L;
        }
    }
}
