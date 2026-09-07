using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    /// <summary>
    /// Projects already-cached Cargo supply counts into issue-specific relevance. Hunger,
    /// dining, and per-pawn food acceptance remain operational concerns and do not gate stock
    /// classification.
    /// </summary>
    internal sealed class ShuttleFoodSupplyIssueAnalyzer
    {
        internal ShuttleFoodSupplyIssueSnapshot Analyze(ShuttleControlReadModel model)
        {
            ShuttleFoodSupplyIssueSnapshot result = new ShuttleFoodSupplyIssueSnapshot();
            if (model == null)
            {
                return result;
            }

            ShuttleCargoSupplySnapshot supply = model.CargoSupply;
            result.GeneralRelevant = this.HasGeneralFoodConsumer(model, supply);
            result.GeneralAssessable = result.GeneralRelevant && supply != null;
            result.GeneralAvailableCount = result.GeneralAssessable
                ? supply.GeneralFoodCount
                : 0;

            ShuttlePrisonCellReadModel prison = model.PrisonCell;
            result.PrisonerRelevant = this.HasLivePrisoner(prison);
            result.PrisonerAssessable = result.PrisonerRelevant &&
                supply != null &&
                prison != null &&
                prison.HasPrisonCell &&
                prison.SupportsCargoFoodSupply &&
                prison.CargoFoodSupplyEnabled;
            result.PrisonerAvailableCount = result.PrisonerAssessable
                ? supply.CountPrisonerFood(
                    prison.MaximumCargoFoodPreferability,
                    prison.RefrigeratedCargoFoodSupplyEnabled)
                : 0;
            return result;
        }

        private bool HasGeneralFoodConsumer(
            ShuttleControlReadModel model,
            ShuttleCargoSupplySnapshot supply)
        {
            if (supply != null && supply.LoadedHumanlikePawnCount > 0)
            {
                return true;
            }

            ShuttleHabitatReadModel habitat = model != null ? model.Habitat : null;
            if (habitat != null && habitat.Occupants != null && habitat.Occupants.Count > 0)
            {
                return true;
            }

            ShuttleMedicalBayReadModel medical = model != null ? model.MedicalBay : null;
            return medical != null &&
                (medical.PatientCount > 0 ||
                    (medical.Patients != null && medical.Patients.Count > 0));
        }

        private bool HasLivePrisoner(ShuttlePrisonCellReadModel prison)
        {
            if (prison == null || !prison.HasPrisonCell)
            {
                return false;
            }

            for (int i = 0; prison.Prisoners != null && i < prison.Prisoners.Count; i++)
            {
                ShuttleHeldPrisonerReadModel prisoner = prison.Prisoners[i];
                if (prisoner != null && !prisoner.IsDead)
                {
                    return true;
                }
            }

            return prison.PrisonerCount > 0;
        }
    }

    internal sealed class ShuttleFoodSupplyIssueSnapshot
    {
        internal bool GeneralRelevant;
        internal bool GeneralAssessable;
        internal int GeneralAvailableCount;
        internal bool PrisonerRelevant;
        internal bool PrisonerAssessable;
        internal int PrisonerAvailableCount;
    }
}
