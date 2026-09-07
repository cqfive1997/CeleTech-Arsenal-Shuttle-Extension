using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Builds detached power projections from durable runtime truth.
    /// </summary>
    internal sealed class ShuttlePowerReadModelBuilder
    {
        internal ShuttlePowerRuntimeSnapshot Build(ShuttleRuntimeState runtimeState)
        {
            return runtimeState != null
                ? new ShuttlePowerRuntimeSnapshot(runtimeState.Power)
                : new ShuttlePowerRuntimeSnapshot(null);
        }
    }
}
