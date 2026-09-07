using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainPageContext
    {
        internal readonly IShuttleMainSegmentInstallUIActions SegmentInstallActions;
        internal readonly IShuttleMainModuleInstallUIActions ModuleInstallActions;
        internal readonly IShuttleMainModuleReplaceUIActions ModuleReplaceActions;
        internal readonly IShuttleMainRemovalUIActions RemovalActions;
        internal readonly IShuttleMainRemovalWorkerUIActions RemovalWorkerActions;
        internal readonly IShuttleMainModuleEnablementUIActions ModuleEnablementActions;
        internal readonly IShuttleMainSegmentModulesEnablementUIActions SegmentModulesEnablementActions;
        internal readonly IShuttleMainAssemblyConstructionUIActions AssemblyConstructionActions;
        internal readonly Action<string, string> OpenExternalRuntime;
        internal readonly IShuttleTutorialTargetService TutorialTargets;

        internal V3MainPageContext(
            IShuttleMainSegmentInstallUIActions segmentInstallActions,
            IShuttleMainModuleInstallUIActions moduleInstallActions,
            IShuttleMainModuleReplaceUIActions moduleReplaceActions,
            IShuttleMainRemovalUIActions removalActions,
            IShuttleMainRemovalWorkerUIActions removalWorkerActions,
            IShuttleMainModuleEnablementUIActions moduleEnablementActions,
            IShuttleMainSegmentModulesEnablementUIActions segmentModulesEnablementActions,
            IShuttleMainAssemblyConstructionUIActions assemblyConstructionActions,
            Action<string, string> openExternalRuntime,
            IShuttleTutorialTargetService tutorialTargets)
        {
            this.SegmentInstallActions = segmentInstallActions;
            this.ModuleInstallActions = moduleInstallActions;
            this.ModuleReplaceActions = moduleReplaceActions;
            this.RemovalActions = removalActions;
            this.RemovalWorkerActions = removalWorkerActions;
            this.ModuleEnablementActions = moduleEnablementActions;
            this.SegmentModulesEnablementActions = segmentModulesEnablementActions;
            this.AssemblyConstructionActions = assemblyConstructionActions;
            this.OpenExternalRuntime = openExternalRuntime;
            this.TutorialTargets = tutorialTargets;
        }
    }
}
