using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Applies the shared logging interval and asks focused metric stores to emit/reset.
    /// </summary>
    internal static class ShuttleWeaponMetricsLogCoordinator
    {
        private const int LogIntervalTicks = 2500;
        private static int lastLogTick = int.MinValue;

        internal static void MaybeLog(int ticksGame)
        {
            bool hasDetailedMetrics = ShuttleWeaponCycleMetricsStore.HasMetrics ||
                ShuttleWeaponReloadStateMetricsStore.HasMetrics ||
                ShuttleWeaponReloadCompletionMetricsStore.HasMetrics;
            if (!ShuttleDiagnosticGate.ShouldLogPerformanceSummaries ||
                ticksGame < 0 ||
                (!ShuttleWeaponSectionMetricsStore.HasMetrics && !hasDetailedMetrics))
            {
                return;
            }

            if (lastLogTick != int.MinValue &&
                ticksGame - lastLogTick < LogIntervalTicks)
            {
                return;
            }

            lastLogTick = ticksGame;
            ShuttleWeaponSectionMetricsStore.LogAndClear(ticksGame);
            if (hasDetailedMetrics)
            {
                ShuttleWeaponCycleMetricsStore.Log(ticksGame);
                ShuttleWeaponReloadStateMetricsStore.Log(ticksGame);
                ShuttleWeaponReloadBlockerMetricsStore.Log(ticksGame);
                ShuttleWeaponReloadCompletionMetricsStore.Log(ticksGame);
                ShuttleWeaponCycleMetricsStore.Clear();
                ShuttleWeaponReloadStateMetricsStore.Clear();
                ShuttleWeaponReloadBlockerMetricsStore.Clear();
                ShuttleWeaponReloadCompletionMetricsStore.Clear();
            }
        }
    }
}
