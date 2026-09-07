namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Optional executor for providers that draw their own command buttons inside the panel.
    /// It uses the same command boundary and validation as host-rendered panel commands.
    /// </summary>
    public interface IShuttleExternalPanelCommandExecutor
    {
        bool CanExecute(
            ShuttleExternalPanelCommandContribution contribution,
            out string disabledReason);

        bool Execute(ShuttleExternalPanelCommandContribution contribution);
    }
}
