using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns elapsed-time samples for named weapon tick sections.
    /// </summary>
    internal static class ShuttleWeaponSectionMetricsStore
    {
        private const int MaxSectionsToLog = 12;
        private static Dictionary<string, SectionMetric> metrics;

        internal static bool HasMetrics
        {
            get { return metrics != null && metrics.Count > 0; }
        }

        internal static long Start(bool enabled)
        {
            return enabled ? Stopwatch.GetTimestamp() : 0L;
        }

        internal static void Record(
            bool enabled,
            string sectionName,
            long startTimestamp)
        {
            if (!enabled || string.IsNullOrEmpty(sectionName) || startTimestamp <= 0L)
            {
                return;
            }

            if (metrics == null)
            {
                metrics = new Dictionary<string, SectionMetric>();
            }

            SectionMetric metric;
            if (!metrics.TryGetValue(sectionName, out metric))
            {
                metric = new SectionMetric();
            }

            metric.Record(GetElapsedMilliseconds(startTimestamp));
            metrics[sectionName] = metric;
        }

        internal static void LogAndClear(int ticksGame)
        {
            if (!HasMetrics)
            {
                return;
            }

            List<KeyValuePair<string, SectionMetric>> sortedMetrics =
                new List<KeyValuePair<string, SectionMetric>>(metrics);
            sortedMetrics.Sort(CompareByAverageDescending);

            string message = "[CeleTech Shuttle] PerfSummary weapon runtime Tick profile" +
                " ticksGame=" + ticksGame;
            int limit = sortedMetrics.Count < MaxSectionsToLog
                ? sortedMetrics.Count
                : MaxSectionsToLog;
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
                message += "\n - ... " +
                    (sortedMetrics.Count - limit) +
                    " more weapon sections";
            }

            Log.Message(message);
            metrics.Clear();
        }

        private static int CompareByAverageDescending(
            KeyValuePair<string, SectionMetric> left,
            KeyValuePair<string, SectionMetric> right)
        {
            int averageComparison =
                right.Value.AverageMs.CompareTo(left.Value.AverageMs);
            if (averageComparison != 0)
            {
                return averageComparison;
            }

            int peakComparison = right.Value.PeakMs.CompareTo(left.Value.PeakMs);
            return peakComparison != 0
                ? peakComparison
                : string.CompareOrdinal(left.Key, right.Key);
        }

        private static float GetElapsedMilliseconds(long startTimestamp)
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            return elapsedTicks > 0L
                ? (float)(elapsedTicks * 1000.0 / Stopwatch.Frequency)
                : 0f;
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
                    return this.SampleCount > 0
                        ? this.TotalMs / this.SampleCount
                        : 0f;
                }
            }

            internal void Record(float elapsedMs)
            {
                if (float.IsNaN(elapsedMs) ||
                    float.IsInfinity(elapsedMs) ||
                    elapsedMs < 0f)
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
