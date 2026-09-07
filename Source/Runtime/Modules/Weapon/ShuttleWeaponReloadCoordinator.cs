namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponReloadProgressAction
    {
        NotActive,
        WaitingForAdmission,
        Progressed,
        ReadyToCommit
    }

    /// <summary>
    /// Owns common reload request and progress state transitions. Eligibility, power admission,
    /// supply resolution, magazine commits and Pawn Job execution stay outside this coordinator.
    /// </summary>
    internal sealed class ShuttleWeaponReloadCoordinator
    {
        internal bool TryRequest(
            ShuttleWeaponAmmoState state,
            bool hasAmmoSystem,
            bool reloadNeeded,
            ShuttleWeaponReloadRequestKind requestKind,
            string noAmmoSystemReason,
            out string failureReason)
        {
            failureReason = null;
            if (!hasAmmoSystem || state == null)
            {
                failureReason = noAmmoSystemReason;
                return false;
            }

            if (!reloadNeeded)
            {
                this.ClearRequest(state);
                return true;
            }

            this.QueueRequest(state, requestKind);
            return true;
        }

        internal void QueueRequest(
            ShuttleWeaponAmmoState state,
            ShuttleWeaponReloadRequestKind requestKind)
        {
            if (state == null)
            {
                return;
            }

            // Pending intent is monotonic. Automatic fire/top-off probes cannot erase a player's
            // fill-magazine request while the current trip owns the executor.
            if (!ShuttleWeaponReloadPolicy.ShouldReplacePending(
                    state.ReloadRequestKind,
                    requestKind))
            {
                return;
            }

            state.RequestReload(requestKind);
        }

        internal bool Cancel(ShuttleWeaponAmmoState state)
        {
            if (state == null)
            {
                return false;
            }

            state.CancelReload();
            return true;
        }

        internal void ClearRequest(ShuttleWeaponAmmoState state)
        {
            if (state != null)
            {
                state.ClearReloadRequest();
            }
        }

        internal void ScheduleRetry(
            ShuttleWeaponAmmoState state,
            int ticksGame,
            int delayTicks)
        {
            if (state != null)
            {
                state.ScheduleAutomaticReloadRetry(ticksGame, delayTicks);
            }
        }

        internal ShuttleWeaponReloadRequestKind ResolveAndRememberEffectiveRequestKind(
            ShuttleWeaponAmmoState state,
            bool canFireLoadedAmmo)
        {
            bool reloadRequested = state != null && state.ReloadRequested;
            ShuttleWeaponReloadRequestKind pendingKind = state != null
                ? state.ReloadRequestKind
                : ShuttleWeaponReloadRequestKind.None;
            ShuttleWeaponReloadRequestKind requestKind =
                ShuttleWeaponReloadPolicy.ResolveEffectiveRequestKind(
                    reloadRequested,
                    pendingKind,
                    canFireLoadedAmmo);
            if (state != null && reloadRequested &&
                pendingKind == ShuttleWeaponReloadRequestKind.None &&
                requestKind != ShuttleWeaponReloadRequestKind.None)
            {
                state.SetReloadRequestKindForRuntimeOnly(requestKind);
            }

            return requestKind;
        }

        internal ShuttleWeaponReloadRequestKind ResolveAndRememberCompletionRequestKind(
            ShuttleWeaponAmmoState state,
            bool canFireLoadedAmmo)
        {
            ShuttleWeaponReloadRequestKind requestKind =
                ShuttleWeaponReloadPolicy.ResolveCompletionRequestKind(
                    state != null
                        ? state.ActiveReloadRequestKind
                        : ShuttleWeaponReloadRequestKind.None,
                    state != null && state.ReloadRequested,
                    state != null
                        ? state.ReloadRequestKind
                        : ShuttleWeaponReloadRequestKind.None,
                    canFireLoadedAmmo);
            if (state != null &&
                state.ActiveReloadRequestKind == ShuttleWeaponReloadRequestKind.None &&
                state.ReloadRequestKind == ShuttleWeaponReloadRequestKind.None &&
                requestKind != ShuttleWeaponReloadRequestKind.None)
            {
                state.SetReloadRequestKindForRuntimeOnly(requestKind);
            }

            return requestKind;
        }

        internal bool TryStartAutomatic(
            ShuttleWeaponAmmoState state,
            int workTicks,
            ShuttleWeaponReloadRequestKind requestKind)
        {
            return state != null && state.TryStartAutomaticReload(workTicks, requestKind);
        }

        internal ShuttleWeaponReloadProgressAction AdvanceProgress(
            ShuttleWeaponAmmoState state,
            bool powerAdmitted)
        {
            if (state == null || !state.ReloadInProgress)
            {
                return ShuttleWeaponReloadProgressAction.NotActive;
            }

            if (!powerAdmitted)
            {
                return ShuttleWeaponReloadProgressAction.WaitingForAdmission;
            }

            state.AddReloadWork(1);
            return state.ReloadWorkDone < state.ReloadWorkTotal
                ? ShuttleWeaponReloadProgressAction.Progressed
                : ShuttleWeaponReloadProgressAction.ReadyToCommit;
        }

        internal void ApplyFailure(
            ShuttleWeaponAmmoState state,
            ShuttleWeaponReloadRequestKind requestKind,
            string failureReason,
            int ticksGame)
        {
            if (state == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(failureReason))
            {
                state.SetLastReloadBlockerReason(failureReason);
            }

            ShuttleWeaponReloadFailureAction failureAction =
                ShuttleWeaponReloadPolicy.ResolveFailureAction(
                    requestKind,
                    state.ManualReloadAllowed,
                    failureReason);
            if (failureAction == ShuttleWeaponReloadFailureAction.ClearManualRequest)
            {
                this.ClearRequest(state);
                if (!string.IsNullOrEmpty(failureReason))
                {
                    state.SetLastReloadBlockerReason(failureReason);
                }

                return;
            }

            if (failureAction == ShuttleWeaponReloadFailureAction.RetryManual)
            {
                this.ScheduleRetry(
                    state,
                    ticksGame,
                    ShuttleWeaponReloadPolicy.GetAutomaticRetryDelayTicks(failureReason));
                return;
            }

            if (failureAction == ShuttleWeaponReloadFailureAction.RetryAutomatic)
            {
                if (!string.IsNullOrEmpty(failureReason))
                {
                    state.SetLastAutomaticReloadBlockerReason(failureReason);
                    state.SetLastReloadBlockerReason(failureReason);
                }

                this.ScheduleRetry(
                    state,
                    ticksGame,
                    ShuttleWeaponReloadPolicy.GetAutomaticRetryDelayTicks(failureReason));
            }
        }

        internal void ScheduleAutomaticFailureRetry(
            ShuttleWeaponAmmoState state,
            ShuttleWeaponReloadRequestKind requestKind,
            string failureReason,
            int ticksGame)
        {
            if (state == null || !ShuttleWeaponReloadPolicy.IsAutomaticRequest(requestKind))
            {
                return;
            }

            this.QueueRequest(state, requestKind);
            if (!string.IsNullOrEmpty(failureReason))
            {
                state.SetLastReloadBlockerReason(failureReason);
                state.SetLastAutomaticReloadBlockerReason(failureReason);
            }

            this.ScheduleRetry(
                state,
                ticksGame,
                ShuttleWeaponReloadPolicy.GetAutomaticRetryDelayTicks(failureReason));
        }
    }
}
