using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing.Dialogs
{
    /// <summary>
    /// Builds the display boundary for the per-order filter. Fixed recipe ingredients are
    /// deliberately omitted because, like vanilla bills, an order filter cannot disable them.
    /// </summary>
    internal sealed class ShuttleProcessingIngredientFilterSource
    {
        private readonly ThingFilter configurableIngredients = new ThingFilter();
        private readonly List<string> fixedIngredientLabels = new List<string>();

        internal ShuttleProcessingIngredientFilterSource(RecipeDef recipeDef)
        {
            this.Build(recipeDef);
        }

        internal ThingFilter ConfigurableIngredients
        {
            get { return this.configurableIngredients; }
        }

        internal int ConfigurableIngredientCount
        {
            get { return this.configurableIngredients.AllowedDefCount; }
        }

        internal string GetFixedIngredientSummary(string separator)
        {
            return this.fixedIngredientLabels.Count > 0
                ? string.Join(separator ?? string.Empty, this.fixedIngredientLabels.ToArray())
                : string.Empty;
        }

        private void Build(RecipeDef recipeDef)
        {
            if (recipeDef == null || recipeDef.ingredients == null)
            {
                return;
            }

            HashSet<ThingDef> seenFixed = new HashSet<ThingDef>();
            for (int i = 0; i < recipeDef.ingredients.Count; i++)
            {
                IngredientCount ingredient = recipeDef.ingredients[i];
                if (ingredient == null || ingredient.filter == null)
                {
                    continue;
                }

                if (ingredient.IsFixedIngredient)
                {
                    ThingDef fixedDef = ingredient.FixedIngredient;
                    if (fixedDef != null && seenFixed.Add(fixedDef))
                    {
                        this.fixedIngredientLabels.Add(fixedDef.LabelCap.ToString());
                    }

                    continue;
                }

                foreach (ThingDef thingDef in ingredient.filter.AllowedThingDefs)
                {
                    if (thingDef == null ||
                        (recipeDef.fixedIngredientFilter != null &&
                         !recipeDef.fixedIngredientFilter.Allows(thingDef)))
                    {
                        continue;
                    }

                    this.configurableIngredients.SetAllow(thingDef, true);
                }
            }
        }
    }
}
