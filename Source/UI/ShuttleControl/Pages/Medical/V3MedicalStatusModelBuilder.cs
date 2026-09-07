using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalStatusModelBuilder
    {
        private readonly V3MedicalReadModelFormatter formatter;

        internal V3MedicalStatusModelBuilder(
            V3MedicalReadModelFormatter formatter)
        {
            this.formatter = formatter;
        }

        internal void ApplyMedicalBayInstallStatus(
            V3MedicalPageReadModel pageModel,
            ShuttleControlReadModel controlModel,
            ShuttleMedicalBayReadModel medicalBay)
        {
            if (pageModel == null)
            {
                return;
            }

            bool foundInstalledModule;
            bool foundEnabledModule;
            this.GetMedicalBayModuleStatus(
                controlModel,
                out foundInstalledModule,
                out foundEnabledModule);

            pageModel.MedicalBayInstalled =
                foundInstalledModule ||
                (medicalBay != null && medicalBay.HasMedicalBay);
            pageModel.HasMedicalBay = pageModel.MedicalBayInstalled;
            pageModel.MedicalBayEnabledKnown = foundInstalledModule;
            pageModel.MedicalBayEnabled = foundInstalledModule
                ? foundEnabledModule
                : (medicalBay != null && medicalBay.HasMedicalBay);

            bool poweredKnown = medicalBay != null && medicalBay.MedicalBayPoweredKnown;
            bool powered = poweredKnown
                ? medicalBay.MedicalBayPowered
                : controlModel != null && controlModel.InternalBusPowered;
            pageModel.MedicalBayPoweredKnown = poweredKnown || controlModel != null;
            pageModel.MedicalBayPowered =
                pageModel.MedicalBayInstalled &&
                pageModel.MedicalBayEnabled &&
                pageModel.MedicalBayPoweredKnown &&
                powered;
            pageModel.PowerStatusPlaceholder =
                pageModel.MedicalBayInstalled &&
                pageModel.MedicalBayEnabled &&
                !pageModel.MedicalBayPoweredKnown;
        }

        internal void ApplyCapacityAndSupply(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay)
        {
            if (pageModel == null)
            {
                return;
            }

            pageModel.TotalBeds = Mathf.Max(
                0,
                medicalBay != null ? medicalBay.MedicalPatientSlots : 0);
            pageModel.PatientCount = Mathf.Max(
                0,
                medicalBay != null ? medicalBay.PatientCount : 0);
            pageModel.OccupiedBeds = pageModel.PatientCount;
            pageModel.FreeBeds = Mathf.Max(
                0,
                medicalBay != null ? medicalBay.FreePatientSlots : 0);
            pageModel.AvailableMedicineCount = Mathf.Max(
                0,
                medicalBay != null ? medicalBay.LoadedCargoMedicineTotalCount : 0);
        }

        internal void ApplyPostPatientStatus(V3MedicalPageReadModel pageModel)
        {
            if (pageModel == null)
            {
                return;
            }

            pageModel.PatientCount = pageModel.Patients.Count > 0
                ? pageModel.Patients.Count
                : pageModel.PatientCount;
            pageModel.OccupiedBeds = pageModel.PatientCount;
            pageModel.EstimatedMedicineNeed = pageModel.PendingTreatmentCount;
            this.ApplyDerivedMedicalBayStatus(pageModel);
            this.ApplyAdmissionState(pageModel);
            pageModel.MedicineStateText = this.formatter.GetMedicineStateText(pageModel);
            pageModel.MedicalBayStateText =
                this.formatter.GetMedicalBayStateText(pageModel.MedicalBayStateKey);
            pageModel.ModuleStatusPlaceholder = false;
        }

        private void ApplyAdmissionState(V3MedicalPageReadModel pageModel)
        {
            pageModel.CanOpenAdmissionDialog = false;
            if (!pageModel.MedicalBayInstalled)
            {
                pageModel.AdmissionBlockedReason =
                    ShuttleUIText.Tr("CT_Shuttle_Medical_NotInstalled");
                return;
            }

            if (!pageModel.MedicalBayEnabled)
            {
                pageModel.AdmissionBlockedReason =
                    ShuttleUIText.Tr("CT_Shuttle_Medical_Disabled");
                return;
            }

            if (pageModel.PowerStatusPlaceholder)
            {
                pageModel.AdmissionBlockedReason =
                    ShuttleUIText.Tr("CT_Shuttle_Medical_StateUnavailableCannotAdmit");
                return;
            }

            if (!pageModel.MedicalBayPowered)
            {
                pageModel.AdmissionBlockedReason =
                    ShuttleUIText.Tr("CT_Shuttle_Medical_Unpowered");
                return;
            }

            if (pageModel.TotalBeds <= 0)
            {
                pageModel.AdmissionBlockedReason =
                    ShuttleUIText.Tr("CT_Shuttle_Medical_CannotAdmitNow");
                return;
            }

            if (pageModel.MedicalBayFull || pageModel.FreeBeds <= 0)
            {
                pageModel.AdmissionBlockedReason =
                    ShuttleUIText.Tr("CT_Shuttle_MedicalBay_Full");
                return;
            }

            pageModel.CanOpenAdmissionDialog = true;
            pageModel.AdmissionBlockedReason =
                pageModel.AdmissionCandidates.Count > 0
                    ? ShuttleUIText.Tr("CT_Shuttle_Medical_AdmitTooltip")
                    : ShuttleUIText.Tr("CT_Shuttle_Medical_AdmitNoCandidatesTooltip");
        }

        private void ApplyDerivedMedicalBayStatus(V3MedicalPageReadModel pageModel)
        {
            pageModel.MedicalBayFull =
                pageModel.MedicalBayInstalled &&
                pageModel.TotalBeds > 0 &&
                pageModel.OccupiedBeds >= pageModel.TotalBeds;
            pageModel.MedicalBayMedicineLow =
                pageModel.MedicalBayInstalled &&
                pageModel.PendingTreatmentCount > 0 &&
                pageModel.AvailableMedicineCount < Math.Max(1, pageModel.EstimatedMedicineNeed);
            pageModel.MedicalBayHasPendingTreatment =
                pageModel.MedicalBayInstalled &&
                pageModel.PendingTreatmentCount > 0;
            pageModel.MedicalBayOffline =
                pageModel.MedicalBayInstalled &&
                pageModel.MedicalBayEnabled &&
                pageModel.MedicalBayPoweredKnown &&
                !pageModel.MedicalBayPowered;
            pageModel.MedicalBayStateKey =
                this.formatter.GetMedicalBayStateKey(pageModel);
        }

        private void GetMedicalBayModuleStatus(
            ShuttleControlReadModel controlModel,
            out bool foundInstalledModule,
            out bool foundEnabledModule)
        {
            foundInstalledModule = false;
            foundEnabledModule = false;
            if (controlModel == null || controlModel.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = controlModel.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                this.ReadSegmentMedicalBayStatus(
                    segment,
                    ref foundInstalledModule,
                    ref foundEnabledModule);
            }
        }

        private void ReadSegmentMedicalBayStatus(
            ShuttleControlSegmentSlotModel segment,
            ref bool foundInstalledModule,
            ref bool foundEnabledModule)
        {
            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel module = segment.ModuleSlots[i];
                if (!this.IsInstalledMedicalBayModule(module))
                {
                    continue;
                }

                foundInstalledModule = true;
                if (module.InstalledModuleEnabled)
                {
                    foundEnabledModule = true;
                }
            }
        }

        private bool IsInstalledMedicalBayModule(ShuttleControlModuleSlotModel module)
        {
            if (module == null ||
                string.IsNullOrEmpty(module.InstalledModuleInstanceID))
            {
                return false;
            }

            if (module.InstalledModuleTypeID == ShuttleModuleTypeCatalog.MedicalBay)
            {
                return true;
            }

            return !string.IsNullOrEmpty(module.InstalledModuleDefName) &&
                module.InstalledModuleDefName.IndexOf(
                    "Medical",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
