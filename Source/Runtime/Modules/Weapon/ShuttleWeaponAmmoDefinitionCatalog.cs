using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Read-only lookup boundary for authored shuttle ammunition definitions. It owns no runtime
    /// state and performs no magazine, reload, cargo or command mutation.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoDefinitionCatalog
    {
        internal ShuttleWeaponModuleAmmoExtension GetExtension(
            ShuttleWeaponModuleDef weaponDef)
        {
            return weaponDef != null
                ? weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>()
                : null;
        }

        internal bool HasAmmoSystem(ShuttleWeaponModuleDef weaponDef)
        {
            ShuttleWeaponModuleAmmoExtension extension = this.GetExtension(weaponDef);
            return extension != null &&
                extension.ammoSet != null &&
                extension.ammoSet.defaultAmmo != null &&
                extension.magazineCapacity > 0 &&
                extension.ammoPerShot > 0;
        }

        internal ShuttleWeaponAmmoDef GetSelectedAmmo(
            ShuttleWeaponModuleDef weaponDef,
            string selectedAmmoDefName)
        {
            ShuttleWeaponModuleAmmoExtension extension = this.GetExtension(weaponDef);
            if (extension == null || extension.ammoSet == null)
            {
                return null;
            }

            ShuttleWeaponAmmoDef selected = this.FindAmmo(
                extension.ammoSet,
                selectedAmmoDefName);
            return selected ?? extension.ammoSet.defaultAmmo;
        }

        internal ShuttleWeaponAmmoDef FindAmmo(
            ShuttleWeaponModuleDef weaponDef,
            string ammoDefName)
        {
            ShuttleWeaponModuleAmmoExtension extension = this.GetExtension(weaponDef);
            return extension != null
                ? this.FindAmmo(extension.ammoSet, ammoDefName)
                : null;
        }

        internal List<ShuttleWeaponAmmoDef> GetOptions(
            ShuttleWeaponModuleDef weaponDef)
        {
            List<ShuttleWeaponAmmoDef> options = new List<ShuttleWeaponAmmoDef>();
            ShuttleWeaponModuleAmmoExtension extension = this.GetExtension(weaponDef);
            if (extension == null || extension.ammoSet == null ||
                extension.ammoSet.ammoDefs == null)
            {
                return options;
            }

            for (int i = 0; i < extension.ammoSet.ammoDefs.Count; i++)
            {
                ShuttleWeaponAmmoDef ammoDef = extension.ammoSet.ammoDefs[i];
                if (ammoDef != null)
                {
                    options.Add(ammoDef);
                }
            }

            return options;
        }

        private ShuttleWeaponAmmoDef FindAmmo(
            ShuttleWeaponAmmoSetDef ammoSet,
            string ammoDefName)
        {
            if (ammoSet == null || ammoSet.ammoDefs == null ||
                string.IsNullOrEmpty(ammoDefName))
            {
                return null;
            }

            for (int i = 0; i < ammoSet.ammoDefs.Count; i++)
            {
                ShuttleWeaponAmmoDef ammoDef = ammoSet.ammoDefs[i];
                if (ammoDef != null && ammoDef.defName == ammoDefName)
                {
                    return ammoDef;
                }
            }

            return null;
        }
    }
}
