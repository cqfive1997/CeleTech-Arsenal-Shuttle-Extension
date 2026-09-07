using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingBillModelBuilder
    {
        private const string WorkbenchModuleIconKey = "workbench_module";

        internal void BuildBillPlaceholders(
            V3ProcessingWorkbenchModel workbench,
            ShuttleAutoWorkTableModuleReadModel module,
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            if (workbench == null || module == null)
            {
                return;
            }

            if (module.Orders == null)
            {
                return;
            }

            for (int i = 0; i < module.Orders.Count; i++)
            {
                ShuttleAutoWorkTableOrderReadModel order = module.Orders[i];
                if (order == null)
                {
                    continue;
                }

                V3ProcessingBillModel bill = new V3ProcessingBillModel();
                bill.OrderId = order.OrderId;
                bill.QueueIndex = order.QueueIndex;
                bill.IsActive = order.IsActive;
                bill.CanMoveUp = order.CanMoveUp;
                bill.CanMoveDown = order.CanMoveDown;
                bill.HasCustomIngredientFilter = order.HasCustomIngredientFilter;
                bill.IngredientFilterRevision = order.IngredientFilterRevision;
                bill.IngredientFilter = order.IngredientFilter;
                bill.SupportsDoUntilStock = order.SupportsDoUntilStock;
                bill.DoUntilStockUnsupportedReason = order.DoUntilStockUnsupportedReason;
                bill.RequestedCount = order.RequestedCount;
                bill.TargetCount = order.TargetCount;
                bill.Id = workbench.Id + ":order:" + order.OrderId.ToString();
                bill.SourceBenchDefName = order.SourceBenchDefName;
                bill.RecipeDefName = order.RecipeDefName;
                bill.Label = !string.IsNullOrEmpty(order.RecipeLabel)
                    ? order.RecipeLabel
                    : V3ProcessingDisplayTextResolver.ResolveRecipeLabel(
                        order.RecipeDefName,
                        null);
                bill.IconKey = WorkbenchModuleIconKey;
                bill.RepeatModeKey = order.ProductionModeKey;
                bill.RepeatModeLabel =
                    V3ProcessingDisplayTextResolver.ResolveProductionModeLabel(
                        order.ProductionModeKey);
                bill.ProgressText = order.IsActive && module.HasActiveProduction && module.WorkLeft >= 0f
                    ? this.BuildActiveProductionProgressText(module, moduleDef)
                    : this.BuildOrderProgressText(order);
                bill.IngredientStatusKey = this.GetIngredientStatusKey(
                    order.IsSuspended ? "Paused" : order.Status);
                bill.IngredientStatusLabel =
                    V3ProcessingDisplayTextResolver.ResolveBillIngredientStatusLabel(
                        bill.IngredientStatusKey,
                        order.IngredientStatusSummary,
                        order.MissingIngredientSummary);
                bill.StatusKey = this.GetBillStatusKey(order, module);
                bill.StatusLabel =
                    V3ProcessingDisplayTextResolver.ResolveBillStatusLabel(
                        bill.StatusKey);
                bill.Suspended = order.IsSuspended;
                bill.IsPlaceholder = false;
                bill.Tooltip = bill.Label + "\n" + bill.RepeatModeLabel;
                if (!string.IsNullOrEmpty(order.FailureReason))
                {
                    bill.Tooltip += "\n" +
                        V3ProcessingDisplayTextResolver.ResolveRuntimeText(order.FailureReason);
                }

                workbench.Bills.Add(bill);
            }
        }

        private string BuildOrderProgressText(ShuttleAutoWorkTableOrderReadModel order)
        {
            if (order == null)
            {
                return "-";
            }

            if (order.ProductionModeKey == "RepeatCount")
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_RemainingCount") + ": " +
                    order.RemainingCount.ToString() + " / " + order.RequestedCount.ToString();
            }

            if (order.ProductionModeKey == "RepeatForever")
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_Mode_RepeatForever");
            }

            if (order.ProductionModeKey == "DoUntilStock")
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_CurrentStock") + ": " +
                    order.TargetStockCount.ToString() + " / " + order.TargetCount.ToString();
            }

            return this.Tr("CT_Shuttle_AutoWorkTable_RemainingCount") + ": " +
                order.RemainingCount.ToString();
        }

        private string BuildActiveProductionProgressText(
            ShuttleAutoWorkTableModuleReadModel module,
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            if (module == null)
            {
                return this.Tr("CT_Shuttle_Processing_Status_Working");
            }

            string recipeDefName = !string.IsNullOrEmpty(module.ActiveRecipeDefName)
                ? module.ActiveRecipeDefName
                : module.SelectedRecipeDefName;
            RecipeDef recipeDef = string.IsNullOrEmpty(recipeDefName)
                ? null
                : DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
            if (recipeDef == null)
            {
                return this.Tr("CT_Shuttle_Processing_Status_Working");
            }

            float factor = moduleDef != null && moduleDef.workAmountFactor > 0f
                ? moduleDef.workAmountFactor
                : 1f;
            float totalWork = Mathf.Max(1f, recipeDef.WorkAmountForStuff(null) * factor);
            float remaining = Mathf.Clamp(module.WorkLeft, 0f, totalWork);
            float progress = Mathf.Clamp01((totalWork - remaining) / totalWork);
            return (progress * 100f).ToString("0") + "%";
        }

        private string GetIngredientStatusKey(string runtimeStatus)
        {
            if (runtimeStatus == "WaitingForCargo")
            {
                return "SystemUnavailable";
            }

            if (runtimeStatus == "WaitingForIngredients")
            {
                return "Missing";
            }

            if (runtimeStatus == "Working" ||
                runtimeStatus == "Idle" ||
                runtimeStatus == "ProductionCompleted" ||
                runtimeStatus == "TargetSatisfied" ||
                runtimeStatus == "Paused")
            {
                return "Ready";
            }

            return "Pending";
        }

        private string GetBillStatusKey(
            ShuttleAutoWorkTableOrderReadModel order,
            ShuttleAutoWorkTableModuleReadModel module)
        {
            if (order == null)
            {
                return "Pending";
            }

            if (order.IsActive && module != null && module.HasActiveProduction)
            {
                return "Working";
            }

            if (order.IsSuspended || order.Status == "Paused")
            {
                return "Paused";
            }

            if (order.Status == "WaitingForIngredients")
            {
                return "WaitingIngredients";
            }

            if (order.Status == "WaitingForCargo")
            {
                return "WaitingCargo";
            }

            if (order.Status == "ProductionCompleted")
            {
                return "Completed";
            }

            if (order.Status == "TargetSatisfied")
            {
                return "TargetSatisfied";
            }

            if (order.Status == "Error")
            {
                return "Blocked";
            }

            return "Queued";
        }

        private string Tr(string key)
        {
            return V3ProcessingDisplayTextResolver.Tr(key);
        }
    }
}
