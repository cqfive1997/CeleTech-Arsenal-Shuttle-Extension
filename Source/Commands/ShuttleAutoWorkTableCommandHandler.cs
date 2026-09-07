using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttleAutoWorkTableCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is SetAutoWorkTableRecipeCommand ||
                command is ClearAutoWorkTableRecipeCommand ||
                command is SetAutoWorkTableProductionPolicyCommand ||
                command is SetAutoWorkTablePausedCommand ||
                command is AddAutoWorkTableOrderCommand ||
                command is RemoveAutoWorkTableOrderCommand ||
                command is MoveAutoWorkTableOrderCommand ||
                command is SetAutoWorkTableOrderSuspendedCommand ||
                command is SetAutoWorkTableOrderPolicyCommand ||
                command is SetAutoWorkTableOrderIngredientFilterCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            SetAutoWorkTableRecipeCommand setRecipe = command as SetAutoWorkTableRecipeCommand;
            if (setRecipe != null)
            {
                return this.ExecuteSetRecipe(context, setRecipe);
            }

            ClearAutoWorkTableRecipeCommand clearRecipe = command as ClearAutoWorkTableRecipeCommand;
            if (clearRecipe != null)
            {
                return this.ExecuteClearRecipe(context, clearRecipe);
            }

            SetAutoWorkTableProductionPolicyCommand setPolicy =
                command as SetAutoWorkTableProductionPolicyCommand;
            if (setPolicy != null)
            {
                return this.ExecuteSetProductionPolicy(context, setPolicy);
            }

            SetAutoWorkTablePausedCommand setPaused = command as SetAutoWorkTablePausedCommand;
            if (setPaused != null)
            {
                return this.ExecuteSetPaused(context, setPaused);
            }

            AddAutoWorkTableOrderCommand addOrder = command as AddAutoWorkTableOrderCommand;
            if (addOrder != null)
            {
                return this.ExecuteAddOrder(context, addOrder);
            }

            RemoveAutoWorkTableOrderCommand removeOrder = command as RemoveAutoWorkTableOrderCommand;
            if (removeOrder != null)
            {
                return this.ExecuteRemoveOrder(context, removeOrder);
            }

            MoveAutoWorkTableOrderCommand moveOrder = command as MoveAutoWorkTableOrderCommand;
            if (moveOrder != null)
            {
                return this.ExecuteMoveOrder(context, moveOrder);
            }

            SetAutoWorkTableOrderSuspendedCommand suspendOrder =
                command as SetAutoWorkTableOrderSuspendedCommand;
            if (suspendOrder != null)
            {
                return this.ExecuteSuspendOrder(context, suspendOrder);
            }

            SetAutoWorkTableOrderPolicyCommand setOrderPolicy =
                command as SetAutoWorkTableOrderPolicyCommand;
            if (setOrderPolicy != null)
            {
                return this.ExecuteSetOrderPolicy(context, setOrderPolicy);
            }

            SetAutoWorkTableOrderIngredientFilterCommand setIngredientFilter =
                command as SetAutoWorkTableOrderIngredientFilterCommand;
            if (setIngredientFilter != null)
            {
                return this.ExecuteSetOrderIngredientFilter(context, setIngredientFilter);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedAutoWorkTable".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetRecipe(
            ShuttleCommandContext context,
            SetAutoWorkTableRecipeCommand command)
        {
            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetAutoWorkTableSelectedRecipe(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.SourceBenchDefName,
                command.RecipeDefName,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_AutoWorkTableRecipeSelected".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteClearRecipe(
            ShuttleCommandContext context,
            ClearAutoWorkTableRecipeCommand command)
        {
            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TryClearAutoWorkTableSelectedRecipe(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_AutoWorkTableRecipeCleared".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetProductionPolicy(
            ShuttleCommandContext context,
            SetAutoWorkTableProductionPolicyCommand command)
        {
            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetAutoWorkTableProductionPolicy(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Mode,
                command.RepeatCount,
                command.TargetCount,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_AutoWorkTablePolicyUpdated".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetPaused(
            ShuttleCommandContext context,
            SetAutoWorkTablePausedCommand command)
        {
            if (context.ModuleRuntimeCoordinator == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            string failReason;
            if (!context.ModuleRuntimeCoordinator.TrySetAutoWorkTablePaused(
                context.AssemblyState,
                runtimeState,
                command.ModuleInstanceID,
                command.Paused,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded(
                command.Paused
                    ? "CT_Shuttle_Command_AutoWorkTablePaused".Translate().ToString()
                    : "CT_Shuttle_Command_AutoWorkTableResumed".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteAddOrder(
            ShuttleCommandContext context,
            AddAutoWorkTableOrderCommand command)
        {
            string failureReason = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TryAddAutoWorkTableProductionOrder(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    command.ModuleInstanceID,
                    command.SourceBenchDefName,
                    command.RecipeDefName,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason ??
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_AutoWorkTableOrderAdded".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteRemoveOrder(
            ShuttleCommandContext context,
            RemoveAutoWorkTableOrderCommand command)
        {
            string failureReason = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TryRemoveAutoWorkTableProductionOrder(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    command.ModuleInstanceID,
                    command.OrderId,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason ??
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_AutoWorkTableOrderRemoved".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteMoveOrder(
            ShuttleCommandContext context,
            MoveAutoWorkTableOrderCommand command)
        {
            string failureReason = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TryMoveAutoWorkTableProductionOrder(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    command.ModuleInstanceID,
                    command.OrderId,
                    command.Direction,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason ??
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_AutoWorkTableOrderMoved".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSuspendOrder(
            ShuttleCommandContext context,
            SetAutoWorkTableOrderSuspendedCommand command)
        {
            string failureReason = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TrySetAutoWorkTableProductionOrderSuspended(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    command.ModuleInstanceID,
                    command.OrderId,
                    command.Suspended,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason ??
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_AutoWorkTableOrderUpdated".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetOrderPolicy(
            ShuttleCommandContext context,
            SetAutoWorkTableOrderPolicyCommand command)
        {
            string failureReason = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TrySetAutoWorkTableProductionOrderPolicy(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    command.ModuleInstanceID,
                    command.OrderId,
                    command.Mode,
                    command.RepeatCount,
                    command.TargetCount,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason ??
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_AutoWorkTablePolicyUpdated".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetOrderIngredientFilter(
            ShuttleCommandContext context,
            SetAutoWorkTableOrderIngredientFilterCommand command)
        {
            string failureReason = null;
            if (context.ModuleRuntimeCoordinator == null ||
                !context.ModuleRuntimeCoordinator.TrySetAutoWorkTableProductionOrderIngredientFilter(
                    context.AssemblyState,
                    context.GetRuntimeState(),
                    command.ModuleInstanceID,
                    command.OrderId,
                    command.IngredientFilter,
                    command.UseRecipeDefault,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason ??
                    "CT_Shuttle_Command_ModuleRuntimeCoordinatorUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_AutoWorkTableIngredientFilterUpdated".Translate().ToString());
        }
    }
}
