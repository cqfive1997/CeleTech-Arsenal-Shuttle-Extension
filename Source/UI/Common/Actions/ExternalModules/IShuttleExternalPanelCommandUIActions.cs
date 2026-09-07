using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules
{
    internal interface IShuttleExternalPanelCommandUIActions
    {
        bool CanExecutePanelCommand(
            ExternalModuleUIReadModel model,
            ShuttleExternalPanelCommandContribution contribution);

        bool ExecutePanelCommand(
            ExternalModuleUIReadModel model,
            ShuttleExternalPanelCommandContribution contribution,
            Action onCommandCompleted);

        string GetPanelCommandDisabledReason(
            ExternalModuleUIReadModel model,
            ShuttleExternalPanelCommandContribution contribution);
    }
}
