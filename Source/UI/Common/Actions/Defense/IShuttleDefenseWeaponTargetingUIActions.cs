namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal interface IShuttleDefenseWeaponTargetingUIActions
    {
        bool CanUseForcedTarget(ShuttleDefenseWeaponActionTarget weapon);

        bool UseForcedTarget(ShuttleDefenseWeaponActionTarget weapon);

        string GetForcedTargetTooltip(ShuttleDefenseWeaponActionTarget weapon);
    }
}
