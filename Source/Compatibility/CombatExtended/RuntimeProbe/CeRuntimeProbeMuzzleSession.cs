using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeMuzzleSession
    {
        private readonly ThingWithComps gun;
        private readonly Thing host;
        private readonly Thing targetThing;
        private readonly IntVec3 targetCell;
        private readonly bool targetHasThing;
        private readonly CeRuntimeProbeMuzzleSource muzzleSource;
        private readonly CeRuntimeProbeMuzzleObservation observation;

        private CeRuntimeProbeAmmoOwner ammoOwner;
        private bool active;
        private bool castAttempted;
        private bool castAccepted;
        private bool completionReportPending;
        private int trackerTickCount;
        private int completionCallbackCount;
        private int initialLoadedCount;

        internal CeRuntimeProbeMuzzleSession(
            ThingWithComps gun,
            Thing host,
            LocalTargetInfo target,
            CeRuntimeProbeMuzzleSource muzzleSource,
            CeRuntimeProbeMuzzleObservation observation)
        {
            this.gun = gun;
            this.host = host;
            this.targetHasThing = target.HasThing;
            this.targetThing = target.Thing;
            this.targetCell = target.Cell;
            this.muzzleSource = muzzleSource;
            this.observation = observation;
        }

        internal ThingWithComps Gun { get { return this.gun; } }

        internal Thing Host { get { return this.host; } }

        internal CeRuntimeProbeMuzzleSource MuzzleSource { get { return this.muzzleSource; } }

        internal CeRuntimeProbeMuzzleObservation Observation { get { return this.observation; } }

        internal CeRuntimeProbeAmmoOwner AmmoOwner
        {
            get { return this.ammoOwner; }
            set { this.ammoOwner = value; }
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

        internal bool CompletionReportPending
        {
            get { return this.completionReportPending; }
            set { this.completionReportPending = value; }
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

        internal LocalTargetInfo ResolveTarget()
        {
            if (this.targetHasThing &&
                this.targetThing != null &&
                !this.targetThing.Destroyed)
            {
                return new LocalTargetInfo(this.targetThing);
            }

            return new LocalTargetInfo(this.targetCell);
        }

        internal CompAmmoUser GetAmmo()
        {
            return CeRuntimeProbeGunAccess.GetAmmo(this.gun);
        }

        internal void Tick()
        {
            CeRuntimeProbeMuzzleSessionRunner.Tick(this);
        }

        internal void Report(string phase, string note)
        {
            CeRuntimeProbeMuzzleReportWriter.Write(this, phase, note);
        }

        internal void Release()
        {
            CeRuntimeProbeMuzzleSessionRunner.Release(this);
        }
    }
}
