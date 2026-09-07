namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation.External
{
    internal sealed class ExternalModuleUIRuntimeSummary
    {
        public int ModuleCount { get; set; }
        public int EnabledCount { get; set; }
        public int DisabledCount { get; set; }
        public int MissingRuntimeCount { get; set; }

        public float TotalIdlePowerDrawWatts { get; set; }
        public float TotalLastKnownPowerDemandWatts { get; set; }
        public float TotalAverageTickCostMs { get; set; }
        public float PeakTickCostMs { get; set; }
        public float TotalStoredEnergyConsumedWd { get; set; }
    }
}
