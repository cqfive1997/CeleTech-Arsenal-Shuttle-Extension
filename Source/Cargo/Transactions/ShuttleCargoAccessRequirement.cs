namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Capability gate applied by the Cargo broker before beginning a real-item withdrawal.
    /// None is reserved for callers that already own an equivalent state/policy gate, such as
    /// the bounded SDK adapter and Medical treatment orchestration.
    /// </summary>
    internal enum ShuttleCargoAccessRequirement
    {
        None,
        ItemTransfer,
        ItemConsumption
    }
}
