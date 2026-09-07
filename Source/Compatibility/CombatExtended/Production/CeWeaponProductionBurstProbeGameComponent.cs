using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Advances only transient DevMode burst sessions at normal game-tick cadence. It owns no
    /// installed weapon state and is not part of the production backend selection path.
    /// </summary>
    public sealed class CeWeaponProductionBurstProbeGameComponent : GameComponent
    {
        private List<CeWeaponProductionBurstProbeSession> sessions;
        private LocalTargetInfo target;

        public CeWeaponProductionBurstProbeGameComponent(Game game)
        {
        }

        internal static CeWeaponProductionBurstProbeGameComponent CurrentComponent
        {
            get
            {
                return Current.Game != null
                    ? Current.Game.GetComponent<CeWeaponProductionBurstProbeGameComponent>()
                    : null;
            }
        }

        internal bool HasActiveBatch
        {
            get { return this.sessions != null && this.sessions.Count > 0; }
        }

        internal bool TryStart(
            List<CeWeaponProductionBurstProbeSession> newSessions,
            LocalTargetInfo selectedTarget,
            out string failureReason)
        {
            failureReason = null;
            if (this.HasActiveBatch)
            {
                failureReason = "A CE production burst probe is already active.";
                return false;
            }

            if (newSessions == null || newSessions.Count == 0)
            {
                failureReason = "No compatible installed shuttle weapons were found.";
                return false;
            }

            this.sessions = newSessions;
            this.target = selectedTarget;
            return true;
        }

        public override void GameComponentTick()
        {
            if (this.sessions == null || this.sessions.Count == 0)
            {
                return;
            }

            bool allFinished = true;
            for (int i = 0; i < this.sessions.Count; i++)
            {
                CeWeaponProductionBurstProbeSession session = this.sessions[i];
                if (session == null)
                {
                    continue;
                }

                session.Tick();
                if (!session.Finished)
                {
                    allFinished = false;
                }
            }

            if (allFinished)
            {
                this.ReportAndRelease();
            }
        }

        private void ReportAndRelease()
        {
            StringBuilder output = new StringBuilder();
            output.Append("[CeleTech Shuttle][CE Production Burst Probe] begin target=")
                .Append(this.target.ToString())
                .AppendLine();

            int inspected = 0;
            int passed = 0;
            for (int i = 0; i < this.sessions.Count; i++)
            {
                CeWeaponProductionBurstProbeSession session = this.sessions[i];
                if (session == null)
                {
                    continue;
                }

                inspected++;
                CeWeaponProductionBurstProbeReport report = session.Report;
                if (report.Passed)
                {
                    passed++;
                }

                AppendReport(output, report);
                session.Release();
            }

            bool allPassed = inspected > 0 && passed == inspected;
            output.Append("[CeleTech Shuttle][CE Production Burst Probe] complete")
                .Append(" inspected=").Append(inspected)
                .Append(" passed=").Append(passed)
                .Append(" allPassed=").Append(allPassed);
            Log.Message(output.ToString());
            Messages.Message(
                allPassed
                    ? "CE production burst boundary passed; installed magazines were not changed."
                    : "CE production burst boundary did not pass; see the log for the exact module and reason.",
                allPassed ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput,
                false);

            this.sessions = null;
            this.target = LocalTargetInfo.Invalid;
        }

        private static void AppendReport(
            StringBuilder output,
            CeWeaponProductionBurstProbeReport report)
        {
            output.Append("[CeleTech Shuttle][CE Production Burst Probe] module=")
                .Append(report.ModuleInstanceID)
                .Append(" moduleDef=").Append(report.ModuleDefName)
                .Append(" hostRotation=").Append(report.HostRotation)
                .Append(" expectedShots=").Append(report.ExpectedShots)
                .Append(" source=").Append(report.SourceLoadedBefore)
                .Append("->").Append(report.SourceLoadedAfter)
                .Append(" ce=").Append(report.CeLoadedBefore)
                .Append("->").Append(report.CeLoadedAfter)
                .Append(" callbacks=").Append(report.CompletionCallbacks)
                .Append(" trackerTicks=").Append(report.TrackerTicks)
                .Append(" ownerReady=").Append(report.OwnerReady)
                .Append(" castAccepted=").Append(report.CastAccepted)
                .Append(" sourceUnchanged=").Append(report.SourceUnchanged)
                .Append(" muzzleCell=").Append(report.MuzzleCell)
                .Append(" muzzleXZ=")
                .Append(report.MuzzleDrawPos.x).Append(',')
                .Append(report.MuzzleDrawPos.z)
                .Append(" originObserved=").Append(report.ProjectileOriginObserved)
                .Append(" originMatches=").Append(report.ProjectileOriginMatches)
                .Append(" passed=").Append(report.Passed)
                .Append(" failure=").Append(report.Failure ?? "none")
                .Append(" trace=").Append(report.FailureTrace ?? "none")
                .AppendLine();
        }
    }
}
