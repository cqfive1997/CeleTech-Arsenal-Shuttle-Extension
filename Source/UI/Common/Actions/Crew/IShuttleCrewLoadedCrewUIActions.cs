namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew
{
    internal interface IShuttleCrewLoadedCrewUIActions
    {
        bool CanUnloadLoadedCrew(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName);

        bool UnloadLoadedCrew(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName);

        string GetLoadedCrewUnloadTooltip(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName);
    }
}
