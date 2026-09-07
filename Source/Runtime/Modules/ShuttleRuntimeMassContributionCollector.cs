using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal sealed class ShuttleRuntimeMassContributionCollector
    {
        private const float MaxExternalMassContributionKg = 1E+9f;

        private readonly List<ShuttleRuntimeMassContributionRecord> records =
            new List<ShuttleRuntimeMassContributionRecord>();

        internal void Add(
            string runtimeSystemKey,
            string moduleInstanceID,
            string moduleDefName,
            float massKg,
            string labelKey,
            string debugLabel,
            int ticksGame)
        {
            massKg = ClampMassKg(massKg);
            if (massKg <= 0f)
            {
                return;
            }

            ShuttleRuntimeMassContributionRecord record =
                new ShuttleRuntimeMassContributionRecord();
            record.RuntimeSystemKey = runtimeSystemKey;
            record.ModuleInstanceID = moduleInstanceID;
            record.ModuleDefName = moduleDefName;
            record.MassKg = massKg;
            record.LabelKey = labelKey;
            record.Label = TranslateIfAvailable(labelKey);
            record.DebugLabel = debugLabel;
            record.Tick = ticksGame;
            this.records.Add(record);
        }

        internal ShuttleRuntimeMassContributionSnapshot BuildSnapshot()
        {
            ShuttleRuntimeMassContributionSnapshot snapshot =
                new ShuttleRuntimeMassContributionSnapshot();
            double totalMassKg = 0d;
            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttleRuntimeMassContributionRecord record = this.records[i];
                if (record == null)
                {
                    continue;
                }

                float massKg = ClampMassKg(record.MassKg);
                if (massKg <= 0f)
                {
                    continue;
                }

                record.MassKg = massKg;
                snapshot.Records.Add(record);
                totalMassKg = SaturatingAdd(totalMassKg, massKg, MaxExternalMassContributionKg);
            }

            snapshot.TotalMassKg = (float)totalMassKg;
            snapshot.Revision = ComputeRevision(snapshot.Records, snapshot.TotalMassKg);
            return snapshot;
        }

        private static float ClampMassKg(float massKg)
        {
            if (massKg <= 0f || float.IsNaN(massKg) || float.IsInfinity(massKg))
            {
                return 0f;
            }

            return massKg > MaxExternalMassContributionKg ? MaxExternalMassContributionKg : massKg;
        }

        private static double SaturatingAdd(double left, double right, double max)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || left < 0d)
            {
                left = 0d;
            }

            if (double.IsNaN(right) || double.IsInfinity(right) || right < 0d)
            {
                right = 0d;
            }

            if (left >= max || right >= max || max - left < right)
            {
                return max;
            }

            return left + right;
        }

        private static int ComputeRevision(
            IReadOnlyList<ShuttleRuntimeMassContributionRecord> records,
            float totalMassKg)
        {
            unchecked
            {
                int hash = 29;
                hash = (hash * 37) + totalMassKg.GetHashCode();
                for (int i = 0; records != null && i < records.Count; i++)
                {
                    ShuttleRuntimeMassContributionRecord record = records[i];
                    if (record == null)
                    {
                        continue;
                    }

                    hash = (hash * 37) + StableStringHash(record.RuntimeSystemKey);
                    hash = (hash * 37) + StableStringHash(record.ModuleInstanceID);
                    hash = (hash * 37) + StableStringHash(record.LabelKey);
                    hash = (hash * 37) + record.MassKg.GetHashCode();
                }

                return hash;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        private static string TranslateIfAvailable(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            string normalized = key.Trim();
            return Translator.CanTranslate(normalized)
                ? normalized.Translate().ToString()
                : null;
        }
    }

    internal sealed class ShuttleRuntimeMassContributionSnapshot
    {
        internal float TotalMassKg;
        internal int Revision;
        internal List<ShuttleRuntimeMassContributionRecord> Records =
            new List<ShuttleRuntimeMassContributionRecord>();
    }

    internal sealed class ShuttleRuntimeMassContributionRecord
    {
        internal string RuntimeSystemKey;
        internal string ModuleInstanceID;
        internal string ModuleDefName;
        internal float MassKg;
        internal string LabelKey;
        internal string Label;
        internal string DebugLabel;
        internal int Tick;
    }
}
