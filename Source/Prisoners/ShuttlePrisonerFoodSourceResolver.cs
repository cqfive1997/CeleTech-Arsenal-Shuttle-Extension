using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Resolves the controller-composed prisoner food source without exposing controller or
    /// cargo internals to validators and jobs.
    /// </summary>
    internal static class ShuttlePrisonerFoodSourceResolver
    {
        internal static bool TryResolve(
            ThingWithComps shuttleHost,
            out ShuttlePrisonerCargoFoodSource foodSource)
        {
            foodSource = null;
            CompModularShuttleCore core = shuttleHost != null
                ? shuttleHost.TryGetComp<CompModularShuttleCore>()
                : null;
            ShuttleController controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                return false;
            }

            foodSource = controller.BuildPrisonerCargoFoodSource();
            return foodSource != null;
        }
    }
}
