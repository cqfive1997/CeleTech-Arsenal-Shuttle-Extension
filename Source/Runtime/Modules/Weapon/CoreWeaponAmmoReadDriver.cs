using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Projects the current core magazine through the backend read boundary. The returned
    /// snapshot is detached; callers cannot mutate the core magazine state through it.
    /// </summary>
    internal sealed class CoreWeaponAmmoReadDriver : IShuttleWeaponAmmoReadDriver
    {
        private readonly ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver;
        private readonly ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions;
        private readonly CoreMagazineDriver coreMagazine;

        internal CoreWeaponAmmoReadDriver(
            ShuttleWeaponAmmoSupplyResolver ammoSupplyResolver,
            ShuttleWeaponAmmoDefinitionCatalog ammoDefinitions,
            CoreMagazineDriver coreMagazine)
        {
            this.ammoSupplyResolver = ammoSupplyResolver ??
                new ShuttleWeaponAmmoSupplyResolver();
            this.ammoDefinitions = ammoDefinitions ??
                new ShuttleWeaponAmmoDefinitionCatalog();
            this.coreMagazine = coreMagazine ?? new CoreMagazineDriver(this.ammoDefinitions);
        }

        public bool TryBuildSnapshot(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponAmmoSnapshot snapshot)
        {
            snapshot = new ShuttleWeaponAmmoSnapshot();
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState weaponState = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            ShuttleWeaponModuleAmmoExtension extension =
                this.ammoDefinitions.GetExtension(weaponDef);
            snapshot.CeAmmoModeActive = false;
            if (context == null ||
                weaponState == null ||
                !this.ammoDefinitions.HasAmmoSystem(weaponDef))
            {
                return context != null && weaponState != null;
            }

            ShuttleWeaponAmmoState ammoState = this.coreMagazine.GetOrCreate(
                context.ModuleInstanceID,
                weaponDef,
                weaponState);
            if (ammoState == null || extension == null)
            {
                return false;
            }

            ShuttleWeaponAmmoDef selectedAmmo =
                this.coreMagazine.GetSelectedAmmo(weaponDef, ammoState);
            snapshot.HasAmmoSystem = true;
            snapshot.SelectedAmmoDefName = selectedAmmo != null ? selectedAmmo.defName : null;
            snapshot.SelectedAmmoLabel = selectedAmmo != null
                ? selectedAmmo.LabelCap.ToString()
                : "-";
            snapshot.LoadedAmmoCount = ammoState.LoadedAmmoCount;
            snapshot.MagazineCapacity = ammoState.MagazineCapacity;
            snapshot.AmmoPerShot = extension.ammoPerShot;
            snapshot.ReloadInProgress = ammoState.ReloadInProgress;
            snapshot.ReloadRequested = ammoState.ReloadRequested;
            snapshot.ReloadProgress01 = ammoState.ReloadProgress01;
            snapshot.AutoReloadEnabled = ammoState.AutoReloadEnabled;
            snapshot.LogisticsAutoFeedEnabled = ammoState.LogisticsAutoFeedEnabled;
            snapshot.ManualReloadAllowed = extension.allowManualReload;
            snapshot.ManualReloadJobActive = ammoState.ManualReloadJobActive;
            snapshot.LogisticsCoreAvailable = context.Profile != null &&
                context.Profile.CargoLogistics != null &&
                context.Profile.CargoLogistics.HasCargoLogistics &&
                context.Profile.CargoLogistics.SupportsItemConsumption;
            snapshot.LastAmmoFailureReason = ammoState.LastFailureReason;
            snapshot.LastReloadBlockerReason = ammoState.LastReloadBlockerReason;
            snapshot.AmmoStockCount = this.CountAvailableAmmo(context, selectedAmmo);

            snapshot.CanReload = context.IsEnabled &&
                !snapshot.CeAmmoModeActive &&
                extension.allowManualReload &&
                snapshot.LoadedAmmoCount < snapshot.MagazineCapacity;
            snapshot.CanCancelReload = context.IsEnabled &&
                (snapshot.ReloadInProgress || snapshot.ReloadRequested);
            snapshot.CanSelectAmmo = context.IsEnabled &&
                extension.ammoSet != null &&
                extension.ammoSet.allowAmmoSwitching &&
                !snapshot.CeAmmoModeActive;
            snapshot.CanToggleAutoReload = context.IsEnabled && !snapshot.CeAmmoModeActive;
            snapshot.CanToggleLogisticsAutoFeed = false;
            snapshot.CanToggleManualReloadAllowed = false;

            List<ShuttleWeaponAmmoDef> options =
                this.ammoDefinitions.GetOptions(weaponDef);
            for (int i = 0; i < options.Count; i++)
            {
                ShuttleWeaponAmmoDef option = options[i];
                snapshot.AmmoOptions.Add(new ShuttleWeaponAmmoOptionSnapshot(
                    option.defName,
                    option.LabelCap.ToString(),
                    this.CountAvailableAmmo(context, option),
                    selectedAmmo != null && option == selectedAmmo));
            }

            return true;
        }

        private int CountAvailableAmmo(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponAmmoDef ammoDef)
        {
            IShuttleWeaponAmmoSupply supply =
                this.ammoSupplyResolver.Resolve(context, false);
            return supply != null && ammoDef != null
                ? supply.CountAvailable(ammoDef.AmmoThingDef)
                : 0;
        }
    }
}
