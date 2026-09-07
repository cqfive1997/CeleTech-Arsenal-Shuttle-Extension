namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing
{
    internal sealed class ShuttleProcessingWorkbenchActionTarget
    {
        internal string Id;
        internal string ModuleInstanceID;
        internal string Label;
        internal bool Enabled;
        internal bool Paused;
        internal string SelectedSourceBenchDefName;
        internal string SelectedRecipeDefName;
        internal string ActiveSourceBenchDefName;
        internal string ActiveRecipeDefName;
        internal string CurrentRecipeLabel;
        internal string ProductionModeKey;
        internal int RequestedCount;
        internal int TargetCount;
        internal bool SelectedRecipeSupportsDoUntilStock = true;
        internal string SelectedRecipeDoUntilStockUnsupportedReason;
        internal string PolicyStatusLabel;
        internal string PolicyTooltip;
    }

    internal sealed class ShuttleProcessingRecipeActionTarget
    {
        internal string Id;
        internal string Label;
        internal string SourceBenchDefName;
        internal string RecipeDefName;
        internal bool CanAdd;
        internal bool IsCurrentSelected;
        internal string Tooltip;
    }

    internal sealed class ShuttleProcessingOrderActionTarget
    {
        internal string ModuleInstanceID;
        internal int OrderId;
        internal int QueueIndex;
        internal string Label;
        internal string SourceBenchDefName;
        internal string RecipeDefName;
        internal bool Active;
        internal bool Suspended;
        internal bool CanMoveUp;
        internal bool CanMoveDown;
        internal string ProductionModeKey;
        internal int RequestedCount;
        internal int TargetCount;
        internal bool HasCustomIngredientFilter;
        internal int IngredientFilterRevision;
        internal Verse.ThingFilter IngredientFilter;
        internal bool SupportsDoUntilStock = true;
        internal string DoUntilStockUnsupportedReason;
        internal string Tooltip;
    }
}
