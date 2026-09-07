using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Read-model-only preview for the shuttle mechanoid disassembly recipe.
    /// It must not stage, split, destroy, or create Things; the runtime transaction remains
    /// the only place that consumes the corpse and creates real products.
    /// </summary>
    internal sealed class AutoWorkTableMechanoidDisassemblyPreview
    {
        internal int CorpseCount;
        internal string CandidateLabel;
        internal string ProductSummary;
        internal bool ProductsVary;
        internal bool HasProductPreview;
    }

    internal sealed class AutoWorkTableMechanoidDisassemblyPreviewUtility
    {
        internal bool TryBuildPreview(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            out AutoWorkTableMechanoidDisassemblyPreview preview)
        {
            preview = null;
            if (!AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef))
            {
                return false;
            }

            AutoWorkTableMechanoidDisassemblyPreview result =
                new AutoWorkTableMechanoidDisassemblyPreview();
            if (inventorySnapshot == null || !inventorySnapshot.IsAvailable)
            {
                preview = result;
                return true;
            }

            IReadOnlyList<CargoThingDefCount> candidates =
                this.GetMechanoidCorpseCandidates(recipeDef, inventorySnapshot);
            if (candidates == null || candidates.Count == 0)
            {
                preview = result;
                return true;
            }

            ThingDef previewPawnDef = null;
            int distinctCorpseDefCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                CargoThingDefCount candidate = candidates[i];
                ThingDef corpseDef = candidate != null ? candidate.ThingDef : null;
                if (corpseDef == null || candidate.Count <= 0)
                {
                    continue;
                }

                result.CorpseCount += candidate.Count;
                distinctCorpseDefCount++;
                if (previewPawnDef == null)
                {
                    // Generated corpse defs point back to their pawn/race def through ingestible.sourceDef.
                    // That lets the UI preview vanilla butcherProducts without inspecting live cargo holders.
                    previewPawnDef = this.TryGetMechanoidPawnDef(corpseDef);
                    result.CandidateLabel = this.LabelForCandidate(corpseDef, previewPawnDef);
                }
            }

            result.ProductsVary = distinctCorpseDefCount > 1;
            if (previewPawnDef != null &&
                previewPawnDef.butcherProducts != null &&
                previewPawnDef.butcherProducts.Count > 0)
            {
                result.ProductSummary = this.BuildProductSummary(previewPawnDef.butcherProducts);
                result.HasProductPreview = !string.IsNullOrEmpty(result.ProductSummary);
            }

            preview = result;
            return true;
        }

        private IReadOnlyList<CargoThingDefCount> GetMechanoidCorpseCandidates(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot)
        {
            if (recipeDef == null || inventorySnapshot == null)
            {
                return new List<CargoThingDefCount>();
            }

            IngredientCount ingredient = recipeDef.ingredients != null && recipeDef.ingredients.Count > 0
                ? recipeDef.ingredients[0]
                : null;
            return ingredient != null && ingredient.filter != null
                ? inventorySnapshot.GetThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter)
                : new List<CargoThingDefCount>();
        }

        private ThingDef TryGetMechanoidPawnDef(ThingDef corpseDef)
        {
            if (corpseDef == null ||
                corpseDef.ingestible == null ||
                corpseDef.ingestible.sourceDef == null)
            {
                return null;
            }

            ThingDef pawnDef = corpseDef.ingestible.sourceDef;
            return pawnDef.race != null && pawnDef.race.IsMechanoid ? pawnDef : null;
        }

        private string LabelForCandidate(ThingDef corpseDef, ThingDef pawnDef)
        {
            if (pawnDef != null)
            {
                return !string.IsNullOrEmpty(pawnDef.label)
                    ? pawnDef.LabelCap.ToString()
                    : pawnDef.defName;
            }

            if (corpseDef != null)
            {
                return !string.IsNullOrEmpty(corpseDef.label)
                    ? corpseDef.LabelCap.ToString()
                    : corpseDef.defName;
            }

            return null;
        }

        private string BuildProductSummary(List<ThingDefCountClass> products)
        {
            if (products == null || products.Count == 0)
            {
                return null;
            }

            string text = string.Empty;
            for (int i = 0; i < products.Count; i++)
            {
                ThingDefCountClass product = products[i];
                if (product == null || product.thingDef == null || product.count <= 0)
                {
                    continue;
                }

                string label = !string.IsNullOrEmpty(product.thingDef.label)
                    ? product.thingDef.LabelCap.ToString()
                    : product.thingDef.defName;
                string entry = label + " x" + product.count.ToString();
                text = string.IsNullOrEmpty(text) ? entry : text + ", " + entry;
            }

            return text;
        }
    }
}
