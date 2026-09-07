using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Starts one exact, identity-checked cargo withdrawal and creates its rollback handle.
    /// </summary>
    internal sealed class ShuttleCargoExactWithdrawalService
    {
        private readonly ThingWithComps host;
        private readonly ShuttleCargoResourceTransferExecutor transferExecutor;
        private readonly ShuttleCargoAccessPolicy accessPolicy;
        private readonly Action invalidateInventorySnapshot;

        internal ShuttleCargoExactWithdrawalService(
            ThingWithComps host,
            ShuttleCargoResourceTransferExecutor transferExecutor,
            ShuttleCargoAccessPolicy accessPolicy,
            Action invalidateInventorySnapshot)
        {
            this.host = host;
            this.transferExecutor = transferExecutor;
            this.accessPolicy = accessPolicy;
            this.invalidateInventorySnapshot = invalidateInventorySnapshot;
        }

        internal bool TryBegin(
            CargoStackRef stackRef,
            int count,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out ShuttleCargoWithdrawal withdrawal,
            out string failureReason)
        {
            withdrawal = null;
            failureReason = null;
            if (this.accessPolicy == null ||
                !this.accessPolicy.CanWithdraw(accessRequirement, out failureReason))
            {
                return false;
            }

            CargoTakeRecord takeRecord;
            if (this.transferExecutor == null ||
                !this.transferExecutor.TryTakeExactThing(
                    stackRef,
                    count,
                    out takeRecord,
                    out failureReason))
            {
                return false;
            }

            withdrawal = new ShuttleCargoWithdrawal(
                this.host,
                takeRecord,
                stackRef.ThingIDNumber,
                stackRef.Count,
                reason,
                this.invalidateInventorySnapshot);
            return true;
        }
    }
}
