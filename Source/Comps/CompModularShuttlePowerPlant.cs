using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularShuttlePowerPlant : CompProperties_Power
    {
        public CompProperties_ModularShuttlePowerPlant()
        {
            this.compClass = typeof(CompModularShuttlePowerPlant);
            // The comp receives runtime export watts, but still needs to be a vanilla
            // power transmitter so nearby conduits/buildings can join its PowerNet.
            this.transmitsPower = true;
        }
    }

    /// <summary>
    /// RimWorld grid adapter for shuttle reactor surplus. Reactor math stays in PowerSystem.
    /// </summary>
    public sealed class CompModularShuttlePowerPlant : CompPowerPlant
    {
        private float cachedShuttleGridOutputWatts;

        protected override float DesiredPowerOutput
        {
            get
            {
                return this.cachedShuttleGridOutputWatts;
            }
        }

        public bool IsOperationalForShuttleReactor
        {
            get
            {
                if (this.breakdownableComp != null && this.breakdownableComp.BrokenDown)
                {
                    return false;
                }

                if (this.flickableComp != null && !this.flickableComp.SwitchIsOn)
                {
                    return false;
                }

                if (this.autoPoweredComp != null && !this.autoPoweredComp.WantsToBeOn)
                {
                    return false;
                }

                if (this.toxifier != null && !this.toxifier.CanPolluteNow)
                {
                    return false;
                }

                return true;
            }
        }

        public void SetShuttleGridOutput(float outputWatts)
        {
            if (outputWatts < 0f)
            {
                outputWatts = 0f;
            }

            this.cachedShuttleGridOutputWatts = outputWatts;
            this.UpdateDesiredPowerOutput();
        }

        public override void UpdateDesiredPowerOutput()
        {
            if (!this.IsOperationalForShuttleReactor)
            {
                this.PowerOutput = 0f;
                return;
            }

            this.PowerOutput = this.DesiredPowerOutput;
        }
    }
}
