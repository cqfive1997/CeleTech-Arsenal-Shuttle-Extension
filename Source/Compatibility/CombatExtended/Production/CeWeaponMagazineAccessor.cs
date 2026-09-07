using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponMagazineAccessor
    {
        internal bool TryResolve(
            ThingWithComps gun,
            CeWeaponCompatibilitySpec spec,
            out CompAmmoUser magazine,
            out AmmoDef ammoDef,
            out string failure)
        {
            magazine = null;
            ammoDef = null;
            failure = null;
            if (gun == null || spec == null)
            {
                failure = "runtime-gun-or-spec-missing";
                return false;
            }

            magazine = gun.TryGetComp<CompAmmoUser>();
            ammoDef = DefDatabase<AmmoDef>.GetNamedSilentFail(spec.AmmoDefName);
            if (magazine == null)
            {
                failure = "ce-magazine-missing";
                return false;
            }

            if (ammoDef == null)
            {
                failure = "ce-ammo-def-missing";
                return false;
            }

            if (!magazine.UseAmmo || !magazine.HasMagazine)
            {
                failure = "ce-magazine-inactive";
                return false;
            }

            if (magazine.MagSize != spec.MagazineCapacity)
            {
                failure = "ce-magazine-capacity-mismatch";
                return false;
            }

            if (magazine.CurAmmoSet == null ||
                magazine.CurAmmoSet.defName != spec.AmmoSetDefName ||
                !Supports(magazine, ammoDef, spec.ProjectileDefName))
            {
                failure = "ce-ammo-set-mismatch";
                return false;
            }

            return true;
        }

        internal bool TryStageExact(
            CompAmmoUser magazine,
            AmmoDef ammoDef,
            int loadedCount,
            out string failure)
        {
            failure = null;
            if (magazine == null || ammoDef == null)
            {
                failure = "ce-magazine-or-ammo-missing";
                return false;
            }

            if (loadedCount < 0 || loadedCount > magazine.MagSize)
            {
                failure = "source-count-out-of-range";
                return false;
            }

            AmmoDef previousCurrent = magazine.CurrentAmmo;
            AmmoDef previousSelected = magazine.SelectedAmmo;
            int previousCount = magazine.CurMagCount;

            magazine.CurrentAmmo = ammoDef;
            magazine.SelectedAmmo = ammoDef;
            magazine.CurMagCount = loadedCount;
            if (magazine.CurrentAmmo == ammoDef &&
                magazine.SelectedAmmo == ammoDef &&
                magazine.CurMagCount == loadedCount)
            {
                return true;
            }

            magazine.CurrentAmmo = previousCurrent;
            magazine.SelectedAmmo = previousSelected;
            magazine.CurMagCount = previousCount;
            failure = "ce-magazine-rejected-exact-state";
            return false;
        }

        private static bool Supports(
            CompAmmoUser magazine,
            AmmoDef ammoDef,
            string projectileDefName)
        {
            if (magazine.CurAmmoSet == null || magazine.CurAmmoSet.ammoTypes == null)
            {
                return false;
            }

            for (int i = 0; i < magazine.CurAmmoSet.ammoTypes.Count; i++)
            {
                AmmoLink link = magazine.CurAmmoSet.ammoTypes[i];
                if (link != null &&
                    link.ammo == ammoDef &&
                    link.projectile != null &&
                    link.projectile.defName == projectileDefName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
