namespace CeleTech.ShuttleExtension.ModularShuttle.API.Commands
{
    /// <summary>
    /// Third-party command handler for a registered external command key.
    /// Handlers receive only the target module DTO, writable external state store, arguments,
    /// and narrow runtime facts.
    /// </summary>
    public interface IShuttleExternalCommandHandler
    {
        ShuttleExternalCommandResult Execute(
            ShuttleExternalCommandContext context);
    }
}
