using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.Flight
{
    public interface IShuttleFlightEnergyCalculator
    {
        ShuttleFlightEnergyQuote Quote(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoSnapshot cargoSnapshot,
            int distanceTiles,
            float rangeDistanceFactor);
    }
}
