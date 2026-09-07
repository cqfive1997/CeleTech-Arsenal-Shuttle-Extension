using System.Diagnostics;
using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal struct ShuttleRuntimeTickDispatchSample
    {
        internal float TotalMs;
        internal float PlanMs;
        internal float BindingMs;
        internal float ContextMs;
        internal float RuntimeSystemMs;
        internal float ResidualMs;
        internal int BindingVisits;
        internal int ContextAcquisitions;
        internal int RuntimeSystemCalls;
    }

    /// <summary>
    /// Samples one complete dispatcher Tick at low frequency. The disabled value does not read the
    /// stopwatch or update counters, keeping ordinary play outside detailed profiling unchanged.
    /// </summary>
    internal struct ShuttleRuntimeTickDispatchDiagnostics
    {
        private const int SampleInterval = 30;

        private readonly bool enabled;
        private readonly IShuttleRuntimeSystemTickProfileSink sink;
        private long totalStart;
        private long planTicks;
        private long bindingTicks;
        private long contextTicks;
        private long runtimeSystemTicks;
        private int bindingVisits;
        private int contextAcquisitions;
        private int runtimeSystemCalls;

        private ShuttleRuntimeTickDispatchDiagnostics(
            IShuttleRuntimeSystemTickProfileSink sink)
        {
            this.enabled = true;
            this.sink = sink;
            this.totalStart = 0L;
            this.planTicks = 0L;
            this.bindingTicks = 0L;
            this.contextTicks = 0L;
            this.runtimeSystemTicks = 0L;
            this.bindingVisits = 0;
            this.contextAcquisitions = 0;
            this.runtimeSystemCalls = 0;
        }

        internal static ShuttleRuntimeTickDispatchDiagnostics Create(
            IShuttleRuntimeSystemTickProfileSink sink,
            int ticksGame)
        {
            if (sink == null ||
                !ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns ||
                !ShouldSample(sink, ticksGame))
            {
                return default(ShuttleRuntimeTickDispatchDiagnostics);
            }

            return new ShuttleRuntimeTickDispatchDiagnostics(sink);
        }

        private static bool ShouldSample(
            IShuttleRuntimeSystemTickProfileSink sink,
            int ticksGame)
        {
            if (sink == null || ticksGame < 0)
            {
                return false;
            }

            int sampleBlock = ticksGame / SampleInterval;
            long phaseSeed = (RuntimeHelpers.GetHashCode(sink) & int.MaxValue) +
                ((long)sampleBlock * 7L);
            int phase = (int)(phaseSeed % SampleInterval);
            return ticksGame % SampleInterval == phase;
        }

        internal void Begin()
        {
            if (this.enabled)
            {
                this.totalStart = Stopwatch.GetTimestamp();
            }
        }

        internal long StartSection()
        {
            return this.enabled ? Stopwatch.GetTimestamp() : 0L;
        }

        internal void RecordPlan(long startTimestamp)
        {
            this.planTicks += this.ReadElapsedTicks(startTimestamp);
        }

        internal void RecordBinding(long startTimestamp)
        {
            if (!this.enabled)
            {
                return;
            }

            this.bindingVisits++;
            this.bindingTicks += this.ReadElapsedTicks(startTimestamp);
        }

        internal void RecordContext(long startTimestamp)
        {
            if (!this.enabled)
            {
                return;
            }

            this.contextAcquisitions++;
            this.contextTicks += this.ReadElapsedTicks(startTimestamp);
        }

        internal void RecordRuntimeSystem(long elapsedTicks)
        {
            if (!this.enabled)
            {
                return;
            }

            this.runtimeSystemCalls++;
            if (elapsedTicks > 0L)
            {
                this.runtimeSystemTicks += elapsedTicks;
            }
        }

        internal void Finish()
        {
            if (!this.enabled || this.totalStart <= 0L || this.sink == null)
            {
                return;
            }

            long totalTicks = Stopwatch.GetTimestamp() - this.totalStart;
            long attributedTicks = this.planTicks + this.bindingTicks +
                this.contextTicks + this.runtimeSystemTicks;
            long residualTicks = totalTicks - attributedTicks;
            if (residualTicks < 0L)
            {
                residualTicks = 0L;
            }

            ShuttleRuntimeTickDispatchSample sample = new ShuttleRuntimeTickDispatchSample();
            sample.TotalMs = ToMilliseconds(totalTicks);
            sample.PlanMs = ToMilliseconds(this.planTicks);
            sample.BindingMs = ToMilliseconds(this.bindingTicks);
            sample.ContextMs = ToMilliseconds(this.contextTicks);
            sample.RuntimeSystemMs = ToMilliseconds(this.runtimeSystemTicks);
            sample.ResidualMs = ToMilliseconds(residualTicks);
            sample.BindingVisits = this.bindingVisits;
            sample.ContextAcquisitions = this.contextAcquisitions;
            sample.RuntimeSystemCalls = this.runtimeSystemCalls;
            this.sink.RecordRuntimeTickDispatchSample(sample);
        }

        private long ReadElapsedTicks(long startTimestamp)
        {
            if (!this.enabled || startTimestamp <= 0L)
            {
                return 0L;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            return elapsedTicks > 0L ? elapsedTicks : 0L;
        }

        private static float ToMilliseconds(long elapsedTicks)
        {
            return elapsedTicks > 0L
                ? (float)(elapsedTicks * 1000.0 / Stopwatch.Frequency)
                : 0f;
        }
    }
}
