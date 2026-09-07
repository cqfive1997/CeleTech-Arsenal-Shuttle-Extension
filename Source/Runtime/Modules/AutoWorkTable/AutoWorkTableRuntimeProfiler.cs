using System.Diagnostics;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal enum AutoWorkTableProfileSection
    {
        RuntimeTick,
        QueueScheduling,
        IngredientResolution,
        IngredientStaging,
        ActiveWork,
        Completion,
        ProductDeposit,
        Recovery,
        Count
    }

    internal enum AutoWorkTableProfileCounter
    {
        QueueChecks,
        OrdersConsidered,
        OrdersSelected,
        CargoRevisionWakes,
        InventorySnapshots,
        StageAttempts,
        CompletionAttempts,
        DepositAttempts,
        Count
    }

    /// <summary>
    /// Opt-in, allocation-free-while-disabled timing for production. Summaries are deliberately
    /// aggregated so profiling a busy shuttle does not create a second performance problem.
    /// </summary>
    internal static class AutoWorkTableRuntimeProfiler
    {
        private const int SummaryIntervalTicks = 2500;
        private static readonly long[] totalStopwatchTicks =
            new long[(int)AutoWorkTableProfileSection.Count];
        private static readonly long[] peakStopwatchTicks =
            new long[(int)AutoWorkTableProfileSection.Count];
        private static readonly long[] samples =
            new long[(int)AutoWorkTableProfileSection.Count];
        private static readonly long[] counters =
            new long[(int)AutoWorkTableProfileCounter.Count];
        private static int lastSummaryTick = int.MinValue;

        internal static bool Enabled
        {
            get { return ShuttleDiagnosticGate.ShouldCollectPerformanceSamples; }
        }

        internal static long Start(bool enabled)
        {
            return enabled ? Stopwatch.GetTimestamp() : 0L;
        }

        internal static void Record(
            bool enabled,
            AutoWorkTableProfileSection section,
            long startedAt)
        {
            if (!enabled || startedAt <= 0L)
            {
                return;
            }

            int index = (int)section;
            if (index < 0 || index >= totalStopwatchTicks.Length)
            {
                return;
            }

            long elapsed = Stopwatch.GetTimestamp() - startedAt;
            if (elapsed < 0L)
            {
                return;
            }

            totalStopwatchTicks[index] += elapsed;
            samples[index]++;
            if (elapsed > peakStopwatchTicks[index])
            {
                peakStopwatchTicks[index] = elapsed;
            }
        }

        internal static void Count(bool enabled, AutoWorkTableProfileCounter counter)
        {
            if (!enabled)
            {
                return;
            }

            int index = (int)counter;
            if (index >= 0 && index < counters.Length)
            {
                counters[index]++;
            }
        }

        internal static void MaybeLog(int ticksGame)
        {
            if (!Enabled || ticksGame < 0)
            {
                return;
            }

            if (lastSummaryTick == int.MinValue)
            {
                lastSummaryTick = ticksGame;
                return;
            }

            if (ticksGame < lastSummaryTick || ticksGame - lastSummaryTick >= SummaryIntervalTicks)
            {
                LogSummary(ticksGame);
                Clear();
                lastSummaryTick = ticksGame;
            }
        }

        private static void LogSummary(int ticksGame)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[CeleTech Shuttle] PerfSummary auto worktable windowTicks=");
            builder.Append(lastSummaryTick == int.MinValue ? 0 : ticksGame - lastSummaryTick);

            for (int i = 0; i < totalStopwatchTicks.Length; i++)
            {
                if (samples[i] <= 0L)
                {
                    continue;
                }

                double totalMs = totalStopwatchTicks[i] * 1000.0 / Stopwatch.Frequency;
                double peakMs = peakStopwatchTicks[i] * 1000.0 / Stopwatch.Frequency;
                builder.Append("\n - ");
                builder.Append(((AutoWorkTableProfileSection)i).ToString());
                builder.Append(": total ");
                builder.Append(totalMs.ToString("0.###"));
                builder.Append(" ms, avg ");
                builder.Append((totalMs / samples[i]).ToString("0.###"));
                builder.Append(" ms, peak ");
                builder.Append(peakMs.ToString("0.###"));
                builder.Append(" ms, samples ");
                builder.Append(samples[i]);
            }

            builder.Append("\n - counters:");
            for (int i = 0; i < counters.Length; i++)
            {
                builder.Append(' ');
                builder.Append(((AutoWorkTableProfileCounter)i).ToString());
                builder.Append('=');
                builder.Append(counters[i]);
            }

            Log.Message(builder.ToString());
        }

        private static void Clear()
        {
            for (int i = 0; i < totalStopwatchTicks.Length; i++)
            {
                totalStopwatchTicks[i] = 0L;
                peakStopwatchTicks[i] = 0L;
                samples[i] = 0L;
            }

            for (int i = 0; i < counters.Length; i++)
            {
                counters[i] = 0L;
            }
        }
    }
}
