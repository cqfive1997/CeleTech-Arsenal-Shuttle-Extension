using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Sole runtime mutation boundary for the non-CE core magazine. It owns no reload timing,
    /// supply selection, cargo access, Pawn Job, firing, targeting or UI behavior.
    /// </summary>
    internal sealed class CoreMagazineDriver
    {
        private readonly ShuttleWeaponAmmoDefinitionCatalog definitions;
        private readonly ShuttleWeaponAmmoStateInitializer stateInitializer;

        internal CoreMagazineDriver(
            ShuttleWeaponAmmoDefinitionCatalog definitions)
        {
            this.definitions = definitions ?? new ShuttleWeaponAmmoDefinitionCatalog();
            this.stateInitializer = new ShuttleWeaponAmmoStateInitializer(this.definitions);
        }

        internal ShuttleWeaponAmmoState GetOrCreate(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState)
        {
            return this.stateInitializer.GetOrCreate(
                moduleInstanceID,
                weaponDef,
                weaponState);
        }

        internal int GetEffectiveCapacity(int authoredCapacity)
        {
            return this.stateInitializer.GetEffectiveMagazineCapacity(authoredCapacity);
        }

        internal int GetNeededLoadCount(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState magazine)
        {
            ShuttleWeaponModuleAmmoExtension extension =
                this.definitions.GetExtension(weaponDef);
            if (extension == null || magazine == null)
            {
                return 0;
            }

            int capacity = magazine.MagazineCapacity > 0
                ? magazine.MagazineCapacity
                : this.GetEffectiveCapacity(extension.magazineCapacity);
            return capacity > magazine.LoadedAmmoCount
                ? capacity - magazine.LoadedAmmoCount
                : 0;
        }

        internal ShuttleWeaponAmmoDef GetSelectedAmmo(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState magazine)
        {
            return this.definitions.GetSelectedAmmo(
                weaponDef,
                magazine != null ? magazine.SelectedAmmoDefName : null);
        }

        internal bool TrySelectAmmo(
            string ammoDefName,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState magazine,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleAmmoExtension extension =
                this.definitions.GetExtension(weaponDef);
            if (extension == null || extension.ammoSet == null || magazine == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            if (!extension.ammoSet.allowAmmoSwitching)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            ShuttleWeaponAmmoDef ammoDef = this.definitions.FindAmmo(
                weaponDef,
                ammoDefName);
            if (ammoDef == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
                return false;
            }

            magazine.ChangeSelectedAmmoAndClearMagazine(ammoDef.defName);
            return true;
        }

        internal bool CanFire(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState magazine)
        {
            if (!this.definitions.HasAmmoSystem(weaponDef))
            {
                return true;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.definitions.GetExtension(weaponDef);
            return magazine != null &&
                !magazine.ReloadInProgress &&
                magazine.LoadedAmmoCount >= extension.ammoPerShot;
        }

        internal bool TryReserveFireCycle(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState magazine,
            out string failureReason)
        {
            failureReason = null;
            if (!this.definitions.HasAmmoSystem(weaponDef))
            {
                return true;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.definitions.GetExtension(weaponDef);
            if (magazine == null ||
                !magazine.TryConsumeLoadedAmmo(extension.ammoPerShot))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NotEnoughAmmo".Translate().ToString();
                if (magazine != null)
                {
                    magazine.SetLastFailureReason(failureReason);
                }

                return false;
            }

            return true;
        }

        internal bool TryRestoreFireCycleReservation(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponAmmoState magazine)
        {
            if (!this.definitions.HasAmmoSystem(weaponDef))
            {
                return true;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.definitions.GetExtension(weaponDef);
            return magazine != null &&
                magazine.TryAddLoadedAmmoExact(extension.ammoPerShot);
        }

        internal bool TryLoadExact(ShuttleWeaponAmmoState magazine, int count)
        {
            return magazine != null && magazine.TryAddLoadedAmmoExact(count);
        }

        internal bool TryRollbackLoadedExact(ShuttleWeaponAmmoState magazine, int count)
        {
            return magazine != null && magazine.TryConsumeLoadedAmmo(count);
        }
    }
}
