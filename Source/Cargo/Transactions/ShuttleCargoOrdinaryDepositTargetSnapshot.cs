namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Copied ordinary-transporter capacity used by internal deposit planners. It exposes no
    /// transporter or holder reference.
    /// </summary>
    internal sealed class ShuttleCargoOrdinaryDepositTargetSnapshot
    {
        internal ShuttleCargoOrdinaryDepositTargetSnapshot(
            int transporterIndex,
            float availableMassKg)
        {
            this.TransporterIndex = transporterIndex;
            this.AvailableMassKg = availableMassKg > 0f ? availableMassKg : 0f;
        }

        internal int TransporterIndex { get; private set; }

        internal float AvailableMassKg { get; private set; }
    }
}
