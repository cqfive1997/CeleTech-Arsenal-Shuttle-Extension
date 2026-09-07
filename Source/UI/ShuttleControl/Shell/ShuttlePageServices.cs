using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal sealed class ShuttlePageServices
    {
        internal IShuttleIconService Icons;
        internal IV3ExternalModulePanelModelProvider ExternalModulePanelModelProvider;
        internal IShuttlePaintPreviewService PaintBrowserPreviewProvider;
        internal ShuttleUIModalService ModalService;
        internal IShuttleTutorialTargetService TutorialTargets;
        internal IShuttlePerformanceCaptureControlPort PerformanceCaptureControlPort;
        internal IShuttlePerformanceDashboardReadPort PerformanceDashboardReadPort;
    }
}
