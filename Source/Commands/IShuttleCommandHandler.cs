namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles one subsystem slice of the shuttle command boundary.
    /// </summary>
    internal interface IShuttleCommandHandler
    {
        bool CanHandle(IShuttleCommand command);
        ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context);
    }
}
