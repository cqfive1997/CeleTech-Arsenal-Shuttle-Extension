using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal sealed class MedicalBayResourceReadModelBuilder
    {
        private readonly IShuttleMedicalSupplySource medicalSupplySource =
            new ShuttleLoadedCargoMedicineSupplySource();

        internal IReadOnlyList<ShuttleMedicineSupplyReadModel> ListLoadedCargoMedicine(
            ThingWithComps shuttleHost,
            Pawn patient)
        {
            return this.medicalSupplySource != null
                ? this.medicalSupplySource.ListLoadedMedicineForPatient(shuttleHost, patient)
                : new List<ShuttleMedicineSupplyReadModel>();
        }

        internal void ApplyLoadedCargoMedicineSummary(
            ShuttleMedicalBayReadModel model,
            IReadOnlyList<ShuttleMedicineSupplyReadModel> loadedMedicine,
            int allowedCount)
        {
            if (model == null)
            {
                return;
            }

            model.LoadedCargoMedicine = loadedMedicine ?? new List<ShuttleMedicineSupplyReadModel>();
            model.LoadedCargoMedicineStackCount = model.LoadedCargoMedicine.Count;
            model.LoadedCargoMedicineTotalCount = this.CountCargoMedicine(model.LoadedCargoMedicine);
            model.LoadedCargoMedicineAllowedCount = allowedCount;
            model.LoadedCargoMedicineSummaryLabel = model.LoadedCargoMedicineTotalCount > 0
                ? "CT_Shuttle_MedicalBay_LoadedCargoMedicineSummary".Translate(
                    model.LoadedCargoMedicineStackCount,
                    model.LoadedCargoMedicineTotalCount).ToString()
                : "CT_Shuttle_MedicalBay_NoLoadedCargoMedicine".Translate().ToString();
        }

        internal void FillPatientCargoMedicineReadModel(
            Pawn pawn,
            ShuttleMedicalPatientReadModel model,
            ThingWithComps shuttleHost)
        {
            if (model == null)
            {
                return;
            }

            IReadOnlyList<ShuttleMedicineSupplyReadModel> loadedMedicine =
                this.ListLoadedCargoMedicine(shuttleHost, pawn);
            model.LoadedCargoMedicine = loadedMedicine;
            model.LoadedCargoMedicineAllowedCount = this.CountAllowedCargoMedicine(loadedMedicine);
            model.LoadedCargoMedicineSummaryLabel =
                "CT_Shuttle_MedicalBay_CargoMedicineAllowedForPatient"
                    .Translate(model.LoadedCargoMedicineAllowedCount)
                    .ToString();
        }

        internal void TrackPatientAllowedCargoMedicine(
            ShuttleMedicalPatientReadModel patientModel,
            HashSet<int> allowedThingIds)
        {
            if (patientModel == null ||
                patientModel.LoadedCargoMedicine == null ||
                allowedThingIds == null)
            {
                return;
            }

            for (int i = 0; i < patientModel.LoadedCargoMedicine.Count; i++)
            {
                ShuttleMedicineSupplyReadModel medicine = patientModel.LoadedCargoMedicine[i];
                if (medicine != null &&
                    medicine.AllowedByPatientMedCare &&
                    medicine.SourceThingID > 0)
                {
                    allowedThingIds.Add(medicine.SourceThingID);
                }
            }
        }

        internal int CountAllowedMedicineByThingIds(
            IReadOnlyList<ShuttleMedicineSupplyReadModel> loadedMedicine,
            HashSet<int> allowedThingIds)
        {
            if (loadedMedicine == null || allowedThingIds == null || allowedThingIds.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < loadedMedicine.Count; i++)
            {
                ShuttleMedicineSupplyReadModel medicine = loadedMedicine[i];
                if (medicine != null &&
                    medicine.SourceThingID > 0 &&
                    medicine.StackCount > 0 &&
                    allowedThingIds.Contains(medicine.SourceThingID))
                {
                    count += medicine.StackCount;
                }
            }

            return count;
        }

        private int CountCargoMedicine(IReadOnlyList<ShuttleMedicineSupplyReadModel> loadedMedicine)
        {
            if (loadedMedicine == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < loadedMedicine.Count; i++)
            {
                ShuttleMedicineSupplyReadModel medicine = loadedMedicine[i];
                if (medicine != null && medicine.StackCount > 0)
                {
                    count += medicine.StackCount;
                }
            }

            return count;
        }

        private int CountAllowedCargoMedicine(IReadOnlyList<ShuttleMedicineSupplyReadModel> loadedMedicine)
        {
            if (loadedMedicine == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < loadedMedicine.Count; i++)
            {
                ShuttleMedicineSupplyReadModel medicine = loadedMedicine[i];
                if (medicine != null &&
                    medicine.AllowedByPatientMedCare &&
                    medicine.StackCount > 0)
                {
                    count += medicine.StackCount;
                }
            }

            return count;
        }
    }
}
