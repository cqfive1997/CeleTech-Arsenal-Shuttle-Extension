using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules
{
    internal interface IShuttleExternalRuntimeEnablementUIActions
    {
        bool CanSetRuntimeEnabled(
            ExternalModuleUIReadModel model,
            bool enabled);

        bool SetRuntimeEnabled(
            ExternalModuleUIReadModel model,
            bool enabled,
            Action onCommandCompleted);

        string GetRuntimeEnablementDisabledReason(
            ExternalModuleUIReadModel model,
            bool enabled);
    }
}
