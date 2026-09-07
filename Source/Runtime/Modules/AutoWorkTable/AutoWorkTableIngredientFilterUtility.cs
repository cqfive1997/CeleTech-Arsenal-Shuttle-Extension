using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Keeps mutable ThingFilter instances behind copy boundaries. Runtime state never stores
    /// a filter instance owned by a UI dialog or command carrier.
    /// </summary>
    internal static class AutoWorkTableIngredientFilterUtility
    {
        internal static ThingFilter CopyOrNull(ThingFilter source)
        {
            if (source == null)
            {
                return null;
            }

            ThingFilter copy = new ThingFilter();
            copy.CopyAllowancesFrom(source);
            return copy;
        }

        internal static ThingFilter BuildRecipeDefault(RecipeDef recipeDef)
        {
            ThingFilter source = recipeDef != null
                ? recipeDef.defaultIngredientFilter
                : null;
            return CopyOrNull(source) ?? new ThingFilter();
        }

        internal static bool Allows(ThingFilter customFilter, ThingDef thingDef)
        {
            return customFilter == null ||
                (thingDef != null && customFilter.Allows(thingDef));
        }
    }
}
