using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime
{
    /// <summary>
    /// Narrow mutation seam for consumers that may spend internal stored shuttle energy.
    /// It represents ShuttleRuntimeState.Power.StoredEnergyWd only, not RimWorld grid power,
    /// reactor output, or any future non-electric resource pool.
    /// </summary>
    public interface IShuttleStoredEnergySink
    {
        bool TryConsumeStoredEnergyWd(ShuttleRuntimeState runtimeState, float amountWd);
    }
}
