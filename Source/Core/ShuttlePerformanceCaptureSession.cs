using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Owns runtime-only page leases and aggregation. Snapshot projection is delegated so
    /// inactive capture remains a small gate and this class stays lifecycle-focused.
    /// </summary>
    internal sealed class ShuttlePerformanceCaptureSession
    {
        private const int PublishIntervalTicks = 15;
        private const int TrendCapacity = 60;
        private const string TotalControllerSection = "total controller tick";

        private HashSet<int> activeLeaseIDs;
        private Dictionary<string, ShuttlePerformanceMetricAccumulator> controllerMetrics;
        private Dictionary<ShuttlePerformanceModuleMetricKey, ShuttlePerformanceMetricAccumulator>
            moduleMetrics;
        private Dictionary<ShuttlePerformanceModuleMetricKey, ShuttlePerformanceModuleMetricIdentity>
            moduleIdentities;
        private float[] recentControllerTickMs;
        private int recentControllerTickWriteIndex;
        private int recentControllerTickCount;
        private int nextLeaseID = 1;
        private int captureProfileRevision = int.MinValue;
        private int activeTickSamples;
        private int lastPublishedActiveTickSamples;
        private float currentControllerTickMs;
        private bool hasCurrentControllerTick;
        private ShuttlePerformanceDashboardReadModel snapshot =
            ShuttlePerformanceDashboardReadModel.Empty;

        internal bool IsActive
        {
            get { return this.activeLeaseIDs != null && this.activeLeaseIDs.Count > 0; }
        }

        internal ShuttlePerformanceDashboardReadModel Snapshot
        {
            get { return this.snapshot ?? ShuttlePerformanceDashboardReadModel.Empty; }
        }

        internal int Begin(int profileRevision, int ticksGame)
        {
            if (this.activeLeaseIDs == null)
            {
                this.activeLeaseIDs = new HashSet<int>();
            }

            if (this.captureProfileRevision != profileRevision)
            {
                this.ResetCore(profileRevision);
            }

            int leaseID = this.NextLeaseID();
            this.activeLeaseIDs.Add(leaseID);
            this.PublishSnapshot(true, ticksGame);
            return leaseID;
        }

        internal void End(int captureLeaseID, int ticksGame)
        {
            if (captureLeaseID <= 0 || this.activeLeaseIDs == null)
            {
                return;
            }

            if (!this.activeLeaseIDs.Remove(captureLeaseID))
            {
                return;
            }

            if (this.activeLeaseIDs.Count == 0)
            {
                this.PublishSnapshot(false, ticksGame);
            }
        }

        internal void Reset(int captureLeaseID, int profileRevision, int ticksGame)
        {
            if (captureLeaseID <= 0 ||
                this.activeLeaseIDs == null ||
                !this.activeLeaseIDs.Contains(captureLeaseID))
            {
                return;
            }

            this.ResetCore(profileRevision);
            this.PublishSnapshot(true, ticksGame);
        }

        internal void PrepareTick(int profileRevision)
        {
            if (!this.IsActive)
            {
                return;
            }

            if (this.captureProfileRevision != profileRevision)
            {
                this.ResetCore(profileRevision);
                this.PublishSnapshot(true, this.snapshot.SnapshotTick);
            }

            this.currentControllerTickMs = 0f;
            this.hasCurrentControllerTick = false;
        }

        internal void RecordControllerSection(string sectionKey, float elapsedMs)
        {
            if (!this.IsActive || string.IsNullOrEmpty(sectionKey))
            {
                return;
            }

            this.EnsureMetricStores();
            ShuttlePerformanceMetricAccumulator metric;
            this.controllerMetrics.TryGetValue(sectionKey, out metric);
            metric.Record(elapsedMs);
            this.controllerMetrics[sectionKey] = metric;
            if (sectionKey == TotalControllerSection)
            {
                this.currentControllerTickMs =
                    ShuttlePerformanceMetricAccumulator.SanitizeElapsed(elapsedMs);
                this.hasCurrentControllerTick = true;
            }
        }

        internal void RecordModule(
            string moduleInstanceID,
            string moduleLabel,
            string runtimeSystemKey,
            int tickInterval,
            float elapsedMs)
        {
            if (!this.IsActive)
            {
                return;
            }

            this.EnsureMetricStores();
            ShuttlePerformanceModuleMetricKey key =
                new ShuttlePerformanceModuleMetricKey(moduleInstanceID, runtimeSystemKey);
            ShuttlePerformanceMetricAccumulator metric;
            this.moduleMetrics.TryGetValue(key, out metric);
            metric.Record(elapsedMs);
            this.moduleMetrics[key] = metric;
            if (!this.moduleIdentities.ContainsKey(key))
            {
                this.moduleIdentities[key] = new ShuttlePerformanceModuleMetricIdentity(
                    moduleInstanceID,
                    moduleLabel,
                    runtimeSystemKey,
                    tickInterval);
            }
        }

        internal void CompleteTick(int ticksGame)
        {
            if (!this.IsActive)
            {
                return;
            }

            this.activeTickSamples++;
            if (this.hasCurrentControllerTick)
            {
                this.EnsureTrendBuffer();
                this.recentControllerTickMs[this.recentControllerTickWriteIndex] =
                    this.currentControllerTickMs;
                this.recentControllerTickWriteIndex =
                    (this.recentControllerTickWriteIndex + 1) % TrendCapacity;
                if (this.recentControllerTickCount < TrendCapacity)
                {
                    this.recentControllerTickCount++;
                }
            }

            if (this.activeTickSamples - this.lastPublishedActiveTickSamples >=
                PublishIntervalTicks)
            {
                this.PublishSnapshot(true, ticksGame);
            }
        }

        private void EnsureMetricStores()
        {
            if (this.controllerMetrics == null)
            {
                this.controllerMetrics =
                    new Dictionary<string, ShuttlePerformanceMetricAccumulator>();
            }

            if (this.moduleMetrics == null)
            {
                this.moduleMetrics =
                    new Dictionary<ShuttlePerformanceModuleMetricKey,
                        ShuttlePerformanceMetricAccumulator>();
            }

            if (this.moduleIdentities == null)
            {
                this.moduleIdentities =
                    new Dictionary<ShuttlePerformanceModuleMetricKey,
                        ShuttlePerformanceModuleMetricIdentity>();
            }
        }

        private void EnsureTrendBuffer()
        {
            if (this.recentControllerTickMs == null)
            {
                this.recentControllerTickMs = new float[TrendCapacity];
            }
        }

        private void ResetCore(int profileRevision)
        {
            if (this.controllerMetrics != null)
            {
                this.controllerMetrics.Clear();
            }

            if (this.moduleMetrics != null)
            {
                this.moduleMetrics.Clear();
            }

            if (this.moduleIdentities != null)
            {
                this.moduleIdentities.Clear();
            }

            if (this.recentControllerTickMs != null)
            {
                Array.Clear(
                    this.recentControllerTickMs,
                    0,
                    this.recentControllerTickMs.Length);
            }

            this.captureProfileRevision = profileRevision;
            this.activeTickSamples = 0;
            this.lastPublishedActiveTickSamples = 0;
            this.recentControllerTickWriteIndex = 0;
            this.recentControllerTickCount = 0;
            this.currentControllerTickMs = 0f;
            this.hasCurrentControllerTick = false;
        }

        private void PublishSnapshot(bool isCapturing, int ticksGame)
        {
            this.snapshot = ShuttlePerformanceDashboardSnapshotBuilder.Build(
                isCapturing,
                this.captureProfileRevision,
                this.activeTickSamples,
                ticksGame,
                TotalControllerSection,
                this.controllerMetrics,
                this.moduleMetrics,
                this.moduleIdentities,
                this.recentControllerTickMs,
                this.recentControllerTickWriteIndex,
                this.recentControllerTickCount,
                TrendCapacity);
            this.lastPublishedActiveTickSamples = this.activeTickSamples;
        }

        private int NextLeaseID()
        {
            int leaseID = this.nextLeaseID;
            this.nextLeaseID++;
            if (this.nextLeaseID <= 0)
            {
                this.nextLeaseID = 1;
            }

            while (this.activeLeaseIDs.Contains(leaseID) || leaseID <= 0)
            {
                leaseID = this.nextLeaseID;
                this.nextLeaseID++;
                if (this.nextLeaseID <= 0)
                {
                    this.nextLeaseID = 1;
                }
            }

            return leaseID;
        }
    }
}
