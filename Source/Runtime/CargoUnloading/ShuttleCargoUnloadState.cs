using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading
{
    public sealed class ShuttleCargoUnloadState : IExposable
    {
        private List<ShuttleCargoUnloadRecord> pendingRecords =
            new List<ShuttleCargoUnloadRecord>();
        private bool active;
        private int totalStackCount;
        private int totalThingCount;
        private int completedStackCount;
        private int completedThingCount;
        private int skippedStackCount;
        private int skippedThingCount;
        private int startedTick = -1;
        private int nextProcessTick = -1;
        private int lastCompletedTick = -1;
        private string lastFailureReason;
        private int revision;

        public bool IsActive
        {
            get { return this.active && this.pendingRecords.Count > 0; }
        }

        public int PendingStackCount
        {
            get { return this.pendingRecords.Count; }
        }

        public int PendingThingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < this.pendingRecords.Count; i++)
                {
                    ShuttleCargoUnloadRecord record = this.pendingRecords[i];
                    if (record != null && record.Count > 0)
                    {
                        count += record.Count;
                    }
                }

                return count;
            }
        }

        public int TotalStackCount { get { return this.totalStackCount; } }
        public int TotalThingCount { get { return this.totalThingCount; } }
        public int CompletedStackCount { get { return this.completedStackCount; } }
        public int CompletedThingCount { get { return this.completedThingCount; } }
        public int SkippedStackCount { get { return this.skippedStackCount; } }
        public int SkippedThingCount { get { return this.skippedThingCount; } }
        public int StartedTick { get { return this.startedTick; } }
        public int NextProcessTick { get { return this.nextProcessTick; } }
        public int LastCompletedTick { get { return this.lastCompletedTick; } }
        public string LastFailureReason { get { return this.lastFailureReason; } }
        public int Revision { get { return this.revision; } }

        public void EnsureInitialized()
        {
            if (this.pendingRecords == null)
            {
                this.pendingRecords = new List<ShuttleCargoUnloadRecord>();
            }
        }

        internal bool Start(
            List<ShuttleCargoUnloadRecord> records,
            int ticksGame)
        {
            this.EnsureInitialized();
            if (this.IsActive || records == null || records.Count == 0)
            {
                return false;
            }

            this.pendingRecords.Clear();
            this.totalStackCount = 0;
            this.totalThingCount = 0;
            for (int i = 0; i < records.Count; i++)
            {
                ShuttleCargoUnloadRecord record = records[i];
                if (record == null || !record.IsValid)
                {
                    continue;
                }

                this.pendingRecords.Add(record.Copy());
                this.totalStackCount++;
                this.totalThingCount += record.Count;
            }

            if (this.pendingRecords.Count == 0)
            {
                return false;
            }

            this.active = true;
            this.completedStackCount = 0;
            this.completedThingCount = 0;
            this.skippedStackCount = 0;
            this.skippedThingCount = 0;
            this.startedTick = ticksGame;
            this.nextProcessTick = ticksGame;
            this.lastCompletedTick = -1;
            this.lastFailureReason = null;
            this.revision++;
            return true;
        }

        internal ShuttleCargoUnloadRecord PeekNext()
        {
            this.EnsureInitialized();
            return this.pendingRecords.Count > 0
                ? this.pendingRecords[this.pendingRecords.Count - 1]
                : null;
        }

        internal void CompleteNext(int unloadedCount)
        {
            ShuttleCargoUnloadRecord record = this.RemoveNext();
            if (record == null)
            {
                return;
            }

            this.completedStackCount++;
            this.completedThingCount += unloadedCount > 0
                ? unloadedCount
                : record.Count;
            this.revision++;
        }

        internal void SkipNext(string failureReason)
        {
            ShuttleCargoUnloadRecord record = this.RemoveNext();
            if (record == null)
            {
                return;
            }

            this.skippedStackCount++;
            this.skippedThingCount += record.Count;
            this.lastFailureReason = failureReason;
            this.revision++;
        }

        internal void ScheduleNext(int nextTick)
        {
            this.nextProcessTick = nextTick;
        }

        internal bool CompleteIfFinished(int ticksGame)
        {
            this.EnsureInitialized();
            if (!this.active || this.pendingRecords.Count > 0)
            {
                return false;
            }

            this.active = false;
            this.nextProcessTick = -1;
            this.lastCompletedTick = ticksGame;
            this.revision++;
            return true;
        }

        internal bool Cancel(int ticksGame)
        {
            this.EnsureInitialized();
            if (!this.active && this.pendingRecords.Count == 0)
            {
                return false;
            }

            this.pendingRecords.Clear();
            this.active = false;
            this.nextProcessTick = -1;
            this.lastCompletedTick = ticksGame;
            this.lastFailureReason = null;
            this.revision++;
            return true;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref this.pendingRecords,
                "pendingRecords",
                LookMode.Deep);
            Scribe_Values.Look(ref this.active, "active", false);
            Scribe_Values.Look(ref this.totalStackCount, "totalStackCount", 0);
            Scribe_Values.Look(ref this.totalThingCount, "totalThingCount", 0);
            Scribe_Values.Look(ref this.completedStackCount, "completedStackCount", 0);
            Scribe_Values.Look(ref this.completedThingCount, "completedThingCount", 0);
            Scribe_Values.Look(ref this.skippedStackCount, "skippedStackCount", 0);
            Scribe_Values.Look(ref this.skippedThingCount, "skippedThingCount", 0);
            Scribe_Values.Look(ref this.startedTick, "startedTick", -1);
            Scribe_Values.Look(ref this.nextProcessTick, "nextProcessTick", -1);
            Scribe_Values.Look(ref this.lastCompletedTick, "lastCompletedTick", -1);
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");
            Scribe_Values.Look(ref this.revision, "revision", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.NormalizeAfterLoad();
            }
        }

        private ShuttleCargoUnloadRecord RemoveNext()
        {
            this.EnsureInitialized();
            if (this.pendingRecords.Count == 0)
            {
                return null;
            }

            int lastIndex = this.pendingRecords.Count - 1;
            ShuttleCargoUnloadRecord record = this.pendingRecords[lastIndex];
            this.pendingRecords.RemoveAt(lastIndex);
            return record;
        }

        private void NormalizeAfterLoad()
        {
            this.EnsureInitialized();
            for (int i = this.pendingRecords.Count - 1; i >= 0; i--)
            {
                ShuttleCargoUnloadRecord record = this.pendingRecords[i];
                if (record == null || !record.IsValid)
                {
                    this.pendingRecords.RemoveAt(i);
                }
            }

            this.totalStackCount = this.ClampNonNegative(this.totalStackCount);
            this.totalThingCount = this.ClampNonNegative(this.totalThingCount);
            this.completedStackCount = this.ClampNonNegative(this.completedStackCount);
            this.completedThingCount = this.ClampNonNegative(this.completedThingCount);
            this.skippedStackCount = this.ClampNonNegative(this.skippedStackCount);
            this.skippedThingCount = this.ClampNonNegative(this.skippedThingCount);
            if (this.pendingRecords.Count == 0)
            {
                this.active = false;
                this.nextProcessTick = -1;
            }
        }

        private int ClampNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
