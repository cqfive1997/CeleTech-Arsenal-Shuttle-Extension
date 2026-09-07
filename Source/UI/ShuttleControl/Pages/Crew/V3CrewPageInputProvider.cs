using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPageInputProvider : IShuttlePageInputProvider
    {
        public void FillInputs(
            ShuttlePageDrawContext context,
            ShuttlePageInputBuildContext inputs)
        {
            if (context == null || inputs == null)
            {
                return;
            }

            context.CrewInputs =
                new V3CrewPageInputs(
                    inputs.ControlModel,
                    inputs.CargoSnapshot,
                    inputs.PawnPresenceSnapshot,
                    inputs.PawnDynamicStatusSnapshot);
        }
    }
}
