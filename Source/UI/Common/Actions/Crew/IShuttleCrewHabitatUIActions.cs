using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew
{
    internal interface IShuttleCrewHabitatUIActions
    {
        bool CanEjectHabitatOccupants(ShuttleControlReadModel model);

        bool EjectHabitatOccupants(ShuttleControlReadModel model);

        string GetHabitatEjectTooltip(ShuttleControlReadModel model);

        bool CanEjectHabitatOccupant(int pawnThingID);

        bool EjectHabitatOccupant(int pawnThingID);

        string GetHabitatOccupantEjectTooltip(int pawnThingID);
    }
}
