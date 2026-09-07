using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModelProviders.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition
{
    /// <summary>
    /// Writes stable V3 services and callbacks into the shared draw context.
    /// </summary>
    internal sealed class ShuttleControlContextComposition
    {
        internal void Bind(
            ShuttlePageDrawContext context,
            ShuttleControlState state,
            IShuttleIconService iconService,
            ShuttleUIModalService modalService,
            IShuttleExternalModuleUIReadPort externalModuleUIReadPort,
            IShuttlePerformanceCaptureControlPort performanceCaptureControlPort,
            IShuttlePerformanceDashboardReadPort performanceDashboardReadPort,
            IShuttlePaintPreviewService paintPreviewProvider,
            IShuttleTutorialTargetService tutorialTargets,
            Action markDirty,
            Action markExternalModuleCommandCompleted)
        {
            if (context == null)
            {
                return;
            }

            context.State = state;
            context.Services.Icons = iconService;
            context.Services.ExternalModulePanelModelProvider =
                externalModuleUIReadPort != null
                    ? new V3ExternalModulePanelModelProvider(externalModuleUIReadPort)
                    : null;
            context.Services.PaintBrowserPreviewProvider = paintPreviewProvider;
            context.Services.ModalService = modalService;
            context.Services.TutorialTargets = tutorialTargets;
            context.Services.PerformanceCaptureControlPort = performanceCaptureControlPort;
            context.Services.PerformanceDashboardReadPort = performanceDashboardReadPort;
            this.BindGlobalChromeActions(context, modalService);
            context.Actions.MarkDirty = markDirty;
            context.Actions.MarkExternalModuleCommandCompleted =
                markExternalModuleCommandCompleted;
        }

        private void BindGlobalChromeActions(
            ShuttlePageDrawContext context,
            ShuttleUIModalService modalService)
        {
            if (modalService == null)
            {
                return;
            }

            context.GlobalChromeActions.OpenLoadCargo = modalService.OpenLoadCargo;
            context.GlobalChromeActions.OpenCargoUnload = modalService.OpenCargoUnload;
            context.GlobalChromeActions.OpenPageMenu = modalService.OpenPageMenu;
            context.GlobalChromeActions.OpenLaunch = modalService.OpenLaunch;
        }
    }
}
