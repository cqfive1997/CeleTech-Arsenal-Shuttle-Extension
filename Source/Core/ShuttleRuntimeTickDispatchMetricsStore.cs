using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Owns only low-frequency module Tick dispatcher samples so the controller profiler remains a
    /// coordinator rather than accumulating another large family of metric fields.
    /// </summary>
    internal sealed class ShuttleRuntimeTickDispatchMetricsStore
    {
        private Metric totalMetric;
        private Metric planMetric;
        private Metric bindingMetric;
        private Metric contextMetric;
        private Metric runtimeSystemMetric;
        private Metric residualMetric;
        private long bindingVisits;
        private long contextAcquisitions;
        private long runtimeSystemCalls;

        internal bool HasMetrics
        {
            get { return this.totalMetric.SampleCount > 0; }
        }

        internal void Record(ShuttleRuntimeTickDispatchSample sample)
        {
            this.totalMetric.Record(sample.TotalMs);
            this.planMetric.Record(sample.PlanMs);
            this.bindingMetric.Record(sample.BindingMs);
            this.contextMetric.Record(sample.ContextMs);
            this.runtimeSystemMetric.Record(sample.RuntimeSystemMs);
            this.residualMetric.Record(sample.ResidualMs);
            this.bindingVisits += sample.BindingVisits;
            this.contextAcquisitions += sample.ContextAcquisitions;
            this.runtimeSystemCalls += sample.RuntimeSystemCalls;
        }

        internal void LogAndClear(int profileRevision, int installedModuleCount)
        {
            if (!this.HasMetrics)
            {
                return;
            }

            string message =
                "[CeleTech Shuttle] PerfSummary module runtime Tick dispatch profile" +
                " profileRevision=" + profileRevision +
                " installedModules=" + installedModuleCount +
                " sampledTicks=" + this.totalMetric.SampleCount +
                FormatMetric("total dispatcher", this.totalMetric) +
                FormatMetric("plan get / reuse", this.planMetric) +
                FormatMetric("binding validation / scheduling", this.bindingMetric) +
                FormatMetric("context acquire / refresh", this.contextMetric) +
                FormatMetric("runtime system calls", this.runtimeSystemMetric) +
                FormatMetric("residual / profiling overhead", this.residualMetric) +
                "\n - sampled work: avg bindings " +
                AverageCount(this.bindingVisits, this.totalMetric.SampleCount).ToString("0.###") +
                ", contexts " +
                AverageCount(this.contextAcquisitions, this.totalMetric.SampleCount).ToString("0.###") +
                ", runtime calls " +
                AverageCount(this.runtimeSystemCalls, this.totalMetric.SampleCount).ToString("0.###");
            Log.Message(message);
            this.Clear();
        }

        internal void Clear()
        {
            this.totalMetric = new Metric();
            this.planMetric = new Metric();
            this.bindingMetric = new Metric();
            this.contextMetric = new Metric();
            this.runtimeSystemMetric = new Metric();
            this.residualMetric = new Metric();
            this.bindingVisits = 0L;
            this.contextAcquisitions = 0L;
            this.runtimeSystemCalls = 0L;
        }

        private static string FormatMetric(string name, Metric metric)
        {
            return "\n - " + name +
                ": avg/sample " + metric.AverageMs.ToString("0.###") +
                " ms, peak " + metric.PeakMs.ToString("0.###") + " ms";
        }

        private static float AverageCount(long total, int samples)
        {
            return samples > 0 ? (float)total / samples : 0f;
        }

        private struct Metric
        {
            internal float TotalMs;
            internal float PeakMs;
            internal int SampleCount;

            internal float AverageMs
            {
                get { return this.SampleCount > 0 ? this.TotalMs / this.SampleCount : 0f; }
            }

            internal void Record(float elapsedMs)
            {
                this.TotalMs += elapsedMs;
                this.SampleCount++;
                if (elapsedMs > this.PeakMs)
                {
                    this.PeakMs = elapsedMs;
                }
            }
        }
    }
}
