using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns cycle, cooldown, full-pipeline and scan counters plus their two summary messages.
    /// </summary>
    internal static class ShuttleWeaponCycleMetricsStore
    {
        private static long totalWeaponTicks;
        private static long burstingTicks;
        private static long cooldownTicks;
        private static long cooldownFastPathHits;
        private static long cooldownFastPathMissReloadInProgress;
        private static long cooldownFastPathMissReloadRequested;
        private static long cooldownFastPathMissLoadedAmmoInsufficient;
        private static long cooldownFastPathMissInvalidAmmoState;
        private static long cooldownFastPathMissOther;
        private static long fullPipelineTicks;
        private static long idleScanEligibleTicks;
        private static long idleScanSkippedTicks;
        private static long idleScanExecutedTicks;
        private static long forcedCurrentTargetFullPipelineTicks;
        private static long reloadRelatedFullPipelineTicks;
        private static long otherFullPipelineTicks;
        private static long automaticRetryThrottledEarlyReturnTicks;

        internal static bool HasMetrics
        {
            get
            {
                return totalWeaponTicks > 0L ||
                    automaticRetryThrottledEarlyReturnTicks > 0L;
            }
        }

        internal static long ReloadRelatedFullPipelineTicks
        {
            get { return reloadRelatedFullPipelineTicks; }
        }

        internal static long AutomaticRetryThrottledEarlyReturnTicks
        {
            get { return automaticRetryThrottledEarlyReturnTicks; }
        }

        internal static void RecordTotalWeaponTick()
        {
            if (DetailedEnabled)
            {
                totalWeaponTicks++;
            }
        }

        internal static void RecordBurstingTick()
        {
            if (DetailedEnabled)
            {
                burstingTicks++;
            }
        }

        internal static void RecordCooldownTick()
        {
            if (DetailedEnabled)
            {
                cooldownTicks++;
            }
        }

        internal static void RecordCooldownFastPathHit()
        {
            if (DetailedEnabled)
            {
                cooldownFastPathHits++;
            }
        }

        internal static void RecordCooldownFastPathMiss(
            WeaponCooldownFastPathMissReason reason)
        {
            if (!DetailedEnabled)
            {
                return;
            }

            switch (reason)
            {
                case WeaponCooldownFastPathMissReason.ReloadInProgress:
                    cooldownFastPathMissReloadInProgress++;
                    break;
                case WeaponCooldownFastPathMissReason.ReloadRequested:
                    cooldownFastPathMissReloadRequested++;
                    break;
                case WeaponCooldownFastPathMissReason.LoadedAmmoInsufficient:
                    cooldownFastPathMissLoadedAmmoInsufficient++;
                    break;
                case WeaponCooldownFastPathMissReason.InvalidAmmoState:
                    cooldownFastPathMissInvalidAmmoState++;
                    break;
                default:
                    cooldownFastPathMissOther++;
                    break;
            }
        }

        internal static void RecordIdleScanEligibleTick()
        {
            if (DetailedEnabled)
            {
                idleScanEligibleTicks++;
            }
        }

        internal static void RecordIdleScanSkippedTick()
        {
            if (DetailedEnabled)
            {
                idleScanSkippedTicks++;
            }
        }

        internal static void RecordIdleScanExecutedTick()
        {
            if (DetailedEnabled)
            {
                idleScanExecutedTicks++;
            }
        }

        internal static void RecordFullPipelineTick(WeaponFullPipelineReason reason)
        {
            if (!DetailedEnabled)
            {
                return;
            }

            fullPipelineTicks++;
            switch (reason)
            {
                case WeaponFullPipelineReason.ForcedOrCurrentTarget:
                    forcedCurrentTargetFullPipelineTicks++;
                    break;
                case WeaponFullPipelineReason.ReloadRelated:
                    reloadRelatedFullPipelineTicks++;
                    break;
                default:
                    otherFullPipelineTicks++;
                    break;
            }
        }

        internal static void RecordAutomaticReloadRetryThrottled()
        {
            if (DetailedEnabled)
            {
                automaticRetryThrottledEarlyReturnTicks++;
            }
        }

        internal static void Log(int ticksGame)
        {
            if (DetailedEnabled)
            {
                Verse.Log.Message(
                    "[CeleTech Shuttle] PerfSummary weapon cooldown fast-path profile" +
                    " ticksGame=" + ticksGame +
                    "\n - total weapon ticks: " + totalWeaponTicks +
                    "\n - bursting ticks: " + burstingTicks +
                    "\n - cooldown ticks: " + cooldownTicks +
                    "\n - cooldown fast-path hits: " + cooldownFastPathHits +
                    "\n - cooldown fast-path hit rate: " +
                    FormatPercent(cooldownFastPathHits, cooldownTicks) +
                    "\n - full pipeline ticks: " + fullPipelineTicks +
                    "\n - full pipeline rate: " +
                    FormatPercent(fullPipelineTicks, totalWeaponTicks) +
                    "\n - miss reload in progress: " + cooldownFastPathMissReloadInProgress +
                    "\n - miss reload requested: " + cooldownFastPathMissReloadRequested +
                    "\n - miss loaded ammo insufficient: " + cooldownFastPathMissLoadedAmmoInsufficient +
                    "\n - miss invalid ammo state: " + cooldownFastPathMissInvalidAmmoState +
                    "\n - miss other: " + cooldownFastPathMissOther);
                Verse.Log.Message(
                    "[CeleTech Shuttle] PerfSummary weapon idle scan profile" +
                    " ticksGame=" + ticksGame +
                    "\n - idle scan eligible ticks: " + idleScanEligibleTicks +
                    "\n - idle scan skipped ticks: " + idleScanSkippedTicks +
                    "\n - idle scan executed ticks: " + idleScanExecutedTicks +
                    "\n - idle scan skip rate: " +
                    FormatPercent(idleScanSkippedTicks, idleScanEligibleTicks) +
                    "\n - forced/current target full pipeline ticks: " +
                    forcedCurrentTargetFullPipelineTicks +
                    "\n - reload-related full pipeline ticks: " +
                    reloadRelatedFullPipelineTicks +
                    "\n - other full pipeline ticks: " + otherFullPipelineTicks);
            }

        }

        private static bool DetailedEnabled
        {
            get { return ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns; }
        }

        private static string FormatPercent(long numerator, long denominator)
        {
            return denominator <= 0L
                ? "0%"
                : (numerator * 100.0 / denominator).ToString("0.##") + "%";
        }

        internal static void Clear()
        {
            totalWeaponTicks = 0L;
            burstingTicks = 0L;
            cooldownTicks = 0L;
            cooldownFastPathHits = 0L;
            cooldownFastPathMissReloadInProgress = 0L;
            cooldownFastPathMissReloadRequested = 0L;
            cooldownFastPathMissLoadedAmmoInsufficient = 0L;
            cooldownFastPathMissInvalidAmmoState = 0L;
            cooldownFastPathMissOther = 0L;
            fullPipelineTicks = 0L;
            idleScanEligibleTicks = 0L;
            idleScanSkippedTicks = 0L;
            idleScanExecutedTicks = 0L;
            forcedCurrentTargetFullPipelineTicks = 0L;
            reloadRelatedFullPipelineTicks = 0L;
            otherFullPipelineTicks = 0L;
            automaticRetryThrottledEarlyReturnTicks = 0L;
        }
    }
}
