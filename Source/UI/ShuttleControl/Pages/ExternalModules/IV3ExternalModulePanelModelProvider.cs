using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal interface IV3ExternalModulePanelModelProvider
    {
        V3ExternalModulesProviderPanelModel BuildProviderPanel(
            ExternalModuleUIReadModel selected,
            float contentWidth,
            float fallbackHeight,
            IShuttleExternalPanelCommandUIActions panelCommandActions,
            System.Action onCommandCompleted);
    }
}
