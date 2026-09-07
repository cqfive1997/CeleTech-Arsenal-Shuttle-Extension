using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal static class AutoWorkTableAnimalButcheryUtility
    {
        // Intentional vanilla RecipeDef whitelist for animal butchery support.
        internal const string RecipeDefName = "ButcherCorpseFlesh";

        internal static bool IsSupportedRecipe(RecipeDef recipeDef)
        {
            return recipeDef != null &&
                recipeDef.defName == RecipeDefName &&
                HasButcherySpecialProduct(recipeDef);
        }

        internal static bool IsSupportedAnimalCorpse(Thing thing)
        {
            Corpse corpse = thing as Corpse;
            Pawn innerPawn = corpse != null ? corpse.InnerPawn : null;
            if (corpse == null ||
                innerPawn == null ||
                !innerPawn.Dead ||
                innerPawn.RaceProps == null ||
                innerPawn.RaceProps.Humanlike ||
                innerPawn.RaceProps.IsMechanoid)
            {
                return false;
            }

            CompRottable rottable = corpse.TryGetComp<CompRottable>();
            if (rottable != null && rottable.Stage != RotStage.Fresh)
            {
                return false;
            }

            return HasAnimalButcheryProducts(innerPawn.def);
        }

        internal static Corpse FindAnimalCorpse(ThingOwner<Thing> owner)
        {
            if (owner == null)
            {
                return null;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (IsSupportedAnimalCorpse(thing))
                {
                    return thing as Corpse;
                }
            }

            return null;
        }

        internal static bool IsSupportedAnimalPawnDef(ThingDef pawnDef)
        {
            if (pawnDef == null ||
                pawnDef.race == null ||
                pawnDef.race.Humanlike ||
                pawnDef.race.IsMechanoid)
            {
                return false;
            }

            return HasAnimalButcheryProducts(pawnDef);
        }

        internal static ThingDef TryGetAnimalPawnDefFromCorpseDef(ThingDef corpseDef)
        {
            if (corpseDef == null ||
                corpseDef.ingestible == null ||
                corpseDef.ingestible.sourceDef == null)
            {
                return null;
            }

            ThingDef pawnDef = corpseDef.ingestible.sourceDef;
            return IsSupportedAnimalPawnDef(pawnDef) ? pawnDef : null;
        }

        internal static string AnimalCorpseLabel()
        {
            return "CT_Shuttle_AutoWorkTable_AnimalCorpse".Translate().ToString();
        }

        internal static bool RejectsDoUntilStock(RecipeDef recipeDef)
        {
            return IsSupportedRecipe(recipeDef);
        }

        private static bool HasAnimalButcheryProducts(ThingDef pawnDef)
        {
            if (pawnDef == null || pawnDef.race == null)
            {
                return false;
            }

            if (pawnDef.race.meatDef != null || pawnDef.race.leatherDef != null)
            {
                return true;
            }

            return pawnDef.butcherProducts != null && pawnDef.butcherProducts.Count > 0;
        }

        private static bool HasButcherySpecialProduct(RecipeDef recipeDef)
        {
            if (recipeDef == null || recipeDef.specialProducts == null)
            {
                return false;
            }

            for (int i = 0; i < recipeDef.specialProducts.Count; i++)
            {
                if (recipeDef.specialProducts[i] == SpecialProductType.Butchery)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
