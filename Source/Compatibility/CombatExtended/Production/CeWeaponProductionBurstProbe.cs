using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Stages and starts one production-component burst without committing magazine authority.
    /// Per-tick ownership is handed to a focused probe session after the cast is accepted.
    /// </summary>
    internal sealed class CeWeaponProductionBurstProbe
    {
        private readonly CeWeaponMagazineAuthorityTransfer transfer =
            new CeWeaponMagazineAuthorityTransfer();
        private readonly CeWeaponShotLifecycle shotLifecycle =
            new CeWeaponShotLifecycle();

        internal CeWeaponProductionBurstProbeSession CreateAndStart(
            Thing host,
            string moduleInstanceID,
            string parentSlotID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            LocalTargetInfo target)
        {
            CeWeaponProductionBurstProbeReport report =
                new CeWeaponProductionBurstProbeReport();
            report.ModuleInstanceID = moduleInstanceID;
            report.ModuleDefName = weaponDef != null ? weaponDef.defName : null;
            report.HostRotation = host != null ? host.Rotation.AsInt : -1;

            ShuttleWeaponAmmoState sourceAmmo = state != null
                ? state.AmmoForRuntimeOnly
                : null;
            string sourceAmmoDefName = sourceAmmo != null
                ? sourceAmmo.SelectedAmmoDefName
                : null;
            report.SourceLoadedBefore = sourceAmmo != null
                ? sourceAmmo.LoadedAmmoCount
                : -1;

            if (state == null)
            {
                return Failed(report, sourceAmmo, sourceAmmoDefName,
                    "installed-weapon-state-missing");
            }

            CeWeaponPreparedMagazineTransfer prepared;
            string failureReason;
            if (!this.transfer.TryPrepare(
                    weaponDef,
                    state,
                    out prepared,
                    out failureReason))
            {
                return Failed(report, sourceAmmo, sourceAmmoDefName, failureReason);
            }

            CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(
                prepared.PreparedGun);
            report.CeLoadedBefore = magazine != null ? magazine.CurMagCount : -1;
            CeWeaponProductionBurstProbeSession session =
                new CeWeaponProductionBurstProbeSession(
                    report,
                    sourceAmmo,
                    sourceAmmoDefName,
                    prepared.PreparedGun,
                    magazine);
            if (magazine == null)
            {
                session.Fail("ce-probe-magazine-missing");
                return session;
            }

            CeWeaponMuzzleVerb verb;
            CeWeaponAmmoOwnerAdapter owner;
            if (!this.shotLifecycle.TryBind(
                    prepared.PreparedGun,
                    host,
                    moduleInstanceID,
                    parentSlotID,
                    weaponDef,
                    state,
                    session.NotifyCastComplete,
                    out verb,
                    out owner,
                    out failureReason))
            {
                session.Attach(verb, owner);
                session.Fail(failureReason);
                return session;
            }

            session.Attach(verb, owner);
            report.OwnerReady = owner != null && owner.ContextMatches(host);
            int expectedShots;
            if (!this.shotLifecycle.TrySetFullBurstMode(
                    prepared.PreparedGun,
                    verb,
                    AimMode.Snapshot,
                    out expectedShots,
                    out failureReason))
            {
                session.Fail(failureReason);
                return session;
            }

            report.ExpectedShots = expectedShots;

            if (magazine.CurMagCount < report.ExpectedShots)
            {
                session.Fail("ce-probe-insufficient-ammo-for-full-burst");
                return session;
            }

            session.Begin(this.shotLifecycle, host, target);
            return session;
        }

        private static CeWeaponProductionBurstProbeSession Failed(
            CeWeaponProductionBurstProbeReport report,
            ShuttleWeaponAmmoState sourceAmmo,
            string sourceAmmoDefName,
            string failureReason)
        {
            CeWeaponProductionBurstProbeSession session =
                new CeWeaponProductionBurstProbeSession(
                    report,
                    sourceAmmo,
                    sourceAmmoDefName,
                    null,
                    null);
            session.Fail(failureReason);
            return session;
        }
    }
}
