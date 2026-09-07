using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing
{
    internal interface IShuttleProcessingIngredientFilterActions
    {
        bool SaveIngredientFilter(
            ShuttleProcessingOrderActionTarget order,
            ThingFilter filter,
            bool useRecipeDefault);
    }
}
