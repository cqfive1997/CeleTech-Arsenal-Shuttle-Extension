namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal interface IShuttleDefenseWeaponLogisticsUIActions
    {
        bool SetWeaponLogisticsAutoFeed(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled);
    }
}
