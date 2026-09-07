using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime
{
    internal interface IShuttlePowerDemandSink
    {
        void AddInternalDemandWatts(ShuttleRuntimeState runtimeState, float watts);
    }
}
