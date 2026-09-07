using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull
{
    internal static class ShuttleHullArmorStuffUtility
    {
        internal static ThingDef ResolveSelectedStuffOrFallback(string selectedStuffDefName)
        {
            if (!string.IsNullOrWhiteSpace(selectedStuffDefName))
            {
                ThingDef selectedStuff = DefDatabase<ThingDef>.GetNamedSilentFail(selectedStuffDefName.Trim());
                if (IsValidHullArmorStuff(selectedStuff))
                {
                    return selectedStuff;
                }
            }

            ThingDef steel = ThingDefOf.Steel;
            if (!IsValidHullArmorStuff(steel))
            {
                steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
            }

            return IsValidHullArmorStuff(steel) ? steel : null;
        }

        internal static string ResolveSelectedStuffDefNameOrFallback(string selectedStuffDefName)
        {
            ThingDef resolved = ResolveSelectedStuffOrFallback(selectedStuffDefName);
            return resolved != null ? resolved.defName : null;
        }

        internal static bool IsValidHullArmorStuff(ThingDef stuffDef)
        {
            if (stuffDef == null || !stuffDef.IsStuff || stuffDef.stuffProps == null)
            {
                return false;
            }

            List<StuffCategoryDef> categories = stuffDef.stuffProps.categories;
            if (categories == null)
            {
                return false;
            }

            return categories.Contains(StuffCategoryDefOf.Metallic);
        }

        internal static List<ThingDef> GetAvailableHullArmorStuffs()
        {
            List<ThingDef> result = new List<ThingDef>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (IsValidHullArmorStuff(def))
                {
                    result.Add(def);
                }
            }

            result.Sort(CompareStuffLabels);
            return result;
        }

        private static int CompareStuffLabels(ThingDef left, ThingDef right)
        {
            return string.Compare(
                GetSortLabel(left),
                GetSortLabel(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSortLabel(ThingDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrEmpty(def.label) ? def.label : def.defName;
        }
    }
}
