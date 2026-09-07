using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Transient copied identities for stacks committed by one owner-to-ordinary-cargo
    /// deposit. It owns no Thing or holder references and is never persisted.
    /// </summary>
    internal sealed class ShuttleCargoDepositReceipt
    {
        private readonly List<CargoStackRef> stackRefs;

        internal ShuttleCargoDepositReceipt(
            List<CargoStackRef> stackRefs,
            int depositedCount)
        {
            this.stackRefs = stackRefs ?? new List<CargoStackRef>();
            this.DepositedCount = depositedCount > 0 ? depositedCount : 0;
        }

        internal IReadOnlyList<CargoStackRef> StackRefs
        {
            get { return this.stackRefs; }
        }

        internal int DepositedCount { get; private set; }

        internal bool HasStacks
        {
            get { return this.stackRefs.Count > 0; }
        }

        internal static ShuttleCargoDepositReceipt Empty()
        {
            return new ShuttleCargoDepositReceipt(
                new List<CargoStackRef>(),
                0);
        }
    }
}
