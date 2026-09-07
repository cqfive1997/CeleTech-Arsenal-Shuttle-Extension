namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Narrow write-only command boundary for UI surfaces.
    /// Passing this interface instead of ShuttleController prevents command-only dialogs from
    /// reaching raw assembly state, host comps, profile rebuilds, or cargo snapshots.
    /// </summary>
    public interface IShuttleCommandExecutor
    {
        ShuttleCommandResult Execute(IShuttleCommand command);
    }
}
