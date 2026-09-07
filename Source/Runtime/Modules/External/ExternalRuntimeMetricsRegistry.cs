using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeMetricsRegistry
    {
        private const int RollingAverageWindow = 64;
        private static readonly object SyncRoot = new object();
        // Metrics-R1 keeps session records until game shutdown. A later cleanup pass can prune
        // records when module/runtime pairs are removed or no longer appear in assembly state.
        private static readonly Dictionary<ExternalRuntimeMetricsKey, ExternalRuntimeMetricsRecord> Records =
            new Dictionary<ExternalRuntimeMetricsKey, ExternalRuntimeMetricsRecord>();

        internal static long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        internal static void Clear()
        {
            lock (SyncRoot)
            {
                Records.Clear();
            }
        }

        internal static void RecordReconcileDuration(
            string moduleInstanceID,
            string runtimeSystemKey,
            long startTimestamp)
        {
            RecordDuration(moduleInstanceID, runtimeSystemKey, startTimestamp, MetricKind.Reconcile);
        }

        internal static void RecordTickDuration(
            string moduleInstanceID,
            string runtimeSystemKey,
            long startTimestamp)
        {
            RecordDuration(moduleInstanceID, runtimeSystemKey, startTimestamp, MetricKind.Tick);
        }

        internal static void RecordPowerDemandDurationAndWatts(
            string moduleInstanceID,
            string runtimeSystemKey,
            long startTimestamp,
            float watts)
        {
            if (!IsValidIdentity(moduleInstanceID, runtimeSystemKey))
            {
                return;
            }

            float elapsedMs = GetElapsedMilliseconds(startTimestamp);
            float safeWatts = IsFinite(watts) && watts > 0f ? watts : 0f;

            lock (SyncRoot)
            {
                ExternalRuntimeMetricsRecord record = GetOrCreateRecord(moduleInstanceID, runtimeSystemKey);
                record.RecordPowerDemand(elapsedMs, safeWatts);
            }
        }

        internal static void RecordLastPowerDemandWatts(
            string moduleInstanceID,
            string runtimeSystemKey,
            float watts)
        {
            if (!IsValidIdentity(moduleInstanceID, runtimeSystemKey))
            {
                return;
            }

            float safeWatts = IsFinite(watts) && watts > 0f ? watts : 0f;
            lock (SyncRoot)
            {
                ExternalRuntimeMetricsRecord record = GetOrCreateRecord(moduleInstanceID, runtimeSystemKey);
                record.LastKnownPowerDemandWatts = safeWatts;
            }
        }

        internal static void RecordStoredEnergyConsumed(
            string moduleInstanceID,
            string runtimeSystemKey,
            float amountWd)
        {
            if (!IsValidIdentity(moduleInstanceID, runtimeSystemKey) ||
                !IsFinite(amountWd) ||
                amountWd <= 0f)
            {
                return;
            }

            lock (SyncRoot)
            {
                ExternalRuntimeMetricsRecord record = GetOrCreateRecord(moduleInstanceID, runtimeSystemKey);
                record.TotalStoredEnergyConsumedWd += amountWd;
            }
        }

        internal static Func<float, bool> CreateTrackedStoredEnergyConsumer(
            string moduleInstanceID,
            string runtimeSystemKey,
            Func<float, bool> innerConsumer)
        {
            if (innerConsumer == null)
            {
                return null;
            }

            return delegate(float amountWd)
            {
                bool consumed = innerConsumer(amountWd);
                if (consumed)
                {
                    RecordStoredEnergyConsumed(moduleInstanceID, runtimeSystemKey, amountWd);
                }

                return consumed;
            };
        }

        internal static ExternalRuntimeMetricsSnapshot GetSnapshot(
            string moduleInstanceID,
            string runtimeSystemKey)
        {
            if (!IsValidIdentity(moduleInstanceID, runtimeSystemKey))
            {
                return null;
            }

            lock (SyncRoot)
            {
                ExternalRuntimeMetricsRecord record;
                if (!Records.TryGetValue(MakeKey(moduleInstanceID, runtimeSystemKey), out record))
                {
                    return null;
                }

                return record.CreateSnapshot();
            }
        }

        private static void RecordDuration(
            string moduleInstanceID,
            string runtimeSystemKey,
            long startTimestamp,
            MetricKind kind)
        {
            if (!IsValidIdentity(moduleInstanceID, runtimeSystemKey))
            {
                return;
            }

            float elapsedMs = GetElapsedMilliseconds(startTimestamp);
            lock (SyncRoot)
            {
                ExternalRuntimeMetricsRecord record = GetOrCreateRecord(moduleInstanceID, runtimeSystemKey);
                if (kind == MetricKind.Reconcile)
                {
                    record.RecordReconcile(elapsedMs);
                }
                else if (kind == MetricKind.Tick)
                {
                    record.RecordTick(elapsedMs);
                }
            }
        }

        private static ExternalRuntimeMetricsRecord GetOrCreateRecord(
            string moduleInstanceID,
            string runtimeSystemKey)
        {
            ExternalRuntimeMetricsKey key = MakeKey(moduleInstanceID, runtimeSystemKey);
            ExternalRuntimeMetricsRecord record;
            if (!Records.TryGetValue(key, out record))
            {
                record = new ExternalRuntimeMetricsRecord(moduleInstanceID, runtimeSystemKey);
                Records[key] = record;
            }

            return record;
        }

        private static ExternalRuntimeMetricsKey MakeKey(
            string moduleInstanceID,
            string runtimeSystemKey)
        {
            return new ExternalRuntimeMetricsKey(moduleInstanceID, runtimeSystemKey);
        }

        private static bool IsValidIdentity(string moduleInstanceID, string runtimeSystemKey)
        {
            return !string.IsNullOrEmpty(moduleInstanceID) &&
                !string.IsNullOrEmpty(runtimeSystemKey);
        }

        private static float GetElapsedMilliseconds(long startTimestamp)
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            if (elapsedTicks <= 0L)
            {
                return 0f;
            }

            return (float)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private struct ExternalRuntimeMetricsKey : IEquatable<ExternalRuntimeMetricsKey>
        {
            private readonly string moduleInstanceID;
            private readonly string runtimeSystemKey;

            internal ExternalRuntimeMetricsKey(
                string moduleInstanceID,
                string runtimeSystemKey)
            {
                this.moduleInstanceID = moduleInstanceID ?? string.Empty;
                this.runtimeSystemKey = runtimeSystemKey ?? string.Empty;
            }

            public bool Equals(ExternalRuntimeMetricsKey other)
            {
                return string.Equals(
                        this.moduleInstanceID,
                        other.moduleInstanceID,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        this.runtimeSystemKey,
                        other.runtimeSystemKey,
                        StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is ExternalRuntimeMetricsKey &&
                    this.Equals((ExternalRuntimeMetricsKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + StringComparer.Ordinal.GetHashCode(this.moduleInstanceID);
                    hash = hash * 31 + StringComparer.Ordinal.GetHashCode(this.runtimeSystemKey);
                    return hash;
                }
            }
        }

        private enum MetricKind
        {
            Reconcile,
            Tick
        }

        private sealed class ExternalRuntimeMetricsRecord
        {
            private readonly string moduleInstanceID;
            private readonly string runtimeSystemKey;
            private RollingMetric reconcileCost;
            private RollingMetric tickCost;
            private RollingMetric powerDemandCost;

            internal ExternalRuntimeMetricsRecord(
                string moduleInstanceID,
                string runtimeSystemKey)
            {
                this.moduleInstanceID = moduleInstanceID;
                this.runtimeSystemKey = runtimeSystemKey;
            }

            internal float LastKnownPowerDemandWatts;
            internal float TotalStoredEnergyConsumedWd;

            internal void RecordReconcile(float elapsedMs)
            {
                this.reconcileCost.Record(elapsedMs);
            }

            internal void RecordTick(float elapsedMs)
            {
                this.tickCost.Record(elapsedMs);
            }

            internal void RecordPowerDemand(float elapsedMs, float watts)
            {
                this.powerDemandCost.Record(elapsedMs);
                this.LastKnownPowerDemandWatts = watts;
            }

            internal ExternalRuntimeMetricsSnapshot CreateSnapshot()
            {
                return new ExternalRuntimeMetricsSnapshot
                {
                    ModuleInstanceID = this.moduleInstanceID,
                    RuntimeSystemKey = this.runtimeSystemKey,
                    AverageReconcileCostMs = this.reconcileCost.AverageMs,
                    PeakReconcileCostMs = this.reconcileCost.PeakMs,
                    AverageTickCostMs = this.tickCost.AverageMs,
                    PeakTickCostMs = this.tickCost.PeakMs,
                    AveragePowerDemandCostMs = this.powerDemandCost.AverageMs,
                    PeakPowerDemandCostMs = this.powerDemandCost.PeakMs,
                    LastKnownPowerDemandWatts = this.LastKnownPowerDemandWatts,
                    TotalStoredEnergyConsumedWd = this.TotalStoredEnergyConsumedWd
                };
            }
        }

        private struct RollingMetric
        {
            private int sampleCount;

            internal float AverageMs;
            internal float PeakMs;

            internal void Record(float elapsedMs)
            {
                if (!ExternalRuntimeMetricsRegistry.IsFinite(elapsedMs) || elapsedMs < 0f)
                {
                    elapsedMs = 0f;
                }

                this.sampleCount++;
                if (this.sampleCount == 1)
                {
                    this.AverageMs = elapsedMs;
                }
                else
                {
                    int divisor = Math.Min(this.sampleCount, RollingAverageWindow);
                    this.AverageMs += (elapsedMs - this.AverageMs) / divisor;
                }

                if (elapsedMs > this.PeakMs)
                {
                    // Peak is a session/lifetime peak for this in-memory metrics record, not a
                    // strict sliding-window peak.
                    this.PeakMs = elapsedMs;
                }
            }
        }
    }
}
