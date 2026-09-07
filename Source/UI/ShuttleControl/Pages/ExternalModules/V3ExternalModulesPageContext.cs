using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesPageContext
    {
        internal readonly IV3ExternalModulePanelModelProvider PanelModelProvider;
        internal readonly IShuttleExternalPanelCommandUIActions PanelCommandActions;
        internal readonly IShuttleExternalRuntimeEnablementUIActions RuntimeEnablementActions;
        internal readonly IShuttleExternalModuleDetailsUIActions ModuleDetailsActions;
        internal readonly Action MarkDirty;
        internal readonly Action MarkCommandCompleted;

        internal V3ExternalModulesPageContext(
            IV3ExternalModulePanelModelProvider panelModelProvider,
            IShuttleExternalPanelCommandUIActions panelCommandActions,
            IShuttleExternalRuntimeEnablementUIActions runtimeEnablementActions,
            IShuttleExternalModuleDetailsUIActions moduleDetailsActions,
            Action markDirty,
            Action markCommandCompleted)
        {
            this.PanelModelProvider = panelModelProvider;
            this.PanelCommandActions = panelCommandActions;
            this.RuntimeEnablementActions = runtimeEnablementActions;
            this.ModuleDetailsActions = moduleDetailsActions;
            this.MarkDirty = markDirty;
            this.MarkCommandCompleted = markCommandCompleted;
        }
    }
}
