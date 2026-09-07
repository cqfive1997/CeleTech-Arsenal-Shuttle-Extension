using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Exercises the installed-state commit boundary and rolls it back before returning. No game
    /// tick, save operation, backend resolution or cargo mutation runs inside this synchronous
    /// probe.
    /// </summary>
    internal sealed class CeWeaponMagazineAuthorityTransferProbe
    {
        private const string LegacyBackendId = "legacy";

        private readonly CeWeaponMagazineAuthorityTransfer transfer =
            new CeWeaponMagazineAuthorityTransfer();

        internal CeWeaponMagazineAuthorityTransferProbeReport Run(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            CeWeaponMagazineAuthorityTransferProbeReport report =
                new CeWeaponMagazineAuthorityTransferProbeReport();
            report.ModuleInstanceID = moduleInstanceID;
            report.ModuleDefName = weaponDef != null ? weaponDef.defName : null;
            if (state == null)
            {
                report.Failure = "installed-weapon-state-missing";
                return report;
            }

            Thing sourceGun = state.GunForRuntimeOnly;
            ShuttleWeaponAmmoState sourceAmmo = state.AmmoForRuntimeOnly;
            string sourceAmmoDefName = sourceAmmo.SelectedAmmoDefName;
            int sourceLoadedCount = sourceAmmo.LoadedAmmoCount;
            report.AmmoDefName = sourceAmmoDefName;
            report.SourceLoadedCount = sourceLoadedCount;
            report.AuthorityBefore = state.MagazineAuthorityBackendIdForRuntimeOnly;

            CeWeaponPreparedMagazineTransfer prepared;
            string failure;
            if (!this.transfer.TryPrepare(
                    weaponDef,
                    state,
                    out prepared,
                    out failure))
            {
                report.Failure = failure;
                return report;
            }

            if (!this.transfer.TryCommit(state, prepared, out failure))
            {
                report.Failure = failure;
                return report;
            }

            report.CommitAccepted = true;
            report.AuthorityAfterCommit = state.MagazineAuthorityBackendIdForRuntimeOnly;
            report.RollbackAccepted = state.TryRollbackMagazineAuthorityTransferForRuntimeOnly(
                CeWeaponBackendFactory.Id,
                LegacyBackendId,
                prepared.PreparedGun,
                sourceGun,
                sourceAmmoDefName,
                sourceLoadedCount);
            report.AuthorityAfterRollback = state.MagazineAuthorityBackendIdForRuntimeOnly;
            report.SourceRestored = report.RollbackAccepted &&
                report.AuthorityAfterRollback == report.AuthorityBefore &&
                ReferenceEquals(state.GunForRuntimeOnly, sourceGun) &&
                sourceAmmo.SelectedAmmoDefName == sourceAmmoDefName &&
                sourceAmmo.LoadedAmmoCount == sourceLoadedCount;
            if (!report.RollbackAccepted)
            {
                report.Failure = "authority-transfer-rollback-failed";
            }
            else if (!report.SourceRestored)
            {
                report.Failure = "authority-transfer-source-not-restored";
            }

            return report;
        }
    }
}
