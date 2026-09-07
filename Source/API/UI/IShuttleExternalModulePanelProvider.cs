using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.UI
{
    /// <summary>
    /// Provider for the right-side External Modules host. Providers must not open independent
    /// RimWorld windows or write runtime state directly. Providers may declare command buttons
    /// through CollectCommands; hosts draw those buttons and execute ShuttleExternalCommand
    /// through the command boundary. When a host supplies CommandExecutor in the panel context,
    /// providers that draw custom controls may use it for the same controlled command path.
    /// Providers may bind only to a runtime key owned by the same package.
    /// </summary>
    public interface IShuttleExternalModulePanelProvider
    {
        string RuntimeSystemKey { get; }

        bool CanShow(ShuttleExternalModulePanelContext context);

        float GetPreferredHeight(
            ShuttleExternalModulePanelContext context,
            float width);

        void DrawPanel(
            Rect rect,
            ShuttleExternalModulePanelContext context);

        void CollectCommands(
            ShuttleExternalModulePanelContext context,
            IShuttleExternalModulePanelCommandSink sink);
    }
}
