namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Marker for player/UI initiated shuttle mutations or external host actions.
    /// Commands carry intent and user selections; handlers own validation and side effects.
    /// </summary>
    public interface IShuttleCommand
    {
        string CommandID
        {
            get;
        }
    }
}
