namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing
{
    internal interface IShuttleProcessingUIActions
    {
        bool CanSetSelectedRecipe(
            ShuttleProcessingWorkbenchActionTarget workbench,
            ShuttleProcessingRecipeActionTarget recipe);

        bool SetSelectedRecipe(
            ShuttleProcessingWorkbenchActionTarget workbench,
            ShuttleProcessingRecipeActionTarget recipe);

        string GetSetSelectedRecipeTooltip(
            ShuttleProcessingWorkbenchActionTarget workbench,
            ShuttleProcessingRecipeActionTarget recipe);

        bool CanClearSelectedRecipe(ShuttleProcessingWorkbenchActionTarget workbench);

        bool ClearSelectedRecipe(ShuttleProcessingWorkbenchActionTarget workbench);

        string GetClearSelectedRecipeTooltip(ShuttleProcessingWorkbenchActionTarget workbench);

        bool CanSetPaused(ShuttleProcessingWorkbenchActionTarget workbench);

        bool SetPaused(ShuttleProcessingWorkbenchActionTarget workbench, bool paused);

        string GetPausedTooltip(ShuttleProcessingWorkbenchActionTarget workbench);

        bool CanOpenProductionPolicy(ShuttleProcessingWorkbenchActionTarget workbench);

        void OpenProductionPolicyDialog(ShuttleProcessingWorkbenchActionTarget workbench);

        string GetProductionPolicyTooltip(ShuttleProcessingWorkbenchActionTarget workbench);

        bool RemoveOrder(ShuttleProcessingOrderActionTarget order);

        bool MoveOrder(ShuttleProcessingOrderActionTarget order, int direction);

        bool SetOrderSuspended(ShuttleProcessingOrderActionTarget order, bool suspended);

        void OpenOrderProductionPolicyDialog(ShuttleProcessingOrderActionTarget order);

        void OpenOrderIngredientFilterDialog(ShuttleProcessingOrderActionTarget order);
    }
}
