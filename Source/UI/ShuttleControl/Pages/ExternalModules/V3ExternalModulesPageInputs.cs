using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly IReadOnlyList<ExternalModuleUIReadModel> ExternalModuleModels;

        internal V3ExternalModulesPageInputs(
            ShuttleControlReadModel controlModel,
            IReadOnlyList<ExternalModuleUIReadModel> externalModuleModels)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.ExternalModuleModels = externalModuleModels;
        }
    }
}
