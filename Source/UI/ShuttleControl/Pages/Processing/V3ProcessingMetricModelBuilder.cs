using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingMetricModelBuilder
    {
        internal void BuildMetrics(V3ProcessingReadModel model)
        {
            if (model == null)
            {
                return;
            }

            V3ProcessingWorkbenchModel selected = model.SelectedWorkbench;
            int onlineCount = this.CountOnlineWorkbenches(model);
            this.AddMetric(
                model,
                this.Tr("CT_Shuttle_Processing_Metric_OnlineWorkbenches"),
                this.Tr("CT_Shuttle_Processing_Metric_Online"),
                onlineCount.ToString(),
                onlineCount > 0 ? "Stable" : "Missing",
                this.Tr("CT_Shuttle_Processing_Metric_OnlineWorkbenchesTooltip"));
            this.AddMetric(
                model,
                this.Tr("CT_Shuttle_Processing_Metric_CurrentWorkbench"),
                this.Tr("CT_Shuttle_AutoWorkTable_Workbench"),
                selected != null ? selected.Label : "-",
                selected != null ? "Stable" : "Missing",
                selected != null ? selected.Tooltip : this.Tr("CT_Shuttle_Processing_Metric_NoWorkbenchSelected"));
            this.AddMetric(
                model,
                this.Tr("CT_Shuttle_AutoWorkTable_CurrentRecipe"),
                this.Tr("CT_Shuttle_Processing_Metric_Recipe"),
                this.GetBillMetricValue(selected),
                selected != null && selected.HasBillStackReadModel ? "Stable" : "Pending",
                this.Tr("CT_Shuttle_AutoWorkTable_CurrentRecipeTooltip"));
            this.AddMetric(
                model,
                this.Tr("CT_Shuttle_Processing_Metric_IngredientState"),
                this.Tr("CT_Shuttle_Processing_Label_Ingredients"),
                this.GetIngredientMetricValue(selected),
                this.GetIngredientMetricSeverity(selected),
                this.Tr("CT_Shuttle_AutoWorkTable_IngredientStateTooltip"));
            this.AddMetric(
                model,
                this.Tr("CT_Shuttle_Processing_Metric_WorkshopDraw"),
                this.Tr("CT_Shuttle_Processing_Label_Draw"),
                selected != null ? this.FormatWatts(selected.PowerWatts) : this.Tr("CT_Shuttle_Processing_Unavailable"),
                selected != null && selected.PowerWatts >= 0f ? "Stable" : "Pending",
                this.Tr("CT_Shuttle_AutoWorkTable_WorkshopDrawTooltip"));
            this.AddMetric(
                model,
                this.Tr("CT_Shuttle_Processing_Metric_WorkshopState"),
                this.Tr("CT_Shuttle_Processing_Label_State"),
                selected != null ? selected.StatusLabel : this.Tr("CT_Shuttle_Processing_Status_NotInstalled"),
                this.GetStatusSeverity(selected),
                selected != null ? selected.Tooltip : this.Tr("CT_Shuttle_Processing_Metric_NoWorkbenchAvailable"));
        }

        private int CountOnlineWorkbenches(V3ProcessingReadModel model)
        {
            int count = 0;
            if (model == null || model.Workbenches == null)
            {
                return 0;
            }

            for (int i = 0; i < model.Workbenches.Count; i++)
            {
                V3ProcessingWorkbenchModel workbench = model.Workbenches[i];
                if (workbench != null && workbench.Online)
                {
                    count++;
                }
            }

            return count;
        }

        private void AddMetric(
            V3ProcessingReadModel model,
            string label,
            string shortLabel,
            string value,
            string severityKey,
            string tooltip)
        {
            V3ProcessingMetricModel metric = new V3ProcessingMetricModel();
            metric.Label = label;
            metric.ShortLabel = string.IsNullOrEmpty(shortLabel) ? label : shortLabel;
            metric.Value = string.IsNullOrEmpty(value) ? "-" : value;
            metric.SeverityKey = severityKey;
            metric.Tooltip = string.IsNullOrEmpty(tooltip) ? metric.Label + ": " + metric.Value : tooltip;
            model.Metrics.Add(metric);
        }

        private string GetIngredientMetricValue(V3ProcessingWorkbenchModel selected)
        {
            if (selected == null)
            {
                return this.Tr("CT_Shuttle_Processing_Unavailable");
            }

            if (selected.StatusKey == "MissingIngredients")
            {
                return !string.IsNullOrEmpty(selected.MissingIngredientSummary)
                    ? selected.MissingIngredientSummary
                    : this.Tr("CT_Shuttle_Processing_Ingredient_Missing");
            }

            if (selected.StatusKey == "IngredientSystemUnavailable")
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_IngredientSystemUnavailable");
            }

            if (selected.StatusKey == "Paused")
            {
                return this.Tr("CT_Shuttle_AutoWorkTable_Paused");
            }

            if (selected.StatusKey == "Normal")
            {
                return this.Tr("CT_Shuttle_Processing_Ingredient_Ready");
            }

            if (selected.HasBillStackReadModel && selected.BillCount > 0)
            {
                return this.Tr("CT_Shuttle_Processing_Status_Executable") + " " +
                    selected.ExecutableBillCount.ToString() + "/" + selected.BillCount.ToString();
            }

            return this.Tr("CT_Shuttle_Processing_Unavailable");
        }

        private string GetIngredientMetricSeverity(V3ProcessingWorkbenchModel selected)
        {
            if (selected == null)
            {
                return "Pending";
            }

            if (selected.StatusKey == "IngredientSystemUnavailable")
            {
                return "Critical";
            }

            if (selected.StatusKey == "MissingIngredients" ||
                selected.StatusKey == "Paused")
            {
                return "Moderate";
            }

            if (selected.StatusKey == "Normal")
            {
                return "Stable";
            }

            return selected.HasBillStackReadModel ? "Stable" : "Pending";
        }

        private string GetBillMetricValue(V3ProcessingWorkbenchModel selected)
        {
            if (selected == null)
            {
                return "0";
            }

            if (selected.HasBillStackReadModel)
            {
                return selected.BillCount.ToString();
            }

            if (selected.Bills != null && selected.Bills.Count > 0)
            {
                return this.Tr("CT_Shuttle_Processing_RecipePreview");
            }

            return "0";
        }

        private string GetStatusSeverity(V3ProcessingWorkbenchModel selected)
        {
            if (selected == null)
            {
                return "Missing";
            }

            if (selected.StatusKey == "Unpowered" ||
                selected.StatusKey == "OutputBlocked" ||
                selected.StatusKey == "IngredientSystemUnavailable" ||
                selected.StatusKey == "Error")
            {
                return "Critical";
            }

            if (selected.StatusKey == "Disabled" ||
                selected.StatusKey == "MissingIngredients" ||
                selected.StatusKey == "Paused" ||
                selected.StatusKey == "NoBills" ||
                selected.StatusKey == "Pending")
            {
                return "Moderate";
            }

            return "Stable";
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
