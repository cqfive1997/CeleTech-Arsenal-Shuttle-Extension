namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalRuntimeMetricsSnapshot
    {
        internal string ModuleInstanceID { get; set; }
        internal string RuntimeSystemKey { get; set; }

        internal float AverageReconcileCostMs { get; set; }
        internal float PeakReconcileCostMs { get; set; }
        internal float AverageTickCostMs { get; set; }
        internal float PeakTickCostMs { get; set; }
        internal float AveragePowerDemandCostMs { get; set; }
        internal float PeakPowerDemandCostMs { get; set; }

        internal float LastKnownPowerDemandWatts { get; set; }
        internal float TotalStoredEnergyConsumedWd { get; set; }
    }
}
