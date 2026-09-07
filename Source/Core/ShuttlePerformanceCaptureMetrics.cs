using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal struct ShuttlePerformanceMetricAccumulator
    {
        internal long CallCount;
        internal float TotalMs;
        internal float PeakMs;

        internal float AverageMs
        {
            get { return this.CallCount > 0L ? this.TotalMs / this.CallCount : 0f; }
        }

        internal void Record(float elapsedMs)
        {
            elapsedMs = SanitizeElapsed(elapsedMs);
            this.CallCount++;
            this.TotalMs += elapsedMs;
            if (elapsedMs > this.PeakMs)
            {
                this.PeakMs = elapsedMs;
            }
        }

        internal static float SanitizeElapsed(float elapsedMs)
        {
            return float.IsNaN(elapsedMs) ||
                float.IsInfinity(elapsedMs) ||
                elapsedMs < 0f
                    ? 0f
                    : elapsedMs;
        }
    }

    internal struct ShuttlePerformanceModuleMetricIdentity
    {
        internal readonly string ModuleInstanceID;
        internal readonly string ModuleLabel;
        internal readonly string RuntimeSystemKey;
        internal readonly int TickInterval;

        internal ShuttlePerformanceModuleMetricIdentity(
            string moduleInstanceID,
            string moduleLabel,
            string runtimeSystemKey,
            int tickInterval)
        {
            this.ModuleInstanceID = moduleInstanceID ?? string.Empty;
            this.ModuleLabel = string.IsNullOrEmpty(moduleLabel)
                ? this.ModuleInstanceID
                : moduleLabel;
            this.RuntimeSystemKey = string.IsNullOrEmpty(runtimeSystemKey)
                ? "unknown-runtime-system"
                : runtimeSystemKey;
            this.TickInterval = tickInterval;
        }
    }

    internal struct ShuttlePerformanceModuleMetricKey :
        IEquatable<ShuttlePerformanceModuleMetricKey>
    {
        internal readonly string ModuleInstanceID;
        internal readonly string RuntimeSystemKey;
        private readonly int hashCode;

        internal ShuttlePerformanceModuleMetricKey(
            string moduleInstanceID,
            string runtimeSystemKey)
        {
            this.ModuleInstanceID = moduleInstanceID ?? string.Empty;
            this.RuntimeSystemKey = runtimeSystemKey ?? string.Empty;
            unchecked
            {
                this.hashCode =
                    (StringComparer.Ordinal.GetHashCode(this.ModuleInstanceID) * 397) ^
                    StringComparer.Ordinal.GetHashCode(this.RuntimeSystemKey);
            }
        }

        public bool Equals(ShuttlePerformanceModuleMetricKey other)
        {
            return string.Equals(
                    this.ModuleInstanceID,
                    other.ModuleInstanceID,
                    StringComparison.Ordinal) &&
                string.Equals(
                    this.RuntimeSystemKey,
                    other.RuntimeSystemKey,
                    StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ShuttlePerformanceModuleMetricKey &&
                this.Equals((ShuttlePerformanceModuleMetricKey)obj);
        }

        public override int GetHashCode()
        {
            return this.hashCode;
        }
    }
}
