using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Detached, CE-neutral ammunition view produced by the selected weapon backend.
    /// It contains no mutable runtime payload and gives UI/read-model code no state-writing path.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoSnapshot
    {
        internal ShuttleWeaponAmmoSnapshot()
        {
            this.AmmoOptions = new List<ShuttleWeaponAmmoOptionSnapshot>();
        }

        internal bool HasAmmoSystem { get; set; }
        internal string SelectedAmmoLabel { get; set; }
        internal string SelectedAmmoDefName { get; set; }
        internal int LoadedAmmoCount { get; set; }
        internal int MagazineCapacity { get; set; }
        internal int AmmoPerShot { get; set; }
        internal bool ReloadInProgress { get; set; }
        internal bool ReloadRequested { get; set; }
        internal float ReloadProgress01 { get; set; }
        internal bool AutoReloadEnabled { get; set; }
        internal bool LogisticsAutoFeedEnabled { get; set; }
        internal bool ManualReloadAllowed { get; set; }
        internal bool ManualReloadJobActive { get; set; }
        internal bool LogisticsCoreAvailable { get; set; }
        internal int AmmoStockCount { get; set; }
        internal string LastAmmoFailureReason { get; set; }
        internal string LastReloadBlockerReason { get; set; }
        internal bool CeAmmoModeActive { get; set; }
        internal bool CanReload { get; set; }
        internal bool CanCancelReload { get; set; }
        internal bool CanSelectAmmo { get; set; }
        internal bool CanToggleAutoReload { get; set; }
        internal bool CanToggleLogisticsAutoFeed { get; set; }
        internal bool CanToggleManualReloadAllowed { get; set; }
        internal List<ShuttleWeaponAmmoOptionSnapshot> AmmoOptions { get; private set; }
    }

    internal sealed class ShuttleWeaponAmmoOptionSnapshot
    {
        internal ShuttleWeaponAmmoOptionSnapshot(
            string ammoDefName,
            string label,
            int availableCount,
            bool selected)
        {
            this.AmmoDefName = ammoDefName;
            this.Label = label;
            this.AvailableCount = availableCount;
            this.Selected = selected;
        }

        internal string AmmoDefName { get; private set; }
        internal string Label { get; private set; }
        internal int AvailableCount { get; private set; }
        internal bool Selected { get; private set; }
    }
}
