namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Declaration sink used by panel providers to request host-owned command buttons.
    /// The sink does not execute commands and does not expose a command executor.
    /// </summary>
    public interface IShuttleExternalModulePanelCommandSink
    {
        void AddCommand(ShuttleExternalPanelCommandContribution contribution);
    }
}
