using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Evaluates only the power and cargo-logistics capability gates for short-lived cargo
    /// transactions. Domain planners remain responsible for their own feature policy.
    /// </summary>
    internal sealed class ShuttleCargoAccessPolicy
    {
        private readonly ShuttleProfile profile;
        private readonly ShuttleRuntimeState runtimeState;

        internal ShuttleCargoAccessPolicy(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState)
        {
            this.profile = profile;
            this.runtimeState = runtimeState;
        }

        internal bool CanWithdraw(
            ShuttleCargoAccessRequirement requirement,
            out string failureReason)
        {
            failureReason = null;
            if (requirement == ShuttleCargoAccessRequirement.None)
            {
                return true;
            }

            if (this.runtimeState == null ||
                this.runtimeState.Power == null ||
                !this.runtimeState.Power.InternalBusPowered)
            {
                failureReason = "Internal power bus is offline.";
                return false;
            }

            if (this.profile == null ||
                this.profile.CargoLogistics == null ||
                !this.profile.CargoLogistics.HasCargoLogistics)
            {
                failureReason = "Cargo logistics is unavailable.";
                return false;
            }

            if (requirement == ShuttleCargoAccessRequirement.ItemTransfer &&
                !this.profile.CargoLogistics.SupportsItemTransfer)
            {
                failureReason = "Cargo logistics does not support item transfer.";
                return false;
            }

            if (requirement == ShuttleCargoAccessRequirement.ItemConsumption &&
                !this.profile.CargoLogistics.SupportsItemConsumption)
            {
                failureReason = "Cargo logistics does not support item consumption.";
                return false;
            }

            return true;
        }

        internal bool CanDeposit(
            ShuttleCargoAccessRequirement requirement,
            out string failureReason)
        {
            if (requirement == ShuttleCargoAccessRequirement.None)
            {
                failureReason = null;
                return true;
            }

            if (!this.CanWithdraw(requirement, out failureReason))
            {
                return false;
            }

            if (this.profile == null ||
                this.profile.CargoLogistics == null ||
                !this.profile.CargoLogistics.SupportsItemDeposit)
            {
                failureReason = "Cargo logistics does not support item deposit.";
                return false;
            }

            return true;
        }
    }
}
