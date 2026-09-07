using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food
{
    internal enum ShuttleCargoFoodFailure
    {
        None,
        Unavailable,
        Unpowered,
        LogisticsUnavailable,
        NoFood
    }

    /// <summary>
    /// Applies food access gates and delegates exact-source mutation to the shared Cargo
    /// withdrawal transaction. Food ranking remains a focused Cargo policy helper.
    /// </summary>
    internal sealed class ShuttleCargoFoodWithdrawalSource
    {
        private readonly IShuttleCargoBackend cargoBackend;
        private readonly IShuttleCargoResourceBroker cargoBroker;
        private readonly ShuttleProfile profile;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleCargoFoodCandidateSelector candidateSelector;

        internal ShuttleCargoFoodWithdrawalSource(
            IShuttleCargoBackend cargoBackend,
            IShuttleCargoResourceBroker cargoBroker,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleAssemblyState assemblyState)
        {
            this.cargoBackend = cargoBackend;
            this.cargoBroker = cargoBroker;
            this.profile = profile;
            this.runtimeState = runtimeState;
            this.candidateSelector = new ShuttleCargoFoodCandidateSelector(
                cargoBackend,
                runtimeState,
                assemblyState);
        }

        internal bool CanProvideFood(
            ThingWithComps shuttleHost,
            Pawn eater,
            FoodPreferability maximumPreferability,
            bool allowRefrigerated,
            bool requireCargoLogistics,
            out ShuttleCargoFoodFailure failure)
        {
            return this.CanProvideFood(
                shuttleHost,
                eater,
                ShuttleCargoFoodSelectionPolicy.LowestAtOrBelowMaximum(
                    maximumPreferability,
                    allowRefrigerated),
                requireCargoLogistics
                    ? ShuttleCargoAccessRequirement.ItemConsumption
                    : ShuttleCargoAccessRequirement.None,
                out failure);
        }

        internal bool CanProvideFood(
            ThingWithComps shuttleHost,
            Pawn eater,
            ShuttleCargoFoodSelectionPolicy policy,
            ShuttleCargoAccessRequirement accessRequirement,
            out ShuttleCargoFoodFailure failure)
        {
            failure = ShuttleCargoFoodFailure.None;
            if (!this.CanAttemptWithdrawal(
                shuttleHost,
                eater,
                accessRequirement,
                out failure))
            {
                return false;
            }

            ShuttleCargoFoodCandidate candidate;
            if (!this.candidateSelector.TryFindBestFood(
                shuttleHost,
                eater,
                policy,
                out candidate))
            {
                failure = ShuttleCargoFoodFailure.NoFood;
                return false;
            }

            return true;
        }

        internal bool TryBeginWithdrawal(
            ThingWithComps shuttleHost,
            Pawn eater,
            FoodPreferability maximumPreferability,
            bool allowRefrigerated,
            bool requireCargoLogistics,
            string reason,
            out ShuttleCargoFoodWithdrawal withdrawal,
            out ShuttleCargoFoodFailure failure)
        {
            return this.TryBeginWithdrawal(
                shuttleHost,
                eater,
                ShuttleCargoFoodSelectionPolicy.LowestAtOrBelowMaximum(
                    maximumPreferability,
                    allowRefrigerated),
                requireCargoLogistics
                    ? ShuttleCargoAccessRequirement.ItemConsumption
                    : ShuttleCargoAccessRequirement.None,
                reason,
                out withdrawal,
                out failure);
        }

        internal bool TryBeginWithdrawal(
            ThingWithComps shuttleHost,
            Pawn eater,
            ShuttleCargoFoodSelectionPolicy policy,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out ShuttleCargoFoodWithdrawal withdrawal,
            out ShuttleCargoFoodFailure failure)
        {
            withdrawal = null;
            failure = ShuttleCargoFoodFailure.None;
            if (!this.CanAttemptWithdrawal(
                shuttleHost,
                eater,
                accessRequirement,
                out failure))
            {
                return false;
            }

            ShuttleCargoFoodCandidate candidate;
            if (!this.candidateSelector.TryFindBestFood(
                shuttleHost,
                eater,
                policy,
                out candidate))
            {
                failure = ShuttleCargoFoodFailure.NoFood;
                return false;
            }

            int count = this.candidateSelector.ComputeIngestCount(eater, candidate.Food);
            if (count <= 0 || candidate.StackRef == null || this.cargoBroker == null)
            {
                failure = ShuttleCargoFoodFailure.NoFood;
                return false;
            }

            ShuttleCargoWithdrawal cargoWithdrawal;
            string transactionFailure;
            if (!this.cargoBroker.TryBeginExactWithdrawal(
                candidate.StackRef,
                count,
                accessRequirement,
                reason,
                out cargoWithdrawal,
                out transactionFailure))
            {
                failure = this.MapTransactionFailure(transactionFailure);
                return false;
            }

            withdrawal = new ShuttleCargoFoodWithdrawal(cargoWithdrawal, reason);
            return true;
        }

        internal float GetNutritionPerItem(Pawn eater, Thing food)
        {
            return this.candidateSelector.GetNutritionPerItem(eater, food);
        }

        private bool CanAttemptWithdrawal(
            ThingWithComps shuttleHost,
            Pawn eater,
            ShuttleCargoAccessRequirement accessRequirement,
            out ShuttleCargoFoodFailure failure)
        {
            failure = ShuttleCargoFoodFailure.None;
            if (shuttleHost == null || eater == null ||
                this.cargoBackend == null || this.cargoBroker == null)
            {
                failure = ShuttleCargoFoodFailure.Unavailable;
                return false;
            }

            if (this.runtimeState == null ||
                this.runtimeState.Power == null ||
                !this.runtimeState.Power.InternalBusPowered)
            {
                failure = ShuttleCargoFoodFailure.Unpowered;
                return false;
            }

            if (accessRequirement != ShuttleCargoAccessRequirement.None &&
                !this.HasRequiredCargoCapability(accessRequirement))
            {
                failure = ShuttleCargoFoodFailure.LogisticsUnavailable;
                return false;
            }

            return true;
        }

        private bool HasRequiredCargoCapability(
            ShuttleCargoAccessRequirement accessRequirement)
        {
            if (this.profile == null ||
                this.profile.CargoLogistics == null ||
                !this.profile.CargoLogistics.HasCargoLogistics)
            {
                return false;
            }

            if (accessRequirement == ShuttleCargoAccessRequirement.ItemTransfer)
            {
                return this.profile.CargoLogistics.SupportsItemTransfer;
            }

            return accessRequirement != ShuttleCargoAccessRequirement.ItemConsumption ||
                this.profile.CargoLogistics.SupportsItemConsumption;
        }

        private ShuttleCargoFoodFailure MapTransactionFailure(string failureReason)
        {
            if (!string.IsNullOrEmpty(failureReason) &&
                failureReason.IndexOf("logistics", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ShuttleCargoFoodFailure.LogisticsUnavailable;
            }

            return ShuttleCargoFoodFailure.NoFood;
        }
    }
}
