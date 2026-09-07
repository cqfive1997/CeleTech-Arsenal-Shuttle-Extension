using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    public sealed class ShuttleModuleRemovalState : IExposable, IThingHolder
    {
        private ShuttleModuleRemovalRecord activeRecord;
        private ThingOwner<Thing> pendingRefunds;
        private ThingOwnerHolderRoot pendingRefundsRoot;
        private string lastFailureReason;

        public ShuttleModuleRemovalState()
        {
            this.pendingRefunds = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.EnsureHolderRoots();
        }

        public IThingHolder ParentHolder { get { return null; } }

        internal ShuttleModuleRemovalRecord ActiveRecord { get { return this.activeRecord; } }

        internal bool HasActiveRemoval
        {
            get
            {
                return this.activeRecord != null && this.activeRecord.IsActive;
            }
        }

        internal string LastFailureReason { get { return this.lastFailureReason; } }

        internal ThingOwner<Thing> PendingRefunds
        {
            get
            {
                this.EnsureInitialized();
                return this.pendingRefunds;
            }
        }

        internal bool HasPendingRefunds
        {
            get
            {
                this.EnsureInitialized();
                return this.pendingRefunds != null && this.pendingRefunds.Count > 0;
            }
        }

        internal bool IsRemovingModule(string moduleInstanceID)
        {
            return this.activeRecord != null &&
                this.activeRecord.TargetKind == ShuttleAssemblyRemovalTargetKind.Module &&
                !string.IsNullOrEmpty(moduleInstanceID) &&
                this.activeRecord.ModuleInstanceID == moduleInstanceID;
        }

        internal bool IsRemovingSegment(string segmentSlotID)
        {
            return this.activeRecord != null &&
                this.activeRecord.TargetKind == ShuttleAssemblyRemovalTargetKind.Segment &&
                !string.IsNullOrEmpty(segmentSlotID) &&
                this.activeRecord.SlotID == segmentSlotID;
        }

        internal bool TryGetRecordForModule(string moduleInstanceID, out ShuttleModuleRemovalRecord record)
        {
            record = null;
            if (this.activeRecord == null ||
                this.activeRecord.TargetKind != ShuttleAssemblyRemovalTargetKind.Module ||
                string.IsNullOrEmpty(moduleInstanceID) ||
                this.activeRecord.ModuleInstanceID != moduleInstanceID)
            {
                return false;
            }

            record = this.activeRecord;
            return true;
        }

        internal bool TryGetRecordForSegmentSlot(string segmentSlotID, out ShuttleModuleRemovalRecord record)
        {
            record = null;
            if (this.activeRecord == null ||
                this.activeRecord.TargetKind != ShuttleAssemblyRemovalTargetKind.Segment ||
                string.IsNullOrEmpty(segmentSlotID) ||
                this.activeRecord.SlotID != segmentSlotID)
            {
                return false;
            }

            record = this.activeRecord;
            return true;
        }

        internal void BeginRemoval(ShuttleModuleRemovalRecord record)
        {
            this.activeRecord = record;
            this.lastFailureReason = null;
        }

        internal void ClearRemoval()
        {
            this.activeRecord = null;
            this.lastFailureReason = null;
            this.EnsureInitialized();
            if (this.pendingRefunds != null && this.pendingRefunds.Count > 0)
            {
                while (this.pendingRefunds.Count > 0)
                {
                    Thing thing = this.pendingRefunds[0];
                    this.pendingRefunds.Remove(thing);
                    if (thing != null && !thing.Destroyed)
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        internal void RecordFailure(string reason)
        {
            this.lastFailureReason = reason;
            if (this.activeRecord != null)
            {
                this.activeRecord.MarkBlocked(reason);
            }
        }

        public void EnsureInitialized()
        {
            if (this.pendingRefunds == null)
            {
                this.pendingRefunds = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            this.EnsureHolderRoots();
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            this.EnsureHolderRoots();
            outChildren.Add(this.pendingRefundsRoot);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref this.activeRecord, "activeRecord");
            Scribe_Deep.Look(ref this.pendingRefunds, "pendingRefunds", new object[] { this });
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        private void EnsureHolderRoots()
        {
            if (this.pendingRefundsRoot == null)
            {
                this.pendingRefundsRoot = new ThingOwnerHolderRoot(
                    this,
                    delegate
                    {
                        this.EnsureInitialized();
                        return this.pendingRefunds;
                    },
                    "module removal pending refunds");
            }
        }
    }
}
