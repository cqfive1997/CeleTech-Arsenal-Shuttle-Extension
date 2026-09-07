using System.Diagnostics;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics
{
    internal static class ShuttleUIProfiler
    {
        private const int SummaryIntervalTicks = 120;

        private static readonly long[] SectionTotalTicks =
            new long[(int)ShuttleUIProfileSection.Count];
        private static readonly long[] SectionMaxTicks =
            new long[(int)ShuttleUIProfileSection.Count];
        private static readonly int[] SectionSampleCounts =
            new int[(int)ShuttleUIProfileSection.Count];
        private static readonly int[] CounterCounts =
            new int[(int)ShuttleUIProfileCounter.Count];

        private static int lastSummaryTick = int.MinValue;

        internal static bool Enabled
        {
            get
            {
                ShuttleEffectiveSettings effective =
                    CeleTechShuttleMod.EffectiveSettings;
                return effective != null && effective.EnableUIProfiler;
            }
        }

        internal static ShuttleUIProfileScope Scope(
            ShuttleUIProfileSection section)
        {
            if (!Enabled)
            {
                return ShuttleUIProfileScope.Inactive;
            }

            return new ShuttleUIProfileScope(
                section,
                Stopwatch.GetTimestamp());
        }

        internal static void RecordCounter(ShuttleUIProfileCounter counter)
        {
            if (!Enabled)
            {
                return;
            }

            int index = (int)counter;
            if (index < 0 || index >= CounterCounts.Length)
            {
                return;
            }

            CounterCounts[index]++;
        }

        internal static void RecordElapsedSince(
            ShuttleUIProfileSection section,
            long startTimestamp)
        {
            if (startTimestamp <= 0L || !Enabled)
            {
                return;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            if (elapsedTicks < 0L)
            {
                return;
            }

            RecordElapsedTicks(section, elapsedTicks);
        }

        internal static void RecordElapsedTicks(
            ShuttleUIProfileSection section,
            long elapsedTicks)
        {
            if (elapsedTicks < 0L || !Enabled)
            {
                return;
            }

            int index = (int)section;
            if (index < 0 || index >= SectionTotalTicks.Length)
            {
                return;
            }

            SectionTotalTicks[index] += elapsedTicks;
            SectionSampleCounts[index]++;
            if (elapsedTicks > SectionMaxTicks[index])
            {
                SectionMaxTicks[index] = elapsedTicks;
            }
        }

        internal static void MaybeLog()
        {
            if (!Enabled ||
                !ShuttleDiagnosticGate.ShouldLogPerformanceSummaries)
            {
                return;
            }

            int ticks = ShuttleTickUtility.TicksGameOrZero();
            if (lastSummaryTick == int.MinValue || ticks < lastSummaryTick)
            {
                lastSummaryTick = ticks;
                return;
            }

            if (ticks - lastSummaryTick < SummaryIntervalTicks)
            {
                return;
            }

            if (!HasSamples())
            {
                lastSummaryTick = ticks;
                return;
            }

            Log.Message(BuildSummaryMessage(ticks));
            if (ShuttleDiagnosticGate.ShouldLogDetailedPerformanceBreakdowns)
            {
                Log.Message(BuildDetailedMessage(ticks));
            }

            ResetSamples();
            lastSummaryTick = ticks;
        }

        private static bool HasSamples()
        {
            for (int i = 0; i < SectionSampleCounts.Length; i++)
            {
                if (SectionSampleCounts[i] > 0)
                {
                    return true;
                }
            }

            for (int i = 0; i < CounterCounts.Length; i++)
            {
                if (CounterCounts[i] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildSummaryMessage(int ticks)
        {
            long totalTicks = 0L;
            int totalSamples = 0;
            for (int i = 0; i < SectionTotalTicks.Length; i++)
            {
                totalTicks += SectionTotalTicks[i];
                totalSamples += SectionSampleCounts[i];
            }

            int topA;
            int topB;
            int topC;
            FindTopSections(out topA, out topB, out topC);

            StringBuilder builder = new StringBuilder(256);
            builder.Append("[CeleTech Shuttle] PerfSummary V3 UI profile");
            builder.Append(" ticks=").Append(ticks);
            builder.Append(" samples=").Append(totalSamples);
            builder.Append(" totalMs=").Append(FormatMilliseconds(totalTicks));
            builder.Append(" top=");
            AppendTopSection(builder, topA);
            AppendTopSection(builder, topB);
            AppendTopSection(builder, topC);
            AppendCounters(builder);
            return builder.ToString();
        }

        private static string BuildDetailedMessage(int ticks)
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append("[CeleTech Shuttle] PerfSummary V3 UI detail");
            builder.Append(" ticks=").Append(ticks);
            for (int i = 0; i < SectionSampleCounts.Length; i++)
            {
                if (SectionSampleCounts[i] <= 0)
                {
                    continue;
                }

                builder.Append(" | ");
                AppendSectionDetail(builder, i);
            }

            AppendCounters(builder);
            return builder.ToString();
        }

        private static void FindTopSections(
            out int topA,
            out int topB,
            out int topC)
        {
            topA = -1;
            topB = -1;
            topC = -1;

            for (int i = 0; i < SectionTotalTicks.Length; i++)
            {
                if (SectionSampleCounts[i] <= 0)
                {
                    continue;
                }

                if (topA < 0 || SectionTotalTicks[i] > SectionTotalTicks[topA])
                {
                    topC = topB;
                    topB = topA;
                    topA = i;
                    continue;
                }

                if (topB < 0 || SectionTotalTicks[i] > SectionTotalTicks[topB])
                {
                    topC = topB;
                    topB = i;
                    continue;
                }

                if (topC < 0 || SectionTotalTicks[i] > SectionTotalTicks[topC])
                {
                    topC = i;
                }
            }
        }

        private static void AppendTopSection(StringBuilder builder, int index)
        {
            if (index < 0)
            {
                return;
            }

            if (builder[builder.Length - 1] != '=')
            {
                builder.Append(", ");
            }

            builder.Append(GetSectionName((ShuttleUIProfileSection)index));
            builder.Append(":");
            builder.Append(FormatMilliseconds(SectionTotalTicks[index]));
            builder.Append("ms/");
            builder.Append(SectionSampleCounts[index]);
        }

        private static void AppendSectionDetail(StringBuilder builder, int index)
        {
            int count = SectionSampleCounts[index];
            long total = SectionTotalTicks[index];
            long average = count > 0 ? total / count : 0L;
            builder.Append(GetSectionName((ShuttleUIProfileSection)index));
            builder.Append(" count=").Append(count);
            builder.Append(" totalMs=").Append(FormatMilliseconds(total));
            builder.Append(" avgMs=").Append(FormatMilliseconds(average));
            builder.Append(" maxMs=").Append(FormatMilliseconds(SectionMaxTicks[index]));
        }

        private static void AppendCounters(StringBuilder builder)
        {
            bool wroteAny = false;
            for (int i = 0; i < CounterCounts.Length; i++)
            {
                if (CounterCounts[i] <= 0)
                {
                    continue;
                }

                if (!wroteAny)
                {
                    builder.Append(" counters=");
                    wroteAny = true;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(GetCounterName((ShuttleUIProfileCounter)i));
                builder.Append(":");
                builder.Append(CounterCounts[i]);
            }
        }

        private static string FormatMilliseconds(long stopwatchTicks)
        {
            if (stopwatchTicks <= 0L)
            {
                return "0";
            }

            double milliseconds =
                ((double)stopwatchTicks * 1000.0) / Stopwatch.Frequency;
            return milliseconds.ToString("0.###");
        }

        private static void ResetSamples()
        {
            for (int i = 0; i < SectionTotalTicks.Length; i++)
            {
                SectionTotalTicks[i] = 0L;
                SectionMaxTicks[i] = 0L;
                SectionSampleCounts[i] = 0;
            }

            for (int i = 0; i < CounterCounts.Length; i++)
            {
                CounterCounts[i] = 0;
            }
        }

        private static string GetSectionName(ShuttleUIProfileSection section)
        {
            if (section == ShuttleUIProfileSection.HubFillContext)
            {
                return "Hub.FillContext";
            }

            if (section == ShuttleUIProfileSection.ControlModelSourceRead)
            {
                return "Source.ControlModel";
            }

            if (section == ShuttleUIProfileSection.CargoSnapshotSourceRead)
            {
                return "Source.CargoSnapshot";
            }

            if (section == ShuttleUIProfileSection.WeaponBaySourceRead)
            {
                return "Source.WeaponBay";
            }

            if (section == ShuttleUIProfileSection.AutoWorkTableSourceRead)
            {
                return "Source.AutoWorkTable";
            }

            if (section == ShuttleUIProfileSection.SharedMessagePanelRows)
            {
                return "SharedMessage.PanelRows";
            }

            if (section == ShuttleUIProfileSection.PawnPresenceBuild)
            {
                return "Pawns.Presence";
            }

            if (section == ShuttleUIProfileSection.PawnDynamicStatusBuild)
            {
                return "Pawns.DynamicStatus";
            }

            if (section == ShuttleUIProfileSection.ExternalModulesFullRead)
            {
                return "ExternalModules.FullRead";
            }

            if (section == ShuttleUIProfileSection.ExternalModulesBadgeRead)
            {
                return "ExternalModules.BadgeRead";
            }

            if (section == ShuttleUIProfileSection.ExternalModulesPageProjection)
            {
                return "ExternalModules.Projection";
            }

            if (section == ShuttleUIProfileSection.CargoPageProjection)
            {
                return "Cargo.Projection";
            }

            if (section == ShuttleUIProfileSection.CargoProjectionBuild)
            {
                return "Cargo.ProjectionBuild";
            }

            if (section == ShuttleUIProfileSection.CargoVisibleStackBuild)
            {
                return "Cargo.VisibleStackBuild";
            }

            if (section == ShuttleUIProfileSection.CrewPageProjection)
            {
                return "Crew.Projection";
            }

            if (section == ShuttleUIProfileSection.MedicalPageProjection)
            {
                return "Medical.Projection";
            }

            if (section == ShuttleUIProfileSection.CargoBodyDraw)
            {
                return "Cargo.BodyDraw";
            }

            if (section == ShuttleUIProfileSection.CargoStackListDraw)
            {
                return "Cargo.StackListDraw";
            }

            if (section == ShuttleUIProfileSection.CargoLogisticsDraw)
            {
                return "Cargo.LogisticsDraw";
            }

            if (section == ShuttleUIProfileSection.CargoStackDetailDraw)
            {
                return "Cargo.StackDetailDraw";
            }

            if (section == ShuttleUIProfileSection.CargoMessagePanelDraw)
            {
                return "Cargo.MessagePanelDraw";
            }

            if (section == ShuttleUIProfileSection.CrewBodyDraw)
            {
                return "Crew.BodyDraw";
            }

            if (section == ShuttleUIProfileSection.MedicalBodyDraw)
            {
                return "Medical.BodyDraw";
            }

            if (section == ShuttleUIProfileSection.ProcessingProjection)
            {
                return "Processing.Projection";
            }

            if (section == ShuttleUIProfileSection.ProcessingBodyDraw)
            {
                return "Processing.BodyDraw";
            }

            if (section == ShuttleUIProfileSection.ProcessingQueueDraw)
            {
                return "Processing.QueueDraw";
            }

            if (section == ShuttleUIProfileSection.ProcessingRecipeDraw)
            {
                return "Processing.RecipeDraw";
            }

            return "Unknown";
        }

        private static string GetCounterName(ShuttleUIProfileCounter counter)
        {
            if (counter == ShuttleUIProfileCounter.CargoProjectionCacheHit)
            {
                return "CargoProjectionHit";
            }

            if (counter == ShuttleUIProfileCounter.CargoProjectionCacheMiss)
            {
                return "CargoProjectionMiss";
            }

            if (counter == ShuttleUIProfileCounter.CargoVisibleStackCacheHit)
            {
                return "CargoVisibleStackHit";
            }

            if (counter == ShuttleUIProfileCounter.CargoVisibleStackCacheMiss)
            {
                return "CargoVisibleStackMiss";
            }

            return "Unknown";
        }
    }
}
