using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableAnimalButcheryPreview
    {
        internal int CorpseCount;
        internal string CandidateLabel;
        internal string ProductSummary;
        internal bool ProductsVary;
        internal bool HasProductPreview;
    }

    internal sealed class AutoWorkTableAnimalButcheryPreviewUtility
    {
        internal bool TryBuildPreview(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            out AutoWorkTableAnimalButcheryPreview preview)
        {
            preview = null;
            if (!AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return false;
            }

            AutoWorkTableAnimalButcheryPreview result =
                new AutoWorkTableAnimalButcheryPreview();
            if (inventorySnapshot == null || !inventorySnapshot.IsAvailable)
            {
                preview = result;
                return true;
            }

            IReadOnlyList<CargoThingDefCount> candidates =
                this.GetAnimalCorpseCandidates(recipeDef, inventorySnapshot);
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

                ThingDef pawnDef = AutoWorkTableAnimalButcheryUtility.TryGetAnimalPawnDefFromCorpseDef(corpseDef);
                if (pawnDef == null)
                {
                    continue;
                }

                result.CorpseCount += candidate.Count;
                distinctCorpseDefCount++;
                if (previewPawnDef == null)
                {
                    previewPawnDef = pawnDef;
                    result.CandidateLabel = this.LabelForCandidate(corpseDef, pawnDef);
                }
            }

            result.ProductsVary = distinctCorpseDefCount > 1;
            if (previewPawnDef != null)
            {
                result.ProductSummary = this.BuildProductSummary(previewPawnDef);
                result.HasProductPreview = !string.IsNullOrEmpty(result.ProductSummary);
            }

            preview = result;
            return true;
        }

        private IReadOnlyList<CargoThingDefCount> GetAnimalCorpseCandidates(
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

        private string BuildProductSummary(ThingDef pawnDef)
        {
            if (pawnDef == null || pawnDef.race == null)
            {
                return null;
            }

            List<string> parts = new List<string>();
            this.AddProductSummaryPart(parts, pawnDef.race.meatDef, this.GetAbstractStatCount(pawnDef, StatDefOf.MeatAmount));
            this.AddProductSummaryPart(parts, pawnDef.race.leatherDef, this.GetAbstractStatCount(pawnDef, StatDefOf.LeatherAmount));
            if (pawnDef.butcherProducts != null)
            {
                for (int i = 0; i < pawnDef.butcherProducts.Count; i++)
                {
                    ThingDefCountClass product = pawnDef.butcherProducts[i];
                    this.AddProductSummaryPart(parts, product != null ? product.thingDef : null, product != null ? product.count : 0);
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : null;
        }

        private int GetAbstractStatCount(ThingDef pawnDef, StatDef statDef)
        {
            if (pawnDef == null || statDef == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(pawnDef.GetStatValueAbstract(statDef, null)));
        }

        private void AddProductSummaryPart(List<string> parts, ThingDef thingDef, int count)
        {
            if (parts == null || thingDef == null || count <= 0)
            {
                return;
            }

            string label = !string.IsNullOrEmpty(thingDef.label)
                ? thingDef.LabelCap.ToString()
                : thingDef.defName;
            parts.Add(label + " x" + count.ToString());
        }
    }
}
