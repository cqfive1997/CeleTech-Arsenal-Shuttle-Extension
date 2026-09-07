using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    internal sealed class ShuttleModuleRemovalRecord : IExposable
    {
        private string moduleInstanceID;
        private string segmentInstanceID;
        private string slotID;
        private string moduleDefName;
        private ShuttleAssemblyRemovalTargetKind targetKind = ShuttleAssemblyRemovalTargetKind.Module;
        private int startedTick;
        private int workTicksRequired;
        private int workTicksDone;
        private int lastWorkTick = -1;
        private ShuttleModuleRemovalStatus status = ShuttleModuleRemovalStatus.WaitingForWorker;
        private string lastFailureReason;

        public ShuttleModuleRemovalRecord()
        {
        }

        internal ShuttleModuleRemovalRecord(
            string moduleInstanceID,
            string segmentInstanceID,
            string slotID,
            string moduleDefName,
            int startedTick,
            int workTicksRequired)
        {
            this.targetKind = ShuttleAssemblyRemovalTargetKind.Module;
            this.moduleInstanceID = moduleInstanceID;
            this.segmentInstanceID = segmentInstanceID;
            this.slotID = slotID;
            this.moduleDefName = moduleDefName;
            this.startedTick = startedTick;
            this.workTicksRequired = workTicksRequired > 0 ? workTicksRequired : 1;
            this.workTicksDone = 0;
            this.lastWorkTick = -1;
            this.status = ShuttleModuleRemovalStatus.WaitingForWorker;
        }

        internal static ShuttleModuleRemovalRecord ForSegment(
            string segmentInstanceID,
            string segmentSlotID,
            string segmentDefName,
            int startedTick,
            int workTicksRequired)
        {
            ShuttleModuleRemovalRecord record = new ShuttleModuleRemovalRecord();
            record.targetKind = ShuttleAssemblyRemovalTargetKind.Segment;
            record.moduleInstanceID = null;
            record.segmentInstanceID = segmentInstanceID;
            record.slotID = segmentSlotID;
            record.moduleDefName = segmentDefName;
            record.startedTick = startedTick;
            record.workTicksRequired = workTicksRequired > 0 ? workTicksRequired : 1;
            record.workTicksDone = 0;
            record.lastWorkTick = -1;
            record.status = ShuttleModuleRemovalStatus.WaitingForWorker;
            return record;
        }

        internal ShuttleAssemblyRemovalTargetKind TargetKind { get { return this.targetKind; } }
        internal string ModuleInstanceID { get { return this.moduleInstanceID; } }
        internal string SegmentInstanceID { get { return this.segmentInstanceID; } }
        internal string SlotID { get { return this.slotID; } }
        internal string ModuleDefName { get { return this.moduleDefName; } }
        internal string SegmentDefName { get { return this.moduleDefName; } }
        internal string TargetInstanceID
        {
            get
            {
                return this.targetKind == ShuttleAssemblyRemovalTargetKind.Segment
                    ? this.segmentInstanceID
                    : this.moduleInstanceID;
            }
        }
        internal int StartedTick { get { return this.startedTick; } }
        internal int WorkTicksRequired { get { return this.workTicksRequired; } }
        internal int WorkTicksDone { get { return this.workTicksDone; } }
        internal int LastWorkTick { get { return this.lastWorkTick; } }
        internal ShuttleModuleRemovalStatus Status { get { return this.status; } }
        internal string LastFailureReason { get { return this.lastFailureReason; } }

        internal float Progress01
        {
            get
            {
                return this.workTicksRequired > 0
                    ? UnityEngine.Mathf.Clamp01(this.workTicksDone / (float)this.workTicksRequired)
                    : 1f;
            }
        }

        internal bool IsActive
        {
            get
            {
                if (this.status == ShuttleModuleRemovalStatus.None)
                {
                    return false;
                }

                if (this.targetKind == ShuttleAssemblyRemovalTargetKind.Segment)
                {
                    return !string.IsNullOrEmpty(this.segmentInstanceID) &&
                        !string.IsNullOrEmpty(this.slotID);
                }

                return !string.IsNullOrEmpty(this.moduleInstanceID);
            }
        }

        internal void AddWork(int ticks, int ticksGame)
        {
            if (this.status == ShuttleModuleRemovalStatus.RefundPending)
            {
                return;
            }

            this.status = ShuttleModuleRemovalStatus.Working;
            this.lastFailureReason = null;
            this.lastWorkTick = ticksGame;
            this.workTicksDone = UnityEngine.Mathf.Clamp(
                this.workTicksDone + UnityEngine.Mathf.Max(0, ticks),
                0,
                UnityEngine.Mathf.Max(1, this.workTicksRequired));
        }

        internal void MarkWaitingForWorker()
        {
            if (this.status == ShuttleModuleRemovalStatus.RefundPending ||
                this.status == ShuttleModuleRemovalStatus.Blocked)
            {
                return;
            }

            this.status = ShuttleModuleRemovalStatus.WaitingForWorker;
        }

        internal void MarkBlocked(string reason)
        {
            this.status = ShuttleModuleRemovalStatus.Blocked;
            this.lastFailureReason = reason;
        }

        internal void MarkRefundPending(string reason)
        {
            this.status = ShuttleModuleRemovalStatus.RefundPending;
            this.lastFailureReason = reason;
        }

        internal void ClearFailure()
        {
            this.lastFailureReason = null;
            if (this.status == ShuttleModuleRemovalStatus.Blocked)
            {
                this.status = ShuttleModuleRemovalStatus.WaitingForWorker;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.targetKind, "targetKind", ShuttleAssemblyRemovalTargetKind.Module);
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.segmentInstanceID, "segmentInstanceID");
            Scribe_Values.Look(ref this.slotID, "slotID");
            Scribe_Values.Look(ref this.moduleDefName, "moduleDefName");
            Scribe_Values.Look(ref this.startedTick, "startedTick", -1);
            Scribe_Values.Look(ref this.workTicksRequired, "workTicksRequired", 1);
            Scribe_Values.Look(ref this.workTicksDone, "workTicksDone", 0);
            Scribe_Values.Look(ref this.lastWorkTick, "lastWorkTick", -1);
            Scribe_Values.Look(ref this.status, "status", ShuttleModuleRemovalStatus.WaitingForWorker);
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.targetKind == ShuttleAssemblyRemovalTargetKind.None &&
                    !string.IsNullOrEmpty(this.moduleInstanceID))
                {
                    this.targetKind = ShuttleAssemblyRemovalTargetKind.Module;
                }

                if (this.workTicksRequired <= 0)
                {
                    this.workTicksRequired = 1;
                }

                this.workTicksDone = UnityEngine.Mathf.Clamp(this.workTicksDone, 0, this.workTicksRequired);
            }
        }
    }
}
