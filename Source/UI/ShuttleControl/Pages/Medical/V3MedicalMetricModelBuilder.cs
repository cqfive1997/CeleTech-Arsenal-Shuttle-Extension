using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalMetricModelBuilder
    {
        private readonly V3MedicalReadModelFormatter formatter;

        internal V3MedicalMetricModelBuilder(
            V3MedicalReadModelFormatter formatter)
        {
            this.formatter = formatter;
        }

        internal void BuildSummaryMetrics(V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null)
            {
                return;
            }

            this.AddMetric(
                pageModel,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Patients"),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Patients"),
                pageModel.PatientCount.ToString(),
                "Stable");
            this.AddMetric(
                pageModel,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Beds"),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Beds"),
                pageModel.OccupiedBeds.ToString() + " / " +
                    pageModel.TotalBeds.ToString(),
                pageModel.TotalBeds > 0 && pageModel.OccupiedBeds >= pageModel.TotalBeds
                    ? "Severe"
                    : "Stable");
            this.AddMetric(
                pageModel,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_CriticalPatients"),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Critical"),
                pageModel.CriticalPatientCount.ToString(),
                pageModel.CriticalPatientCount > 0 ? "Severe" : "Stable");
            this.AddPendingMetric(pageModel);
            this.AddMedicineMetric(pageModel);
            this.AddBayMetric(pageModel);
        }

        private void AddPendingMetric(V3MedicalPageReadModel pageModel)
        {
            this.AddMetric(
                pageModel,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Pending"),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Pending"),
                pageModel.PendingTreatmentCount.ToString(),
                pageModel.PendingTreatmentCount > 0 ? "Moderate" : "Stable",
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_PendingTooltip"));
        }

        private void AddMedicineMetric(V3MedicalPageReadModel pageModel)
        {
            this.AddMetric(
                pageModel,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Medicine"),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Medicine"),
                pageModel.MedicineStateText,
                this.formatter.GetMedicineSeverityKey(pageModel),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Medicine") + ": " +
                    pageModel.AvailableMedicineCount.ToString() +
                    " / " + ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_EstimatedNeed") + ": " +
                    pageModel.EstimatedMedicineNeed.ToString());
        }

        private void AddBayMetric(V3MedicalPageReadModel pageModel)
        {
            this.AddMetric(
                pageModel,
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_BayStatus"),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_Bay"),
                pageModel.MedicalBayStateText,
                this.formatter.GetBaySeverityKey(pageModel),
                ShuttleUIText.Tr("CT_Shuttle_Medical_Metric_BayStatusTooltip"));
        }

        private void AddMetric(
            V3MedicalPageReadModel pageModel,
            string label,
            string shortLabel,
            string value,
            string severityKey)
        {
            this.AddMetric(
                pageModel,
                label,
                shortLabel,
                value,
                severityKey,
                label + ": " + value);
        }

        private void AddMetric(
            V3MedicalPageReadModel pageModel,
            string label,
            string shortLabel,
            string value,
            string severityKey,
            string tooltip)
        {
            V3MedicalSummaryMetricModel metric = new V3MedicalSummaryMetricModel();
            metric.Label = label;
            metric.ShortLabel = string.IsNullOrEmpty(shortLabel) ? label : shortLabel;
            metric.Value = value;
            metric.Tooltip = string.IsNullOrEmpty(tooltip) ? label + ": " + value : tooltip;
            metric.SeverityKey = severityKey;
            pageModel.SummaryMetrics.Add(metric);
        }

    }
}
