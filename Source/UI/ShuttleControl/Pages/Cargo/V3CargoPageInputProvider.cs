using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoPageInputProvider : IShuttlePageInputProvider
    {
        public void FillInputs(
            ShuttlePageDrawContext context,
            ShuttlePageInputBuildContext inputs)
        {
            if (context == null || inputs == null)
            {
                return;
            }

            context.CargoInputs =
                new V3CargoPageInputs(
                    inputs.ControlModel,
                    inputs.CargoSnapshot,
                    inputs.CargoRegionSettings,
                    inputs.CargoSnapshotRevision);
        }
    }
}
