using System.Diagnostics;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal enum DBHPerformanceSection
    {
        PipeBridge,
        OccupantService,
        Count
    }

    /// <summary>
    /// Fixed-size, session-only timing for the two low-frequency DBH paths. Normal play does
    /// not take timestamps or write diagnostic state; developer mode pays only for the samples
    /// that run on the configured service/pipe intervals.
    /// </summary>
    internal static class DBHRuntimePerformanceMetrics
    {
        private static readonly long[] TotalStopwatchTicks =
            new long[(int)DBHPerformanceSection.Count];
        private static readonly long[] PeakStopwatchTicks =
            new long[(int)DBHPerformanceSection.Count];
        private static readonly long[] Samples =
            new long[(int)DBHPerformanceSection.Count];

        private static int lastObservedGameTick = int.MinValue;

        internal static long Start(int ticksGame)
        {
            if (!Prefs.DevMode)
            {
                return 0L;
            }

            ResetForNewGameIfNeeded(ticksGame);
            return Stopwatch.GetTimestamp();
        }

        internal static void Record(DBHPerformanceSection section, long startedAt)
        {
            if (startedAt <= 0L || !Prefs.DevMode)
            {
                return;
            }

            int index = (int)section;
            if (index < 0 || index >= TotalStopwatchTicks.Length)
            {
                return;
            }

            long elapsed = Stopwatch.GetTimestamp() - startedAt;
            if (elapsed < 0L)
            {
                return;
            }

            TotalStopwatchTicks[index] += elapsed;
            Samples[index]++;
            if (elapsed > PeakStopwatchTicks[index])
            {
                PeakStopwatchTicks[index] = elapsed;
            }
        }

        internal static bool TryGetSnapshot(
            DBHPerformanceSection section,
            out double averageMs,
            out double peakMs,
            out long samples)
        {
            averageMs = 0.0;
            peakMs = 0.0;
            samples = 0L;
            if (!Prefs.DevMode)
            {
                return false;
            }

            int index = (int)section;
            if (index < 0 || index >= TotalStopwatchTicks.Length || Samples[index] <= 0L)
            {
                return false;
            }

            samples = Samples[index];
            averageMs = TotalStopwatchTicks[index] * 1000.0 / Stopwatch.Frequency / samples;
            peakMs = PeakStopwatchTicks[index] * 1000.0 / Stopwatch.Frequency;
            return true;
        }

        private static void ResetForNewGameIfNeeded(int ticksGame)
        {
            if (lastObservedGameTick != int.MinValue &&
                ticksGame >= 0 &&
                ticksGame < lastObservedGameTick)
            {
                Clear();
            }

            if (ticksGame >= 0)
            {
                lastObservedGameTick = ticksGame;
            }
        }

        private static void Clear()
        {
            for (int i = 0; i < TotalStopwatchTicks.Length; i++)
            {
                TotalStopwatchTicks[i] = 0L;
                PeakStopwatchTicks[i] = 0L;
                Samples[i] = 0L;
            }
        }
    }
}
