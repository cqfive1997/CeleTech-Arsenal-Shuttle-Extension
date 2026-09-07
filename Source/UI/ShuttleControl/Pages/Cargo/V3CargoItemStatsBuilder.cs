using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoItemStatsBuilder
    {
        private readonly Dictionary<string, AggregateRecord> aggregateStats =
            new Dictionary<string, AggregateRecord>();

        private readonly Dictionary<V3CargoCategory, int> categoryThingCounts =
            new Dictionary<V3CargoCategory, int>();

        internal void Reset()
        {
            this.aggregateStats.Clear();
            this.categoryThingCounts.Clear();
        }

        internal void AddStackToStats(V3CargoStackCardModel stack)
        {
            if (stack == null || stack.Category == V3CargoCategory.None)
            {
                return;
            }

            int existingCategoryCount;
            this.categoryThingCounts.TryGetValue(stack.Category, out existingCategoryCount);
            this.categoryThingCounts[stack.Category] =
                existingCategoryCount + Mathf.Max(0, stack.StackCount);

            string key = ((int)stack.Category).ToString() + "|" +
                (!string.IsNullOrEmpty(stack.DefName) ? stack.DefName : stack.Label);
            AggregateRecord record;
            if (!this.aggregateStats.TryGetValue(key, out record))
            {
                record = new AggregateRecord();
                record.Category = stack.Category;
                record.Label = stack.Label;
                record.DefName = stack.DefName;
                record.DisplayThing = stack.DisplayThing;
                this.aggregateStats[key] = record;
            }

            record.StackCount++;
            record.ThingCount += Mathf.Max(0, stack.StackCount);
            if (record.DisplayThing == null && stack.DisplayThing != null)
            {
                record.DisplayThing = stack.DisplayThing;
            }
        }

        internal int GetCategoryThingCount(V3CargoCategory category)
        {
            int count;
            this.categoryThingCounts.TryGetValue(category, out count);
            return count;
        }

        internal void BuildStatLines(V3CargoPageReadModel pageModel)
        {
            if (pageModel == null)
            {
                return;
            }

            foreach (KeyValuePair<string, AggregateRecord> pair in this.aggregateStats)
            {
                AggregateRecord aggregate = pair.Value;
                if (aggregate == null)
                {
                    continue;
                }

                V3CargoStatLineModel line = new V3CargoStatLineModel();
                line.Category = aggregate.Category;
                line.Label = aggregate.Label;
                line.DefName = aggregate.DefName;
                line.DisplayThing = aggregate.DisplayThing;
                line.StackCount = aggregate.StackCount;
                line.ThingCount = aggregate.ThingCount;
                pageModel.ItemStats.Add(line);
            }

            pageModel.ItemStats.Sort(CompareStatLines);
        }

        private static int CompareStatLines(
            V3CargoStatLineModel left,
            V3CargoStatLineModel right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int countCompare = right.ThingCount.CompareTo(left.ThingCount);
            return countCompare != 0
                ? countCompare
                : string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class AggregateRecord
        {
            internal V3CargoCategory Category;
            internal string Label;
            internal string DefName;
            internal int StackCount;
            internal int ThingCount;
            internal Thing DisplayThing;
        }
    }
}
