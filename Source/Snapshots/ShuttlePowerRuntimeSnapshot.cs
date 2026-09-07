using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Snapshots
{
    /// <summary>
    /// Detached read projection for UI/bridges that only need current power diagnostics.
    /// </summary>
    public sealed class ShuttlePowerRuntimeSnapshot
    {
        public ShuttlePowerRuntimeSnapshot(PowerRuntimeState power)
        {
            if (power == null)
            {
                return;
            }

            this.StoredEnergyWd = power.StoredEnergyWd;
            this.HasInitializedCharge = power.HasInitializedCharge;
            this.LastGridExportWatts = power.LastGridExportWatts;
            this.InternalBusPowered = power.InternalBusPowered;
            this.LastReactorGenerationWatts = power.LastReactorGenerationWatts;
            this.LastInternalDemandWatts = power.LastInternalDemandWatts;
            this.LastBatteryChargeWatts = power.LastBatteryChargeWatts;
            this.LastBatteryDischargeWatts = power.LastBatteryDischargeWatts;
            this.LastUnmetDemandWatts = power.LastUnmetDemandWatts;
            this.LastTransientInternalDemandWatts = power.LastTransientInternalDemandWatts;
        }

        // Current stored launch energy copied out of RuntimeState.
        public float StoredEnergyWd { get; private set; }

        // Whether the runtime bucket has already received its first configured charge fill.
        public bool HasInitializedCharge { get; private set; }

        // Last-tick diagnostics from the internal power system and grid bridge.
        public float LastGridExportWatts { get; private set; }
        public bool InternalBusPowered { get; private set; }
        public float LastReactorGenerationWatts { get; private set; }
        public float LastInternalDemandWatts { get; private set; }
        public float LastBatteryChargeWatts { get; private set; }
        public float LastBatteryDischargeWatts { get; private set; }
        public float LastUnmetDemandWatts { get; private set; }
        public float LastTransientInternalDemandWatts { get; private set; }
    }
}
