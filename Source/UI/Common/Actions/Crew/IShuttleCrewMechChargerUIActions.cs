namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew
{
    internal interface IShuttleCrewMechChargerUIActions
    {
        bool CanEjectChargingMech(int mechThingID);

        bool EjectChargingMech(int mechThingID);

        string GetChargingMechEjectTooltip(int mechThingID);
    }
}
