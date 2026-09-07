using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal interface IShuttleDefenseWeaponFireControlUIActions
    {
        bool CanToggleHoldFire(ShuttleDefenseWeaponActionTarget weapon);

        bool ToggleHoldFire(ShuttleDefenseWeaponActionTarget weapon);

        string GetHoldFireTooltip(ShuttleDefenseWeaponActionTarget weapon);

        bool CanToggleWeaponFireControlLink(ShuttleDefenseWeaponActionTarget weapon);

        bool ToggleWeaponFireControlLink(ShuttleDefenseWeaponActionTarget weapon);

        string GetFireControlLinkTooltip(ShuttleDefenseWeaponActionTarget weapon);

        bool SetWeaponFireControlMode(
            ShuttleDefenseWeaponActionTarget weapon,
            ShuttleWeaponFireControlMode mode);

        bool SetWeaponTargetPriority(
            ShuttleDefenseWeaponActionTarget weapon,
            ShuttleWeaponTargetPriority priority);

        bool SetWeaponAutoFireEnabled(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled);
    }
}
