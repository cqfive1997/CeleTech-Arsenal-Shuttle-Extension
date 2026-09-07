using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal static class ShuttlePerformanceDashboardSnapshotBuilder
    {
        internal static ShuttlePerformanceDashboardReadModel Build(
            bool isCapturing,
            int profileRevision,
            int activeTickSamples,
            int ticksGame,
            string totalControllerSection,
            IDictionary<string, ShuttlePerformanceMetricAccumulator> controllerMetrics,
            IDictionary<ShuttlePerformanceModuleMetricKey, ShuttlePerformanceMetricAccumulator>
                moduleMetrics,
            IDictionary<ShuttlePerformanceModuleMetricKey, ShuttlePerformanceModuleMetricIdentity>
                moduleIdentities,
            float[] recentControllerTickMs,
            int recentControllerTickWriteIndex,
            int recentControllerTickCount,
            int trendCapacity)
        {
            ShuttlePerformanceMetricAccumulator totalController = GetControllerMetric(
                controllerMetrics,
                totalControllerSection);
            float controllerTotalMs = totalController.TotalMs;
            float moduleTotalMs;
            List<ShuttlePerformanceModuleMetricReadModel> modules = BuildModuleRows(
                moduleMetrics,
                moduleIdentities,
                controllerTotalMs,
                activeTickSamples,
                out moduleTotalMs);
            List<ShuttlePerformanceControllerPhaseReadModel> phases =
                BuildControllerRows(
                    controllerMetrics,
                    totalControllerSection,
                    controllerTotalMs,
                    activeTickSamples);
            float coreTotalMs = controllerTotalMs - moduleTotalMs;
            if (coreTotalMs < 0f)
            {
                coreTotalMs = 0f;
            }

            ShuttlePerformanceDashboardReadModel result =
                new ShuttlePerformanceDashboardReadModel();
            result.IsCapturing = isCapturing;
            result.HasCachedSamples = activeTickSamples > 0;
            result.ProfileRevision = profileRevision == int.MinValue ? 0 : profileRevision;
            result.ActiveTickSamples = activeTickSamples;
            result.SnapshotTick = ticksGame;
            result.ControllerAverageMsPerTick = AveragePerActiveTick(
                controllerTotalMs,
                activeTickSamples);
            result.ControllerPeakMs = totalController.PeakMs;
            result.ModuleAverageMsPerTick = AveragePerActiveTick(
                moduleTotalMs,
                activeTickSamples);
            result.CoreAverageMsPerTick = AveragePerActiveTick(
                coreTotalMs,
                activeTickSamples);
            result.ModuleShare01 = Ratio01(moduleTotalMs, controllerTotalMs);
            result.ModuleMetrics = modules;
            result.ControllerPhases = phases;
            result.RecentControllerTickMs = BuildTrendSnapshot(
                recentControllerTickMs,
                recentControllerTickWriteIndex,
                recentControllerTickCount,
                trendCapacity);
            return result;
        }

        private static List<ShuttlePerformanceModuleMetricReadModel> BuildModuleRows(
            IDictionary<ShuttlePerformanceModuleMetricKey, ShuttlePerformanceMetricAccumulator>
                metrics,
            IDictionary<ShuttlePerformanceModuleMetricKey, ShuttlePerformanceModuleMetricIdentity>
                identities,
            float controllerTotalMs,
            int activeTickSamples,
            out float moduleTotalMs)
        {
            moduleTotalMs = 0f;
            List<ShuttlePerformanceModuleMetricReadModel> rows =
                new List<ShuttlePerformanceModuleMetricReadModel>();
            if (metrics == null)
            {
                return rows;
            }

            foreach (KeyValuePair<ShuttlePerformanceModuleMetricKey,
                ShuttlePerformanceMetricAccumulator> pair in metrics)
            {
                ShuttlePerformanceModuleMetricIdentity identity;
                if (identities == null || !identities.TryGetValue(pair.Key, out identity))
                {
                    identity = new ShuttlePerformanceModuleMetricIdentity(
                        pair.Key.ModuleInstanceID,
                        pair.Key.RuntimeSystemKey,
                        pair.Key.RuntimeSystemKey,
                        0);
                }

                ShuttlePerformanceMetricAccumulator metric = pair.Value;
                moduleTotalMs += metric.TotalMs;
                ShuttlePerformanceModuleMetricReadModel row =
                    new ShuttlePerformanceModuleMetricReadModel();
                row.ModuleInstanceID = identity.ModuleInstanceID;
                row.ModuleLabel = identity.ModuleLabel;
                row.RuntimeSystemKey = identity.RuntimeSystemKey;
                row.TickInterval = identity.TickInterval;
                row.CallCount = metric.CallCount;
                row.AverageMsPerTick = AveragePerActiveTick(
                    metric.TotalMs,
                    activeTickSamples);
                row.AverageMsPerCall = metric.AverageMs;
                row.PeakMs = metric.PeakMs;
                row.ControllerShare01 = Ratio01(metric.TotalMs, controllerTotalMs);
                rows.Add(row);
            }

            rows.Sort(CompareModuleRows);
            return rows;
        }

        private static List<ShuttlePerformanceControllerPhaseReadModel>
            BuildControllerRows(
                IDictionary<string, ShuttlePerformanceMetricAccumulator> metrics,
                string totalControllerSection,
                float controllerTotalMs,
                int activeTickSamples)
        {
            List<ShuttlePerformanceControllerPhaseReadModel> rows =
                new List<ShuttlePerformanceControllerPhaseReadModel>();
            if (metrics == null)
            {
                return rows;
            }

            foreach (KeyValuePair<string, ShuttlePerformanceMetricAccumulator> pair in metrics)
            {
                if (pair.Key == totalControllerSection)
                {
                    continue;
                }

                ShuttlePerformanceMetricAccumulator metric = pair.Value;
                ShuttlePerformanceControllerPhaseReadModel row =
                    new ShuttlePerformanceControllerPhaseReadModel();
                row.SectionKey = pair.Key;
                row.CallCount = metric.CallCount;
                row.AverageMsPerTick = AveragePerActiveTick(
                    metric.TotalMs,
                    activeTickSamples);
                row.AverageMsPerCall = metric.AverageMs;
                row.PeakMs = metric.PeakMs;
                row.ControllerShare01 = Ratio01(metric.TotalMs, controllerTotalMs);
                rows.Add(row);
            }

            rows.Sort(CompareControllerRows);
            return rows;
        }

        private static IReadOnlyList<float> BuildTrendSnapshot(
            float[] source,
            int writeIndex,
            int count,
            int capacity)
        {
            List<float> values = new List<float>(count);
            if (source == null || count <= 0 || capacity <= 0)
            {
                return values;
            }

            int start = count < capacity ? 0 : writeIndex;
            for (int i = 0; i < count; i++)
            {
                values.Add(source[(start + i) % capacity]);
            }

            return values;
        }

        private static ShuttlePerformanceMetricAccumulator GetControllerMetric(
            IDictionary<string, ShuttlePerformanceMetricAccumulator> metrics,
            string sectionKey)
        {
            ShuttlePerformanceMetricAccumulator metric;
            return metrics != null && metrics.TryGetValue(sectionKey, out metric)
                ? metric
                : default(ShuttlePerformanceMetricAccumulator);
        }

        private static float AveragePerActiveTick(float totalMs, int activeTickSamples)
        {
            return activeTickSamples > 0 ? totalMs / activeTickSamples : 0f;
        }

        private static float Ratio01(float numerator, float denominator)
        {
            if (denominator <= 0f || numerator <= 0f)
            {
                return 0f;
            }

            float value = numerator / denominator;
            return value > 1f ? 1f : value;
        }

        private static int CompareModuleRows(
            ShuttlePerformanceModuleMetricReadModel left,
            ShuttlePerformanceModuleMetricReadModel right)
        {
            int average = right.AverageMsPerTick.CompareTo(left.AverageMsPerTick);
            if (average != 0)
            {
                return average;
            }

            int peak = right.PeakMs.CompareTo(left.PeakMs);
            return peak != 0
                ? peak
                : string.CompareOrdinal(left.ModuleInstanceID, right.ModuleInstanceID);
        }

        private static int CompareControllerRows(
            ShuttlePerformanceControllerPhaseReadModel left,
            ShuttlePerformanceControllerPhaseReadModel right)
        {
            int average = right.AverageMsPerTick.CompareTo(left.AverageMsPerTick);
            return average != 0
                ? average
                : string.CompareOrdinal(left.SectionKey, right.SectionKey);
        }
    }
}
