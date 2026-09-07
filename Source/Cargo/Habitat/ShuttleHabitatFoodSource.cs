using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat
{
    /// <summary>
    /// Habitat-facing adapter over the shared Cargo food transaction. Habitat supplies its
    /// preferred-food policy and destination holder but does not inspect cargo holders.
    /// </summary>
    internal sealed class ShuttleHabitatFoodSource : IShuttleHabitatFoodSource
    {
        private readonly ShuttleCargoFoodWithdrawalSource foodSource;

        internal ShuttleHabitatFoodSource(ShuttleCargoFoodWithdrawalSource foodSource)
        {
            this.foodSource = foodSource;
        }

        public bool CanProvideFood(
            ThingWithComps shuttleHost,
            Pawn eater,
            HabitatProfile habitat)
        {
            if (!this.CanAttemptWithdrawal(shuttleHost, eater, habitat))
            {
                return false;
            }

            ShuttleCargoFoodFailure failure;
            return this.foodSource.CanProvideFood(
                shuttleHost,
                eater,
                ShuttleCargoFoodSelectionPolicy.Habitat(habitat.PreferredAutoFoodDefs),
                this.GetAccessRequirement(habitat),
                out failure);
        }

        public bool TryBeginHabitatFoodWithdrawal(
            ThingWithComps shuttleHost,
            Pawn eater,
            HabitatProfile habitat,
            ThingOwner<Thing> destination,
            string reason,
            out HabitatFoodWithdrawal withdrawal)
        {
            withdrawal = null;
            if (destination == null ||
                !this.CanAttemptWithdrawal(shuttleHost, eater, habitat))
            {
                return false;
            }

            ShuttleCargoFoodWithdrawal cargoWithdrawal;
            ShuttleCargoFoodFailure failure;
            if (!this.foodSource.TryBeginWithdrawal(
                shuttleHost,
                eater,
                ShuttleCargoFoodSelectionPolicy.Habitat(habitat.PreferredAutoFoodDefs),
                this.GetAccessRequirement(habitat),
                reason,
                out cargoWithdrawal,
                out failure))
            {
                ShuttleLog.Debug(
                    "HabitatFood",
                    "No shuttle cargo food available for Habitat dining. failure=" + failure +
                        " reason=" + (reason ?? "null"));
                return false;
            }

            string transferFailure = null;
            if (cargoWithdrawal == null ||
                !cargoWithdrawal.TryTransferTo(destination, out transferFailure))
            {
                if (cargoWithdrawal != null)
                {
                    cargoWithdrawal.RollBack();
                }

                ShuttleLog.Debug(
                    "HabitatFood",
                    "Habitat dining holder rejected cargo food. reason=" +
                        (reason ?? "null") + " failure=" +
                        (transferFailure ?? "null"));
                return false;
            }

            Thing food = cargoWithdrawal.Food;
            withdrawal = new HabitatFoodWithdrawal(
                cargoWithdrawal,
                food != null ? food.stackCount : 0,
                reason);
            return true;
        }

        private bool CanAttemptWithdrawal(
            ThingWithComps shuttleHost,
            Pawn eater,
            HabitatProfile habitat)
        {
            return shuttleHost != null &&
                eater != null &&
                habitat != null &&
                habitat.HasHabitat &&
                habitat.SupportsDining &&
                habitat.AllowsCargoFoodWithdrawal &&
                this.foodSource != null;
        }

        private ShuttleCargoAccessRequirement GetAccessRequirement(HabitatProfile habitat)
        {
            return habitat != null && habitat.RequiresCargoLogisticsForFoodWithdrawal
                ? ShuttleCargoAccessRequirement.ItemTransfer
                : ShuttleCargoAccessRequirement.None;
        }
    }
}
