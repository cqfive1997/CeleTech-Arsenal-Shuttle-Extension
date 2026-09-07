namespace CeleTech.ShuttleExtension.ModularShuttle.Flight
{
    /// <summary>
    /// Detached launch-energy calculation result.
    /// It supports targeting labels and launch validation, but spending energy remains a
    /// separate runtime mutation.
    /// </summary>
    public sealed class ShuttleFlightEnergyQuote
    {
        public bool CanLaunch;
        public string FailureReason;

        // Wd values compare runtime stored energy with the projected launch cost.
        public float AvailableEnergyWd;
        public float RequiredEnergyWd;
        public float PayloadLiftEnergyWd;

        // Mass values are copied into the quote so UI and validators explain the same result.
        public float StructuralMassKg;
        public float LoadedCargoMassKg;
        public float QueuedCargoMassKg;
        public float TotalLaunchMassKg;

        // Range diagnostics for hover text and failure messages.
        public int DistanceTiles;
        public int MaxDistanceTilesAtCurrentLoad;
    }
}
