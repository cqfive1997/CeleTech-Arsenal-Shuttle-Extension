using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    public sealed class CeRuntimeProbeReloadSession : IExposable
    {
        private ThingWithComps gun;
        private Thing host;
        private AmmoDef stagedAmmo;
        private int requestedCount;
        private int stagedCount;
        private bool active;
        private CeRuntimeProbeReloadSnapshot savedSnapshot =
            new CeRuntimeProbeReloadSnapshot();

        private CeRuntimeProbeAmmoOwner ammoOwner;
        private bool postLoadRebindPending;
        private int postLoadRebindFailureCount;
        private bool postLoadReportPending;

        public CeRuntimeProbeReloadSession()
        {
        }

        internal CeRuntimeProbeReloadSession(ThingWithComps gun, Thing host)
        {
            this.gun = gun;
            this.host = host;
        }

        internal ThingWithComps Gun
        {
            get { return this.gun; }
        }

        internal Thing Host
        {
            get { return this.host; }
        }

        internal AmmoDef StagedAmmo
        {
            get { return this.stagedAmmo; }
        }

        internal int RequestedCount
        {
            get { return this.requestedCount; }
        }

        internal int StagedCount
        {
            get { return this.stagedCount; }
        }

        internal bool Active
        {
            get { return this.active; }
            set { this.active = value; }
        }

        internal CeRuntimeProbeAmmoOwner AmmoOwner
        {
            get { return this.ammoOwner; }
            set { this.ammoOwner = value; }
        }

        internal bool PostLoadRebindPending
        {
            get { return this.postLoadRebindPending; }
            set { this.postLoadRebindPending = value; }
        }

        internal int PostLoadRebindFailureCount
        {
            get { return this.postLoadRebindFailureCount; }
            set { this.postLoadRebindFailureCount = value; }
        }

        internal bool PostLoadReportPending
        {
            get { return this.postLoadReportPending; }
            set { this.postLoadReportPending = value; }
        }

        internal bool PersistedSnapshotMatches
        {
            get
            {
                return this.savedSnapshot != null &&
                    this.savedSnapshot.Matches(this);
            }
        }

        internal static bool TryCreate(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            out CeRuntimeProbeReloadSession session,
            out string failure)
        {
            return CeRuntimeProbeReloadSessionRunner.TryCreate(
                host,
                candidate,
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
            Scribe_Defs.Look(ref this.stagedAmmo, "stagedAmmo");
            Scribe_Values.Look(ref this.requestedCount, "requestedCount", 0);
            Scribe_Values.Look(ref this.stagedCount, "stagedCount", 0);
            Scribe_Values.Look(ref this.active, "active", false);
            Scribe_Deep.Look(ref this.savedSnapshot, "savedSnapshot");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.savedSnapshot == null)
                {
                    this.savedSnapshot = new CeRuntimeProbeReloadSnapshot();
                }

                this.postLoadRebindPending = this.gun != null;
                this.postLoadRebindFailureCount = 0;
                this.postLoadReportPending = this.gun != null;
            }
        }

        internal CompAmmoUser GetAmmo()
        {
            return CeRuntimeProbeGunAccess.GetAmmo(this.gun);
        }

        internal void SetStagedGrant(AmmoDef ammoDef, int requested, int staged)
        {
            this.stagedAmmo = ammoDef;
            this.requestedCount = requested;
            this.stagedCount = staged;
        }

        internal void ClearStagedGrant()
        {
            this.stagedAmmo = null;
            this.requestedCount = 0;
            this.stagedCount = 0;
        }

        internal void CaptureExpectedState()
        {
            if (this.savedSnapshot == null)
            {
                this.savedSnapshot = new CeRuntimeProbeReloadSnapshot();
            }

            this.savedSnapshot.Capture(this);
        }

        internal void Tick()
        {
            CeRuntimeProbeReloadSessionRunner.Tick(this);
        }

        internal void Report(string phase, CeRuntimeProbeReloadOperation operation, string note)
        {
            CeRuntimeProbeReloadReportWriter.Write(this, phase, operation, note);
        }

        internal void Release()
        {
            CeRuntimeProbeReloadSessionRunner.Release(this);
        }
    }
}
