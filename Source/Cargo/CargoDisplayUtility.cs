using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal static class CargoDisplayUtility
    {
        public static string GetCountSuffix(int count)
        {
            return count > 1 ? " x" + count : string.Empty;
        }

        public static float GetThingMass(Thing thing, int stackCount)
        {
            if (thing == null)
            {
                return 0f;
            }

            return thing.GetStatValue(StatDefOf.Mass, true, -1) * stackCount;
        }

        public static string GetDisplayLabel(Thing thing)
        {
            if (thing == null)
            {
                return "CT_Shuttle_Cargo_Unknown".Translate().ToString();
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return pawn.LabelCap.ToString();
            }

            if (!string.IsNullOrEmpty(thing.LabelNoCount))
            {
                return thing.LabelNoCount.CapitalizeFirst();
            }

            return thing.LabelCap.ToString();
        }

        public static string GetDisplayCategory(Thing thing)
        {
            if (thing == null || thing.def == null)
            {
                return "CT_Shuttle_Cargo_Other".Translate().ToString();
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                if (pawn.RaceProps != null)
                {
                    if (pawn.RaceProps.Humanlike)
                    {
                        return "CT_Shuttle_Cargo_Humans".Translate().ToString();
                    }

                    if (pawn.RaceProps.Animal)
                    {
                        return "CT_Shuttle_Cargo_Animals".Translate().ToString();
                    }

                    if (pawn.RaceProps.IsMechanoid)
                    {
                        return "CT_Shuttle_Cargo_Mechs".Translate().ToString();
                    }
                }

                return "CT_Shuttle_Cargo_Pawns".Translate().ToString();
            }

            if (thing.def.category == ThingCategory.Item)
            {
                return "CT_Shuttle_Cargo_Items".Translate().ToString();
            }

            if (thing.def.category == ThingCategory.Building)
            {
                return "CT_Shuttle_Cargo_Buildings".Translate().ToString();
            }

            return "CT_Shuttle_Cargo_Other".Translate().ToString();
        }
    }
}
