using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesPageInputProvider : IShuttlePageInputProvider
    {
        public void FillInputs(
            ShuttlePageDrawContext context,
            ShuttlePageInputBuildContext inputs)
        {
            if (context == null || inputs == null)
            {
                return;
            }

            context.ExternalModulesInputs =
                new V3ExternalModulesPageInputs(
                    inputs.ControlModel,
                    inputs.ExternalModuleModels);
        }
    }
}
