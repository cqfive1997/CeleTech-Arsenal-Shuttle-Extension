using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Projects the CE-owned magazine and common reload policy without exposing CE objects to the
    /// main read-model path.
    /// </summary>
    internal sealed class CeWeaponAmmoReadDriver : IShuttleWeaponAmmoReadDriver
    {
        public bool TryBuildSnapshot(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponAmmoSnapshot snapshot)
        {
            snapshot = new ShuttleWeaponAmmoSnapshot();
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            CompAmmoUser magazine = state != null
                ? CeWeaponRuntimeGunAccess.GetMagazine(state.GunForRuntimeOnly)
                : null;
            ShuttleWeaponModuleAmmoExtension extension = weaponDef != null
                ? weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>()
                : null;
            if (context == null || state == null || magazine == null || extension == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly != CeWeaponBackendFactory.Id)
            {
                return false;
            }

            AmmoDef selectedAmmo = magazine.SelectedAmmo ?? magazine.CurrentAmmo;
            ShuttleWeaponAmmoState commonState = state.AmmoForRuntimeOnly;
            snapshot.HasAmmoSystem = true;
            snapshot.CeAmmoModeActive = true;
            snapshot.SelectedAmmoDefName = selectedAmmo != null
                ? selectedAmmo.defName
                : null;
            snapshot.SelectedAmmoLabel = selectedAmmo != null
                ? selectedAmmo.LabelCap.ToString()
                : "-";
            snapshot.LoadedAmmoCount = magazine.CurMagCount;
            snapshot.MagazineCapacity = magazine.MagSize;
            snapshot.AmmoPerShot = extension.ammoPerShot;
            snapshot.ReloadInProgress = commonState.ReloadInProgress;
            snapshot.ReloadRequested = commonState.ReloadRequested;
            snapshot.ReloadProgress01 = commonState.ReloadInProgress &&
                commonState.ReloadWorkTotal > 0
                    ? (float)commonState.ReloadWorkDone / commonState.ReloadWorkTotal
                    : 0f;
            snapshot.AutoReloadEnabled = commonState.AutoReloadEnabled;
            snapshot.LogisticsAutoFeedEnabled = commonState.LogisticsAutoFeedEnabled;
            snapshot.ManualReloadAllowed = extension.allowManualReload;
            snapshot.ManualReloadJobActive = commonState.ManualReloadJobActive;
            snapshot.LogisticsCoreAvailable = context.Profile != null &&
                context.Profile.CargoLogistics != null &&
                context.Profile.CargoLogistics.HasCargoLogistics &&
                context.Profile.CargoLogistics.SupportsItemConsumption;
            snapshot.AmmoStockCount = selectedAmmo != null &&
                context.CargoResourceBroker != null
                    ? context.CargoResourceBroker.CountStored(selectedAmmo)
                    : 0;
            snapshot.LastAmmoFailureReason = commonState.LastFailureReason;
            snapshot.LastReloadBlockerReason = commonState.LastReloadBlockerReason;
            snapshot.CanReload = context.IsEnabled &&
                extension.allowManualReload &&
                magazine.CurMagCount < magazine.MagSize;
            snapshot.CanCancelReload = context.IsEnabled &&
                (commonState.ReloadRequested || commonState.ReloadInProgress ||
                 commonState.ManualReloadJobActive);
            snapshot.CanSelectAmmo = false;
            snapshot.CanToggleAutoReload = context.IsEnabled;
            snapshot.CanToggleLogisticsAutoFeed = false;
            snapshot.CanToggleManualReloadAllowed = false;
            if (selectedAmmo != null)
            {
                snapshot.AmmoOptions.Add(new ShuttleWeaponAmmoOptionSnapshot(
                    selectedAmmo.defName,
                    selectedAmmo.LabelCap.ToString(),
                    snapshot.AmmoStockCount,
                    true));
            }

            return true;
        }
    }
}
