using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Preserves common player policy settings and queues reload work for the CE-owned magazine.
    /// Ammo switching remains fail-closed because the authored weapons expose one ammo type each.
    /// </summary>
    internal sealed class CeWeaponAmmoCommandDriver : IShuttleWeaponAmmoCommandDriver
    {
        private readonly CeWeaponReloadDriver reloadDriver;

        internal CeWeaponAmmoCommandDriver(CeWeaponReloadDriver reloadDriver)
        {
            this.reloadDriver = reloadDriver;
        }

        public bool TrySelectAmmo(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            string ammoDefName,
            out string failureReason)
        {
            failureReason = null;
            CompAmmoUser magazine = GetMagazine(weaponState);
            AmmoDef selected = magazine != null
                ? magazine.SelectedAmmo ?? magazine.CurrentAmmo
                : null;
            if (selected != null && selected.defName == ammoDefName)
            {
                return true;
            }

            failureReason = "CT_Shuttle_WeaponAmmo_Incompatible".Translate().ToString();
            return false;
        }

        public bool TryRequestReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            ShuttleWeaponReloadRequestKind requestKind,
            out string failureReason)
        {
            if (this.reloadDriver == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ReloadFailed".Translate().ToString();
                return false;
            }

            return this.reloadDriver.TryRequest(
                weaponDef,
                weaponState,
                requestKind,
                out failureReason);
        }

        public bool TryCancelReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            out string failureReason)
        {
            failureReason = null;
            if (!IsAuthoritative(weaponState))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            // Prevent an empty auto-fire weapon from recreating RequiredForFire immediately after
            // the player cancels. Explicit Reload remains available while automatic refill is off.
            weaponState.AmmoForRuntimeOnly.SetAutoReloadEnabled(false);
            weaponState.AmmoForRuntimeOnly.CancelReload();
            return true;
        }

        public bool TrySetAutoReloadEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            failureReason = null;
            if (!IsAuthoritative(weaponState))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
                return false;
            }

            weaponState.AmmoForRuntimeOnly.SetAutoReloadEnabled(enabled);
            return true;
        }

        public bool TrySetManualReloadAllowed(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponModuleAmmoExtension extension = GetExtension(weaponDef);
            if (extension == null || !extension.allowManualReload ||
                !IsAuthoritative(weaponState))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ManualReloadUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            failureReason = null;
            weaponState.AmmoForRuntimeOnly.SetManualReloadAllowed(enabled);
            return true;
        }

        public bool TrySetLogisticsAutoFeedEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponModuleAmmoExtension extension = GetExtension(weaponDef);
            if (extension == null || !extension.logisticsAutoFeedEligible ||
                !IsAuthoritative(weaponState))
            {
                failureReason = "CT_Shuttle_WeaponAmmo_LogisticsUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            failureReason = null;
            weaponState.AmmoForRuntimeOnly.SetLogisticsAutoFeedEnabled(enabled);
            return true;
        }

        private static bool IsAuthoritative(ShuttleWeaponRuntimeState state)
        {
            return state != null &&
                state.MagazineAuthorityBackendIdForRuntimeOnly == CeWeaponBackendFactory.Id &&
                GetMagazine(state) != null;
        }

        private static CompAmmoUser GetMagazine(ShuttleWeaponRuntimeState state)
        {
            return state != null
                ? CeWeaponRuntimeGunAccess.GetMagazine(state.GunForRuntimeOnly)
                : null;
        }

        private static ShuttleWeaponModuleAmmoExtension GetExtension(
            ShuttleWeaponModuleDef weaponDef)
        {
            return weaponDef != null
                ? weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>()
                : null;
        }
    }
}
