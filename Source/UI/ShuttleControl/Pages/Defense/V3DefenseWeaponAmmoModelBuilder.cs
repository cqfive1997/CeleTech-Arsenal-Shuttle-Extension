using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseWeaponAmmoModelBuilder
    {
        internal void ApplyAmmo(
            ShuttleWeaponControlReadModel control,
            V3DefenseWeaponEntryModel weapon)
        {
            if (control == null || weapon == null)
            {
                return;
            }

            weapon.HasAmmoSystem = control.HasAmmoSystem;
            weapon.SelectedAmmoLabel = control.SelectedAmmoLabel;
            weapon.SelectedAmmoDefName = control.SelectedAmmoDefName;
            weapon.LoadedAmmoCount = control.LoadedAmmoCount;
            weapon.MagazineCapacity = control.MagazineCapacity;
            weapon.AmmoPerShot = control.AmmoPerShot;
            weapon.ReloadInProgress = control.ReloadInProgress;
            weapon.ReloadRequested = control.ReloadRequested;
            weapon.ReloadProgress01 = control.ReloadProgress01;
            weapon.AutoReloadEnabled = control.AutoReloadEnabled;
            weapon.LogisticsAutoFeedEnabled = control.LogisticsAutoFeedEnabled;
            weapon.ManualReloadAllowed = control.ManualReloadAllowed;
            weapon.ManualReloadJobActive = control.ManualReloadJobActive;
            weapon.LogisticsCoreAvailable = control.LogisticsCoreAvailable;
            weapon.AmmoStockCount = control.AmmoStockCount;
            weapon.LastAmmoFailureReason = control.LastAmmoFailureReason;
            weapon.LastReloadBlockerReason = control.LastReloadBlockerReason;
            weapon.CeAmmoModeActive = control.CeAmmoModeActive;
            weapon.CanReload = control.CanReload;
            weapon.CanCancelReload = control.CanCancelReload;
            weapon.CanSelectAmmo = control.CanSelectAmmo;
            weapon.CanToggleAutoReload = control.CanToggleAutoReload;
            weapon.CanToggleLogisticsAutoFeed = control.CanToggleLogisticsAutoFeed;
            weapon.CanToggleManualReloadAllowed =
                control.CanToggleManualReloadAllowed;
            this.BuildAmmoOptions(control, weapon);
        }

        private void BuildAmmoOptions(
            ShuttleWeaponControlReadModel control,
            V3DefenseWeaponEntryModel weapon)
        {
            if (control.AmmoOptions == null)
            {
                return;
            }

            for (int i = 0; i < control.AmmoOptions.Count; i++)
            {
                ShuttleWeaponAmmoOptionReadModel option = control.AmmoOptions[i];
                if (option == null)
                {
                    continue;
                }

                V3DefenseAmmoOptionModel uiOption =
                    new V3DefenseAmmoOptionModel();
                uiOption.AmmoDefName = option.AmmoDefName;
                uiOption.Label = option.Label;
                uiOption.StockCount = option.StockCount;
                uiOption.Selected = option.Selected;
                weapon.AmmoOptions.Add(uiOption);
            }
        }
    }
}
