using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal static class ShuttleCargoThingFilterUtility
    {
        internal static ThingFilter CopyFilter(ThingFilter source)
        {
            ThingFilter copy = ThingFilter.CreateOnlyEverStorableThingFilter();
            if (source != null)
            {
                copy.CopyAllowancesFrom(source);
            }

            return copy;
        }

        internal static bool FiltersEquivalent(ThingFilter left, ThingFilter right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left.AllowedHitPointsPercents != right.AllowedHitPointsPercents ||
                left.AllowedMentalBreakChance != right.AllowedMentalBreakChance ||
                left.AllowedQualityLevels != right.AllowedQualityLevels)
            {
                return false;
            }

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (thingDef != null && left.Allows(thingDef) != right.Allows(thingDef))
                {
                    return false;
                }
            }

            foreach (SpecialThingFilterDef filterDef in DefDatabase<SpecialThingFilterDef>.AllDefsListForReading)
            {
                if (filterDef != null && left.Allows(filterDef) != right.Allows(filterDef))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
