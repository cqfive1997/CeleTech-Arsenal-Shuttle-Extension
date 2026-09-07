using System.Collections.Generic;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingReadModel
    {
        internal readonly List<V3ProcessingWorkbenchModel> Workbenches =
            new List<V3ProcessingWorkbenchModel>();

        internal readonly List<V3ProcessingMetricModel> Metrics =
            new List<V3ProcessingMetricModel>();

        internal V3ProcessingWorkbenchModel SelectedWorkbench;
    }

    internal sealed class V3ProcessingWorkbenchModel
    {
        internal string Id;
        internal string ModuleInstanceID;
        internal string Label;
        internal string KindKey;
        internal string KindLabel;
        internal string IconKey;
        internal bool Installed;
        internal bool Online;
        internal bool Powered;
        internal bool Enabled;
        internal bool Paused;
        internal bool HasRuntimeState;
        internal string StatusKey;
        internal string StatusLabel;
        internal float PowerWatts;
        internal int BillCount;
        internal int ExecutableBillCount;
        internal bool HasBillStackReadModel;
        internal string SelectedSourceBenchDefName;
        internal string SelectedRecipeDefName;
        internal string ActiveSourceBenchDefName;
        internal string ActiveRecipeDefName;
        internal string CurrentRecipeLabel;
        internal string ProductionModeKey;
        internal string ProductionModeLabel;
        internal int RequestedCount;
        internal int RepeatCount;
        internal int RemainingCount;
        internal int TargetCount;
        internal int CompletedCount;
        internal int TargetStockCount;
        internal string TargetProductDefName;
        internal string TargetProductLabel;
        internal bool SelectedRecipeSupportsDoUntilStock = true;
        internal string SelectedRecipeDoUntilStockUnsupportedReason;
        internal string PolicyStatusKey;
        internal string PolicyStatusLabel;
        internal string PolicyTooltip;
        internal string IngredientStatusSummary;
        internal string MissingIngredientSummary;
        internal int TargetShortfallCount;
        internal string Tooltip;

        internal readonly List<V3ProcessingBillModel> Bills =
            new List<V3ProcessingBillModel>();

        internal readonly List<V3ProcessingRecipeModel> Recipes =
            new List<V3ProcessingRecipeModel>();
    }

    internal sealed class V3ProcessingBillModel
    {
        internal int OrderId;
        internal int QueueIndex;
        internal bool IsActive;
        internal bool CanMoveUp;
        internal bool CanMoveDown;
        internal bool HasCustomIngredientFilter;
        internal int IngredientFilterRevision;
        internal Verse.ThingFilter IngredientFilter;
        internal bool SupportsDoUntilStock = true;
        internal string DoUntilStockUnsupportedReason;
        internal int RequestedCount;
        internal int TargetCount;
        internal string Id;
        internal string Label;
        internal string SourceBenchDefName;
        internal string RecipeDefName;
        internal string IconKey;
        internal string RepeatModeKey;
        internal string RepeatModeLabel;
        internal string ProgressText;
        internal string IngredientStatusKey;
        internal string IngredientStatusLabel;
        internal string StatusKey;
        internal string StatusLabel;
        internal bool Suspended;
        internal bool IsPlaceholder;
        internal string Tooltip;
    }

    internal sealed class V3ProcessingRecipeModel
    {
        internal string Id;
        internal string Label;
        internal string SourceBenchDefName;
        internal string SourceBenchLabel;
        internal string RecipeDefName;
        internal string IconKey;
        internal Texture2D ProductIcon;
        internal string ProductLabel;
        internal string ProductCategoryKey;
        internal string IngredientSummary;
        internal string MissingIngredientSummary;
        internal bool IngredientsAvailable;
        internal string IngredientStatusKey;
        internal string WorkAmountLabel;
        internal string AvailabilityKey;
        internal string AvailabilityLabel;
        internal bool CanAdd;
        internal bool IsCurrentSelected;
        internal bool IsPlaceholder;
        internal bool SupportsDoUntilStock = true;
        internal string DoUntilStockUnsupportedReason;
        internal string SearchText;
        internal string Tooltip;
    }

    internal sealed class V3ProcessingMetricModel
    {
        internal string Label;
        internal string ShortLabel;
        internal string Value;
        internal string SeverityKey;
        internal string Tooltip;
    }
}
