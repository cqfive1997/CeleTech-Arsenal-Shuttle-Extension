using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Fires one single shot from a transient prepared CE gun. It exercises production owner,
    /// Verb and muzzle components without committing installed magazine authority.
    /// </summary>
    internal sealed class CeWeaponProductionShotProbe
    {
        private const float OriginTolerance = 0.001f;

        private readonly CeWeaponMagazineAuthorityTransfer transfer =
            new CeWeaponMagazineAuthorityTransfer();
        private readonly CeWeaponShotLifecycle shotLifecycle =
            new CeWeaponShotLifecycle();

        internal CeWeaponProductionShotProbeReport Run(
            Thing host,
            string moduleInstanceID,
            string parentSlotID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            LocalTargetInfo target)
        {
            CeWeaponProductionShotProbeReport report =
                new CeWeaponProductionShotProbeReport();
            report.ModuleInstanceID = moduleInstanceID;
            report.ModuleDefName = weaponDef != null ? weaponDef.defName : null;
            if (state == null)
            {
                report.Failure = "installed-weapon-state-missing";
                return report;
            }

            ShuttleWeaponAmmoState sourceAmmo = state.AmmoForRuntimeOnly;
            string sourceAmmoDefName = sourceAmmo.SelectedAmmoDefName;
            report.SourceLoadedBefore = sourceAmmo.LoadedAmmoCount;

            CeWeaponPreparedMagazineTransfer prepared;
            string failureReason;
            if (!this.transfer.TryPrepare(
                    weaponDef,
                    state,
                    out prepared,
                    out failureReason))
            {
                report.Failure = failureReason;
                return report;
            }

            CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(prepared.PreparedGun);
            report.CeLoadedBefore = magazine != null ? magazine.CurMagCount : -1;
            if (magazine == null || magazine.CurMagCount <= 0)
            {
                report.Failure = "ce-probe-magazine-empty";
                return report;
            }

            int callbackCount = 0;
            CeWeaponMuzzleVerb verb;
            CeWeaponAmmoOwnerAdapter owner;
            if (!this.shotLifecycle.TryBind(
                    prepared.PreparedGun,
                    host,
                    moduleInstanceID,
                    parentSlotID,
                    weaponDef,
                    state,
                    delegate { callbackCount++; },
                    out verb,
                    out owner,
                    out failureReason))
            {
                report.Failure = failureReason;
                if (owner != null)
                {
                    owner.Release(magazine);
                }

                CeWeaponRuntimeGunAccess.ReleaseVerb(prepared.PreparedGun);
                return report;
            }

            report.OwnerReady = owner != null && owner.ContextMatches(host);
            if (!this.shotLifecycle.TrySetFireMode(
                    prepared.PreparedGun,
                    FireMode.SingleFire,
                    AimMode.Snapshot,
                    out failureReason))
            {
                report.Failure = failureReason;
                owner.Release(magazine);
                CeWeaponRuntimeGunAccess.ReleaseVerb(prepared.PreparedGun);
                return report;
            }

            report.CastAccepted = this.shotLifecycle.TryStart(
                verb,
                target,
                out failureReason);
            report.CompletionCallbacks = callbackCount;
            report.CeLoadedAfter = magazine.CurMagCount;
            report.SourceLoadedAfter = sourceAmmo.LoadedAmmoCount;
            report.SourceUnchanged =
                sourceAmmo.SelectedAmmoDefName == sourceAmmoDefName &&
                report.SourceLoadedAfter == report.SourceLoadedBefore;
            report.MuzzleCell = verb.LastMuzzleCell;
            report.MuzzleDrawPos = verb.LastMuzzleDrawPos;
            report.ProjectileOriginObserved = verb.LastProjectileOriginObserved;
            if (report.ProjectileOriginObserved)
            {
                Vector2 origin = verb.LastProjectileOrigin;
                report.ProjectileOriginMatches =
                    Mathf.Abs(origin.x - report.MuzzleDrawPos.x) <= OriginTolerance &&
                    Mathf.Abs(origin.y - report.MuzzleDrawPos.z) <= OriginTolerance;
            }

            if (!report.CastAccepted)
            {
                report.Failure = failureReason;
                report.FailureTrace = CeWeaponShotFailureTrace.Capture(
                    host,
                    verb,
                    target);
            }
            else if (callbackCount != 1)
            {
                report.Failure = "ce-single-shot-not-complete-immediately";
            }
            else if (report.CeLoadedBefore - report.CeLoadedAfter != 1)
            {
                report.Failure = "ce-single-shot-ammo-delta-mismatch";
            }
            else if (!report.SourceUnchanged)
            {
                report.Failure = "legacy-source-mutated-by-ce-probe";
            }
            else if (!report.ProjectileOriginObserved || !report.ProjectileOriginMatches)
            {
                report.Failure = "ce-projectile-origin-mismatch";
            }

            owner.Release(magazine);
            CeWeaponRuntimeGunAccess.ReleaseVerb(prepared.PreparedGun);
            return report;
        }
    }
}
