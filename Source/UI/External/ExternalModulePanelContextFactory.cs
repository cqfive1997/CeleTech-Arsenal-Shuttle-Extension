using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal static class ExternalModulePanelContextFactory
    {
        internal static ShuttleExternalModulePanelContext Create(
            string panelKey,
            string runtimeSystemKey,
            ShuttleModule module,
            ExternalModuleRuntimeState state,
            bool runtimeEnabled,
            int ticksGame)
        {
            if (module == null || state == null)
            {
                return null;
            }

            return new ShuttleExternalModulePanelContext(
                panelKey,
                runtimeSystemKey,
                ExternalRuntimeInfoFactory.CreateModuleInfo(module),
                new ExternalRuntimeStateReader(state),
                runtimeEnabled,
                ticksGame);
        }
    }
}
