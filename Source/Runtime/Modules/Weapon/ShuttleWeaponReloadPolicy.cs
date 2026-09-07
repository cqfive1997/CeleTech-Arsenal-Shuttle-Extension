using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal enum ShuttleWeaponReloadFailureAction
    {
        None,
        ClearManualRequest,
        RetryManual,
        RetryAutomatic
    }

    internal enum ShuttleWeaponPartialReloadAction
    {
        None,
        QueueManual,
        QueueRequiredForFire,
        QueueAutoTopOff,
        StopSourceEmpty
    }

    /// <summary>
    /// Pure reload decisions over detached enum/scalar inputs. It owns no state, timing clock,
    /// cargo, magazine, Pawn Job, backend, target, firing or UI behavior.
    /// </summary>
    internal static class ShuttleWeaponReloadPolicy
    {
        internal const int PartialReloadContinuationDelayTicks = 45;
        internal const int PawnManualReloadRecheckDelayTicks = 60;

        internal static bool ShouldReplacePending(
            ShuttleWeaponReloadRequestKind pendingKind,
            ShuttleWeaponReloadRequestKind requestedKind)
        {
            return GetPriority(requestedKind) >= GetPriority(pendingKind);
        }

        internal static ShuttleWeaponReloadRequestKind ResolveContinuation(
            ShuttleWeaponReloadRequestKind completedKind,
            ShuttleWeaponReloadRequestKind pendingKind)
        {
            return GetPriority(pendingKind) > GetPriority(completedKind)
                ? pendingKind
                : completedKind;
        }

        internal static ShuttleWeaponReloadRequestKind ResolveEffectiveRequestKind(
            bool reloadRequested,
            ShuttleWeaponReloadRequestKind pendingKind,
            bool canFireLoadedAmmo)
        {
            if (!reloadRequested)
            {
                return ShuttleWeaponReloadRequestKind.None;
            }

            if (pendingKind != ShuttleWeaponReloadRequestKind.None)
            {
                return pendingKind;
            }

            return canFireLoadedAmmo
                ? ShuttleWeaponReloadRequestKind.AutoTopOff
                : ShuttleWeaponReloadRequestKind.RequiredForFire;
        }

        internal static ShuttleWeaponReloadRequestKind ResolveCompletionRequestKind(
            ShuttleWeaponReloadRequestKind activeKind,
            bool reloadRequested,
            ShuttleWeaponReloadRequestKind pendingKind,
            bool canFireLoadedAmmo)
        {
            return activeKind != ShuttleWeaponReloadRequestKind.None
                ? activeKind
                : ResolveEffectiveRequestKind(
                    reloadRequested,
                    pendingKind,
                    canFireLoadedAmmo);
        }

        internal static bool IsAutomaticRequest(
            ShuttleWeaponReloadRequestKind requestKind)
        {
            return requestKind == ShuttleWeaponReloadRequestKind.RequiredForFire ||
                requestKind == ShuttleWeaponReloadRequestKind.AutoTopOff;
        }

        internal static ShuttleWeaponReloadFailureAction ResolveFailureAction(
            ShuttleWeaponReloadRequestKind requestKind,
            bool manualReloadAllowed,
            string failureReason)
        {
            if (requestKind == ShuttleWeaponReloadRequestKind.Manual)
            {
                return !manualReloadAllowed || IsImmediateManualFailure(failureReason)
                    ? ShuttleWeaponReloadFailureAction.ClearManualRequest
                    : ShuttleWeaponReloadFailureAction.RetryManual;
            }

            return IsAutomaticRequest(requestKind)
                ? ShuttleWeaponReloadFailureAction.RetryAutomatic
                : ShuttleWeaponReloadFailureAction.None;
        }

        internal static int GetAutomaticRetryDelayTicks(string failureReason)
        {
            if (failureReason == "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString())
            {
                return 90;
            }

            if (failureReason == "CT_Shuttle_WeaponAmmo_AmmoLoaderUnavailable".Translate().ToString())
            {
                return 120;
            }

            if (failureReason == "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString() ||
                failureReason == "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString())
            {
                return 250;
            }

            return 60;
        }

        internal static bool ShouldRequestAutomaticTopOff(
            bool autoReloadEnabled,
            bool reloadRequested,
            bool reloadInProgress,
            bool manualReloadJobActive,
            ShuttleWeaponReloadExecutorKind executorKind,
            int ammoPerShot,
            int remainingAmmoThreshold,
            int magazineCapacity,
            int loadedAmmoCount)
        {
            if (!autoReloadEnabled || reloadRequested || reloadInProgress ||
                manualReloadJobActive ||
                executorKind != ShuttleWeaponReloadExecutorKind.None ||
                ammoPerShot <= 0 || remainingAmmoThreshold < 0)
            {
                return false;
            }

            return magazineCapacity > 0 &&
                loadedAmmoCount >= ammoPerShot &&
                loadedAmmoCount < magazineCapacity &&
                loadedAmmoCount <= remainingAmmoThreshold;
        }

        internal static ShuttleWeaponPartialReloadAction ResolvePartialReloadAction(
            ShuttleWeaponReloadRequestKind requestKind,
            bool autoReloadEnabled,
            int loadedAmmoCount,
            int ammoPerShot,
            bool reloadStillNeeded,
            bool sourceStillHasAmmo)
        {
            if (requestKind == ShuttleWeaponReloadRequestKind.Manual)
            {
                return ShuttleWeaponPartialReloadAction.QueueManual;
            }

            if (!IsAutomaticRequest(requestKind) || !autoReloadEnabled)
            {
                return ShuttleWeaponPartialReloadAction.None;
            }

            if (loadedAmmoCount < ammoPerShot)
            {
                return ShuttleWeaponPartialReloadAction.QueueRequiredForFire;
            }

            if (!reloadStillNeeded)
            {
                return ShuttleWeaponPartialReloadAction.None;
            }

            return sourceStillHasAmmo
                ? ShuttleWeaponPartialReloadAction.QueueAutoTopOff
                : ShuttleWeaponPartialReloadAction.StopSourceEmpty;
        }

        private static bool IsImmediateManualFailure(string failureReason)
        {
            return string.IsNullOrEmpty(failureReason) ||
                failureReason == "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString() ||
                failureReason == "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
        }

        private static int GetPriority(ShuttleWeaponReloadRequestKind requestKind)
        {
            if (requestKind == ShuttleWeaponReloadRequestKind.Manual)
            {
                return 3;
            }

            if (requestKind == ShuttleWeaponReloadRequestKind.RequiredForFire)
            {
                return 2;
            }

            return requestKind == ShuttleWeaponReloadRequestKind.AutoTopOff ? 1 : 0;
        }
    }
}
