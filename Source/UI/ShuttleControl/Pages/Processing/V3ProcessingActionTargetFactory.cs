using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal static class V3ProcessingActionTargetFactory
    {
        internal static ShuttleProcessingWorkbenchActionTarget CreateWorkbenchTarget(
            V3ProcessingWorkbenchModel workbench)
        {
            if (workbench == null)
            {
                return null;
            }

            ShuttleProcessingWorkbenchActionTarget target =
                new ShuttleProcessingWorkbenchActionTarget();
            target.Id = workbench.Id;
            target.ModuleInstanceID = workbench.ModuleInstanceID;
            target.Label = workbench.Label;
            target.Enabled = workbench.Enabled;
            target.Paused = workbench.Paused;
            target.SelectedSourceBenchDefName = workbench.SelectedSourceBenchDefName;
            target.SelectedRecipeDefName = workbench.SelectedRecipeDefName;
            target.ActiveSourceBenchDefName = workbench.ActiveSourceBenchDefName;
            target.ActiveRecipeDefName = workbench.ActiveRecipeDefName;
            target.CurrentRecipeLabel = workbench.CurrentRecipeLabel;
            target.ProductionModeKey = workbench.ProductionModeKey;
            target.RequestedCount = workbench.RequestedCount;
            target.TargetCount = workbench.TargetCount;
            target.SelectedRecipeSupportsDoUntilStock =
                workbench.SelectedRecipeSupportsDoUntilStock;
            target.SelectedRecipeDoUntilStockUnsupportedReason =
                workbench.SelectedRecipeDoUntilStockUnsupportedReason;
            target.PolicyStatusLabel = workbench.PolicyStatusLabel;
            target.PolicyTooltip = workbench.PolicyTooltip;
            return target;
        }

        internal static ShuttleProcessingRecipeActionTarget CreateRecipeTarget(
            V3ProcessingRecipeModel recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            ShuttleProcessingRecipeActionTarget target =
                new ShuttleProcessingRecipeActionTarget();
            target.Id = recipe.Id;
            target.Label = recipe.Label;
            target.SourceBenchDefName = recipe.SourceBenchDefName;
            target.RecipeDefName = recipe.RecipeDefName;
            target.CanAdd = recipe.CanAdd;
            target.IsCurrentSelected = recipe.IsCurrentSelected;
            target.Tooltip = recipe.Tooltip;
            return target;
        }

        internal static ShuttleProcessingOrderActionTarget CreateOrderTarget(
            V3ProcessingWorkbenchModel workbench,
            V3ProcessingBillModel bill)
        {
            if (workbench == null || bill == null)
            {
                return null;
            }

            ShuttleProcessingOrderActionTarget target =
                new ShuttleProcessingOrderActionTarget();
            target.ModuleInstanceID = workbench.ModuleInstanceID;
            target.OrderId = bill.OrderId;
            target.QueueIndex = bill.QueueIndex;
            target.Label = bill.Label;
            target.SourceBenchDefName = bill.SourceBenchDefName;
            target.RecipeDefName = bill.RecipeDefName;
            target.Active = bill.IsActive;
            target.Suspended = bill.Suspended;
            target.CanMoveUp = bill.CanMoveUp;
            target.CanMoveDown = bill.CanMoveDown;
            target.ProductionModeKey = bill.RepeatModeKey;
            target.RequestedCount = bill.RequestedCount;
            target.TargetCount = bill.TargetCount;
            target.HasCustomIngredientFilter = bill.HasCustomIngredientFilter;
            target.IngredientFilterRevision = bill.IngredientFilterRevision;
            target.IngredientFilter = bill.IngredientFilter;
            target.SupportsDoUntilStock = bill.SupportsDoUntilStock;
            target.DoUntilStockUnsupportedReason = bill.DoUntilStockUnsupportedReason;
            target.Tooltip = bill.Tooltip;
            return target;
        }
    }
}
