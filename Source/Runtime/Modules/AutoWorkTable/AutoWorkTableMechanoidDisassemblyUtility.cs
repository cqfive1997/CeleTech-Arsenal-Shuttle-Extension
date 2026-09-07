using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal static class AutoWorkTableMechanoidDisassemblyUtility
    {
        // Intentional vanilla RecipeDef whitelist for mechanoid disassembly support.
        internal const string RecipeDefName = "ButcherCorpseMechanoid";

        internal static bool IsSupportedRecipe(RecipeDef recipeDef)
        {
            return recipeDef != null &&
                recipeDef.defName == RecipeDefName &&
                HasButcherySpecialProduct(recipeDef);
        }

        internal static bool IsMechanoidCorpse(Thing thing)
        {
            Corpse corpse = thing as Corpse;
            Pawn innerPawn = corpse != null ? corpse.InnerPawn : null;
            return innerPawn != null &&
                innerPawn.Dead &&
                innerPawn.RaceProps != null &&
                innerPawn.RaceProps.IsMechanoid;
        }

        internal static Corpse FindMechanoidCorpse(ThingOwner<Thing> owner)
        {
            if (owner == null)
            {
                return null;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Thing thing = owner[i];
                if (IsMechanoidCorpse(thing))
                {
                    return thing as Corpse;
                }
            }

            return null;
        }

        internal static string MechanoidCorpseLabel()
        {
            return "CT_Shuttle_AutoWorkTable_MechanoidCorpse".Translate().ToString();
        }

        internal static bool RejectsDoUntilStock(RecipeDef recipeDef)
        {
            return IsSupportedRecipe(recipeDef);
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
