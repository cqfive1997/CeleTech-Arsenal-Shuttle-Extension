using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal static class ShuttleDiagnosticGate
    {
        /// <summary>
        /// Low-frequency developer diagnostics. This gate intentionally does not enable
        /// tick/repaint profilers.
        /// </summary>
        internal static bool ShouldLogBasicDiagnostics
        {
            get
            {
                ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
                return effective != null && effective.DevTimeCostLoggingEnabled;
            }
        }

        /// <summary>
        /// Throttled performance summaries intended for short profiling sessions.
        /// </summary>
        internal static bool ShouldLogPerformanceSummaries
        {
            get
            {
                ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
                return effective != null && effective.PerformanceSummaryLogsEnabled;
            }
        }

        /// <summary>
        /// Enables performance sample collection. Hot paths should skip timestamps and
        /// profiler dictionary work when this is false.
        /// </summary>
        internal static bool ShouldCollectPerformanceSamples
        {
            get
            {
                return ShouldLogPerformanceSummaries || ShouldLogDetailedPerformanceBreakdowns;
            }
        }

        /// <summary>
        /// Extra detailed breakdowns layered on top of performance summaries.
        /// </summary>
        internal static bool ShouldLogDetailedPerformanceBreakdowns
        {
            get
            {
                ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
                return effective != null && effective.DetailedPerformanceBreakdownLogsEnabled;
            }
        }

        /// <summary>
        /// Reserved for explicit temporary sessions; normal settings do not enable it.
        /// </summary>
        internal static bool ShouldLogHighFrequencyDiagnostics
        {
            get
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Simple mod-specific logging wrapper.
    /// It keeps log formatting consistent and automatically reduces verbosity in release builds.
    /// </summary>
    public static class ShuttleLog
    {
        private const string Prefix = "[CeleTech.Shuttle] ";
        private static readonly HashSet<string> WarnedOnceKeys = new HashSet<string>();
        private static readonly object WarnedOnceLock = new object();

        public static bool IsVerboseLogging
        {
            get
            {
                return ShuttleDiagnosticGate.ShouldLogBasicDiagnostics;
            }
        }

        public static bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static void Debug(string message)
        {
            if (!IsVerboseLogging)
            {
                return;
            }

            Log.Message(Prefix + "DEBUG: " + message);
        }

        public static void Debug(string category, string message)
        {
            if (!IsVerboseLogging)
            {
                return;
            }

            Log.Message(Format(category, "DEBUG", message));
        }

        public static void Info(string message)
        {
            if (!IsVerboseLogging)
            {
                return;
            }

            Log.Message(Prefix + message);
        }

        public static void Info(string category, string message)
        {
            if (!IsVerboseLogging)
            {
                return;
            }

            Log.Message(Format(category, "INFO", message));
        }

        public static void Warn(string message)
        {
            Log.Warning(Prefix + message);
        }

        public static void Warn(string category, string message)
        {
            Log.Warning(Format(category, "WARN", message));
        }

        public static void WarnOnce(string category, string key, string message)
        {
            string normalizedKey = category + "|" + (key ?? string.Empty);
            lock (WarnedOnceLock)
            {
                if (WarnedOnceKeys.Contains(normalizedKey))
                {
                    return;
                }

                WarnedOnceKeys.Add(normalizedKey);
            }

            Warn(category, message);
        }

        public static void Error(string message)
        {
            Log.Error(Prefix + message);
        }

        public static void Error(string category, string message)
        {
            Log.Error(Format(category, "ERROR", message));
        }

        private static string Format(string category, string level, string message)
        {
            if (string.IsNullOrEmpty(category))
            {
                return Prefix + level + ": " + message;
            }

            return Prefix + category + " " + level + ": " + message;
        }
    }
}
