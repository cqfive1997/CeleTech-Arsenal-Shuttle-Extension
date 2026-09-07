using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableProductFactory
    {
        internal bool TryCreateProducts(
            RecipeDef recipeDef,
            ThingOwner<Thing> pendingProducts,
            out string failureReason)
        {
            return this.TryCreateProducts(
                recipeDef,
                null,
                pendingProducts,
                out failureReason);
        }

        internal bool TryCreateProducts(
            RecipeDef recipeDef,
            ThingOwner<Thing> stagedIngredients,
            ThingOwner<Thing> pendingProducts,
            out string failureReason)
        {
            failureReason = null;
            if (recipeDef == null)
            {
                failureReason = "Recipe is missing.";
                return false;
            }

            if (pendingProducts == null)
            {
                failureReason = "Pending product owner is missing.";
                return false;
            }

            if (pendingProducts.Count > 0)
            {
                failureReason = "Pending products must be deposited before creating more products.";
                return false;
            }

            if (AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef))
            {
                return this.TryCreateMechanoidDisassemblyProducts(
                    recipeDef,
                    stagedIngredients,
                    pendingProducts,
                    out failureReason);
            }

            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return this.TryCreateAnimalButcheryProducts(
                    recipeDef,
                    stagedIngredients,
                    pendingProducts,
                    out failureReason);
            }

            if (recipeDef.products == null || recipeDef.products.Count == 0)
            {
                failureReason = "Recipe " + recipeDef.defName + " has no products.";
                return false;
            }

            List<Thing> createdProducts = new List<Thing>();
            try
            {
                for (int i = 0; i < recipeDef.products.Count; i++)
                {
                    ThingDefCountClass product = recipeDef.products[i];
                    ThingDef productDef = product != null ? product.thingDef : null;
                    int count = product != null ? product.count : 0;
                    if (productDef == null || count <= 0)
                    {
                        failureReason = "Recipe " + recipeDef.defName + " has an invalid product.";
                        this.DestroyLooseThings(createdProducts);
                        return false;
                    }

                    if (!this.TryCreateProductStacks(
                        productDef,
                        count,
                        createdProducts,
                        out failureReason))
                    {
                        this.DestroyLooseThings(createdProducts);
                        return false;
                    }
                }

                for (int i = 0; i < createdProducts.Count; i++)
                {
                    Thing thing = createdProducts[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    this.TryApplyCompIngredients(thing, stagedIngredients);

                    if (!pendingProducts.TryAdd(thing, false))
                    {
                        this.DestroyThing(thing);
                        this.DestroyPendingProducts(pendingProducts);
                        this.DestroyLooseThingsFromIndex(createdProducts, i + 1);
                        failureReason = "Failed to move product " +
                            (thing.def != null ? thing.def.defName : "unknown") +
                            " into pending products.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                this.DestroyPendingProducts(pendingProducts);
                this.DestroyLooseThings(createdProducts);
                failureReason = "Failed to create products for recipe " + recipeDef.defName +
                    ": " + exception.GetType().Name + ".";
                return false;
            }
        }

        private void TryApplyCompIngredients(
            Thing product,
            ThingOwner<Thing> stagedIngredients)
        {
            if (product == null || product.Destroyed || stagedIngredients == null)
            {
                return;
            }

            CompIngredients compIngredients = product.TryGetComp<CompIngredients>();
            if (compIngredients == null)
            {
                return;
            }

            for (int i = 0; i < stagedIngredients.Count; i++)
            {
                Thing ingredient = stagedIngredients[i];
                if (ingredient == null || ingredient.Destroyed || ingredient.def == null)
                {
                    continue;
                }

                compIngredients.RegisterIngredient(ingredient.def);
            }
        }

        private bool TryCreateMechanoidDisassemblyProducts(
            RecipeDef recipeDef,
            ThingOwner<Thing> stagedIngredients,
            ThingOwner<Thing> pendingProducts,
            out string failureReason)
        {
            failureReason = null;
            Corpse corpse = AutoWorkTableMechanoidDisassemblyUtility.FindMechanoidCorpse(stagedIngredients);
            Pawn innerPawn = corpse != null ? corpse.InnerPawn : null;
            ThingDef pawnDef = innerPawn != null ? innerPawn.def : null;
            if (corpse == null || innerPawn == null || pawnDef == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_MechanoidDisassemblyFailed".Translate().ToString();
                return false;
            }

            if (pawnDef.butcherProducts == null || pawnDef.butcherProducts.Count == 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_MechanoidProductsUnavailable".Translate().ToString();
                return false;
            }

            List<Thing> createdProducts = new List<Thing>();
            try
            {
                for (int i = 0; i < pawnDef.butcherProducts.Count; i++)
                {
                    ThingDefCountClass product = pawnDef.butcherProducts[i];
                    ThingDef productDef = product != null ? product.thingDef : null;
                    int count = product != null ? product.count : 0;
                    if (productDef == null || count <= 0)
                    {
                        failureReason = "CT_Shuttle_AutoWorkTable_MechanoidProductsUnavailable".Translate().ToString();
                        this.DestroyLooseThings(createdProducts);
                        return false;
                    }

                    if (!this.TryCreateProductStacks(
                        productDef,
                        count,
                        createdProducts,
                        out failureReason))
                    {
                        this.DestroyLooseThings(createdProducts);
                        return false;
                    }
                }

                for (int i = 0; i < createdProducts.Count; i++)
                {
                    Thing thing = createdProducts[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    if (!pendingProducts.TryAdd(thing, false))
                    {
                        this.DestroyThing(thing);
                        this.DestroyPendingProducts(pendingProducts);
                        this.DestroyLooseThingsFromIndex(createdProducts, i + 1);
                        failureReason = "CT_Shuttle_AutoWorkTable_MechanoidDisassemblyFailed".Translate().ToString();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                this.DestroyPendingProducts(pendingProducts);
                this.DestroyLooseThings(createdProducts);
                failureReason = "CT_Shuttle_AutoWorkTable_MechanoidDisassemblyFailed".Translate().ToString() +
                    ": " + exception.GetType().Name + ".";
                return false;
            }
        }

        private bool TryCreateAnimalButcheryProducts(
            RecipeDef recipeDef,
            ThingOwner<Thing> stagedIngredients,
            ThingOwner<Thing> pendingProducts,
            out string failureReason)
        {
            failureReason = null;
            Corpse corpse = AutoWorkTableAnimalButcheryUtility.FindAnimalCorpse(stagedIngredients);
            Pawn innerPawn = corpse != null ? corpse.InnerPawn : null;
            if (corpse == null || innerPawn == null || innerPawn.def == null || innerPawn.RaceProps == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_AnimalButcheryFailed".Translate().ToString();
                return false;
            }

            List<ThingDefCountClass> products = this.BuildAnimalButcheryProductList(innerPawn);
            if (products == null || products.Count == 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_AnimalButcheryProductsUnavailable".Translate().ToString();
                return false;
            }

            List<Thing> createdProducts = new List<Thing>();
            try
            {
                for (int i = 0; i < products.Count; i++)
                {
                    ThingDefCountClass product = products[i];
                    ThingDef productDef = product != null ? product.thingDef : null;
                    int count = product != null ? product.count : 0;
                    if (productDef == null || count <= 0)
                    {
                        failureReason = "CT_Shuttle_AutoWorkTable_AnimalButcheryProductsUnavailable".Translate().ToString();
                        this.DestroyLooseThings(createdProducts);
                        return false;
                    }

                    if (!this.TryCreateProductStacks(
                        productDef,
                        count,
                        createdProducts,
                        out failureReason))
                    {
                        this.DestroyLooseThings(createdProducts);
                        return false;
                    }
                }

                for (int i = 0; i < createdProducts.Count; i++)
                {
                    Thing thing = createdProducts[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    if (!pendingProducts.TryAdd(thing, false))
                    {
                        this.DestroyThing(thing);
                        this.DestroyPendingProducts(pendingProducts);
                        this.DestroyLooseThingsFromIndex(createdProducts, i + 1);
                        failureReason = "CT_Shuttle_AutoWorkTable_AnimalButcheryFailed".Translate().ToString();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                this.DestroyPendingProducts(pendingProducts);
                this.DestroyLooseThings(createdProducts);
                failureReason = "CT_Shuttle_AutoWorkTable_AnimalButcheryFailed".Translate().ToString() +
                    ": " + exception.GetType().Name + ".";
                return false;
            }
        }

        private List<ThingDefCountClass> BuildAnimalButcheryProductList(Pawn pawn)
        {
            List<ThingDefCountClass> products = new List<ThingDefCountClass>();
            if (pawn == null || pawn.def == null || pawn.RaceProps == null)
            {
                return products;
            }

            this.AddProduct(products, pawn.RaceProps.meatDef, this.GetPawnStatCount(pawn, StatDefOf.MeatAmount));
            this.AddProduct(products, pawn.RaceProps.leatherDef, this.GetPawnStatCount(pawn, StatDefOf.LeatherAmount));

            if (pawn.def.butcherProducts != null)
            {
                for (int i = 0; i < pawn.def.butcherProducts.Count; i++)
                {
                    ThingDefCountClass product = pawn.def.butcherProducts[i];
                    this.AddProduct(
                        products,
                        product != null ? product.thingDef : null,
                        product != null ? product.count : 0);
                }
            }

            return products;
        }

        private int GetPawnStatCount(Pawn pawn, StatDef statDef)
        {
            if (pawn == null || statDef == null)
            {
                return 0;
            }

            return GenMath.RoundRandom(pawn.GetStatValue(statDef, true, -1));
        }

        private void AddProduct(List<ThingDefCountClass> products, ThingDef thingDef, int count)
        {
            if (products == null || thingDef == null || count <= 0)
            {
                return;
            }

            products.Add(new ThingDefCountClass(thingDef, count));
        }

        private bool TryCreateProductStacks(
            ThingDef productDef,
            int count,
            List<Thing> createdProducts,
            out string failureReason)
        {
            failureReason = null;
            int remaining = count;
            int stackLimit = productDef.stackLimit > 0 ? productDef.stackLimit : count;
            while (remaining > 0)
            {
                int stackCount = remaining < stackLimit ? remaining : stackLimit;
                Thing thing = ThingMaker.MakeThing(productDef);
                if (thing == null)
                {
                    failureReason = "Failed to create product " + productDef.defName + ".";
                    return false;
                }

                thing.stackCount = stackCount;
                createdProducts.Add(thing);

                remaining -= stackCount;
            }

            return true;
        }

        private void DestroyPendingProducts(ThingOwner<Thing> pendingProducts)
        {
            if (pendingProducts == null)
            {
                return;
            }

            while (pendingProducts.Count > 0)
            {
                Thing thing = pendingProducts[0];
                if (thing == null)
                {
                    break;
                }

                Thing taken = pendingProducts.Take(thing, thing.stackCount);
                this.DestroyThing(taken ?? thing);
            }
        }

        private void DestroyLooseThings(List<Thing> things)
        {
            this.DestroyLooseThingsFromIndex(things, 0);
        }

        private void DestroyLooseThingsFromIndex(List<Thing> things, int startIndex)
        {
            if (things == null)
            {
                return;
            }

            for (int i = startIndex; i < things.Count; i++)
            {
                this.DestroyThing(things[i]);
            }
        }

        private void DestroyThing(Thing thing)
        {
            if (thing != null && !thing.Destroyed)
            {
                thing.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
