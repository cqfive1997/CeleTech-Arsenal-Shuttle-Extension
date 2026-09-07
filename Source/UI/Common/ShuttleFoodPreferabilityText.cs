using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleFoodPreferabilityText
    {
        internal static string GetMaximumTierLabel(FoodPreferability preferability)
        {
            if (preferability == FoodPreferability.MealSimple)
            {
                return ShuttleUIText.Tr("CT_Shuttle_PrisonSupply_TierSimple");
            }

            if (preferability == FoodPreferability.MealFine)
            {
                return ShuttleUIText.Tr("CT_Shuttle_PrisonSupply_TierFine");
            }

            if (preferability == FoodPreferability.MealLavish)
            {
                return ShuttleUIText.Tr("CT_Shuttle_PrisonSupply_TierLavish");
            }

            return ShuttleUIText.Tr("CT_Shuttle_PrisonSupply_TierAwful");
        }
    }
}
