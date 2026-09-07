namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation.External
{
    internal sealed class ExternalModuleUIReadModel
    {
        public string ModuleInstanceID { get; set; }
        public string ModuleDefName { get; set; }
        public string RuntimeSystemKey { get; set; }

        public string Label { get; set; }
        public string ModuleTypeID { get; set; }
        public ExternalModuleUICategory Category { get; set; }

        public bool RuntimeRegistered { get; set; }
        public bool RuntimeStateExists { get; set; }
        public bool RuntimeStateEnvelopeValid { get; set; }
        public bool RuntimeEnabled { get; set; }
        public bool HasExternalPanel { get; set; }
        public int ExternalPanelCount { get; set; }

        public float IdlePowerDrawWatts { get; set; }
        public float LastKnownPowerDemandWatts { get; set; }
        public float AverageTickCostMs { get; set; }
        public float PeakTickCostMs { get; set; }
        public float TotalStoredEnergyConsumedWd { get; set; }
        public float AverageReconcileCostMs { get; set; }
        public float PeakReconcileCostMs { get; set; }
        public float AveragePowerDemandCostMs { get; set; }
        public float PeakPowerDemandCostMs { get; set; }

        public string StatusText { get; set; }
        public string DisabledReason { get; set; }
    }
}
