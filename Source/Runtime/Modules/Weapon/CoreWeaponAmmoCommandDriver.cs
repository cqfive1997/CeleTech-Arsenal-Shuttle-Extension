using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Adapts player-facing ammunition commands to focused magazine and reload boundaries. Pawn reload Job
    /// execution remains a separate manual-reload concern and is intentionally not part of this facet.
    /// </summary>
    internal sealed class CoreWeaponAmmoCommandDriver : IShuttleWeaponAmmoCommandDriver
    {
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponReloadCoordinator reloadCoordinator;

        internal CoreWeaponAmmoCommandDriver(
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponReloadCoordinator reloadCoordinator)
        {
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
            this.reloadCoordinator = reloadCoordinator ??
                new ShuttleWeaponReloadCoordinator();
        }

        public bool TrySelectAmmo(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            string ammoDefName,
            out string failureReason)
        {
            ShuttleWeaponAmmoState ammoState = this.GetAmmoState(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
            return ammoState != null &&
                this.coreMagazine.TrySelectAmmo(
                    ammoDefName,
                    weaponDef,
                    ammoState,
                    out failureReason);
        }

        public bool TryRequestReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            ShuttleWeaponReloadRequestKind requestKind,
            out string failureReason)
        {
            ShuttleWeaponAmmoState ammoState = this.GetAmmoState(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
            if (ammoState == null)
            {
                return false;
            }

            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            return this.reloadCoordinator.TryRequest(
                ammoState,
                extension != null,
                this.coreMagazine.GetNeededLoadCount(weaponDef, ammoState) > 0,
                requestKind,
                "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString(),
                out failureReason);
        }

        public bool TryCancelReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            out string failureReason)
        {
            ShuttleWeaponAmmoState ammoState = this.GetAmmoState(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
            if (ammoState == null)
            {
                return false;
            }

            // Cancel must be durable from the player's point of view. Otherwise an empty weapon
            // linked to auto-fire recreates RequiredForFire on the next tick.
            ammoState.SetAutoReloadEnabled(false);
            return this.reloadCoordinator.Cancel(ammoState);
        }

        public bool TrySetAutoReloadEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponAmmoState ammoState = this.GetAmmoState(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
            if (ammoState == null)
            {
                return false;
            }

            ammoState.SetAutoReloadEnabled(enabled);
            return true;
        }

        public bool TrySetManualReloadAllowed(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null || !extension.allowManualReload)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_ManualReloadUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            ShuttleWeaponAmmoState ammoState = this.GetAmmoState(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
            if (ammoState == null)
            {
                return false;
            }

            ammoState.SetManualReloadAllowed(enabled);
            return true;
        }

        public bool TrySetLogisticsAutoFeedEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            if (extension == null || !extension.logisticsAutoFeedEligible)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_LogisticsUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            ShuttleWeaponAmmoState ammoState = this.GetAmmoState(
                moduleInstanceID,
                weaponDef,
                weaponState,
                out failureReason);
            if (ammoState == null)
            {
                return false;
            }

            ammoState.SetLogisticsAutoFeedEnabled(enabled);
            return true;
        }

        private ShuttleWeaponAmmoState GetAmmoState(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponAmmoState ammoState = this.coreMagazine.GetOrCreate(
                moduleInstanceID,
                weaponDef,
                weaponState);
            if (ammoState == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_NoAmmoSystem".Translate().ToString();
            }

            return ammoState;
        }
    }
}
