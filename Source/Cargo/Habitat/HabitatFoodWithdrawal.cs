using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat
{
    /// <summary>
    /// Habitat-facing transaction adapter. The shared Cargo transaction owns source rollback,
    /// notification, recovery, and cache invalidation; Habitat only accepts or rejects dining
    /// holder ownership.
    /// </summary>
    public sealed class HabitatFoodWithdrawal
    {
        private readonly ShuttleCargoFoodWithdrawal cargoWithdrawal;
        private readonly string reason;
        private bool isCommitted;
        private bool isRolledBack;

        internal HabitatFoodWithdrawal(
            ShuttleCargoFoodWithdrawal cargoWithdrawal,
            int count,
            string reason)
        {
            this.cargoWithdrawal = cargoWithdrawal;
            this.Count = count;
            this.reason = reason;
        }

        public Thing Food
        {
            get { return this.cargoWithdrawal != null ? this.cargoWithdrawal.Food : null; }
        }

        public int Count { get; private set; }

        public bool IsCommitted
        {
            get { return this.isCommitted; }
        }

        public bool IsRolledBack
        {
            get { return this.isRolledBack; }
        }

        public bool TryCommit()
        {
            if (this.isCommitted)
            {
                return true;
            }

            if (this.isRolledBack || this.cargoWithdrawal == null ||
                !this.cargoWithdrawal.CommitTransferred())
            {
                return false;
            }

            this.isCommitted = true;
            ShuttleLog.Debug(
                "HabitatFood",
                "Committed cargo food withdrawal for " + this.DescribeFood() +
                    " reason=" + (this.reason ?? "null"));
            return true;
        }

        public bool TryRollback()
        {
            if (this.isRolledBack)
            {
                return true;
            }

            if (this.isCommitted || this.cargoWithdrawal == null)
            {
                return false;
            }

            bool success = this.cargoWithdrawal.RollBack();
            if (success)
            {
                this.isRolledBack = true;
                ShuttleLog.Debug(
                    "HabitatFood",
                    "Rolled back cargo food withdrawal for " + this.DescribeFood() +
                        " reason=" + (this.reason ?? "null"));
            }

            return success;
        }

        private string DescribeFood()
        {
            Thing food = this.Food;
            if (food == null || food.def == null)
            {
                return "<null>";
            }

            return food.def.defName + " x" + this.Count;
        }
    }
}
