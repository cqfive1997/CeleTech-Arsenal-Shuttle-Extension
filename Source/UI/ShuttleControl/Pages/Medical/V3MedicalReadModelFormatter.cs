using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalReadModelFormatter
    {
        internal string GetMedicineStateText(V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null || !pageModel.MedicalBayInstalled)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_Unavailable");
            }

            if (pageModel.EstimatedMedicineNeed <= 0)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_MedicineState_Enough");
            }

            if (pageModel.AvailableMedicineCount <= 0)
            {
                return ShuttleUIText.Tr("CT_Shuttle_MedicalBay_TendPatientNoMedicine");
            }

            if (pageModel.AvailableMedicineCount >= pageModel.EstimatedMedicineNeed * 2)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_MedicineState_Enough");
            }

            if (pageModel.AvailableMedicineCount >= pageModel.EstimatedMedicineNeed)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_MedicineState_Tight");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Medical_MedicineState_Low");
        }

        internal string GetMedicalBayStateKey(V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null || !pageModel.MedicalBayInstalled)
            {
                return "Missing";
            }

            if (!pageModel.MedicalBayEnabled)
            {
                return "Disabled";
            }

            if (pageModel.PowerStatusPlaceholder)
            {
                return "PowerPending";
            }

            if (!pageModel.MedicalBayPowered)
            {
                return "Unpowered";
            }

            if (pageModel.MedicalBayFull)
            {
                return "Full";
            }

            if (pageModel.MedicalBayMedicineLow)
            {
                return "MedicineLow";
            }

            if (pageModel.PatientCount <= 0)
            {
                return "NoPatients";
            }

            if (pageModel.MedicalBayHasPendingTreatment)
            {
                return "Pending";
            }

            return "Normal";
        }

        internal string GetMedicalBayStateText(string stateKey)
        {
            if (stateKey == "Disabled")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Disabled");
            }

            if (stateKey == "Unpowered")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Unpowered");
            }

            if (stateKey == "PowerPending")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_PowerUnavailable");
            }

            if (stateKey == "Full")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_BayState_Full");
            }

            if (stateKey == "MedicineLow")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Issue_MedicalBayMedicineLow_Title");
            }

            if (stateKey == "NoPatients")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Medical_NoPatients");
            }

            if (stateKey == "Pending")
            {
                return ShuttleUIText.Tr("CT_Shuttle_UI_Status_Pending");
            }

            if (stateKey == "Normal")
            {
                return ShuttleUIText.Tr("CT_Shuttle_Common_Normal");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Common_Missing");
        }

        internal string GetMedicineSeverityKey(V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null || !pageModel.MedicalBayInstalled)
            {
                return "Moderate";
            }

            if (pageModel.EstimatedMedicineNeed <= 0)
            {
                return "Stable";
            }

            if (pageModel.AvailableMedicineCount <= 0)
            {
                return "Critical";
            }

            if (pageModel.AvailableMedicineCount < pageModel.EstimatedMedicineNeed)
            {
                return "Severe";
            }

            if (pageModel.AvailableMedicineCount < pageModel.EstimatedMedicineNeed * 2)
            {
                return "Moderate";
            }

            return "Stable";
        }

        internal string GetBaySeverityKey(V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null || !pageModel.MedicalBayInstalled)
            {
                return "Moderate";
            }

            if (pageModel.MedicalBayStateKey == "Unpowered" ||
                pageModel.MedicalBayStateKey == "MedicineLow")
            {
                return "Critical";
            }

            if (pageModel.MedicalBayStateKey == "Disabled" ||
                pageModel.MedicalBayStateKey == "Full")
            {
                return "Severe";
            }

            if (pageModel.MedicalBayStateKey == "PowerPending" ||
                pageModel.MedicalBayStateKey == "Pending")
            {
                return "Moderate";
            }

            return "Stable";
        }

        internal string GetHediffFallbackSummary(ShuttleMedicalHediffReadModel hediff)
        {
            if (hediff == null)
            {
                return string.Empty;
            }

            if (hediff.Tendable && !hediff.Tended)
            {
                return ShuttleUIText.Tr("CT_Shuttle_MedicalBay_HediffUntended");
            }

            if (hediff.Tended)
            {
                return ShuttleUIText.Tr("CT_Shuttle_MedicalBay_HediffTended");
            }

            return hediff.Severity > 0f
                ? hediff.Severity.ToString("0.##")
                : string.Empty;
        }

        internal string GetPositiveVitalSeverity(float value01)
        {
            if (value01 < 0f)
            {
                return "Moderate";
            }

            if (value01 < 0.25f)
            {
                return "Severe";
            }

            if (value01 < 0.50f)
            {
                return "Moderate";
            }

            return "Stable";
        }

        internal string GetNegativeVitalSeverity(float value01)
        {
            if (value01 < 0f)
            {
                return "Moderate";
            }

            if (value01 >= 0.75f)
            {
                return "Severe";
            }

            if (value01 >= 0.35f)
            {
                return "Moderate";
            }

            return "Stable";
        }

        internal float GetCapacityPct(Pawn pawn, PawnCapacityDef capacityDef)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.capacities == null ||
                capacityDef == null)
            {
                return -1f;
            }

            return Mathf.Clamp01(pawn.health.capacities.GetLevel(capacityDef));
        }

        internal string ValueOrFallback(string value, string fallback)
        {
            return !string.IsNullOrEmpty(value) ? value : fallback;
        }

    }
}
