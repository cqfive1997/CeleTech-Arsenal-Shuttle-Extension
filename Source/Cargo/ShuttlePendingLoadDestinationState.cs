using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal enum ShuttleCargoLoadDestinationKind
    {
        RegularCargo = 0,
        RefrigeratedCargo = 1
    }

    internal sealed class ShuttlePendingLoadDestinationState : IExposable
    {
        private List<ShuttlePendingLoadDestinationRecord> records =
            new List<ShuttlePendingLoadDestinationRecord>();

        internal IReadOnlyList<ShuttlePendingLoadDestinationRecord> Records
        {
            get
            {
                this.EnsureInitialized();
                return this.records;
            }
        }

        internal void EnsureInitialized()
        {
            if (this.records == null)
            {
                this.records = new List<ShuttlePendingLoadDestinationRecord>();
            }

            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttlePendingLoadDestinationRecord record = this.records[i];
                if (record == null || !record.IsUsable)
                {
                    this.records.RemoveAt(i);
                    continue;
                }

                record.EnsureInitialized();
            }
        }

        internal void Clear()
        {
            this.EnsureInitialized();
            this.records.Clear();
        }

        internal void AddRange(IEnumerable<ShuttlePendingLoadDestinationRecord> pendingRecords)
        {
            if (pendingRecords == null)
            {
                return;
            }

            this.EnsureInitialized();
            foreach (ShuttlePendingLoadDestinationRecord record in pendingRecords)
            {
                if (record != null && record.IsUsable)
                {
                    this.records.Add(record);
                }
            }
        }

        internal bool TryFindRefrigeratedDestination(
            Thing thing,
            out ShuttlePendingLoadDestinationRecord record)
        {
            record = null;
            this.EnsureInitialized();
            if (thing == null)
            {
                return false;
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttlePendingLoadDestinationRecord candidate = this.records[i];
                if (candidate != null &&
                    candidate.Kind == ShuttleCargoLoadDestinationKind.RefrigeratedCargo &&
                    candidate.Matches(thing))
                {
                    record = candidate;
                    return true;
                }
            }

            return false;
        }

        internal void Consume(ShuttlePendingLoadDestinationRecord record, int count)
        {
            if (record == null)
            {
                return;
            }

            this.EnsureInitialized();
            record.Consume(count);
            if (!record.IsUsable)
            {
                this.records.Remove(record);
            }
        }

        internal void RemoveForTransferable(TransferableOneWay transferable)
        {
            this.RemoveForTransferable(transferable, int.MaxValue);
        }

        internal void RemoveForTransferable(TransferableOneWay transferable, int count)
        {
            this.EnsureInitialized();
            if (transferable == null || transferable.things == null || count <= 0)
            {
                return;
            }

            int remaining = count;
            for (int i = 0; i < transferable.things.Count; i++)
            {
                if (remaining <= 0)
                {
                    break;
                }

                int removed = this.RemoveForThing(transferable.things[i], remaining);
                remaining -= removed;
            }
        }

        private int RemoveForThing(Thing thing, int count)
        {
            if (thing == null || count <= 0)
            {
                return 0;
            }

            int remaining = count;
            int removed = 0;
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttlePendingLoadDestinationRecord record = this.records[i];
                if (record == null)
                {
                    this.records.RemoveAt(i);
                    continue;
                }

                if (!record.Matches(thing))
                {
                    continue;
                }

                int consume = record.RemainingCount < remaining ? record.RemainingCount : remaining;
                record.Consume(consume);
                removed += consume;
                remaining -= consume;
                if (!record.IsUsable)
                {
                    this.records.RemoveAt(i);
                }

                if (remaining <= 0)
                {
                    break;
                }
            }

            return removed;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.records, "records", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }
    }

    internal sealed class ShuttlePendingLoadDestinationRecord : IExposable
    {
        private int sourceThingIDNumber;
        private string defName;
        private string stuffDefName;
        private string qualityCategory;
        private int hitPoints = -1;
        private int remainingCount;
        private ShuttleCargoLoadDestinationKind kind;
        private string refrigeratedModuleInstanceID;

        public ShuttlePendingLoadDestinationRecord()
        {
        }

        internal ShuttlePendingLoadDestinationRecord(
            Thing sourceThing,
            int count,
            string refrigeratedModuleInstanceID)
        {
            this.sourceThingIDNumber = sourceThing != null ? sourceThing.thingIDNumber : 0;
            this.defName = sourceThing != null && sourceThing.def != null ? sourceThing.def.defName : null;
            this.stuffDefName = sourceThing != null && sourceThing.Stuff != null ? sourceThing.Stuff.defName : null;
            this.qualityCategory = GetQualityCategory(sourceThing);
            this.hitPoints = GetHitPoints(sourceThing);
            this.remainingCount = count > 0 ? count : 0;
            this.kind = ShuttleCargoLoadDestinationKind.RefrigeratedCargo;
            this.refrigeratedModuleInstanceID = refrigeratedModuleInstanceID;
        }

        internal ShuttleCargoLoadDestinationKind Kind
        {
            get
            {
                return this.kind;
            }
        }

        internal string RefrigeratedModuleInstanceID
        {
            get
            {
                return this.refrigeratedModuleInstanceID;
            }
        }

        internal int RemainingCount
        {
            get
            {
                return this.remainingCount;
            }
        }

        internal bool IsUsable
        {
            get
            {
                return this.remainingCount > 0 &&
                    this.kind == ShuttleCargoLoadDestinationKind.RefrigeratedCargo &&
                    !string.IsNullOrEmpty(this.refrigeratedModuleInstanceID) &&
                    !string.IsNullOrEmpty(this.defName);
            }
        }

        internal void EnsureInitialized()
        {
            if (this.remainingCount < 0)
            {
                this.remainingCount = 0;
            }

            if (this.hitPoints < -1)
            {
                this.hitPoints = -1;
            }
        }

        internal bool Matches(Thing thing)
        {
            if (thing == null || thing.def == null || string.IsNullOrEmpty(this.defName))
            {
                return false;
            }

            if (this.sourceThingIDNumber > 0 && thing.thingIDNumber == this.sourceThingIDNumber)
            {
                return true;
            }

            if (thing.def.defName != this.defName)
            {
                return false;
            }

            string thingStuffDefName = thing.Stuff != null ? thing.Stuff.defName : null;
            if (thingStuffDefName != this.stuffDefName)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(this.qualityCategory))
            {
                string thingQualityCategory = GetQualityCategory(thing);
                if (thingQualityCategory != this.qualityCategory)
                {
                    return false;
                }
            }

            if (this.hitPoints >= 0 &&
                thing.def != null &&
                thing.def.useHitPoints &&
                thing.HitPoints != this.hitPoints)
            {
                return false;
            }

            return true;
        }

        internal void Consume(int count)
        {
            if (count <= 0)
            {
                count = 1;
            }

            this.remainingCount -= count;
            if (this.remainingCount < 0)
            {
                this.remainingCount = 0;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.sourceThingIDNumber, "sourceThingIDNumber", 0);
            Scribe_Values.Look(ref this.defName, "defName");
            Scribe_Values.Look(ref this.stuffDefName, "stuffDefName");
            Scribe_Values.Look(ref this.qualityCategory, "qualityCategory");
            Scribe_Values.Look(ref this.hitPoints, "hitPoints", -1);
            Scribe_Values.Look(ref this.remainingCount, "remainingCount", 0);
            Scribe_Values.Look(ref this.kind, "kind", ShuttleCargoLoadDestinationKind.RegularCargo);
            Scribe_Values.Look(ref this.refrigeratedModuleInstanceID, "refrigeratedModuleInstanceID");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        private static string GetQualityCategory(Thing thing)
        {
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null)
            {
                return null;
            }

            CompQuality qualityComp = thingWithComps.GetComp<CompQuality>();
            return qualityComp != null ? qualityComp.Quality.ToString() : null;
        }

        private static int GetHitPoints(Thing thing)
        {
            return thing != null &&
                thing.def != null &&
                thing.def.useHitPoints
                ? thing.HitPoints
                : -1;
        }
    }
}
