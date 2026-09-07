using System.Text;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    public sealed class CeWeaponProductionCycleProbeGameComponent : GameComponent
    {
        private CeWeaponProductionCycleProbeSession session;

        public CeWeaponProductionCycleProbeGameComponent(Game game)
        {
        }

        internal static CeWeaponProductionCycleProbeGameComponent CurrentComponent
        {
            get
            {
                return Current.Game != null
                    ? Current.Game.GetComponent<CeWeaponProductionCycleProbeGameComponent>()
                    : null;
            }
        }

        internal bool HasActiveSession
        {
            get { return this.session != null; }
        }

        internal bool TryStart(
            CeWeaponProductionCycleProbeSession newSession,
            out string failureReason)
        {
            failureReason = null;
            if (this.session != null)
            {
                failureReason = "A CE production firing-cycle probe is already active.";
                return false;
            }

            if (newSession == null)
            {
                failureReason = "The CE production firing-cycle session was not created.";
                return false;
            }

            this.session = newSession;
            return true;
        }

        public override void GameComponentTick()
        {
            if (this.session == null)
            {
                return;
            }

            this.session.Tick();
            if (!this.session.Finished)
            {
                return;
            }

            CeWeaponProductionCycleProbeReport report = this.session.Report;
            Log.Message(Format(report));
            Messages.Message(
                report.Passed
                    ? "CE production firing cycle passed; installed weapon state was not changed."
                    : "CE production firing cycle did not pass; see the log for the exact stage.",
                report.Passed ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                false);
            this.session.Release();
            this.session = null;
        }

        private static string Format(CeWeaponProductionCycleProbeReport report)
        {
            StringBuilder output = new StringBuilder();
            output.Append("[CeleTech Shuttle][CE Production Cycle Probe]")
                .Append(" module=").Append(report.ModuleInstanceID)
                .Append(" moduleDef=").Append(report.ModuleDefName)
                .Append(" hostRotation=").Append(report.HostRotation)
                .Append(" target=").Append(report.Target)
                .Append(" targetAccepted=").Append(report.TargetAccepted)
                .Append(" targetAcquired=").Append(report.TargetAcquired)
                .Append(" warmup=").Append(report.ObservedWarmupTicks)
                .Append('/').Append(report.ExpectedWarmupTicks)
                .Append(" burstStarted=").Append(report.BurstStarted)
                .Append(" expectedShots=").Append(report.ExpectedShots)
                .Append(" burstTrackerTicks=").Append(report.BurstTrackerTicks)
                .Append(" callbacks=").Append(report.CompletionCallbacks)
                .Append(" cooldownAtCompletion=").Append(report.CooldownTicksAtCompletion)
                .Append('/').Append(report.ExpectedCooldownTicks)
                .Append(" cooldownCompleted=").Append(report.CooldownCompleted)
                .Append(" totalTicks=").Append(report.TotalTicks)
                .Append(" source=").Append(report.SourceLoadedBefore)
                .Append("->").Append(report.SourceLoadedAfter)
                .Append(" ce=").Append(report.CeLoadedBefore)
                .Append("->").Append(report.CeLoadedAfter)
                .Append(" sourceUnchanged=").Append(report.SourceUnchanged)
                .Append(" powerTicks=").Append(report.PowerDemandTicks)
                .Append(" peakWatts=").Append(report.PeakPowerWatts)
                .Append('/').Append(report.ExpectedPowerWatts)
                .Append(" ownerReady=").Append(report.OwnerReady)
                .Append(" muzzleCell=").Append(report.MuzzleCell)
                .Append(" muzzleXZ=").Append(report.MuzzleDrawPos.x)
                .Append(',').Append(report.MuzzleDrawPos.z)
                .Append(" originObserved=").Append(report.ProjectileOriginObserved)
                .Append(" originMatches=").Append(report.ProjectileOriginMatches)
                .Append(" passed=").Append(report.Passed)
                .Append(" failure=").Append(report.Failure ?? "none");
            return output.ToString();
        }
    }
}
