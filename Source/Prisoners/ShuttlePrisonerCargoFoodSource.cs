using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Prisoner-specific nutrition boundary over the Cargo-owned withdrawal transaction.
    /// It never inspects or mutates transporter/refrigerated holders directly.
    /// </summary>
    internal sealed class ShuttlePrisonerCargoFoodSource
    {
        private readonly ShuttleProfile profile;
        private readonly ShuttleCargoFoodWithdrawalSource withdrawalSource;

        internal ShuttlePrisonerCargoFoodSource(
            ShuttleProfile profile,
            ShuttleCargoFoodWithdrawalSource withdrawalSource)
        {
            this.profile = profile;
            this.withdrawalSource = withdrawalSource;
        }

        internal bool CanProvideFood(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            PrisonCellProfile prisonCell;
            if (!this.TryGetEnabledSupplyProfile(out prisonCell, out failReason))
            {
                return false;
            }

            if (this.withdrawalSource == null)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            ShuttleCargoFoodFailure failure;
            if (!this.withdrawalSource.CanProvideFood(
                shuttleHost,
                prisoner,
                prisonCell.MaximumCargoFoodPreferability,
                prisonCell.RefrigeratedCargoFoodSupplyEnabled,
                prisonCell.RequiresCargoLogisticsForFoodSupply,
                out failure))
            {
                failReason = this.TranslateFailure(failure);
                return false;
            }

            return true;
        }

        internal bool TryConsumeFoodForPrisoner(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out PrisonCellFoodConsumption consumption,
            out string failReason)
        {
            consumption = null;
            failReason = null;
            if (!this.IsValidHungryPrisoner(prisoner, out failReason))
            {
                return false;
            }

            PrisonCellProfile prisonCell;
            if (!this.TryGetEnabledSupplyProfile(out prisonCell, out failReason) ||
                this.withdrawalSource == null)
            {
                return false;
            }

            ShuttleCargoFoodWithdrawal withdrawal;
            ShuttleCargoFoodFailure failure;
            if (!this.withdrawalSource.TryBeginWithdrawal(
                shuttleHost,
                prisoner,
                prisonCell.MaximumCargoFoodPreferability,
                prisonCell.RefrigeratedCargoFoodSupplyEnabled,
                prisonCell.RequiresCargoLogisticsForFoodSupply,
                "Prison Cell feeding",
                out withdrawal,
                out failure))
            {
                failReason = this.TranslateFailure(failure);
                return false;
            }

            if (withdrawal == null || withdrawal.Food == null)
            {
                failReason = "CT_Shuttle_PrisonCell_NoFood".Translate().ToString();
                return false;
            }

            if (!this.IsValidHungryPrisoner(prisoner, out failReason))
            {
                withdrawal.RollBack();
                return false;
            }

            Thing taken = withdrawal.Food;
            float nutritionPerItem = this.withdrawalSource.GetNutritionPerItem(
                prisoner,
                taken);
            float totalNutrition = nutritionPerItem * taken.stackCount;
            float nutritionWanted = prisoner.needs.food.NutritionWanted;
            float appliedNutrition = totalNutrition < nutritionWanted
                ? totalNutrition
                : nutritionWanted;
            if (appliedNutrition <= 0f)
            {
                withdrawal.RollBack();
                failReason = "CT_Shuttle_PrisonCell_NoFood".Translate().ToString();
                return false;
            }

            float previousFoodLevel = prisoner.needs.food.CurLevel;
            string foodLabel = taken.LabelCap;
            int consumedCount = taken.stackCount;
            try
            {
                prisoner.needs.food.CurLevel += appliedNutrition;
            }
            catch (System.Exception ex)
            {
                withdrawal.RollBack();
                ShuttleLog.Error(
                    "PrisonCell",
                    "Failed to apply prisoner nutrition; cargo food was rolled back. exception=" + ex);
                failReason = "CT_Shuttle_PrisonCell_FeedFailed".Translate().ToString();
                return false;
            }

            if (!withdrawal.CommitConsumed())
            {
                prisoner.needs.food.CurLevel = previousFoodLevel;
                withdrawal.RollBack();
                failReason = "CT_Shuttle_PrisonCell_FeedFailed".Translate().ToString();
                return false;
            }

            if (prisoner.records != null)
            {
                try
                {
                    prisoner.records.AddTo(RecordDefOf.NutritionEaten, appliedNutrition);
                }
                catch (System.Exception ex)
                {
                    ShuttleLog.Error(
                        "PrisonCell",
                        "Prisoner was fed but NutritionEaten record update failed. exception=" + ex);
                }
            }

            consumption = new PrisonCellFoodConsumption(
                foodLabel,
                consumedCount,
                appliedNutrition);
            return true;
        }

        private bool TryGetEnabledSupplyProfile(
            out PrisonCellProfile prisonCell,
            out string failReason)
        {
            failReason = null;
            prisonCell = this.profile != null ? this.profile.PrisonCell : null;
            if (prisonCell == null ||
                !prisonCell.HasPrisonCell ||
                !prisonCell.SupportsFeeding ||
                !prisonCell.SupportsCargoFoodSupply)
            {
                failReason = "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
                return false;
            }

            if (!prisonCell.CargoFoodSupplyEnabled)
            {
                failReason = "CT_Shuttle_PrisonCell_SupplyDisabled".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool IsValidHungryPrisoner(Pawn prisoner, out string failReason)
        {
            failReason = null;
            if (prisoner == null ||
                prisoner.Destroyed ||
                prisoner.Dead ||
                prisoner.needs == null ||
                prisoner.needs.food == null)
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            if (prisoner.needs.food.NutritionWanted <= 0.01f)
            {
                failReason = "CT_Shuttle_PrisonCell_NotHungry".Translate().ToString();
                return false;
            }

            return true;
        }

        private string TranslateFailure(ShuttleCargoFoodFailure failure)
        {
            if (failure == ShuttleCargoFoodFailure.Unpowered)
            {
                return "CT_Shuttle_PrisonCell_SupplyUnpowered".Translate().ToString();
            }

            if (failure == ShuttleCargoFoodFailure.LogisticsUnavailable)
            {
                return "CT_Shuttle_PrisonCell_LogisticsUnavailable".Translate().ToString();
            }

            if (failure == ShuttleCargoFoodFailure.NoFood)
            {
                return "CT_Shuttle_PrisonCell_NoFood".Translate().ToString();
            }

            return "CT_Shuttle_PrisonCell_Unavailable".Translate().ToString();
        }
    }

    internal sealed class PrisonCellFoodConsumption
    {
        public PrisonCellFoodConsumption(string foodLabel, int count, float nutrition)
        {
            this.FoodLabel = foodLabel;
            this.Count = count;
            this.Nutrition = nutrition;
        }

        internal string FoodLabel { get; private set; }
        internal int Count { get; private set; }
        internal float Nutrition { get; private set; }
    }
}
