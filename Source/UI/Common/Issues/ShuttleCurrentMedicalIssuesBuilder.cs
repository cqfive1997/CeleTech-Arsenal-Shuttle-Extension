using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentMedicalIssuesBuilder
    {
        private const int MedicineLowThreshold = 10;

        internal void AddIssues(
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || issues == null)
            {
                return;
            }

            ShuttleMedicalBayReadModel medical = model.MedicalBay;
            bool hasMedicalBay = medical != null && medical.HasMedicalBay;
            bool hasPrisonTreatment = model.PrisonCell != null &&
                model.PrisonCell.HasPrisonCell;
            int patientCount = this.GetMedicalPatientCount(medical, cargoSnapshot);
            if (!hasMedicalBay)
            {
                if (patientCount > 0)
                {
                    ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                        "medical:patients-without-bay",
                        ShuttleIssueSeverity.Error,
                        ShuttleIssueCategory.Medical,
                        "CT_Shuttle_Issue_MedicalBayPatientsWithoutModule_Title",
                        "CT_Shuttle_Issue_MedicalBayPatientsWithoutModule_Detail",
                        ShuttleIssueActionKind.OpenAssembly,
                        120);
                    issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                        "CT_Shuttle_Issue_Evidence_PatientCount",
                        patientCount.ToString()));
                    ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
                }
            }
            else if (patientCount > 0 &&
                medical.MedicalBayPoweredKnown &&
                !medical.MedicalBayPowered)
            {
                this.AddSimpleIssue(
                    issues,
                    "medical:bay-offline",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.Medical,
                    "CT_Shuttle_Issue_MedicalBayOffline_Title",
                    "CT_Shuttle_Issue_MedicalBayOffline_Detail",
                    ShuttleIssueActionKind.OpenMedical,
                    130);
            }

            if (hasMedicalBay &&
                patientCount > 0 &&
                medical.MedicalPatientSlots > 0 &&
                medical.FreePatientSlots <= 0)
            {
                ShuttleIssueReadModel fullIssue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    "medical:bay-full",
                    ShuttleIssueSeverity.Warning,
                    ShuttleIssueCategory.Medical,
                    "CT_Shuttle_Issue_MedicalBayFull_Title",
                    "CT_Shuttle_Issue_MedicalBayFull_Detail",
                    ShuttleIssueActionKind.OpenMedical,
                    135);
                fullIssue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_PatientCount",
                    patientCount.ToString()));
                fullIssue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_Capacity",
                    medical.MedicalPatientSlots.ToString()));
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, fullIssue);
            }

            if (!hasMedicalBay && !hasPrisonTreatment)
            {
                return;
            }

            int pendingTreatment = this.CountPendingMedicalTreatment(medical);
            this.AddMedicineSupplyIssue(
                medical,
                model.CargoSupply,
                pendingTreatment,
                hasMedicalBay,
                issues);
        }

        private void AddMedicineSupplyIssue(
            ShuttleMedicalBayReadModel medical,
            ShuttleCargoSupplySnapshot supply,
            int pendingTreatment,
            bool openMedicalBay,
            List<ShuttleIssueReadModel> issues)
        {
            int totalCount = supply != null
                ? Mathf.Max(0, supply.RegularMedicineCount)
                : 0;
            int evidenceAvailableCount = totalCount;
            string stableId;
            string titleKey;
            string detailKey;

            if (supply == null || !supply.HasRegularCargo)
            {
                stableId = "medical:medicine-inaccessible";
                titleKey = "CT_Shuttle_Issue_MedicalBayMedicineLow_Title";
                detailKey = "CT_Shuttle_Issue_MedicalBayMedicineLow_Detail";
            }
            else if (totalCount <= 0)
            {
                stableId = "medical:medicine-depleted";
                titleKey = "CT_Shuttle_Issue_MedicalBayMedicineDepleted_Title";
                detailKey = "CT_Shuttle_Issue_MedicalBayMedicineDepleted_Detail";
            }
            else if (pendingTreatment > 0 &&
                medical != null &&
                medical.LoadedCargoMedicineAllowedCount <= 0)
            {
                stableId = "medical:medicine-inaccessible";
                titleKey = "CT_Shuttle_Issue_MedicalBayMedicineLow_Title";
                detailKey = "CT_Shuttle_Issue_MedicalBayMedicineLow_Detail";
                evidenceAvailableCount = 0;
            }
            else if (totalCount < MedicineLowThreshold)
            {
                stableId = "medical:medicine-low";
                titleKey = "CT_Shuttle_Issue_MedicalBayMedicineInsufficient_Title";
                detailKey = "CT_Shuttle_Issue_MedicalBayMedicineInsufficient_Detail";
            }
            else
            {
                return;
            }

            ShuttleIssueReadModel issue = ShuttleIssueReadModelFactory.CreateSimpleIssue(
                stableId,
                ShuttleIssueSeverity.Warning,
                openMedicalBay
                    ? ShuttleIssueCategory.Medical
                    : ShuttleIssueCategory.PrisonCell,
                titleKey,
                detailKey,
                openMedicalBay
                    ? ShuttleIssueActionKind.OpenMedical
                    : ShuttleIssueActionKind.OpenPrisonCell,
                140);
            if (pendingTreatment > 0)
            {
                issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                    "CT_Shuttle_Issue_Evidence_PendingTreatment",
                    pendingTreatment.ToString()));
            }

            issue.Evidence.Add(ShuttleIssuePlayerTextHelper.BuildEvidenceLine(
                "CT_Shuttle_Issue_Evidence_AvailableMedicine",
                evidenceAvailableCount.ToString()));
            ShuttleIssueReadModelSet.AddIssueIfUnique(issues, issue);
        }

        private int GetMedicalPatientCount(
            ShuttleMedicalBayReadModel medical,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            int patientCount = medical != null ? Mathf.Max(0, medical.PatientCount) : 0;
            if (patientCount <= 0 && medical != null && medical.Patients != null)
            {
                patientCount = medical.Patients.Count;
            }

            if (patientCount <= 0 && cargoSnapshot != null)
            {
                patientCount = Mathf.Max(0, cargoSnapshot.MedicalBayPatientCount);
            }

            return patientCount;
        }

        private int CountPendingMedicalTreatment(ShuttleMedicalBayReadModel medical)
        {
            if (medical == null || medical.Patients == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < medical.Patients.Count; i++)
            {
                ShuttleMedicalPatientReadModel patient = medical.Patients[i];
                if (patient == null)
                {
                    continue;
                }

                if (patient.NeedsTend || patient.HasUntendedInjury)
                {
                    count += Mathf.Max(1, patient.UntendedHediffCount);
                }
            }

            return count;
        }

        private void AddSimpleIssue(
            List<ShuttleIssueReadModel> issues,
            string stableId,
            ShuttleIssueSeverity severity,
            ShuttleIssueCategory category,
            string titleKey,
            string detailKey,
            ShuttleIssueActionKind actionKind,
            int sortPriority)
        {
            ShuttleIssueReadModelSet.AddIssueIfUnique(
                issues,
                ShuttleIssueReadModelFactory.CreateSimpleIssue(
                    stableId,
                    severity,
                    category,
                    titleKey,
                    detailKey,
                    actionKind,
                    sortPriority));
        }
    }
}
