using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing.Dialogs
{
    /// <summary>
    /// Hosts the vanilla filter tree while clipping its brown bulk-action strip. The dialog
    /// supplies V3-styled bulk buttons, while the original tree keeps modded ThingDefs working.
    /// </summary>
    internal sealed class ShuttleProcessingIngredientFilterDrawer
    {
        private const float VanillaBulkButtonStripHeight = 27f;

        private readonly ThingFilterUI.UIState state = new ThingFilterUI.UIState();

        internal void Draw(
            Rect rect,
            ThingFilter workingFilter,
            ThingFilter configurableIngredients,
            RecipeDef recipeDef)
        {
            if (workingFilter == null ||
                configurableIngredients == null ||
                rect.width <= 0f ||
                rect.height <= 0f)
            {
                return;
            }

            IEnumerable<SpecialThingFilterDef> hiddenSpecialFilters = recipeDef != null
                ? recipeDef.forceHiddenSpecialFilters
                : null;

            GUI.BeginGroup(rect);
            try
            {
                Rect shiftedRect = new Rect(
                    0f,
                    -VanillaBulkButtonStripHeight,
                    rect.width,
                    rect.height + VanillaBulkButtonStripHeight);
                ThingFilterUI.DoThingFilterConfigWindow(
                    shiftedRect,
                    this.state,
                    workingFilter,
                    configurableIngredients,
                    4,
                    null,
                    hiddenSpecialFilters,
                    false,
                    false,
                    false,
                    recipeDef != null ? recipeDef.GetPremultipliedSmallIngredients() : null,
                    null);
            }
            finally
            {
                GUI.EndGroup();
            }
        }
    }
}
