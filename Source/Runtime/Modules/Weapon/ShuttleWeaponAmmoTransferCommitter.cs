using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns one synchronous source-to-core-magazine mutation. The magazine delta is staged only
    /// after capacity validation, then reconciled to the source's exact consumed count before the
    /// call returns. It owns no reload timing, retry or continuation policy.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoTransferCommitter
    {
        private const string MagazineRollbackFailure =
            "weapon-ammo-magazine-rollback-failed";

        internal ShuttleWeaponAmmoTransferResult Commit(
            CoreMagazineDriver magazineDriver,
            ShuttleWeaponAmmoState magazine,
            IShuttleWeaponAmmoSupply supply,
            ThingDef ammoThingDef,
            int requestedCount,
            string reason)
        {
            if (magazineDriver == null || magazine == null || ammoThingDef == null ||
                requestedCount <= 0)
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    false,
                    "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString());
            }

            if (supply == null || !supply.IsAvailable)
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    false,
                    "CT_Shuttle_WeaponAmmo_SourceUnavailable".Translate().ToString());
            }

            if (supply.CountAvailable(ammoThingDef) < requestedCount)
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    false,
                    "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString());
            }

            int loadedBefore = magazine.LoadedAmmoCount;
            if (!magazineDriver.TryLoadExact(magazine, requestedCount))
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    false,
                    "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString());
            }

            int consumedCount;
            string failureReason;
            bool sourceSucceeded;
            try
            {
                sourceSucceeded = supply.TryConsume(
                    ammoThingDef,
                    requestedCount,
                    reason,
                    out consumedCount,
                    out failureReason);
            }
            catch (Exception)
            {
                bool restoredAfterException = this.TryRestoreStagedMagazine(
                    magazineDriver,
                    magazine,
                    loadedBefore,
                    requestedCount);
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    !restoredAfterException,
                    restoredAfterException
                        ? "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString()
                        : MagazineRollbackFailure);
            }
            if (consumedCount < 0 || consumedCount > requestedCount)
            {
                bool restoredInvalid = this.TryRestoreStagedMagazine(
                    magazineDriver,
                    magazine,
                    loadedBefore,
                    requestedCount);
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    !restoredInvalid,
                    restoredInvalid
                        ? "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString()
                        : MagazineRollbackFailure);
            }

            int uncommittedCount = requestedCount - consumedCount;
            if (uncommittedCount > 0 &&
                !magazineDriver.TryRollbackLoadedExact(magazine, uncommittedCount))
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    true,
                    MagazineRollbackFailure);
            }

            if (magazine.LoadedAmmoCount - loadedBefore != consumedCount)
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    true,
                    MagazineRollbackFailure);
            }

            if (consumedCount <= 0)
            {
                return ShuttleWeaponAmmoTransferResult.Failed(
                    requestedCount,
                    false,
                    !string.IsNullOrEmpty(failureReason)
                        ? failureReason
                        : "CT_Shuttle_WeaponAmmo_SourceEmpty".Translate().ToString());
            }

            return ShuttleWeaponAmmoTransferResult.Completed(
                requestedCount,
                consumedCount,
                sourceSucceeded ? null : failureReason);
        }

        private bool TryRestoreStagedMagazine(
            CoreMagazineDriver magazineDriver,
            ShuttleWeaponAmmoState magazine,
            int loadedBefore,
            int stagedCount)
        {
            return magazineDriver != null &&
                magazine != null &&
                magazineDriver.TryRollbackLoadedExact(magazine, stagedCount) &&
                magazine.LoadedAmmoCount == loadedBefore;
        }
    }
}
