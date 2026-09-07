using System.Collections.Generic;
using System.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal sealed class ShuttleControllerTickProfiler :
        IShuttleRuntimeSystemTickProfileSink,
        IShuttlePowerDemandProfileSink
    {
        private const int LogIntervalTicks = 2500;
        private const int SlowDiagnosticThrottleTicks = 250;
        private const float SlowSectionDiagnosticThresholdMs = 1.0f;
        private const int MaxControllerSectionsToLog = 24;
        private const int MaxRuntimeSystemMetricsToLog = 12;
        private readonly Dictionary<string, SectionMetric> metrics =
            new Dictionary<string, SectionMetric>();
        private readonly Dictionary<string, SectionMetric> runtimeSystemMetrics =
            new Dictionary<string, SectionMetric>();
        private readonly Dictionary<string, PowerDemandRuntimeSystemMetric> powerDemandRuntimeSystemMetrics =
            new Dictionary<string, PowerDemandRuntimeSystemMetric>();
        private readonly Dictionary<string, int> lastDiagnosticTickBySection =
            new Dictionary<string, int>();
        private readonly List<ShuttleDeveloperDiagnosticModel> performanceDiagnostics =
            new List<ShuttleDeveloperDiagnosticModel>();
        private readonly ShuttleRuntimeTickDispatchMetricsStore runtimeTickDispatchMetrics =
            new ShuttleRuntimeTickDispatchMetricsStore();
        private ShuttlePerformanceCaptureSession dashboardCapture;
        private SectionMetric powerDemandCollectionMetric;
        private SectionMetric powerDemandContextCreationMetric;
        private SectionMetric powerDemandRuntimeSystemMetric;
        private SectionMetric powerDemandAggregationMetric;
        private SectionMetric powerDemandPlanMetric;
        private SectionMetric powerDemandBindingValidationMetric;
        private SectionMetric powerDemandTryRunRuntimeActionMetric;
        private long powerDemandPlanBuilds;
        private long powerDemandPlanReuses;
        private long powerDemandInvalidBindings;
        private long powerDemandValidationSkippedDueStablePlan;
        private long powerDemandFullValidationPasses;
        private long powerDemandLowFrequencyValidationPasses;
        private long powerDemandInvalidRebuilds;
        private long powerDemandDispatchLoopIterations;
        private long powerDemandValidBindings;
        private long powerDemandSkippedInvalidBindings;
        private long powerDemandDirectDispatches;
        private long powerDemandFastContextCreations;
        private long powerDemandFastContextReuses;
        private long powerDemandFullContextCreations;
        private int lastLogTick = int.MinValue;

        private bool LogSamplingEnabled
        {
            get { return ShuttleDiagnosticGate.ShouldCollectPerformanceSamples; }
        }

        bool IShuttlePowerDemandProfileSink.Enabled
        {
            get { return this.LogSamplingEnabled; }
        }

        internal int BeginDashboardCapture(int profileRevision, int ticksGame)
        {
            if (this.dashboardCapture == null)
            {
                this.dashboardCapture = new ShuttlePerformanceCaptureSession();
            }

            return this.dashboardCapture.Begin(profileRevision, ticksGame);
        }

        internal void EndDashboardCapture(int captureLeaseID, int ticksGame)
        {
            if (this.dashboardCapture != null)
            {
                this.dashboardCapture.End(captureLeaseID, ticksGame);
            }
        }

        internal void ResetDashboardCapture(
            int captureLeaseID,
            int profileRevision,
            int ticksGame)
        {
            if (this.dashboardCapture != null)
            {
                this.dashboardCapture.Reset(captureLeaseID, profileRevision, ticksGame);
            }
        }

        internal ShuttlePerformanceDashboardReadModel GetDashboardSnapshot()
        {
            return this.dashboardCapture != null
                ? this.dashboardCapture.Snapshot
                : ShuttlePerformanceDashboardReadModel.Empty;
        }

        internal bool IsTickProfilingRequested
        {
            get
            {
                return this.LogSamplingEnabled ||
                    (this.dashboardCapture != null && this.dashboardCapture.IsActive);
            }
        }

        internal bool PrepareTickProfiling(int profileRevision)
        {
            bool logSampling = this.LogSamplingEnabled;
            if (this.dashboardCapture == null || !this.dashboardCapture.IsActive)
            {
                return logSampling;
            }

            this.dashboardCapture.PrepareTick(profileRevision);
            return true;
        }

        internal void CompleteDashboardTick(int ticksGame)
        {
            if (this.dashboardCapture != null)
            {
                this.dashboardCapture.CompleteTick(ticksGame);
            }
        }

        internal void Record(string sectionName, long startTimestamp)
        {
            bool logSampling = this.LogSamplingEnabled;
            bool dashboardSampling = this.dashboardCapture != null &&
                this.dashboardCapture.IsActive;
            if ((!logSampling && !dashboardSampling) ||
                string.IsNullOrEmpty(sectionName) ||
                startTimestamp <= 0L)
            {
                return;
            }

            float elapsedMs = this.GetElapsedMilliseconds(startTimestamp);
            if (logSampling)
            {
                SectionMetric metric;
                if (!this.metrics.TryGetValue(sectionName, out metric))
                {
                    metric = new SectionMetric();
                }

                metric.Record(elapsedMs);
                this.metrics[sectionName] = metric;
                this.MaybeRecordSlowDiagnostic(sectionName, elapsedMs);
            }

            if (dashboardSampling)
            {
                this.dashboardCapture.RecordControllerSection(sectionName, elapsedMs);
            }
        }

        public void RecordRuntimeSystemTick(
            string runtimeSystemKey,
            string moduleInstanceID,
            string moduleLabel,
            int tickInterval,
            long elapsedStopwatchTicks)
        {
            bool logSampling = this.LogSamplingEnabled;
            bool dashboardSampling = this.dashboardCapture != null &&
                this.dashboardCapture.IsActive;
            if ((!logSampling && !dashboardSampling) || elapsedStopwatchTicks <= 0L)
            {
                return;
            }

            float elapsedMs = ConvertElapsedStopwatchTicks(elapsedStopwatchTicks);
            if (logSampling)
            {
                string systemKey = string.IsNullOrEmpty(runtimeSystemKey)
                    ? "unknown-runtime-system"
                    : runtimeSystemKey;
                SectionMetric metric;
                if (!this.runtimeSystemMetrics.TryGetValue(systemKey, out metric))
                {
                    metric = new SectionMetric();
                }

                metric.Record(elapsedMs);
                this.runtimeSystemMetrics[systemKey] = metric;
            }

            if (dashboardSampling)
            {
                this.dashboardCapture.RecordModule(
                    moduleInstanceID,
                    moduleLabel,
                    runtimeSystemKey,
                    tickInterval,
                    elapsedMs);
            }
        }

        public void RecordRuntimeTickDispatchSample(
            ShuttleRuntimeTickDispatchSample sample)
        {
            if (this.LogSamplingEnabled)
            {
                this.runtimeTickDispatchMetrics.Record(sample);
            }
        }

        public void RecordPowerDemandCollection(long startTimestamp)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            this.powerDemandCollectionMetric.Record(this.GetElapsedMilliseconds(startTimestamp));
        }

        public void RecordPowerDemandContextCreation(long startTimestamp)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            this.powerDemandContextCreationMetric.Record(this.GetElapsedMilliseconds(startTimestamp));
        }

        public void RecordPowerDemandFastContextCreation()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandFastContextCreations++;
            }
        }

        public void RecordPowerDemandFastContextReuse()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandFastContextReuses++;
            }
        }

        public void RecordPowerDemandFullContextCreation()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandFullContextCreations++;
            }
        }

        public void RecordPowerDemandRuntimeSystem(
            IShuttleModuleRuntimeSystem system,
            long startTimestamp,
            float demandWatts)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            float elapsedMs = this.GetElapsedMilliseconds(startTimestamp);
            this.powerDemandRuntimeSystemMetric.Record(elapsedMs);

            string systemKey = ResolveRuntimeSystemKey(system);
            PowerDemandRuntimeSystemMetric metric;
            if (!this.powerDemandRuntimeSystemMetrics.TryGetValue(systemKey, out metric))
            {
                metric = new PowerDemandRuntimeSystemMetric();
            }

            metric.Record(elapsedMs, demandWatts);
            this.powerDemandRuntimeSystemMetrics[systemKey] = metric;
        }

        public void RecordPowerDemandAggregation(long startTimestamp)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            this.powerDemandAggregationMetric.Record(this.GetElapsedMilliseconds(startTimestamp));
        }

        public void RecordPowerDemandPlanGet(long startTimestamp)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            this.powerDemandPlanMetric.Record(this.GetElapsedMilliseconds(startTimestamp));
        }

        public void RecordPowerDemandPlanReuse()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandPlanReuses++;
            }
        }

        public void RecordPowerDemandPlanBuild(long startTimestamp)
        {
            if (!this.LogSamplingEnabled)
            {
                return;
            }

            this.powerDemandPlanBuilds++;
        }

        public void RecordPowerDemandBindingValidation(long startTimestamp, bool valid)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            this.powerDemandBindingValidationMetric.Record(
                this.GetElapsedMilliseconds(startTimestamp));
            if (!valid)
            {
                this.powerDemandInvalidBindings++;
            }
        }

        public void RecordPowerDemandValidationSkippedDueStablePlan()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandValidationSkippedDueStablePlan++;
            }
        }

        public void RecordPowerDemandFullValidationPass()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandFullValidationPasses++;
            }
        }

        public void RecordPowerDemandLowFrequencyValidationPass()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandLowFrequencyValidationPasses++;
            }
        }

        public void RecordPowerDemandInvalidRebuild()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandInvalidRebuilds++;
            }
        }

        public void RecordPowerDemandDispatchLoopIteration()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandDispatchLoopIterations++;
            }
        }

        public void RecordPowerDemandValidBinding()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandValidBindings++;
            }
        }

        public void RecordPowerDemandSkippedInvalidBinding()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandSkippedInvalidBindings++;
            }
        }

        public void RecordPowerDemandTryRunRuntimeAction(long startTimestamp)
        {
            if (!this.LogSamplingEnabled || startTimestamp <= 0L)
            {
                return;
            }

            this.powerDemandTryRunRuntimeActionMetric.Record(
                this.GetElapsedMilliseconds(startTimestamp));
        }

        public void RecordPowerDemandDirectDispatch()
        {
            if (this.LogSamplingEnabled)
            {
                this.powerDemandDirectDispatches++;
            }
        }

        internal void MaybeLog(int ticksGame, int profileRevision, int installedModuleCount)
        {
            if (!ShuttleDiagnosticGate.ShouldLogPerformanceSummaries ||
                (this.metrics.Count == 0 &&
                    this.runtimeSystemMetrics.Count == 0 &&
                    !this.runtimeTickDispatchMetrics.HasMetrics &&
                    !this.HasPowerDemandMetrics()))
            {
                return;
            }

            if (this.lastLogTick != int.MinValue &&
                ticksGame - this.lastLogTick < LogIntervalTicks)
            {
                return;
            }

            int lastLogTickBeforeUpdate = this.lastLogTick;
            int powerDemandProfileWindowTicks =
                this.ResolvePowerDemandProfileWindowTicks(ticksGame, lastLogTickBeforeUpdate);
            this.lastLogTick = ticksGame;
            if (this.metrics.Count > 0)
            {
                List<KeyValuePair<string, SectionMetric>> sortedMetrics =
                    new List<KeyValuePair<string, SectionMetric>>(this.metrics);
                sortedMetrics.Sort(CompareSectionMetricsByAverageDescending);

                string message = "[CeleTech Shuttle] PerfSummary ShuttleController.Tick profile" +
                    " profileRevision=" + profileRevision +
                    " installedModules=" + installedModuleCount;
                int limit = sortedMetrics.Count < MaxControllerSectionsToLog
                    ? sortedMetrics.Count
                    : MaxControllerSectionsToLog;
                for (int i = 0; i < limit; i++)
                {
                    KeyValuePair<string, SectionMetric> pair = sortedMetrics[i];
                    SectionMetric metric = pair.Value;
                    message += "\n - " + pair.Key +
                        ": avg " + metric.AverageMs.ToString("0.###") +
                        " ms, peak " + metric.PeakMs.ToString("0.###") +
                        " ms, samples " + metric.SampleCount;
                }

                if (sortedMetrics.Count > limit)
                {
                    message += "\n - ... " + (sortedMetrics.Count - limit) + " more sections";
                }

                Log.Message(message);
                this.metrics.Clear();
            }

            if (this.runtimeSystemMetrics.Count > 0)
            {
                List<KeyValuePair<string, SectionMetric>> sortedRuntimeSystemMetrics =
                    new List<KeyValuePair<string, SectionMetric>>(this.runtimeSystemMetrics);
                sortedRuntimeSystemMetrics.Sort(CompareSectionMetricsByAverageDescending);

                string runtimeMessage =
                    "[CeleTech Shuttle] PerfSummary ShuttleController.Tick module runtime systems profile" +
                    " profileRevision=" + profileRevision +
                    " installedModules=" + installedModuleCount;
                int runtimeLimit = sortedRuntimeSystemMetrics.Count < MaxRuntimeSystemMetricsToLog
                    ? sortedRuntimeSystemMetrics.Count
                    : MaxRuntimeSystemMetricsToLog;
                for (int i = 0; i < runtimeLimit; i++)
                {
                    KeyValuePair<string, SectionMetric> pair = sortedRuntimeSystemMetrics[i];
                    SectionMetric metric = pair.Value;
                    runtimeMessage += "\n - " + pair.Key +
                        ": avg " + metric.AverageMs.ToString("0.###") +
                        " ms, peak " + metric.PeakMs.ToString("0.###") +
                        " ms, samples " + metric.SampleCount;
                }

                if (sortedRuntimeSystemMetrics.Count > runtimeLimit)
                {
                    runtimeMessage += "\n - ... " +
                        (sortedRuntimeSystemMetrics.Count - runtimeLimit) +
                        " more runtime systems";
                }

                Log.Message(runtimeMessage);
                this.runtimeSystemMetrics.Clear();
            }

            this.LogPowerDemandRuntimeSystemProfile(
                profileRevision,
                installedModuleCount,
                powerDemandProfileWindowTicks);
            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                this.runtimeTickDispatchMetrics.LogAndClear(
                    profileRevision,
                    installedModuleCount);
                this.LogPowerDemandDispatchProfile(
                    profileRevision,
                    installedModuleCount,
                    powerDemandProfileWindowTicks);
            }
            else
            {
                this.runtimeTickDispatchMetrics.Clear();
                this.ClearPowerDemandDispatchProfile();
            }

            this.LogPowerDemandCollectionProfile(
                profileRevision,
                installedModuleCount,
                powerDemandProfileWindowTicks);
        }

        private void LogPowerDemandCollectionProfile(
            int profileRevision,
            int installedModuleCount,
            int windowTicks)
        {
            if (this.powerDemandCollectionMetric.SampleCount <= 0)
            {
                return;
            }

            float unattributedTotalMs =
                this.powerDemandCollectionMetric.TotalMs -
                this.powerDemandContextCreationMetric.TotalMs -
                this.powerDemandRuntimeSystemMetric.TotalMs;
            if (unattributedTotalMs < 0f)
            {
                unattributedTotalMs = 0f;
            }

            string message =
                "[CeleTech Shuttle] PerfSummary power demand collection profile" +
                " profileRevision=" + profileRevision +
                " installedModules=" + installedModuleCount +
                " windowTicks=" + windowTicks +
                "\n - total collection: avg/tick " +
                this.AveragePerTick(this.powerDemandCollectionMetric.TotalMs, windowTicks).ToString("0.###") +
                " ms, peak " + this.powerDemandCollectionMetric.PeakMs.ToString("0.###") +
                " ms, samples " + this.powerDemandCollectionMetric.SampleCount;

            if (this.powerDemandRuntimeSystemMetric.SampleCount > 0)
            {
                message +=
                    "\n - runtime system demand: avg/tick " +
                    this.AveragePerTick(this.powerDemandRuntimeSystemMetric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + this.powerDemandRuntimeSystemMetric.TotalMs.ToString("0.###") +
                    " ms, peak " + this.powerDemandRuntimeSystemMetric.PeakMs.ToString("0.###") +
                    " ms, calls " + this.powerDemandRuntimeSystemMetric.SampleCount;
            }

            if (this.powerDemandContextCreationMetric.SampleCount > 0)
            {
                message +=
                    "\n - runtime context acquire/refresh: avg/tick " +
                    this.AveragePerTick(this.powerDemandContextCreationMetric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + this.powerDemandContextCreationMetric.TotalMs.ToString("0.###") +
                    " ms, peak " + this.powerDemandContextCreationMetric.PeakMs.ToString("0.###") +
                    " ms, calls " + this.powerDemandContextCreationMetric.SampleCount;
            }

            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                message +=
                    "\n - power demand fast context allocations: " +
                    this.powerDemandFastContextCreations +
                    "\n - power demand fast context reuses: " +
                    this.powerDemandFastContextReuses +
                    "\n - power demand full context creations: " +
                    this.powerDemandFullContextCreations;
            }

            if (this.powerDemandAggregationMetric.SampleCount > 0)
            {
                message +=
                    "\n - demand aggregation/apply: avg/tick " +
                    this.AveragePerTick(this.powerDemandAggregationMetric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + this.powerDemandAggregationMetric.TotalMs.ToString("0.###") +
                    " ms, peak " + this.powerDemandAggregationMetric.PeakMs.ToString("0.###") +
                    " ms, calls " + this.powerDemandAggregationMetric.SampleCount;
            }

            message +=
                "\n - dispatch overhead/unattributed: avg/tick " +
                this.AveragePerTick(unattributedTotalMs, windowTicks).ToString("0.###") +
                " ms, total " + unattributedTotalMs.ToString("0.###") + " ms";

            Log.Message(message);
            this.powerDemandCollectionMetric = new SectionMetric();
            this.powerDemandContextCreationMetric = new SectionMetric();
            this.powerDemandRuntimeSystemMetric = new SectionMetric();
            this.powerDemandAggregationMetric = new SectionMetric();
            this.powerDemandFastContextCreations = 0L;
            this.powerDemandFastContextReuses = 0L;
            this.powerDemandFullContextCreations = 0L;
        }

        private void LogPowerDemandRuntimeSystemProfile(
            int profileRevision,
            int installedModuleCount,
            int windowTicks)
        {
            if (this.powerDemandRuntimeSystemMetrics.Count <= 0)
            {
                return;
            }

            List<KeyValuePair<string, PowerDemandRuntimeSystemMetric>> sortedMetrics =
                new List<KeyValuePair<string, PowerDemandRuntimeSystemMetric>>(
                    this.powerDemandRuntimeSystemMetrics);
            sortedMetrics.Sort(ComparePowerDemandRuntimeSystemMetricsByTotalDescending);

            string message =
                "[CeleTech Shuttle] PerfSummary power demand by runtime system profile" +
                " profileRevision=" + profileRevision +
                " installedModules=" + installedModuleCount +
                " windowTicks=" + windowTicks;
            int limit = sortedMetrics.Count < MaxRuntimeSystemMetricsToLog
                ? sortedMetrics.Count
                : MaxRuntimeSystemMetricsToLog;
            for (int i = 0; i < limit; i++)
            {
                KeyValuePair<string, PowerDemandRuntimeSystemMetric> pair = sortedMetrics[i];
                PowerDemandRuntimeSystemMetric metric = pair.Value;
                message += "\n - " + pair.Key +
                    ": avg/call " + metric.AverageMs.ToString("0.###") +
                    " ms, avg/tick " + this.AveragePerTick(metric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + metric.TotalMs.ToString("0.###") +
                    " ms, peak " + metric.PeakMs.ToString("0.###") +
                    " ms, calls " + metric.CallCount +
                    ", demand avg/tick " +
                    this.AveragePerTick(metric.TotalDemandWatts, windowTicks).ToString("0.###") +
                    " watts";
            }

            if (sortedMetrics.Count > limit)
            {
                message += "\n - ... " +
                    (sortedMetrics.Count - limit) +
                    " more runtime systems";
            }

            Log.Message(message);
            this.powerDemandRuntimeSystemMetrics.Clear();
        }

        private void LogPowerDemandDispatchProfile(
            int profileRevision,
            int installedModuleCount,
            int windowTicks)
        {
            if (this.powerDemandPlanMetric.SampleCount <= 0 &&
                this.powerDemandBindingValidationMetric.SampleCount <= 0 &&
                this.powerDemandTryRunRuntimeActionMetric.SampleCount <= 0 &&
                this.powerDemandDispatchLoopIterations <= 0L)
            {
                return;
            }

            string message =
                "[CeleTech Shuttle] PerfSummary power demand dispatch profile" +
                " profileRevision=" + profileRevision +
                " installedModules=" + installedModuleCount +
                " windowTicks=" + windowTicks;
            if (this.powerDemandPlanMetric.SampleCount > 0)
            {
                message +=
                    "\n - plan get/reuse/build: avg/tick " +
                    this.AveragePerTick(this.powerDemandPlanMetric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + this.powerDemandPlanMetric.TotalMs.ToString("0.###") +
                    " ms, peak " + this.powerDemandPlanMetric.PeakMs.ToString("0.###") +
                    " ms, calls " + this.powerDemandPlanMetric.SampleCount;
            }

            message +=
                "\n - plan builds: " + this.powerDemandPlanBuilds +
                "\n - plan reuses: " + this.powerDemandPlanReuses;
            if (this.powerDemandBindingValidationMetric.SampleCount > 0)
            {
                message +=
                    "\n - binding validation: avg/tick " +
                    this.AveragePerTick(this.powerDemandBindingValidationMetric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + this.powerDemandBindingValidationMetric.TotalMs.ToString("0.###") +
                    " ms, peak " + this.powerDemandBindingValidationMetric.PeakMs.ToString("0.###") +
                    " ms, calls " + this.powerDemandBindingValidationMetric.SampleCount;
            }

            message +=
                "\n - validation skipped due to stable plan: " +
                this.powerDemandValidationSkippedDueStablePlan +
                "\n - full validation passes: " + this.powerDemandFullValidationPasses +
                "\n - low-frequency validation passes: " +
                this.powerDemandLowFrequencyValidationPasses +
                "\n - invalid bindings: " + this.powerDemandInvalidBindings +
                "\n - invalid rebuilds: " + this.powerDemandInvalidRebuilds +
                "\n - dispatch loop iterations: " + this.powerDemandDispatchLoopIterations +
                "\n - valid bindings: " + this.powerDemandValidBindings +
                "\n - skipped invalid bindings: " + this.powerDemandSkippedInvalidBindings;
            if (this.powerDemandTryRunRuntimeActionMetric.SampleCount > 0)
            {
                message +=
                    "\n - TryRunRuntimeAction total: avg/tick " +
                    this.AveragePerTick(this.powerDemandTryRunRuntimeActionMetric.TotalMs, windowTicks).ToString("0.###") +
                    " ms, total " + this.powerDemandTryRunRuntimeActionMetric.TotalMs.ToString("0.###") +
                    " ms, peak " + this.powerDemandTryRunRuntimeActionMetric.PeakMs.ToString("0.###") +
                    " ms, calls " + this.powerDemandTryRunRuntimeActionMetric.SampleCount;
            }

            message +=
                "\n - lambda dispatches: 0" +
                "\n - direct power demand dispatches: " +
                this.powerDemandDirectDispatches;

            Log.Message(message);
            this.ClearPowerDemandDispatchProfile();
        }

        private void ClearPowerDemandDispatchProfile()
        {
            this.powerDemandPlanMetric = new SectionMetric();
            this.powerDemandBindingValidationMetric = new SectionMetric();
            this.powerDemandTryRunRuntimeActionMetric = new SectionMetric();
            this.powerDemandPlanBuilds = 0L;
            this.powerDemandPlanReuses = 0L;
            this.powerDemandInvalidBindings = 0L;
            this.powerDemandValidationSkippedDueStablePlan = 0L;
            this.powerDemandFullValidationPasses = 0L;
            this.powerDemandLowFrequencyValidationPasses = 0L;
            this.powerDemandInvalidRebuilds = 0L;
            this.powerDemandDispatchLoopIterations = 0L;
            this.powerDemandValidBindings = 0L;
            this.powerDemandSkippedInvalidBindings = 0L;
            this.powerDemandDirectDispatches = 0L;
        }

        internal IReadOnlyList<ShuttleDeveloperDiagnosticModel> BuildDiagnosticsSnapshot()
        {
            List<ShuttleDeveloperDiagnosticModel> snapshot =
                new List<ShuttleDeveloperDiagnosticModel>();
            for (int i = 0; i < this.performanceDiagnostics.Count; i++)
            {
                ShuttleDeveloperDiagnosticModel source = this.performanceDiagnostics[i];
                if (source == null)
                {
                    continue;
                }

                ShuttleDeveloperDiagnosticModel copy = new ShuttleDeveloperDiagnosticModel();
                copy.Code = source.Code;
                copy.Message = source.Message;
                copy.Kind = source.Kind;
                copy.Severity = source.Severity;
                copy.Scope = source.Scope;
                copy.ReferenceID = source.ReferenceID;
                copy.Tick = source.Tick;
                copy.ElapsedMs = source.ElapsedMs;
                copy.Tooltip = source.Tooltip;
                snapshot.Add(copy);
            }

            return snapshot;
        }

        private void MaybeRecordSlowDiagnostic(string sectionName, float elapsedMs)
        {
            if (!this.LogSamplingEnabled ||
                string.IsNullOrEmpty(sectionName) ||
                elapsedMs < SlowSectionDiagnosticThresholdMs)
            {
                return;
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            int lastTick;
            if (ticksGame >= 0 &&
                this.lastDiagnosticTickBySection.TryGetValue(sectionName, out lastTick) &&
                ticksGame - lastTick < SlowDiagnosticThrottleTicks)
            {
                return;
            }

            if (ticksGame >= 0)
            {
                this.lastDiagnosticTickBySection[sectionName] = ticksGame;
            }

            ShuttleDeveloperDiagnosticModel diagnostic = new ShuttleDeveloperDiagnosticModel();
            diagnostic.Code = "tick-section-slow";
            diagnostic.Kind = "Perf";
            diagnostic.Severity = elapsedMs >= 2f ? "Warning" : "Info";
            diagnostic.Scope = "ShuttleController.Tick";
            diagnostic.ReferenceID = sectionName;
            diagnostic.Tick = ticksGame;
            diagnostic.ElapsedMs = elapsedMs;
            diagnostic.Message = sectionName + " took " + elapsedMs.ToString("0.###") + " ms.";
            diagnostic.Tooltip =
                "Code: tick-section-slow\nScope: ShuttleController.Tick\nReference: " +
                sectionName;
            this.performanceDiagnostics.Add(diagnostic);
            while (this.performanceDiagnostics.Count > ShuttleControlReadModel.PerformanceDiagnosticsMax)
            {
                this.performanceDiagnostics.RemoveAt(0);
            }
        }

        private static int CompareSectionMetricsByAverageDescending(
            KeyValuePair<string, SectionMetric> left,
            KeyValuePair<string, SectionMetric> right)
        {
            int averageComparison = right.Value.AverageMs.CompareTo(left.Value.AverageMs);
            if (averageComparison != 0)
            {
                return averageComparison;
            }

            int peakComparison = right.Value.PeakMs.CompareTo(left.Value.PeakMs);
            if (peakComparison != 0)
            {
                return peakComparison;
            }

            return string.CompareOrdinal(left.Key, right.Key);
        }

        private static int ComparePowerDemandRuntimeSystemMetricsByTotalDescending(
            KeyValuePair<string, PowerDemandRuntimeSystemMetric> left,
            KeyValuePair<string, PowerDemandRuntimeSystemMetric> right)
        {
            int totalComparison = right.Value.TotalMs.CompareTo(left.Value.TotalMs);
            if (totalComparison != 0)
            {
                return totalComparison;
            }

            int peakComparison = right.Value.PeakMs.CompareTo(left.Value.PeakMs);
            if (peakComparison != 0)
            {
                return peakComparison;
            }

            return string.CompareOrdinal(left.Key, right.Key);
        }

        private static string ResolveRuntimeSystemKey(IShuttleModuleRuntimeSystem system)
        {
            string systemKey = null;
            if (system != null)
            {
                systemKey = ShuttleRuntimeSystemKeyUtility.Normalize(system.RuntimeSystemKey);
                if (string.IsNullOrEmpty(systemKey))
                {
                    systemKey = system.GetType().Name;
                }
            }

            return string.IsNullOrEmpty(systemKey)
                ? "unknown-runtime-system"
                : systemKey;
        }

        private int ResolvePowerDemandProfileWindowTicks(
            int ticksGame,
            int lastLogTickBeforeUpdate)
        {
            if (lastLogTickBeforeUpdate != int.MinValue)
            {
                int deltaTicks = ticksGame - lastLogTickBeforeUpdate;
                if (deltaTicks > 0)
                {
                    return deltaTicks;
                }
            }

            if (this.powerDemandCollectionMetric.SampleCount > 0)
            {
                return this.powerDemandCollectionMetric.SampleCount;
            }

            return LogIntervalTicks;
        }

        private float GetElapsedMilliseconds(long startTimestamp)
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            if (elapsedTicks <= 0L)
            {
                return 0f;
            }

            return (float)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
        }

        private static float ConvertElapsedStopwatchTicks(long elapsedTicks)
        {
            return elapsedTicks > 0L
                ? (float)(elapsedTicks * 1000.0 / Stopwatch.Frequency)
                : 0f;
        }

        private bool HasPowerDemandMetrics()
        {
            return this.powerDemandCollectionMetric.SampleCount > 0 ||
                this.powerDemandContextCreationMetric.SampleCount > 0 ||
                this.powerDemandRuntimeSystemMetric.SampleCount > 0 ||
                this.powerDemandAggregationMetric.SampleCount > 0 ||
                this.powerDemandRuntimeSystemMetrics.Count > 0 ||
                this.powerDemandPlanMetric.SampleCount > 0 ||
                this.powerDemandBindingValidationMetric.SampleCount > 0 ||
                this.powerDemandTryRunRuntimeActionMetric.SampleCount > 0 ||
                this.powerDemandPlanBuilds > 0L ||
                this.powerDemandPlanReuses > 0L ||
                this.powerDemandInvalidBindings > 0L ||
                this.powerDemandValidationSkippedDueStablePlan > 0L ||
                this.powerDemandFullValidationPasses > 0L ||
                this.powerDemandLowFrequencyValidationPasses > 0L ||
                this.powerDemandInvalidRebuilds > 0L ||
                this.powerDemandDispatchLoopIterations > 0L ||
                this.powerDemandValidBindings > 0L ||
                this.powerDemandSkippedInvalidBindings > 0L ||
                this.powerDemandDirectDispatches > 0L ||
                this.powerDemandFastContextCreations > 0L ||
                this.powerDemandFastContextReuses > 0L ||
                this.powerDemandFullContextCreations > 0L;
        }

        private float AveragePerTick(float total, int windowTicks)
        {
            return windowTicks > 0 ? total / windowTicks : 0f;
        }

        private struct PowerDemandRuntimeSystemMetric
        {
            private SectionMetric sectionMetric;

            internal float TotalDemandWatts;

            internal int CallCount
            {
                get { return this.sectionMetric.SampleCount; }
            }

            internal float TotalMs
            {
                get { return this.sectionMetric.TotalMs; }
            }

            internal float PeakMs
            {
                get { return this.sectionMetric.PeakMs; }
            }

            internal float AverageMs
            {
                get { return this.sectionMetric.AverageMs; }
            }

            internal void Record(float elapsedMs, float demandWatts)
            {
                this.sectionMetric.Record(elapsedMs);
                if (!float.IsNaN(demandWatts) &&
                    !float.IsInfinity(demandWatts) &&
                    demandWatts > 0f)
                {
                    this.TotalDemandWatts += demandWatts;
                }
            }
        }

        private struct SectionMetric
        {
            internal int SampleCount;
            internal float TotalMs;
            internal float PeakMs;

            internal float AverageMs
            {
                get
                {
                    return this.SampleCount > 0 ? this.TotalMs / this.SampleCount : 0f;
                }
            }

            internal void Record(float elapsedMs)
            {
                if (float.IsNaN(elapsedMs) || float.IsInfinity(elapsedMs) || elapsedMs < 0f)
                {
                    elapsedMs = 0f;
                }

                this.SampleCount++;
                this.TotalMs += elapsedMs;
                if (elapsedMs > this.PeakMs)
                {
                    this.PeakMs = elapsedMs;
                }
            }
        }
    }
}
