using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    public sealed class CeRuntimeProbeGameComponent : GameComponent
    {
        private CeRuntimeProbeSession session;
        private CeRuntimeProbeReloadSession reloadSession;
        private CeRuntimeProbeMuzzleSession muzzleSession;

        public CeRuntimeProbeGameComponent(Game game)
        {
        }

        internal static CeRuntimeProbeGameComponent CurrentComponent
        {
            get
            {
                return Current.Game != null
                    ? Current.Game.GetComponent<CeRuntimeProbeGameComponent>()
                    : null;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.session, "ceRuntimeProbeSession");
            Scribe_Deep.Look(ref this.reloadSession, "ceRuntimeProbeReloadSession");
        }

        public override void GameComponentTick()
        {
            if (this.session != null)
            {
                this.session.Tick();
            }

            if (this.reloadSession != null)
            {
                this.reloadSession.Tick();
            }

            if (this.muzzleSession != null)
            {
                this.muzzleSession.Tick();
            }
        }

        internal bool TryStart(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            LocalTargetInfo target,
            bool useAmmoOwnerAdapter,
            bool useBurstFire,
            bool holdBurstForSaveLoad,
            out string failure)
        {
            this.Clear();
            CeRuntimeProbeSession newSession;
            if (!CeRuntimeProbeSession.TryCreateAndStart(
                host,
                candidate,
                target,
                useAmmoOwnerAdapter,
                useBurstFire,
                holdBurstForSaveLoad,
                out newSession,
                out failure))
            {
                if (newSession != null)
                {
                    newSession.Release();
                }

                return false;
            }

            this.session = newSession;
            return true;
        }

        internal bool TryStartReloadProbe(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            out string failure)
        {
            this.Clear();
            CeRuntimeProbeReloadSession newSession;
            if (!CeRuntimeProbeReloadSession.TryCreate(
                host,
                candidate,
                out newSession,
                out failure))
            {
                if (newSession != null)
                {
                    newSession.Release();
                }

                return false;
            }

            this.reloadSession = newSession;
            return true;
        }

        internal bool TryStartMuzzleProbe(
            Thing host,
            CeRuntimeProbeCandidate candidate,
            CeRuntimeProbeMuzzleSource source,
            LocalTargetInfo target,
            out string failure)
        {
            this.Clear();
            CeRuntimeProbeMuzzleSession newSession;
            if (!CeRuntimeProbeMuzzleSessionRunner.TryCreateAndStart(
                host,
                candidate,
                target,
                source,
                out newSession,
                out failure))
            {
                if (newSession != null)
                {
                    newSession.Release();
                }

                return false;
            }

            this.muzzleSession = newSession;
            return true;
        }

        internal bool ResumeHeldBurst(out string failure)
        {
            return CeRuntimeProbeSessionRunner.ResumeHeldBurst(this.session, out failure);
        }

        internal bool StageInsufficientReload(out string failure)
        {
            CeRuntimeProbeReloadOperation operation;
            if (!CeRuntimeProbeReloadStaging.TryStageInsufficient(
                this.reloadSession,
                out operation,
                out failure))
            {
                return false;
            }

            this.reloadSession.Report("reload-stage-insufficient", operation, null);
            return true;
        }

        internal bool StageRemainingReload(out string failure)
        {
            CeRuntimeProbeReloadOperation operation;
            if (!CeRuntimeProbeReloadStaging.TryStageRemaining(
                this.reloadSession,
                out operation,
                out failure))
            {
                return false;
            }

            this.reloadSession.Report("reload-stage-remaining", operation, null);
            return true;
        }

        internal bool CommitStagedReload(out string failure)
        {
            CeRuntimeProbeReloadOperation operation;
            if (!CeRuntimeProbeMagazineCommitter.TryCommit(
                this.reloadSession,
                false,
                out operation,
                out failure))
            {
                if (this.reloadSession != null)
                {
                    this.reloadSession.Report("reload-commit-failed", operation, failure);
                }

                return false;
            }

            this.reloadSession.Report("reload-commit", operation, null);
            return true;
        }

        internal bool InjectReloadCommitRollback(out string failure)
        {
            CeRuntimeProbeReloadOperation operation;
            if (!CeRuntimeProbeMagazineCommitter.TryCommit(
                this.reloadSession,
                true,
                out operation,
                out failure))
            {
                if (this.reloadSession != null)
                {
                    this.reloadSession.Report("reload-rollback-failed", operation, failure);
                }

                return false;
            }

            this.reloadSession.Report("reload-rollback-verified", operation, null);
            return true;
        }

        internal bool CancelStagedReload(out string failure)
        {
            CeRuntimeProbeReloadOperation operation;
            if (!CeRuntimeProbeReloadStaging.TryCancel(
                this.reloadSession,
                out operation,
                out failure))
            {
                return false;
            }

            this.reloadSession.Report("reload-cancel", operation, null);
            return true;
        }

        internal bool CommitReloadFromShuttleCargo(out string failure)
        {
            return this.RunCargoReloadProbe(
                false,
                "reload-cargo-commit",
                "reload-cargo-commit-failed",
                out failure);
        }

        internal bool InjectShuttleCargoReloadRollback(out string failure)
        {
            return this.RunCargoReloadProbe(
                true,
                "reload-cargo-rollback-verified",
                "reload-cargo-rollback-failed",
                out failure);
        }

        private bool RunCargoReloadProbe(
            bool injectRollback,
            string successPhase,
            string failurePhase,
            out string failure)
        {
            CeRuntimeProbeReloadOperation operation;
            if (!CeRuntimeProbeCargoReloadAdapter.TryRun(
                this.reloadSession,
                injectRollback,
                out operation,
                out failure))
            {
                if (this.reloadSession != null)
                {
                    this.reloadSession.Report(failurePhase, operation, failure);
                }

                return false;
            }

            this.reloadSession.Report(successPhase, operation, null);
            return true;
        }

        internal bool Report()
        {
            if (this.session == null)
            {
                if (this.reloadSession != null)
                {
                    this.reloadSession.Report("reload-manual-report", null, null);
                    return true;
                }

                if (this.muzzleSession != null)
                {
                    this.muzzleSession.Report("muzzle-manual-report", null);
                    return true;
                }

                return false;
            }

            this.session.Report("manual-report");
            return true;
        }

        internal bool Clear()
        {
            bool cleared = false;
            if (this.session != null)
            {
                this.session.Release();
                this.session = null;
                cleared = true;
            }

            if (this.reloadSession != null)
            {
                this.reloadSession.Release();
                this.reloadSession = null;
                cleared = true;
            }

            if (this.muzzleSession != null)
            {
                this.muzzleSession.Release();
                this.muzzleSession = null;
                cleared = true;
            }

            return cleared;
        }
    }
}
