using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    /// <summary>
    /// Narrow event-driven seam for offering newly committed ordinary cargo to configured cold
    /// modules. Callers receive counts and diagnostics, never holders or backend references.
    /// </summary>
    internal interface IShuttleCargoPostDepositRouter
    {
        bool TryRoute(
            ShuttleCargoDepositReceipt receipt,
            string reason,
            out int movedStackCount,
            out int movedThingCount,
            out string failureReason);
    }
}
