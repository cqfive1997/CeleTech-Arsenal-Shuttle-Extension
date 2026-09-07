using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalProcedureModelBuilder
    {
        private readonly V3MedicalReadModelFormatter formatter;

        internal V3MedicalProcedureModelBuilder(
            V3MedicalReadModelFormatter formatter)
        {
            this.formatter = formatter;
        }

        internal void ApplyActiveProcedureState(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay)
        {
            if (pageModel == null)
            {
                return;
            }

            pageModel.HasActiveProcedure = medicalBay != null && medicalBay.HasActiveProcedure;
            pageModel.ActiveProcedureType = medicalBay != null ? medicalBay.ActiveProcedureType : null;
            pageModel.ActiveProcedureStatus = medicalBay != null ? medicalBay.ActiveProcedureStatus : null;
            pageModel.ActiveProcedureID = medicalBay != null ? medicalBay.ActiveProcedureID : 0;
            pageModel.ActiveProcedureDoctorThingID = medicalBay != null
                ? medicalBay.ActiveProcedureDoctorThingID
                : -1;
            pageModel.ActiveProcedurePatientThingID = medicalBay != null
                ? medicalBay.ActiveProcedurePatientThingID
                : -1;
            pageModel.ActiveProcedureDoctorLabel = medicalBay != null
                ? medicalBay.ActiveProcedureDoctorLabel
                : null;
            pageModel.ActiveProcedurePatientLabel = medicalBay != null
                ? medicalBay.ActiveProcedurePatientLabel
                : null;
            pageModel.ActiveProcedureWorkTicksDone = medicalBay != null
                ? Mathf.Max(0, medicalBay.ActiveProcedureWorkTicksDone)
                : 0;
            pageModel.ActiveProcedureWorkTicksTotal = medicalBay != null
                ? Mathf.Max(0, medicalBay.ActiveProcedureWorkTicksTotal)
                : 0;
            pageModel.ActiveProcedureProgress = medicalBay != null
                ? Mathf.Clamp01(medicalBay.ActiveProcedureProgress)
                : 0f;
            pageModel.ActiveProcedureTendCyclesCompleted = medicalBay != null
                ? Mathf.Max(0, medicalBay.ActiveProcedureTendCyclesCompleted)
                : 0;
            pageModel.ActiveProcedureLastKnownRemainingTendableCount = medicalBay != null
                ? Mathf.Max(0, medicalBay.ActiveProcedureLastKnownRemainingTendableCount)
                : 0;
            pageModel.ActiveProcedureStatusLabel = medicalBay != null &&
                !string.IsNullOrEmpty(medicalBay.ActiveProcedureStatusLabel)
                    ? medicalBay.ActiveProcedureStatusLabel
                    : ShuttleUIText.Tr("CT_Shuttle_MedicalProcedure_Status_None");
            pageModel.ActiveProcedureTooltip = medicalBay != null &&
                !string.IsNullOrEmpty(medicalBay.ActiveProcedureTooltip)
                    ? medicalBay.ActiveProcedureTooltip
                    : pageModel.ActiveProcedureStatusLabel;
        }

        internal V3MedicalDetailLineModel BuildActiveProcedureDetail(
            V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null)
            {
                return null;
            }

            int percent = Mathf.RoundToInt(
                Mathf.Clamp01(pageModel.ActiveProcedureProgress) * 100f);
            V3MedicalDetailLineModel line = new V3MedicalDetailLineModel();
            line.Label = pageModel.ActiveProcedureStatusLabel;
            line.Summary =
                ShuttleUIText.Tr(
                    "CT_Shuttle_MedicalProcedure_Doctor",
                    this.formatter.ValueOrFallback(
                        pageModel.ActiveProcedureDoctorLabel,
                        "-")) +
                " / " +
                ShuttleUIText.Tr(
                    "CT_Shuttle_MedicalProcedure_Progress",
                    pageModel.ActiveProcedureWorkTicksDone,
                    pageModel.ActiveProcedureWorkTicksTotal,
                    percent);
            line.PendingTreatment = true;
            line.IsPlaceholder = false;
            line.SeverityKey = pageModel.ActiveProcedureStatus == "RecoveryRequired"
                ? "Critical"
                : "Moderate";
            return line;
        }
    }
}
