namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal interface IShuttleDefenseWeaponAmmoUIActions
    {
        bool ReloadWeapon(ShuttleDefenseWeaponActionTarget weapon);

        bool CancelWeaponReload(ShuttleDefenseWeaponActionTarget weapon);

        bool SetWeaponAmmo(
            ShuttleDefenseWeaponActionTarget weapon,
            string ammoDefName);

        bool SetWeaponAutoReload(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled);

        bool SetWeaponManualReloadAllowed(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled);
    }
}
