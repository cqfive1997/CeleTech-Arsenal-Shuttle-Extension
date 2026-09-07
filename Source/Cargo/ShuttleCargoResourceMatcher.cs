using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal static class ShuttleCargoResourceMatcher
    {
        internal static bool Matches(Thing thing, ThingDef thingDef)
        {
            return thing != null &&
                !thing.Destroyed &&
                thing.def == thingDef &&
                thing.stackCount > 0;
        }

        internal static bool MatchesAvailableThing(Thing thing)
        {
            return thing != null &&
                !thing.Destroyed &&
                thing.def != null &&
                thing.stackCount > 0;
        }

        internal static bool AllowedByFilters(
            ThingDef thingDef,
            ThingFilter ingredientFilter,
            ThingFilter fixedIngredientFilter)
        {
            if (thingDef == null || ingredientFilter == null || !ingredientFilter.Allows(thingDef))
            {
                return false;
            }

            return fixedIngredientFilter == null || fixedIngredientFilter.Allows(thingDef);
        }

        internal static int CompareThingDefNames(ThingDef left, ThingDef right)
        {
            string leftName = GetThingDefName(left);
            string rightName = GetThingDefName(right);
            return string.CompareOrdinal(leftName, rightName);
        }

        internal static int CompareThingDefCountNames(
            CargoThingDefCount left,
            CargoThingDefCount right)
        {
            string leftName = left != null ? GetThingDefName(left.ThingDef) : string.Empty;
            string rightName = right != null ? GetThingDefName(right.ThingDef) : string.Empty;
            return string.CompareOrdinal(leftName, rightName);
        }

        internal static string GetThingDefName(ThingDef thingDef)
        {
            return thingDef != null && !string.IsNullOrEmpty(thingDef.defName)
                ? thingDef.defName
                : string.Empty;
        }
    }
}
