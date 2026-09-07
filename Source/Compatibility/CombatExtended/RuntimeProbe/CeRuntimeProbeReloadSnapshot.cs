using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    public sealed class CeRuntimeProbeReloadSnapshot : IExposable
    {
        private CeRuntimeProbeStateSnapshot gunSnapshot =
            new CeRuntimeProbeStateSnapshot();
        private string stagedAmmoDefName;
        private int requestedCount;
        private int stagedCount;
        private bool active;

        public CeRuntimeProbeReloadSnapshot()
        {
        }

        internal void Capture(CeRuntimeProbeReloadSession session)
        {
            if (this.gunSnapshot == null)
            {
                this.gunSnapshot = new CeRuntimeProbeStateSnapshot();
            }

            this.gunSnapshot.Capture(session != null ? session.Gun : null);
            this.stagedAmmoDefName = session != null && session.StagedAmmo != null
                ? session.StagedAmmo.defName
                : null;
            this.requestedCount = session != null ? session.RequestedCount : 0;
            this.stagedCount = session != null ? session.StagedCount : 0;
            this.active = session != null && session.Active;
        }

        internal bool Matches(CeRuntimeProbeReloadSession session)
        {
            string currentStagedAmmo = session != null && session.StagedAmmo != null
                ? session.StagedAmmo.defName
                : null;
            return session != null &&
                this.gunSnapshot != null &&
                this.gunSnapshot.Matches(session.Gun) &&
                this.stagedAmmoDefName == currentStagedAmmo &&
                this.requestedCount == session.RequestedCount &&
                this.stagedCount == session.StagedCount &&
                this.active == session.Active;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref this.gunSnapshot, "gunSnapshot");
            Scribe_Values.Look(ref this.stagedAmmoDefName, "stagedAmmoDefName");
            Scribe_Values.Look(ref this.requestedCount, "requestedCount", 0);
            Scribe_Values.Look(ref this.stagedCount, "stagedCount", 0);
            Scribe_Values.Look(ref this.active, "active", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.gunSnapshot == null)
            {
                this.gunSnapshot = new CeRuntimeProbeStateSnapshot();
            }
        }
    }
}
