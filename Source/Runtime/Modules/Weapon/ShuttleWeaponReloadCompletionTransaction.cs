using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns one core-magazine completion attempt. It quotes available ammunition, delegates the
    /// exact paired mutation, then applies only terminal reload state. Retry and continuation stay
    /// with the outer orchestration layer.
    /// </summary>
    internal sealed class ShuttleWeaponReloadCompletionTransaction
    {
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponAmmoTransferCommitter transferCommitter;

        internal ShuttleWeaponReloadCompletionTransaction(
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponAmmoTransferCommitter transferCommitter)
        {
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(null);
            this.transferCommitter = transferCommitter ??
                new ShuttleWeaponAmmoTransferCommitter();
        }

        internal int GetLoadableCount(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            IShuttleWeaponAmmoSupply supply)
        {
            int needed = this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState);
            ShuttleWeaponAmmoDef selectedAmmo =
                this.coreMagazine.GetSelectedAmmo(weaponDef, ammoState);
            if (needed <= 0 ||
                supply == null ||
                !supply.IsAvailable ||
                selectedAmmo == null ||
                selectedAmmo.AmmoThingDef == null)
            {
                return 0;
            }

            int available = supply.CountAvailable(selectedAmmo.AmmoThingDef);
            if (available <= 0)
            {
                return 0;
            }

            return available < needed ? available : needed;
        }

        internal ShuttleWeaponReloadCompletionResult Complete(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState ammoState,
            IShuttleWeaponAmmoSupply supply,
            string noAmmoSystemReason,
            string sourceUnavailableReason,
            string sourceEmptyReason,
            string reloadFailedReason,
            string consumeReason)
        {
            if (weaponDef == null || ammoState == null)
            {
                return ShuttleWeaponReloadCompletionResult.Failed(noAmmoSystemReason);
            }

            int needed = this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState);
            if (needed <= 0)
            {
                ammoState.FinishReload();
                return ShuttleWeaponReloadCompletionResult.CompletedWithoutTransfer();
            }

            if (supply == null || !supply.IsAvailable)
            {
                return this.Fail(
                    ammoState,
                    !string.IsNullOrEmpty(sourceUnavailableReason)
                        ? sourceUnavailableReason
                        : reloadFailedReason);
            }

            ShuttleWeaponAmmoDef selectedAmmo =
                this.coreMagazine.GetSelectedAmmo(weaponDef, ammoState);
            int requested = this.GetLoadableCount(weaponDef, ammoState, supply);
            if (requested <= 0)
            {
                return this.Fail(ammoState, sourceEmptyReason);
            }

            int loadedBefore = ammoState.LoadedAmmoCount;
            ShuttleWeaponReloadExecutorKind completedExecutorKind =
                ammoState.ReloadExecutorKind;
            ShuttleWeaponAmmoTransferResult transferResult =
                this.transferCommitter.Commit(
                    this.coreMagazine,
                    ammoState,
                    supply,
                    selectedAmmo != null ? selectedAmmo.AmmoThingDef : null,
                    requested,
                    consumeReason);
            int committed = transferResult != null
                ? transferResult.CommittedCount
                : 0;
            string failureReason = transferResult != null
                ? transferResult.FailureReason
                : reloadFailedReason;
            if (committed <= 0)
            {
                return this.Fail(
                    ammoState,
                    string.IsNullOrEmpty(failureReason)
                        ? sourceEmptyReason
                        : failureReason);
            }

            bool fullReload =
                this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) <= 0;
            bool sourceStillHasAmmo = !fullReload &&
                supply.CountAvailable(selectedAmmo.AmmoThingDef) > 0;
            ammoState.FinishReload();
            return ShuttleWeaponReloadCompletionResult.CompletedTransfer(
                fullReload,
                sourceStillHasAmmo,
                selectedAmmo,
                completedExecutorKind,
                loadedBefore,
                requested,
                committed,
                failureReason);
        }

        private ShuttleWeaponReloadCompletionResult Fail(
            ShuttleWeaponAmmoState ammoState,
            string failureReason)
        {
            if (ammoState != null)
            {
                ammoState.FailReload(failureReason);
            }

            return ShuttleWeaponReloadCompletionResult.Failed(failureReason);
        }
    }
}
