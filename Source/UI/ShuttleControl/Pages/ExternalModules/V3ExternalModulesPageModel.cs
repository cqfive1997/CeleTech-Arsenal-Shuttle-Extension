using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesPageModel
    {
        internal ShuttleControlReadModel ControlModel;
        internal IReadOnlyList<ExternalModuleUIReadModel> ExternalModuleModels;
        internal ExternalModuleUIRuntimeSummary Summary;
        internal Dictionary<string, string> SegmentLabelsByModuleId =
            new Dictionary<string, string>(System.StringComparer.Ordinal);
        internal IShuttleExternalRuntimeEnablementUIActions RuntimeEnablementActions;
        internal IShuttleExternalModuleDetailsUIActions ExternalModuleDetailsActions;
    }
}
