using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingWorkbenchModelBuilder
    {
        private const string WorkbenchModuleIconKey = "workbench_module";
        private const string ModuleAutoKitchenIconKey = "module_auto_kitchen";

        private readonly V3ProcessingBillModelBuilder billBuilder;
        private readonly V3ProcessingRecipeModelBuilder recipeBuilder;

        internal V3ProcessingWorkbenchModelBuilder(
            V3ProcessingBillModelBuilder billBuilder,
            V3ProcessingRecipeModelBuilder recipeBuilder)
        {
            this.billBuilder = billBuilder;
            this.recipeBuilder = recipeBuilder;
        }

        internal void BuildWorkbenches(
            V3ProcessingReadModel model,
            ShuttleAutoWorkTableReadModel autoWorkTableModel,
            ShuttleControlReadModel controlModel)
        {
            if (model == null ||
                autoWorkTableModel == null ||
                autoWorkTableModel.Modules == null)
            {
                return;
            }

            for (int i = 0; i < autoWorkTableModel.Modules.Count; i++)
            {
                ShuttleAutoWorkTableModuleReadModel module = autoWorkTableModel.Modules[i];
                if (module == null)
                {
                    continue;
                }

                model.Workbenches.Add(this.BuildWorkbench(module, controlModel, i));
            }
        }

        internal void SelectWorkbench(V3ProcessingReadModel model, string selectedWorkbenchId)
        {
            if (model == null || model.Workbenches == null || model.Workbenches.Count == 0)
            {
                return;
            }

            for (int i = 0; i < model.Workbenches.Count; i++)
            {
                V3ProcessingWorkbenchModel workbench = model.Workbenches[i];
                if (workbench != null && workbench.Id == selectedWorkbenchId)
                {
                    model.SelectedWorkbench = workbench;
                    return;
                }
            }

            model.SelectedWorkbench = model.Workbenches[0];
        }

        private V3ProcessingWorkbenchModel BuildWorkbench(
            ShuttleAutoWorkTableModuleReadModel module,
            ShuttleControlReadModel controlModel,
            int index)
        {
            ShuttleAutoWorkTableModuleDef moduleDef = this.GetAutoWorkTableModuleDef(module.ModuleDefName);
            V3ProcessingWorkbenchModel workbench = new V3ProcessingWorkbenchModel();
            workbench.Id = !string.IsNullOrEmpty(module.ModuleInstanceID)
                ? module.ModuleInstanceID
                : "processing-workbench-" + index.ToString();
            workbench.ModuleInstanceID = module.ModuleInstanceID;
            workbench.Label =
                V3ProcessingDisplayTextResolver.ResolveAutoWorkTableModuleLabel(
                    module.ModuleDefName,
                    module.ModuleLabel);
            workbench.KindKey = this.GetWorkbenchKindKey(module, moduleDef);
            workbench.KindLabel =
                V3ProcessingDisplayTextResolver.ResolveWorkbenchKindLabel(
                    workbench.KindKey);
            workbench.IconKey = this.GetWorkbenchIconKey(workbench.KindKey);
            workbench.Installed = true;
            workbench.Enabled = module.IsEnabled;
            workbench.Paused = module.IsPaused;
            workbench.Powered = controlModel == null || controlModel.InternalBusPowered;
            workbench.HasRuntimeState = module.HasRuntimeState;
            workbench.PowerWatts = this.GetCurrentPowerWatts(module, moduleDef);
            workbench.SelectedSourceBenchDefName = module.SelectedSourceBenchDefName;
            workbench.SelectedRecipeDefName = module.SelectedRecipeDefName;
            workbench.ActiveSourceBenchDefName = module.ActiveSourceBenchDefName;
            workbench.ActiveRecipeDefName = module.ActiveRecipeDefName;
            workbench.CurrentRecipeLabel =
                V3ProcessingDisplayTextResolver.ResolveCurrentRecipeLabel(
                    module.SelectedRecipeDefName,
                    module.ActiveRecipeDefName);
            workbench.ProductionModeKey = string.IsNullOrEmpty(module.ProductionModeKey)
                ? "OneShot"
                : module.ProductionModeKey;
            workbench.ProductionModeLabel =
                V3ProcessingDisplayTextResolver.ResolveProductionModeLabel(
                    workbench.ProductionModeKey);
            workbench.RequestedCount = module.RequestedCount;
            workbench.RepeatCount = module.RequestedCount;
            workbench.RemainingCount = module.RemainingCount;
            workbench.TargetCount = module.TargetCount;
            workbench.CompletedCount = module.CompletedCount;
            workbench.TargetStockCount = module.TargetStockCount;
            workbench.TargetProductDefName = module.TargetProductDefName;
            workbench.TargetProductLabel =
                V3ProcessingDisplayTextResolver.ResolveThingLabel(
                    module.TargetProductDefName,
                    module.TargetProductLabel);
            workbench.SelectedRecipeSupportsDoUntilStock = module.SelectedRecipeSupportsDoUntilStock;
            workbench.SelectedRecipeDoUntilStockUnsupportedReason =
                !string.IsNullOrEmpty(module.SelectedRecipeDoUntilStockUnsupportedReason)
                    ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        module.SelectedRecipeDoUntilStockUnsupportedReason)
                    : null;
            workbench.PolicyStatusKey = this.GetPolicyStatusKey(module, workbench);
            workbench.PolicyStatusLabel =
                V3ProcessingDisplayTextResolver.ResolvePolicyStatusLabel(
                    workbench.PolicyStatusKey);
            workbench.IngredientStatusSummary =
                !string.IsNullOrEmpty(module.IngredientStatusSummary)
                    ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        module.IngredientStatusSummary)
                    : null;
            workbench.MissingIngredientSummary =
                !string.IsNullOrEmpty(module.MissingIngredientSummary)
                    ? V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        module.MissingIngredientSummary)
                    : null;
            workbench.TargetShortfallCount = Mathf.Max(0, workbench.TargetCount - workbench.TargetStockCount);
            workbench.PolicyTooltip = this.BuildPolicyTooltip(workbench);
            workbench.StatusKey = this.GetWorkbenchStatusKey(module, workbench);
            workbench.StatusLabel =
                V3ProcessingDisplayTextResolver.ResolveWorkbenchStatusLabel(
                    workbench.StatusKey);
            workbench.Online = workbench.Installed &&
                workbench.Enabled &&
                workbench.Powered &&
                module.HasRuntimeState &&
                workbench.StatusKey != "OutputBlocked" &&
                workbench.StatusKey != "Error";
            workbench.HasBillStackReadModel = false;
            workbench.BillCount = 0;
            workbench.ExecutableBillCount = 0;
            workbench.Tooltip = this.BuildWorkbenchTooltip(module, workbench);

            this.billBuilder.BuildBillPlaceholders(workbench, module, moduleDef);
            workbench.HasBillStackReadModel = true;
            workbench.BillCount = workbench.Bills.Count;
            workbench.ExecutableBillCount = 0;
            for (int billIndex = 0; billIndex < workbench.Bills.Count; billIndex++)
            {
                V3ProcessingBillModel bill = workbench.Bills[billIndex];
                if (bill != null &&
                    !bill.Suspended &&
                    bill.StatusKey != "Completed" &&
                    bill.StatusKey != "TargetSatisfied" &&
                    bill.StatusKey != "Blocked")
                {
                    workbench.ExecutableBillCount++;
                }
            }
            this.recipeBuilder.BuildRecipes(workbench, module);
            return workbench;
        }

        private float GetCurrentPowerWatts(
            ShuttleAutoWorkTableModuleReadModel module,
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            if (moduleDef == null)
            {
                return -1f;
            }

            if (module == null || !module.IsEnabled)
            {
                return 0f;
            }

            float powerWatts = Mathf.Max(0f, moduleDef.idlePowerDrawWatts);
            if (!module.IsPaused && module.HasActiveProduction)
            {
                powerWatts += Mathf.Max(0f, moduleDef.activePowerDrawWatts);
            }

            return powerWatts;
        }

        private ShuttleAutoWorkTableModuleDef GetAutoWorkTableModuleDef(string moduleDefName)
        {
            return string.IsNullOrEmpty(moduleDefName)
                ? null
                : DefDatabase<ShuttleAutoWorkTableModuleDef>.GetNamedSilentFail(moduleDefName);
        }

        private string GetWorkbenchKindKey(
            ShuttleAutoWorkTableModuleReadModel module,
            ShuttleAutoWorkTableModuleDef moduleDef)
        {
            string text = ((module != null ? module.ModuleDefName : string.Empty) + " " +
                (module != null ? module.ModuleLabel : string.Empty) + " " +
                (moduleDef != null ? moduleDef.defName : string.Empty)).ToLowerInvariant();
            if (text.Contains("kitchen") || text.Contains("cook") || text.Contains("meal"))
            {
                return "Kitchen";
            }

            if (text.Contains("drug") || text.Contains("medicine") || text.Contains("pharma"))
            {
                return "DrugLab";
            }

            if (text.Contains("machin") || text.Contains("smith") || text.Contains("fabricat"))
            {
                return "Machining";
            }

            if (text.Contains("research"))
            {
                return "Research";
            }

            return "AutoWorkTable";
        }

        private string GetWorkbenchIconKey(string key)
        {
            if (key == "Kitchen")
            {
                return ModuleAutoKitchenIconKey;
            }

            return WorkbenchModuleIconKey;
        }

        private string GetWorkbenchStatusKey(
            ShuttleAutoWorkTableModuleReadModel module,
            V3ProcessingWorkbenchModel workbench)
        {
            if (module == null)
            {
                return "Pending";
            }

            if (!workbench.Enabled)
            {
                return "Disabled";
            }

            if (!workbench.Powered)
            {
                return "Unpowered";
            }

            if (!module.HasRuntimeState)
            {
                return "Pending";
            }

            string status = module.Status ?? string.Empty;
            if (module.IsPaused)
            {
                return "Paused";
            }

            if (status == "OutputBlocked" || status == "CompletionCommitBlocked")
            {
                return "OutputBlocked";
            }

            if (status == "WaitingForCargo")
            {
                return "IngredientSystemUnavailable";
            }

            if (status == "WaitingForIngredients")
            {
                return "MissingIngredients";
            }

            if (status == "Paused")
            {
                return "Paused";
            }

            if (status == "Error" || status == "RecoveryBlocked")
            {
                return "Error";
            }

            if (string.IsNullOrEmpty(module.SelectedRecipeDefName) &&
                string.IsNullOrEmpty(module.ActiveRecipeDefName))
            {
                return "NoBills";
            }

            return "Normal";
        }

        private string GetPolicyStatusKey(
            ShuttleAutoWorkTableModuleReadModel module,
            V3ProcessingWorkbenchModel workbench)
        {
            if (module == null || workbench == null)
            {
                return "Pending";
            }

            if (!workbench.Enabled)
            {
                return "Disabled";
            }

            string status = module.Status ?? string.Empty;
            if (module.IsPaused)
            {
                return "Paused";
            }

            if (status == "Working")
            {
                return "Working";
            }

            if (status == "Paused")
            {
                return "Paused";
            }

            if (status == "ProductionCompleted")
            {
                return "Completed";
            }

            if (status == "TargetSatisfied")
            {
                return "TargetSatisfied";
            }

            if (status == "WaitingForCargo")
            {
                return "WaitingForCargo";
            }

            if (status == "WaitingForIngredients")
            {
                return "WaitingForIngredients";
            }

            if (status == "OutputBlocked" ||
                status == "Error" ||
                status == "RecoveryBlocked" ||
                status == "CompletionCommitBlocked")
            {
                return "Blocked";
            }

            return "Ready";
        }

        private string BuildPolicyTooltip(V3ProcessingWorkbenchModel workbench)
        {
            if (workbench == null)
            {
                return "-";
            }

            string text =
                this.Tr("CT_Shuttle_AutoWorkTable_ProductionMode") + ": " +
                workbench.ProductionModeLabel + "\n" +
                this.Tr("CT_Shuttle_AutoWorkTable_CompletedCount") + ": " +
                workbench.CompletedCount.ToString() + "\n" +
                this.Tr("CT_Shuttle_AutoWorkTable_PolicyStatus") + ": " +
                workbench.PolicyStatusLabel;

            if (workbench.ProductionModeKey == "RepeatCount")
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_RemainingCount") +
                    ": " + workbench.RemainingCount.ToString() +
                    " / " + this.Tr("CT_Shuttle_AutoWorkTable_RepeatCount") +
                    ": " + workbench.RequestedCount.ToString();
            }
            else if (workbench.ProductionModeKey == "DoUntilStock")
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_TargetCount") +
                    ": " + workbench.TargetCount.ToString() +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_CurrentStock") +
                    ": " + workbench.TargetStockCount.ToString() +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_TargetShortfall") +
                    ": " + workbench.TargetShortfallCount.ToString() +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_TargetProduct") +
                    ": " + workbench.TargetProductLabel +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_PrimaryProductOnly");
            }

            if (!workbench.SelectedRecipeSupportsDoUntilStock)
            {
                string reason = !string.IsNullOrEmpty(workbench.SelectedRecipeDoUntilStockUnsupportedReason)
                    ? workbench.SelectedRecipeDoUntilStockUnsupportedReason
                    : this.Tr("CT_Shuttle_AutoWorkTable_DoUntilStockUnsupportedDynamicProducts");
                text += "\n" + reason +
                    "\n" + this.Tr("CT_Shuttle_AutoWorkTable_DynamicProductsUseRepeatForever");
            }

            return text;
        }

        private string BuildWorkbenchTooltip(
            ShuttleAutoWorkTableModuleReadModel module,
            V3ProcessingWorkbenchModel workbench)
        {
            string text =
                workbench.Label + "\n" +
                this.Tr("CT_Shuttle_Processing_Label_Type") + ": " + workbench.KindLabel + "\n" +
                this.Tr("CT_Shuttle_Processing_Label_Status") + ": " + workbench.StatusLabel + "\n" +
                this.Tr("CT_Shuttle_AutoWorkTable_CurrentRecipe") + ": " + workbench.BillCount.ToString() + "\n" +
                this.Tr("CT_Shuttle_Processing_Label_PowerDraw") + ": " + this.FormatWatts(workbench.PowerWatts);

            if (workbench.KindKey == "Machining" ||
                workbench.KindKey == "DrugLab" ||
                workbench.KindKey == "Research")
            {
                text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchKindPendingTooltip");
            }

            if (module != null && !string.IsNullOrEmpty(module.LastFailureReason))
            {
                text += "\n" + this.Tr("CT_Shuttle_Processing_Label_LastReason") + ": " +
                    V3ProcessingDisplayTextResolver.ResolveRuntimeText(
                        module.LastFailureReason);
            }

            text += "\n" + this.Tr("CT_Shuttle_AutoWorkTable_WorkbenchSelectionTooltip");
            return text;
        }

        private string FormatWatts(float watts)
        {
            return ShuttleUIMetricFormatter.FormatWattsOrUnavailable(watts, this.Tr("CT_Shuttle_Processing_Unavailable"));
        }

        private string Tr(string key)
        {
            return V3ProcessingDisplayTextResolver.Tr(key);
        }
    }
}
