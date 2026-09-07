using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal sealed class ShuttleHullRepairMaterialLedger : IExposable
    {
        private List<ShuttleHullRepairMaterialRecord> records =
            new List<ShuttleHullRepairMaterialRecord>();

        internal void EnsureInitialized()
        {
            if (this.records == null)
            {
                this.records = new List<ShuttleHullRepairMaterialRecord>();
            }

            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttleHullRepairMaterialRecord record = this.records[i];
                if (record == null || record.DeliveredCount <= 0 || record.Thing == null)
                {
                    this.records.RemoveAt(i);
                }
            }
        }

        internal bool Contains(Thing thing)
        {
            if (thing == null || this.records == null)
            {
                return false;
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttleHullRepairMaterialRecord record = this.records[i];
                if (record != null && record.Thing == thing && record.DeliveredCount > 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal void RecordDelivery(Thing thing, int count)
        {
            if (thing == null || count <= 0)
            {
                return;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttleHullRepairMaterialRecord record = this.records[i];
                if (record != null && record.Thing == thing)
                {
                    record.AddDeliveredCount(count);
                    return;
                }
            }

            this.records.Add(new ShuttleHullRepairMaterialRecord(thing, count));
        }

        internal int CountAvailable(ThingDef thingDef, Map map)
        {
            if (thingDef == null || map == null)
            {
                return 0;
            }

            this.EnsureInitialized();
            int total = 0;
            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttleHullRepairMaterialRecord record = this.records[i];
                int available = this.GetAvailableCount(record, thingDef, map);
                total = SaturatingAdd(total, available);
            }

            return total;
        }

        internal bool CanSatisfy(IReadOnlyList<ThingDefCountClass> requirements, Map map)
        {
            if (requirements == null)
            {
                return true;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                ThingDefCountClass requirement = requirements[i];
                if (requirement == null || requirement.thingDef == null || requirement.count <= 0)
                {
                    continue;
                }

                if (this.CountAvailable(requirement.thingDef, map) < requirement.count)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool TryConsume(
            IReadOnlyList<ThingDefCountClass> requirements,
            Map map)
        {
            this.EnsureInitialized();
            if (!this.CanSatisfy(requirements, map))
            {
                return false;
            }

            List<ShuttleHullRepairMaterialConsumption> consumptionPlan =
                this.BuildConsumptionPlan(requirements, map);
            if (consumptionPlan == null)
            {
                return false;
            }

            for (int i = 0; i < consumptionPlan.Count; i++)
            {
                ShuttleHullRepairMaterialConsumption entry = consumptionPlan[i];
                if (entry == null ||
                    entry.Record == null ||
                    entry.Thing == null ||
                    entry.Thing.Destroyed ||
                    entry.Count <= 0 ||
                    entry.Thing.stackCount < entry.Count)
                {
                    return false;
                }
            }

            for (int i = 0; i < consumptionPlan.Count; i++)
            {
                ShuttleHullRepairMaterialConsumption entry = consumptionPlan[i];
                if (entry.Thing.stackCount > entry.Count)
                {
                    Thing split = entry.Thing.SplitOff(entry.Count);
                    if (split == null || split.Destroyed)
                    {
                        return false;
                    }

                    split.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    entry.Thing.Destroy(DestroyMode.Vanish);
                }

                entry.Record.Consume(entry.Count);
            }

            this.RemoveExhaustedRecords();
            return true;
        }

        internal void ReleaseForNormalHauling()
        {
            this.EnsureInitialized();
            this.records.Clear();
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.records, "records", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        private List<ShuttleHullRepairMaterialConsumption> BuildConsumptionPlan(
            IReadOnlyList<ThingDefCountClass> requirements,
            Map map)
        {
            List<ShuttleHullRepairMaterialConsumption> result =
                new List<ShuttleHullRepairMaterialConsumption>();
            if (requirements == null)
            {
                return result;
            }

            for (int requirementIndex = 0; requirementIndex < requirements.Count; requirementIndex++)
            {
                ThingDefCountClass requirement = requirements[requirementIndex];
                if (requirement == null || requirement.thingDef == null || requirement.count <= 0)
                {
                    continue;
                }

                int remaining = requirement.count;
                for (int recordIndex = 0; recordIndex < this.records.Count && remaining > 0; recordIndex++)
                {
                    ShuttleHullRepairMaterialRecord record = this.records[recordIndex];
                    int available = this.GetAvailableCount(record, requirement.thingDef, map);
                    int takeCount = Math.Min(remaining, available);
                    if (takeCount <= 0)
                    {
                        continue;
                    }

                    result.Add(new ShuttleHullRepairMaterialConsumption(
                        record,
                        record.Thing,
                        takeCount));
                    remaining -= takeCount;
                }

                if (remaining > 0)
                {
                    return null;
                }
            }

            return result;
        }

        private int GetAvailableCount(
            ShuttleHullRepairMaterialRecord record,
            ThingDef thingDef,
            Map map)
        {
            Thing thing = record != null ? record.Thing : null;
            if (thing == null ||
                thing.Destroyed ||
                !thing.Spawned ||
                thing.Map != map ||
                thing.def != thingDef ||
                thing.stackCount <= 0 ||
                record.DeliveredCount <= 0)
            {
                return 0;
            }

            return Math.Min(record.DeliveredCount, thing.stackCount);
        }

        private void RemoveExhaustedRecords()
        {
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttleHullRepairMaterialRecord record = this.records[i];
                if (record == null ||
                    record.DeliveredCount <= 0 ||
                    record.Thing == null ||
                    record.Thing.Destroyed)
                {
                    this.records.RemoveAt(i);
                }
            }
        }

        private static int SaturatingAdd(int left, int right)
        {
            if (right <= 0)
            {
                return left;
            }

            return int.MaxValue - left < right ? int.MaxValue : left + right;
        }
    }

    internal sealed class ShuttleHullRepairMaterialRecord : IExposable
    {
        private Thing thing;
        private int deliveredCount;

        public ShuttleHullRepairMaterialRecord()
        {
        }

        internal ShuttleHullRepairMaterialRecord(Thing thing, int deliveredCount)
        {
            this.thing = thing;
            this.deliveredCount = deliveredCount > 0 ? deliveredCount : 0;
        }

        internal Thing Thing
        {
            get { return this.thing; }
        }

        internal int DeliveredCount
        {
            get { return this.deliveredCount; }
        }

        internal void AddDeliveredCount(int count)
        {
            if (count <= 0)
            {
                return;
            }

            this.deliveredCount = int.MaxValue - this.deliveredCount < count
                ? int.MaxValue
                : this.deliveredCount + count;
        }

        internal void Consume(int count)
        {
            if (count <= 0)
            {
                return;
            }

            this.deliveredCount = Math.Max(0, this.deliveredCount - count);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.thing, "thing");
            Scribe_Values.Look(ref this.deliveredCount, "deliveredCount", 0);
        }
    }

    internal sealed class ShuttleHullRepairMaterialConsumption
    {
        internal ShuttleHullRepairMaterialConsumption(
            ShuttleHullRepairMaterialRecord record,
            Thing thing,
            int count)
        {
            this.Record = record;
            this.Thing = thing;
            this.Count = count;
        }

        internal ShuttleHullRepairMaterialRecord Record { get; private set; }
        internal Thing Thing { get; private set; }
        internal int Count { get; private set; }
    }
}
