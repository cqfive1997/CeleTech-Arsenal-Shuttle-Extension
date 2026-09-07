using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Processing
{
    /// <summary>
    /// Focused command adapter for one queue order. Keeping this separate prevents recipe-list,
    /// global workbench, policy-dialog, and filter-dialog behavior from accumulating in one UI class.
    /// </summary>
    internal sealed class ShuttleProcessingOrderUIActionService :
        IShuttleProcessingProductionPolicyActions,
        IShuttleProcessingIngredientFilterActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action<IShuttleProcessingProductionPolicyActions, ShuttleProcessingOrderActionTarget>
            openProductionPolicyDialog;
        private readonly Action<IShuttleProcessingIngredientFilterActions, ShuttleProcessingOrderActionTarget>
            openIngredientFilterDialog;

        internal ShuttleProcessingOrderUIActionService(
            IShuttleCommandExecutor commandExecutor,
            Action<IShuttleProcessingProductionPolicyActions, ShuttleProcessingOrderActionTarget>
                openProductionPolicyDialog,
            Action<IShuttleProcessingIngredientFilterActions, ShuttleProcessingOrderActionTarget>
                openIngredientFilterDialog)
        {
            this.commandExecutor = commandExecutor;
            this.openProductionPolicyDialog = openProductionPolicyDialog;
            this.openIngredientFilterDialog = openIngredientFilterDialog;
        }

        internal bool RemoveOrder(ShuttleProcessingOrderActionTarget order)
        {
            if (!this.CanUseOrder(order))
            {
                ShuttleUICommandFeedback.ShowReject(
                    this.Tr("CT_Shuttle_AutoWorkTable_OrderMissing"));
                return false;
            }

            return this.Execute(new RemoveAutoWorkTableOrderCommand(
                order.ModuleInstanceID,
                order.OrderId));
        }

        internal bool MoveOrder(ShuttleProcessingOrderActionTarget order, int direction)
        {
            if (!this.CanUseOrder(order) ||
                (direction < 0 && !order.CanMoveUp) ||
                (direction > 0 && !order.CanMoveDown))
            {
                return false;
            }

            return this.Execute(new MoveAutoWorkTableOrderCommand(
                order.ModuleInstanceID,
                order.OrderId,
                direction));
        }

        internal bool SetOrderSuspended(
            ShuttleProcessingOrderActionTarget order,
            bool suspended)
        {
            return this.CanUseOrder(order) &&
                this.Execute(new SetAutoWorkTableOrderSuspendedCommand(
                    order.ModuleInstanceID,
                    order.OrderId,
                    suspended));
        }

        internal void OpenProductionPolicy(ShuttleProcessingOrderActionTarget order)
        {
            if (!this.CanUseOrder(order) || order.Active || this.openProductionPolicyDialog == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    this.Tr("CT_Shuttle_AutoWorkTable_CannotChangeActiveOrder"));
                return;
            }

            this.openProductionPolicyDialog(this, order);
        }

        internal void OpenIngredientFilter(ShuttleProcessingOrderActionTarget order)
        {
            if (!this.CanUseOrder(order) || order.Active || this.openIngredientFilterDialog == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    this.Tr("CT_Shuttle_AutoWorkTable_CannotChangeActiveOrder"));
                return;
            }

            this.openIngredientFilterDialog(this, order);
        }

        public bool CanSaveProductionPolicy(
            ShuttleProcessingOrderActionTarget order,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            return this.CanUseOrder(order) &&
                !order.Active &&
                (mode != AutoWorkTableProductionMode.RepeatCount || repeatCount > 0) &&
                (mode != AutoWorkTableProductionMode.DoUntilStock ||
                    (targetCount > 0 && order.SupportsDoUntilStock));
        }

        public bool SaveProductionPolicy(
            ShuttleProcessingOrderActionTarget order,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            return this.CanSaveProductionPolicy(order, mode, repeatCount, targetCount) &&
                this.Execute(new SetAutoWorkTableOrderPolicyCommand(
                    order.ModuleInstanceID,
                    order.OrderId,
                    mode,
                    repeatCount,
                    targetCount));
        }

        public string GetProductionPolicyTooltip(ShuttleProcessingOrderActionTarget order)
        {
            return order != null && !string.IsNullOrEmpty(order.Tooltip)
                ? order.Tooltip
                : this.Tr("CT_Shuttle_AutoWorkTable_ConfigPolicy");
        }

        public string GetDoUntilStockUnsupportedReason(ShuttleProcessingOrderActionTarget order)
        {
            return order != null && !string.IsNullOrEmpty(order.DoUntilStockUnsupportedReason)
                ? order.DoUntilStockUnsupportedReason
                : this.Tr("CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts");
        }

        public bool SaveIngredientFilter(
            ShuttleProcessingOrderActionTarget order,
            ThingFilter filter,
            bool useRecipeDefault)
        {
            return this.CanUseOrder(order) &&
                !order.Active &&
                this.Execute(new SetAutoWorkTableOrderIngredientFilterCommand(
                    order.ModuleInstanceID,
                    order.OrderId,
                    filter,
                    useRecipeDefault));
        }

        private bool CanUseOrder(ShuttleProcessingOrderActionTarget order)
        {
            return this.commandExecutor != null &&
                order != null &&
                order.OrderId > 0 &&
                !string.IsNullOrEmpty(order.ModuleInstanceID);
        }

        private bool Execute(IShuttleCommand command)
        {
            return ShuttleUICommandFeedback.ShowResult(
                this.commandExecutor.Execute(command));
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
