using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Keeps performance instrumentation outside gameplay decisions. Section timing is sampled
    /// so a profiling session does not add dictionary and timestamp work to every weapon Tick.
    /// Detailed counters remain exact when their separate setting is enabled.
    /// </summary>
    internal struct ShuttleWeaponTickDiagnostics
    {
        private const int SectionSampleInterval = 30;
        private static int sectionSampleCounter;
        private readonly bool collectSections;
        private readonly bool collectDetailedCounters;
        private readonly int ticksGame;
        private long sectionStart;

        private ShuttleWeaponTickDiagnostics(
            bool collectSections,
            bool collectDetailedCounters,
            int ticksGame)
        {
            this.collectSections = collectSections;
            this.collectDetailedCounters = collectDetailedCounters;
            this.ticksGame = ticksGame;
            this.sectionStart = 0L;
        }

        internal static ShuttleWeaponTickDiagnostics Create(
            ShuttleModuleRuntimeContext context)
        {
            if (!ShuttleWeaponRuntimeProfiler.Enabled)
            {
                return default(ShuttleWeaponTickDiagnostics);
            }

            return new ShuttleWeaponTickDiagnostics(
                ShouldCollectSectionSample(),
                ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns,
                context != null ? context.TicksGame : -1);
        }

        internal bool CollectsDetailedCounters
        {
            get
            {
                return this.collectDetailedCounters;
            }
        }

        internal void Begin()
        {
            if (this.collectDetailedCounters)
            {
                ShuttleWeaponRuntimeProfiler.RecordTotalWeaponTick();
            }

            this.sectionStart = ShuttleWeaponRuntimeProfiler.StartSection(
                this.collectSections);
        }

        internal void CompleteAndStart(string sectionName)
        {
            if (!this.collectSections)
            {
                return;
            }

            this.Complete(sectionName);
            this.sectionStart = ShuttleWeaponRuntimeProfiler.StartSection(true);
        }

        internal void Complete(string sectionName)
        {
            if (!this.collectSections)
            {
                return;
            }

            ShuttleWeaponRuntimeProfiler.Record(
                this.collectSections,
                sectionName,
                this.sectionStart);
        }

        internal void RecordReloadState(
            ShuttleWeaponAmmoState ammoState,
            bool canFireLoadedAmmo,
            ShuttleWeaponReloadRequestKind requestKind,
            bool automaticRetryAllowed,
            bool reloadNeeded)
        {
            if (this.collectDetailedCounters)
            {
                ShuttleWeaponRuntimeProfiler.RecordReloadStateTick(
                    ammoState,
                    canFireLoadedAmmo,
                    requestKind,
                    automaticRetryAllowed,
                    reloadNeeded);
            }
        }

        internal void RecordBursting()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordBurstingTick();
        }

        internal void RecordCooldown()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordCooldownTick();
        }

        internal void RecordCooldownFastPathHit()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordCooldownFastPathHit();
        }

        internal void RecordCooldownFastPathMiss(WeaponCooldownFastPathMissReason reason)
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordCooldownFastPathMiss(reason);
        }

        internal void RecordAutomaticReloadRetryThrottled()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordAutomaticReloadRetryThrottledEarlyReturn();
        }

        internal void RecordIdleScanEligible()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordIdleScanEligibleTick();
        }

        internal void RecordIdleScanSkipped()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordIdleScanSkippedTick();
        }

        internal void RecordIdleScanExecuted()
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordIdleScanExecutedTick();
        }

        internal void RecordFullPipeline(WeaponFullPipelineReason reason)
        {
            if (this.collectDetailedCounters)
                ShuttleWeaponRuntimeProfiler.RecordFullPipelineTick(reason);
        }

        internal void Finish()
        {
            if (this.collectSections)
            {
                ShuttleWeaponRuntimeProfiler.MaybeLog(this.ticksGame);
            }
        }

        private static bool ShouldCollectSectionSample()
        {
            sectionSampleCounter++;
            if (sectionSampleCounter < SectionSampleInterval)
            {
                return false;
            }

            sectionSampleCounter = 0;
            return true;
        }
    }
}
