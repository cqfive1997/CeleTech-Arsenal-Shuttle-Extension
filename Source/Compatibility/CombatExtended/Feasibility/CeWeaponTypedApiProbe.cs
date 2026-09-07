using System;
using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.Feasibility
{
    /// <summary>
    /// Compile-only proof of the CE 16.7.3.0 API surface needed by a later shuttle backend.
    /// This type is not registered, packaged, or called by the production mod.
    /// </summary>
    internal static class CeWeaponTypedApiProbe
    {
        internal static CeWeaponTypedApiSnapshot Inspect(ThingWithComps gun)
        {
            if (gun == null)
            {
                return CeWeaponTypedApiSnapshot.Empty;
            }

            CompEquippable equippable = gun.TryGetComp<CompEquippable>();
            CompAmmoUser ammo = gun.TryGetComp<CompAmmoUser>();
            CompFireModes modes = gun.TryGetComp<CompFireModes>();
            Verb_LaunchProjectileCE launchVerb = equippable != null
                ? equippable.PrimaryVerb as Verb_LaunchProjectileCE
                : null;
            Verb_ShootCE shootVerb = launchVerb as Verb_ShootCE;

            return new CeWeaponTypedApiSnapshot(
                launchVerb,
                shootVerb,
                ammo,
                modes,
                ammo != null ? ammo.Holder : null,
                ammo != null ? ammo.CurMagCount : 0,
                ammo != null ? ammo.MagSize : 0,
                ammo != null ? ammo.SelectedAmmo : null,
                modes != null ? modes.CurrentFireMode : FireMode.AutoFire,
                modes != null ? modes.CurrentAimMode : AimMode.Snapshot);
        }

        internal static void BindHiddenGun(
            Verb_LaunchProjectileCE verb,
            Thing caster,
            Action castCompleteCallback)
        {
            if (verb == null)
            {
                return;
            }

            verb.caster = caster;
            verb.castCompleteCallback = castCompleteCallback;
        }

        internal static bool CanHitFrom(
            Verb_LaunchProjectileCE verb,
            IntVec3 sourceCell,
            LocalTargetInfo target)
        {
            return verb != null &&
                verb.Available() &&
                verb.CanHitTargetFrom(sourceCell, target);
        }

        internal static bool TryStartCast(
            Verb_LaunchProjectileCE verb,
            LocalTargetInfo target)
        {
            return verb != null &&
                verb.TryStartCastOn(target, false, true, false, false);
        }

        internal static void TickCeVerb(Verb_LaunchProjectileCE verb)
        {
            if (verb != null)
            {
                verb.VerbTickCE();
            }
        }

        internal static void SetMuzzleDrawPosition(
            Verb_ShootCE verb,
            Vector3 drawPosition)
        {
            if (verb != null)
            {
                verb.drawPos = drawPosition;
            }
        }

        internal static bool TryPrepareShot(CompAmmoUser ammo)
        {
            return ammo != null && ammo.TryPrepareShot();
        }

        internal static void NotifyShotFired(CompAmmoUser ammo, int ammoConsumed)
        {
            if (ammo != null)
            {
                ammo.Notify_ShotFired(ammoConsumed);
            }
        }

        internal static void SetMagazineAuthority(
            CompAmmoUser ammo,
            AmmoDef selectedAmmo,
            int loadedCount)
        {
            if (ammo == null)
            {
                return;
            }

            ammo.SelectedAmmo = selectedAmmo;
            ammo.CurMagCount = loadedCount;
        }

        internal static Building_Turret ReadCeTurretOwner(CompAmmoUser ammo)
        {
            return ammo != null ? ammo.turret : null;
        }

        internal static void SetModes(
            CompFireModes modes,
            FireMode fireMode,
            AimMode aimMode)
        {
            if (modes == null)
            {
                return;
            }

            modes.CurrentFireMode = fireMode;
            modes.CurrentAimMode = aimMode;
        }

        internal static IList<FireMode> ReadAvailableFireModes(CompFireModes modes)
        {
            return modes != null ? modes.AvailableFireModes : null;
        }

        internal static IList<AimMode> ReadAvailableAimModes(CompFireModes modes)
        {
            return modes != null ? modes.AvailableAimModes : null;
        }
    }

    /// <summary>
    /// Compile-only proof that a conditional CE Verb can replace both the shoot-line root and
    /// the final projectile source calculation without reflecting private CE fields.
    /// </summary>
    internal sealed class CeMuzzleOverrideShapeProbe : Verb_ShootCE
    {
        public override bool TryFindCEShootLineFromTo(
            IntVec3 root,
            LocalTargetInfo target,
            out ShootLine resultingLine,
            out Vector3 targetPosition)
        {
            return base.TryFindCEShootLineFromTo(
                root,
                target,
                out resultingLine,
                out targetPosition);
        }

        public override void ShiftTarget(
            ShiftVecReport report,
            Vector3 targetPosition,
            out float targetHeight,
            bool calculateMechanicalOnly = false,
            bool isInstant = false)
        {
            base.ShiftTarget(
                report,
                targetPosition,
                out targetHeight,
                calculateMechanicalOnly,
                isInstant);
        }
    }

    internal sealed class CeWeaponTypedApiSnapshot
    {
        internal static readonly CeWeaponTypedApiSnapshot Empty =
            new CeWeaponTypedApiSnapshot(
                null,
                null,
                null,
                null,
                null,
                0,
                0,
                null,
                FireMode.AutoFire,
                AimMode.Snapshot);

        internal CeWeaponTypedApiSnapshot(
            Verb_LaunchProjectileCE launchVerb,
            Verb_ShootCE shootVerb,
            CompAmmoUser ammo,
            CompFireModes modes,
            Pawn ammoHolder,
            int loadedCount,
            int magazineSize,
            AmmoDef selectedAmmo,
            FireMode fireMode,
            AimMode aimMode)
        {
            this.LaunchVerb = launchVerb;
            this.ShootVerb = shootVerb;
            this.Ammo = ammo;
            this.Modes = modes;
            this.AmmoHolder = ammoHolder;
            this.LoadedCount = loadedCount;
            this.MagazineSize = magazineSize;
            this.SelectedAmmo = selectedAmmo;
            this.FireMode = fireMode;
            this.AimMode = aimMode;
        }

        internal Verb_LaunchProjectileCE LaunchVerb { get; private set; }

        internal Verb_ShootCE ShootVerb { get; private set; }

        internal CompAmmoUser Ammo { get; private set; }

        internal CompFireModes Modes { get; private set; }

        internal Pawn AmmoHolder { get; private set; }

        internal int LoadedCount { get; private set; }

        internal int MagazineSize { get; private set; }

        internal AmmoDef SelectedAmmo { get; private set; }

        internal FireMode FireMode { get; private set; }

        internal AimMode AimMode { get; private set; }
    }
}
