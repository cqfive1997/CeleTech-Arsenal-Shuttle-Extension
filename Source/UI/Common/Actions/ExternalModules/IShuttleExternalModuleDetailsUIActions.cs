using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules
{
    internal interface IShuttleExternalModuleDetailsUIActions
    {
        bool CanOpenDetails(ExternalModuleUIReadModel model);

        void OpenDetailsDialog(
            ExternalModuleUIReadModel model,
            Action onCommandCompleted);

        string GetOpenDetailsDisabledReason(ExternalModuleUIReadModel model);
    }
}
