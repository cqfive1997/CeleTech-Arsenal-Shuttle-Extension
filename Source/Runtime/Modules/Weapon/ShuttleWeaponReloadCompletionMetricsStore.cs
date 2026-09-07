using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns only reload-completion outcome counters and their summary message.
    /// </summary>
    internal static class ShuttleWeaponReloadCompletionMetricsStore
    {
        private static long completedReloads;
        private static long completedFullReloads;
        private static long completedPartialReloads;
        private static long partialReloadContinuedRequiredForFire;
        private static long partialReloadContinuedAutoTopOff;
        private static long partialReloadStoppedManual;
        private static long partialReloadStoppedSourceEmpty;

        internal static bool HasMetrics
        {
            get { return completedReloads > 0L; }
        }

        internal static void Record(
            bool fullReload,
            bool partialReload,
            bool continuedRequiredForFire,
            bool continuedAutoTopOff,
            bool stoppedManual,
            bool stoppedSourceEmpty)
        {
            if (!ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                return;
            }

            completedReloads++;
            completedFullReloads += fullReload ? 1L : 0L;
            completedPartialReloads += partialReload ? 1L : 0L;
            partialReloadContinuedRequiredForFire +=
                continuedRequiredForFire ? 1L : 0L;
            partialReloadContinuedAutoTopOff += continuedAutoTopOff ? 1L : 0L;
            partialReloadStoppedManual += stoppedManual ? 1L : 0L;
            partialReloadStoppedSourceEmpty += stoppedSourceEmpty ? 1L : 0L;
        }

        internal static void Log(int ticksGame)
        {
            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                Verse.Log.Message(
                    "[CeleTech Shuttle] PerfSummary weapon reload completion profile" +
                    " ticksGame=" + ticksGame +
                    "\n - completed reloads: " + completedReloads +
                    "\n - completed full reloads: " + completedFullReloads +
                    "\n - completed partial reloads: " + completedPartialReloads +
                    "\n - partial reload continued as required-for-fire: " +
                    partialReloadContinuedRequiredForFire +
                    "\n - partial reload continued as auto-top-off: " +
                    partialReloadContinuedAutoTopOff +
                    "\n - partial reload stopped manual: " +
                    partialReloadStoppedManual +
                    "\n - partial reload stopped source empty: " +
                    partialReloadStoppedSourceEmpty);
            }

        }

        internal static void Clear()
        {
            completedReloads = 0L;
            completedFullReloads = 0L;
            completedPartialReloads = 0L;
            partialReloadContinuedRequiredForFire = 0L;
            partialReloadContinuedAutoTopOff = 0L;
            partialReloadStoppedManual = 0L;
            partialReloadStoppedSourceEmpty = 0L;
        }
    }
}
