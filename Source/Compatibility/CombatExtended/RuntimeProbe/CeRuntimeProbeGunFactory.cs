using System;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeGunFactory
    {
        internal static bool TryCreate(
            CeRuntimeProbeCandidate candidate,
            int initialLoadedCount,
            bool useBurstFire,
            out ThingWithComps gun,
            out string failure)
        {
            gun = null;
            failure = null;
            if (candidate == null || candidate.WeaponDef == null)
            {
                failure = "No CE weapon candidate was supplied.";
                return false;
            }

            try
            {
                gun = ThingMaker.MakeThing(candidate.WeaponDef) as ThingWithComps;
            }
            catch (Exception exception)
            {
                failure = "ThingMaker failed: " + exception.GetType().Name + ": " + exception.Message;
                return false;
            }

            CompEquippable equippable = gun != null
                ? gun.TryGetComp<CompEquippable>()
                : null;
            Verb_ShootCE verb = equippable != null
                ? equippable.PrimaryVerb as Verb_ShootCE
                : null;
            CompAmmoUser ammo = gun != null ? gun.TryGetComp<CompAmmoUser>() : null;
            if (gun == null || equippable == null || verb == null || ammo == null)
            {
                failure = "The resolved Def did not construct a CE shoot Verb with CompAmmoUser.";
                gun = null;
                return false;
            }

            if (candidate.InitialAmmo != null)
            {
                ammo.SelectedAmmo = candidate.InitialAmmo;
            }

            ammo.CurMagCount = initialLoadedCount >= 0
                ? Math.Min(initialLoadedCount, ammo.MagSize)
                : ammo.MagSize;

            CompFireModes modes = gun.TryGetComp<CompFireModes>();
            if (modes != null)
            {
                FireMode requestedMode = useBurstFire
                    ? FireMode.BurstFire
                    : FireMode.SingleFire;
                if (modes.AvailableFireModes == null ||
                    !modes.AvailableFireModes.Contains(requestedMode))
                {
                    failure = useBurstFire
                        ? "The selected CE candidate has no optional short BurstFire mode. " +
                            "Use 'CE production burst boundary' for the shuttle weapon's full authored burst."
                        : "The CE weapon does not expose the requested " +
                            requestedMode + " mode.";
                    gun = null;
                    return false;
                }

                modes.CurrentFireMode = requestedMode;

                if (modes.AvailableAimModes != null &&
                    modes.AvailableAimModes.Contains(AimMode.Snapshot))
                {
                    modes.CurrentAimMode = AimMode.Snapshot;
                }
            }
            else if (useBurstFire)
            {
                failure = "The selected CE candidate has no CompFireModes for the legacy " +
                    "short-burst probe.";
                gun = null;
                return false;
            }

            return true;
        }
    }
}
