using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    /// <summary>
    /// Native V3 owner for Processing UI state.
    /// </summary>
    internal sealed class V3ProcessingPageState
    {
        internal Vector2 WorkbenchScroll;
        internal Vector2 BillScroll;
        internal Vector2 RecipeScroll;
        internal Vector2 MessageScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal string RecipeSearchText = string.Empty;
        internal string LastRecipeSearchText = null;
        internal string LastRecipeSearchWorkbenchId = null;
        internal int LastRecipeSearchCount = -1;
        internal string RecipeAvailabilityFacet = "All";
        internal string RecipeProductCategoryFacet = "All";
        internal string RecipeSourceBenchFacet = "All";
        internal string LastRecipeFacetKey;
        internal readonly List<int> FilteredRecipeIndices = new List<int>();
        internal string SelectedWorkbenchId;
    }
}
