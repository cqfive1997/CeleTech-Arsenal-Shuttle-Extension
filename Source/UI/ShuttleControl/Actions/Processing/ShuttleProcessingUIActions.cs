using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Processing
{
    internal sealed class ShuttleProcessingUIActions : IShuttleProcessingUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly ShuttleProcessingOrderUIActionService orderActions;

        internal ShuttleProcessingUIActions(
            IShuttleCommandExecutor commandExecutor,
            Action<IShuttleProcessingProductionPolicyActions, ShuttleProcessingOrderActionTarget> openProductionPolicyDialog,
            Action<IShuttleProcessingIngredientFilterActions, ShuttleProcessingOrderActionTarget> openIngredientFilterDialog)
        {
            this.commandExecutor = commandExecutor;
            this.orderActions = new ShuttleProcessingOrderUIActionService(
                commandExecutor,
                openProductionPolicyDialog,
                openIngredientFilterDialog);
        }

        public bool CanSetSelectedRecipe(
            ShuttleProcessingWorkbenchActionTarget workbench,
            ShuttleProcessingRecipeActionTarget recipe)
        {
            return this.commandExecutor != null &&
                workbench != null &&
                recipe != null &&
                !string.IsNullOrEmpty(workbench.ModuleInstanceID) &&
                !string.IsNullOrEmpty(recipe.SourceBenchDefName) &&
                !string.IsNullOrEmpty(recipe.RecipeDefName) &&
                workbench.Enabled &&
                recipe.CanAdd;
        }

        public bool SetSelectedRecipe(
            ShuttleProcessingWorkbenchActionTarget workbench,
            ShuttleProcessingRecipeActionTarget recipe)
        {
            if (!this.CanSetSelectedRecipe(workbench, recipe))
            {
                this.ShowReject(this.GetSetSelectedRecipeTooltip(workbench, recipe));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new AddAutoWorkTableOrderCommand(
                    workbench.ModuleInstanceID,
                    recipe.SourceBenchDefName,
                    recipe.RecipeDefName));
            return this.ShowResult(result);
        }

        public string GetSetSelectedRecipeTooltip(
            ShuttleProcessingWorkbenchActionTarget workbench,
            ShuttleProcessingRecipeActionTarget recipe)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (workbench == null)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (recipe == null)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_RecipeInfoMissing");
            }

            if (string.IsNullOrEmpty(workbench.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (!workbench.Enabled)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_ModuleDisabled");
            }

            if (string.IsNullOrEmpty(recipe.SourceBenchDefName) ||
                string.IsNullOrEmpty(recipe.RecipeDefName))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_RecipeInfoMissing");
            }

            if (!recipe.CanAdd)
            {
                return recipe.Tooltip;
            }

            return this.Tr("CT_Shuttle_AutoWorkTable_SelectRecipeTooltip");
        }

        public bool CanClearSelectedRecipe(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            return this.commandExecutor != null &&
                workbench != null &&
                !string.IsNullOrEmpty(workbench.ModuleInstanceID) &&
                workbench.Enabled &&
                (!string.IsNullOrEmpty(workbench.SelectedRecipeDefName) ||
                    !string.IsNullOrEmpty(workbench.ActiveRecipeDefName));
        }

        public bool ClearSelectedRecipe(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            if (!this.CanClearSelectedRecipe(workbench))
            {
                this.ShowReject(this.GetClearSelectedRecipeTooltip(workbench));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new ClearAutoWorkTableRecipeCommand(workbench.ModuleInstanceID));
            return this.ShowResult(result);
        }

        public string GetClearSelectedRecipeTooltip(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (workbench == null)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (string.IsNullOrEmpty(workbench.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (!workbench.Enabled)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_ModuleDisabled");
            }

            if (string.IsNullOrEmpty(workbench.SelectedRecipeDefName) &&
                string.IsNullOrEmpty(workbench.ActiveRecipeDefName))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_NoSelectedRecipe");
            }

            return this.Tr("CT_Shuttle_AutoWorkTable_ClearRecipeTooltip");
        }

        public bool CanSetPaused(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            return this.commandExecutor != null &&
                workbench != null &&
                !string.IsNullOrEmpty(workbench.ModuleInstanceID) &&
                workbench.Enabled &&
                (!string.IsNullOrEmpty(workbench.SelectedRecipeDefName) ||
                    !string.IsNullOrEmpty(workbench.ActiveRecipeDefName));
        }

        public bool SetPaused(ShuttleProcessingWorkbenchActionTarget workbench, bool paused)
        {
            if (!this.CanSetPaused(workbench))
            {
                this.ShowReject(this.GetPausedTooltip(workbench));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetAutoWorkTablePausedCommand(workbench.ModuleInstanceID, paused));
            return this.ShowResult(result);
        }

        public string GetPausedTooltip(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (workbench == null)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (string.IsNullOrEmpty(workbench.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (!workbench.Enabled)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_ModuleDisabled");
            }

            if (string.IsNullOrEmpty(workbench.SelectedRecipeDefName) &&
                string.IsNullOrEmpty(workbench.ActiveRecipeDefName))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_NoSelectedRecipe");
            }

            return this.Tr("CT_Shuttle_AutoWorkTable_PauseTooltip");
        }

        public bool CanOpenProductionPolicy(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            return workbench != null &&
                !string.IsNullOrEmpty(workbench.ModuleInstanceID) &&
                (!string.IsNullOrEmpty(workbench.SelectedRecipeDefName) ||
                    !string.IsNullOrEmpty(workbench.ActiveRecipeDefName));
        }

        public void OpenProductionPolicyDialog(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            if (!this.CanOpenProductionPolicy(workbench))
            {
                this.ShowReject(this.GetProductionPolicyTooltip(workbench));
                return;
            }

            this.ShowReject(this.Tr("CT_Shuttle_AutoWorkTable_SelectOrderToConfigure"));
        }

        public bool CanSaveProductionPolicy(
            ShuttleProcessingWorkbenchActionTarget workbench,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            if (this.commandExecutor == null ||
                workbench == null ||
                string.IsNullOrEmpty(workbench.ModuleInstanceID) ||
                !workbench.Enabled ||
                string.IsNullOrEmpty(workbench.SelectedRecipeDefName))
            {
                return false;
            }

            if (mode == AutoWorkTableProductionMode.RepeatCount && repeatCount < 1)
            {
                return false;
            }

            if (mode == AutoWorkTableProductionMode.DoUntilStock && targetCount < 1)
            {
                return false;
            }

            if (mode == AutoWorkTableProductionMode.DoUntilStock &&
                !workbench.SelectedRecipeSupportsDoUntilStock)
            {
                return false;
            }

            return true;
        }

        public bool SaveProductionPolicy(
            ShuttleProcessingWorkbenchActionTarget workbench,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            if (!this.CanSaveProductionPolicy(workbench, mode, repeatCount, targetCount))
            {
                this.ShowReject(this.GetProductionPolicyTooltip(workbench));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetAutoWorkTableProductionPolicyCommand(
                    workbench.ModuleInstanceID,
                    mode,
                    repeatCount,
                    targetCount));
            return this.ShowResult(result);
        }

        public string GetProductionPolicyTooltip(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (workbench == null)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (string.IsNullOrEmpty(workbench.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchInfoMissing");
            }

            if (string.IsNullOrEmpty(workbench.SelectedRecipeDefName) &&
                string.IsNullOrEmpty(workbench.ActiveRecipeDefName))
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_NoSelectedRecipe");
            }

            if (!workbench.Enabled)
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_ModuleDisabled");
            }

            string text = this.Tr("CT_Shuttle_AutoWorkTable_ConfigPolicy");
            if (!workbench.SelectedRecipeSupportsDoUntilStock)
            {
                text += "\n" + this.GetDoUntilStockUnsupportedReason(workbench) +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_DynamicProductsUseRepeatForever");
            }

            if (!string.IsNullOrEmpty(workbench.PolicyTooltip))
            {
                text += "\n" + workbench.PolicyTooltip;
            }

            text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_CloseSavesPolicy");
            return text;
        }

        public string GetDoUntilStockUnsupportedReason(ShuttleProcessingWorkbenchActionTarget workbench)
        {
            return workbench != null &&
                !string.IsNullOrEmpty(workbench.SelectedRecipeDoUntilStockUnsupportedReason)
                    ? workbench.SelectedRecipeDoUntilStockUnsupportedReason
                    : this.Tr("CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts");
        }

        public bool RemoveOrder(ShuttleProcessingOrderActionTarget order)
        {
            return this.orderActions.RemoveOrder(order);
        }

        public bool MoveOrder(ShuttleProcessingOrderActionTarget order, int direction)
        {
            return this.orderActions.MoveOrder(order, direction);
        }

        public bool SetOrderSuspended(
            ShuttleProcessingOrderActionTarget order,
            bool suspended)
        {
            return this.orderActions.SetOrderSuspended(order, suspended);
        }

        public void OpenOrderProductionPolicyDialog(
            ShuttleProcessingOrderActionTarget order)
        {
            this.orderActions.OpenProductionPolicy(order);
        }

        public void OpenOrderIngredientFilterDialog(
            ShuttleProcessingOrderActionTarget order)
        {
            this.orderActions.OpenIngredientFilter(order);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
