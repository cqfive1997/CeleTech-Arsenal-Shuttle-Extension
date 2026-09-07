using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Prepares and synchronously commits the one-time Legacy-to-CE magazine authority transfer.
    /// Cargo, firing, targeting and backend selection remain outside this component.
    /// </summary>
    internal sealed class CeWeaponMagazineAuthorityTransfer
    {
        private const string LegacyBackendId = "legacy";

        private readonly CeWeaponRuntimeGunFactory gunFactory =
            new CeWeaponRuntimeGunFactory();
        private readonly CeWeaponMagazineAccessor magazineAccessor =
            new CeWeaponMagazineAccessor();

        internal bool TryPrepare(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            out CeWeaponPreparedMagazineTransfer prepared,
            out string failureReason)
        {
            prepared = null;
            failureReason = null;
            if (weaponDef == null || state == null)
            {
                failureReason = "installed-weapon-state-missing";
                return false;
            }

            if (state.MagazineAuthorityBackendIdForRuntimeOnly != LegacyBackendId)
            {
                failureReason = "source-magazine-authority-not-legacy";
                return false;
            }

            CeWeaponCompatibilitySpec spec = CeWeaponCompatibilitySpec.ForWeapon(
                weaponDef.defName);
            if (spec == null ||
                weaponDef.weaponDef == null ||
                weaponDef.weaponDef.defName != spec.SourceWeaponDefName)
            {
                failureReason = "installed-weapon-def-mismatch";
                return false;
            }

            ShuttleWeaponAmmoState source = state.AmmoForRuntimeOnly;
            if (source.MagazineCapacity != spec.MagazineCapacity)
            {
                failureReason = "core-magazine-capacity-mismatch";
                return false;
            }

            if (source.SelectedAmmoDefName != spec.AmmoDefName)
            {
                failureReason = "core-selected-ammo-mismatch";
                return false;
            }

            ThingWithComps preparedGun;
            if (!this.gunFactory.TryCreate(spec, out preparedGun, out failureReason))
            {
                return false;
            }

            CompAmmoUser magazine;
            AmmoDef ammoDef;
            if (!this.magazineAccessor.TryResolve(
                    preparedGun,
                    spec,
                    out magazine,
                    out ammoDef,
                    out failureReason) ||
                !this.magazineAccessor.TryStageExact(
                    magazine,
                    ammoDef,
                    source.LoadedAmmoCount,
                    out failureReason))
            {
                return false;
            }

            prepared = new CeWeaponPreparedMagazineTransfer(
                spec,
                state.GunForRuntimeOnly,
                preparedGun,
                source.SelectedAmmoDefName,
                source.LoadedAmmoCount);
            return true;
        }

        internal bool TryCommit(
            ShuttleWeaponRuntimeState state,
            CeWeaponPreparedMagazineTransfer prepared,
            out string failureReason)
        {
            failureReason = null;
            if (state == null ||
                prepared == null ||
                prepared.Spec == null ||
                prepared.PreparedGun == null)
            {
                failureReason = "prepared-magazine-transfer-missing";
                return false;
            }

            CompAmmoUser magazine;
            AmmoDef ammoDef;
            if (!this.magazineAccessor.TryResolve(
                    prepared.PreparedGun,
                    prepared.Spec,
                    out magazine,
                    out ammoDef,
                    out failureReason) ||
                magazine.CurMagCount != prepared.SourceLoadedCount ||
                magazine.CurrentAmmo != ammoDef ||
                magazine.SelectedAmmo != ammoDef)
            {
                if (string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "prepared-magazine-state-changed";
                }

                return false;
            }

            if (!state.TryCommitMagazineAuthorityTransferForRuntimeOnly(
                    LegacyBackendId,
                    CeWeaponBackendFactory.Id,
                    prepared.ExpectedSourceGun,
                    prepared.PreparedGun,
                    prepared.SourceAmmoDefName,
                    prepared.SourceLoadedCount,
                    out failureReason))
            {
                return false;
            }

            if (state.MagazineAuthorityBackendIdForRuntimeOnly == CeWeaponBackendFactory.Id &&
                ReferenceEquals(state.GunForRuntimeOnly, prepared.PreparedGun) &&
                magazine.CurMagCount == prepared.SourceLoadedCount &&
                magazine.CurrentAmmo == ammoDef &&
                magazine.SelectedAmmo == ammoDef)
            {
                return true;
            }

            bool rolledBack = state.TryRollbackMagazineAuthorityTransferForRuntimeOnly(
                CeWeaponBackendFactory.Id,
                LegacyBackendId,
                prepared.PreparedGun,
                prepared.ExpectedSourceGun,
                prepared.SourceAmmoDefName,
                prepared.SourceLoadedCount);
            failureReason = rolledBack
                ? "authority-transfer-postcondition-failed"
                : "authority-transfer-rollback-failed";
            return false;
        }
    }
}
