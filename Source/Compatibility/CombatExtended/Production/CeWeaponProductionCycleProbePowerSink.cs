using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponProductionCycleProbePowerSink : IShuttlePowerDemandSink
    {
        private float frameWatts;

        internal int DemandTicks { get; private set; }

        internal float PeakWatts { get; private set; }

        internal void BeginFrame()
        {
            this.frameWatts = 0f;
        }

        internal void EndFrame()
        {
            if (this.frameWatts <= 0f)
            {
                return;
            }

            this.DemandTicks++;
            if (this.frameWatts > this.PeakWatts)
            {
                this.PeakWatts = this.frameWatts;
            }
        }

        public void AddInternalDemandWatts(
            ShuttleRuntimeState runtimeState,
            float watts)
        {
            if (watts > 0f)
            {
                this.frameWatts += watts;
            }
        }
    }
}
