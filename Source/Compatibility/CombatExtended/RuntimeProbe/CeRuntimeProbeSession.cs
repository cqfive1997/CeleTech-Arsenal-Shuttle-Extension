using System;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    public sealed class CeRuntimeProbeSession : IExposable
    {
        private const int MaxTrackerTicks = 2000;

        private ThingWithComps gun;
        private Thing host;
        private Thing targetThing;
        private IntVec3 targetCell = IntVec3.Invalid;
        private bool targetHasThing;
        private bool active;
        private bool castAttempted;
        private bool castAccepted;
        private bool callbackObserved;
        private int trackerTickCount;
        private int completionCallbackCount;
        private int initialLoadedCount;
        private bool useAmmoOwnerAdapter;
        private bool useBurstFire;
        private bool holdBurstForSaveLoad;
        private bool burstResumeAuthorized;
        private CeRuntimeProbeStateSnapshot savedSnapshot =
            new CeRuntimeProbeStateSnapshot();

        private CeRuntimeProbeAmmoOwner ammoOwner;
        private bool postLoadRebindPending;
        private int postLoadRebindAttemptCount;
        private bool postLoadReportPending;
        private bool completionReportPending;

        public CeRuntimeProbeSession()
        {
        }

        internal CeRuntimeProbeSession(
            ThingWithComps gun,
            Thing host,
            LocalTargetInfo target,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad)
        {
            this.gun = gun;
            this.host = host;
            this.targetHasThing = target.HasThing;
            this.targetThing = target.Thing;
            this.targetCell = target.Cell;
            this.useAmmoOwnerAdapter = useAmmoOwnerAdapter;
            this.useBurstFire = useBurstFire;
            this.holdBurstForSaveLoad = holdBurstForSaveLoad;
        }

        internal ThingWithComps Gun
        {
            get { return this.gun; }
        }

        internal Thing Host
        {
            get { return this.host; }
        }

        internal bool Active
        {
            get { return this.active; }
            set { this.active = value; }
        }

        internal bool CastAttempted
        {
            get { return this.castAttempted; }
            set { this.castAttempted = value; }
        }

        internal bool CastAccepted
        {
            get { return this.castAccepted; }
            set { this.castAccepted = value; }
        }

        internal int TrackerTickCount
        {
            get { return this.trackerTickCount; }
            set { this.trackerTickCount = value; }
        }

        internal int CompletionCallbackCount
        {
            get { return this.completionCallbackCount; }
            set { this.completionCallbackCount = value; }
        }

        internal int InitialLoadedCount
        {
            get { return this.initialLoadedCount; }
            set { this.initialLoadedCount = value; }
        }

        internal bool UseAmmoOwnerAdapter
        {
            get { return this.useAmmoOwnerAdapter; }
        }

        internal bool UseBurstFire
        {
            get { return this.useBurstFire; }
        }

        internal bool HoldBurstForSaveLoad
        {
            get { return this.holdBurstForSaveLoad; }
        }

        internal bool BurstResumeAuthorized
        {
            get { return this.burstResumeAuthorized; }
            set { this.burstResumeAuthorized = value; }
        }

        internal CeRuntimeProbeAmmoOwner AmmoOwner
        {
            get { return this.ammoOwner; }
            set { this.ammoOwner = value; }
        }

        internal bool CallbackObserved
        {
            set { this.callbackObserved = value; }
        }

        internal bool PostLoadReportPending
        {
            get { return this.postLoadReportPending; }
            set { this.postLoadReportPending = value; }
        }

        internal bool PostLoadRebindPending
        {
            get { return this.postLoadRebindPending; }
            set { this.postLoadRebindPending = value; }
        }

        internal int PostLoadRebindAttemptCount
        {
            get { return this.postLoadRebindAttemptCount; }
            set { this.postLoadRebindAttemptCount = value; }
        }

        internal bool CompletionReportPending
        {
            get { return this.completionReportPending; }
            set { this.completionReportPending = value; }
        }

        internal string TargetDescription
        {
            get
            {
                if (this.targetHasThing && this.targetThing != null)
                {
                    return this.targetThing.LabelShortCap;
                }

                return this.targetCell.IsValid ? this.targetCell.ToString() : "invalid";
            }
        }

        internal bool PersistedSnapshotMatches
        {
            get
            {
                return this.savedSnapshot != null && this.savedSnapshot.Matches(this.gun);
            }
        }

        internal static bool TryCreateAndStart(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            LocalTargetInfo target,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad,
            out CeRuntimeProbeSession session,
            out string failure)
        {
            return CeRuntimeProbeSessionRunner.TryCreateAndStart(
                host,
                candidate,
                target,
                useAmmoOwnerAdapter,
                useBurstFire,
                holdBurstForSaveLoad,
                out session,
                out failure);
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.CaptureExpectedState();
            }

            Scribe_Deep.Look(ref this.gun, "gun");
            Scribe_References.Look(ref this.host, "host");
            Scribe_References.Look(ref this.targetThing, "targetThing");
            Scribe_Values.Look(ref this.targetCell, "targetCell", default(IntVec3));
            Scribe_Values.Look(ref this.targetHasThing, "targetHasThing", false);
            Scribe_Values.Look(ref this.active, "active", false);
            Scribe_Values.Look(ref this.castAttempted, "castAttempted", false);
            Scribe_Values.Look(ref this.castAccepted, "castAccepted", false);
            Scribe_Values.Look(ref this.callbackObserved, "callbackObserved", false);
            Scribe_Values.Look(ref this.trackerTickCount, "trackerTickCount", 0);
            Scribe_Values.Look(ref this.completionCallbackCount, "completionCallbackCount", 0);
            Scribe_Values.Look(ref this.initialLoadedCount, "initialLoadedCount", 0);
            Scribe_Values.Look(ref this.useAmmoOwnerAdapter, "useAmmoOwnerAdapter", false);
            Scribe_Values.Look(ref this.useBurstFire, "useBurstFire", false);
            Scribe_Values.Look(ref this.holdBurstForSaveLoad, "holdBurstForSaveLoad", false);
            Scribe_Values.Look(ref this.burstResumeAuthorized, "burstResumeAuthorized", false);
            Scribe_Deep.Look(ref this.savedSnapshot, "savedSnapshot");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.savedSnapshot == null)
                {
                    this.savedSnapshot = new CeRuntimeProbeStateSnapshot();
                }

                this.postLoadRebindPending = this.gun != null;
                this.postLoadRebindAttemptCount = 0;
                this.postLoadReportPending = this.gun != null;
            }
        }

        internal void Tick()
        {
            CeRuntimeProbeSessionRunner.Tick(this, MaxTrackerTicks);
        }

        internal void Report(string phase)
        {
            CeRuntimeProbeReportWriter.Write(this, phase, null);
        }

        internal void Release()
        {
            CeRuntimeProbeSessionRunner.Release(this);
        }

        internal CompAmmoUser GetAmmo()
        {
            return CeRuntimeProbeGunAccess.GetAmmo(this.gun);
        }

        internal CompFireModes GetModes()
        {
            return CeRuntimeProbeGunAccess.GetModes(this.gun);
        }

        internal LocalTargetInfo ResolveTarget()
        {
            if (this.targetHasThing && this.targetThing != null && !this.targetThing.Destroyed)
            {
                return new LocalTargetInfo(this.targetThing);
            }

            return new LocalTargetInfo(this.targetCell);
        }

        internal void CaptureExpectedState()
        {
            if (this.savedSnapshot == null)
            {
                this.savedSnapshot = new CeRuntimeProbeStateSnapshot();
            }

            this.savedSnapshot.Capture(this.gun);
        }

        internal void ClearReferences()
        {
            this.active = false;
            this.gun = null;
            this.host = null;
            this.targetThing = null;
            this.ammoOwner = null;
        }
    }
}
