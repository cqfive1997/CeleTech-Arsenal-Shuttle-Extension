using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Bridges
{
    /// <summary>
    /// Host-comp adapter for reactor availability and grid export. It does not calculate power.
    /// </summary>
    public sealed class PowerPlantBridge
    {
        public ShuttlePowerHostStatus BuildHostStatus(ThingWithComps host)
        {
            ShuttlePowerHostStatus status = new ShuttlePowerHostStatus();
            CompModularShuttlePowerPlant plant = this.GetPowerPlant(host);
            status.ReactorOperational = plant != null && plant.IsOperationalForShuttleReactor;
            return status;
        }

        public void ApplyRuntimeOutput(ThingWithComps host, ShuttleRuntimeState runtimeState)
        {
            CompModularShuttlePowerPlant plant = this.GetPowerPlant(host);
            if (plant == null)
            {
                return;
            }

            float outputWatts = 0f;
            if (runtimeState != null && runtimeState.Power != null)
            {
                outputWatts = runtimeState.Power.LastGridExportWatts;
            }

            plant.SetShuttleGridOutput(outputWatts);
        }

        private CompModularShuttlePowerPlant GetPowerPlant(ThingWithComps host)
        {
            return host != null ? host.TryGetComp<CompModularShuttlePowerPlant>() : null;
        }
    }
}
