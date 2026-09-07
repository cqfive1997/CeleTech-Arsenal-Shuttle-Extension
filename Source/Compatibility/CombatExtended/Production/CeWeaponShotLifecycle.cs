using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Binds and starts a CE shot on an already-authoritative runtime gun. Target selection,
    /// cooldown, power admission, reload policy and per-tick orchestration remain outside it.
    /// </summary>
    internal sealed class CeWeaponShotLifecycle
    {
        private readonly CeWeaponMuzzleVerbInstaller verbInstaller =
            new CeWeaponMuzzleVerbInstaller();

        internal bool TryBind(
            Thing gun,
            Thing host,
            string moduleInstanceID,
            string parentSlotID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState,
            Action completionCallback,
            out CeWeaponMuzzleVerb verb,
            out CeWeaponAmmoOwnerAdapter owner,
            out string failureReason)
        {
            verb = null;
            owner = null;
            failureReason = null;
            CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(gun);
            if (!CeWeaponAmmoOwnerAdapter.TryEnsure(
                    magazine,
                    host,
                    out owner,
                    out failureReason))
            {
                return false;
            }

            if (!this.verbInstaller.TryEnsureInstalled(
                    gun,
                    moduleInstanceID,
                    parentSlotID,
                    weaponDef,
                    runtimeState,
                    out verb,
                    out failureReason) ||
                !CeWeaponRuntimeGunAccess.TryBindVerb(
                    gun,
                    host,
                    moduleInstanceID,
                    parentSlotID,
                    weaponDef,
                    runtimeState,
                    completionCallback,
                    out verb,
                    out failureReason))
            {
                return false;
            }

            return true;
        }

        internal bool TrySetFireMode(
            Thing gun,
            FireMode fireMode,
            AimMode aimMode,
            out string failureReason)
        {
            failureReason = null;
            CompFireModes modes = CeWeaponRuntimeGunAccess.GetFireModes(gun);
            if (modes == null ||
                modes.AvailableFireModes == null ||
                !modes.AvailableFireModes.Contains(fireMode))
            {
                failureReason = "ce-fire-mode-unavailable:" + fireMode;
                return false;
            }

            modes.CurrentFireMode = fireMode;
            if (modes.AvailableAimModes != null &&
                modes.AvailableAimModes.Contains(aimMode))
            {
                modes.CurrentAimMode = aimMode;
            }

            return true;
        }

        internal bool TrySetFullBurstMode(
            Thing gun,
            CeWeaponMuzzleVerb verb,
            AimMode aimMode,
            out int shotsPerBurst,
            out string failureReason)
        {
            shotsPerBurst = 0;
            if (!this.TrySetFireMode(
                    gun,
                    FireMode.AutoFire,
                    aimMode,
                    out failureReason))
            {
                return false;
            }

            shotsPerBurst = verb != null ? verb.ShotsPerBurst : 0;
            if (shotsPerBurst <= 1)
            {
                failureReason = "ce-full-burst-shot-count-invalid";
                return false;
            }

            return true;
        }

        internal bool TryStart(
            CeWeaponMuzzleVerb verb,
            LocalTargetInfo target,
            out string failureReason)
        {
            failureReason = null;
            if (verb == null || !target.IsValid)
            {
                failureReason = "ce-verb-or-target-missing";
                return false;
            }

            if (!verb.TryPrepareMuzzle(target, false, out failureReason))
            {
                return false;
            }

            if (verb.state == VerbState.Bursting)
            {
                failureReason = "ce-verb-already-bursting";
                return false;
            }

            string hitReport;
            if (!verb.CanHitTarget(target, out hitReport))
            {
                failureReason = "ce-cannot-hit-target:" +
                    (string.IsNullOrEmpty(hitReport) ? "unspecified" : hitReport);
                return false;
            }

            try
            {
                if (verb.TryStartCastOn(target, false, true, false, false))
                {
                    return true;
                }

                failureReason = "ce-cast-rejected";
                return false;
            }
            catch (Exception exception)
            {
                failureReason = "ce-cast-exception:" + exception.GetType().Name;
                return false;
            }
        }
    }
}
