using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayMedicineSourceResolver
    {
        internal bool HasAnyDoctorInventoryMedicine(
            Pawn doctor,
            MedicalBayTreatmentQueryService queryService)
        {
            return queryService.HasAnyDoctorInventoryMedicine(doctor);
        }

        internal bool HasAnyLoadedCargoMedicine(
            ThingWithComps shuttleHost,
            ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource)
        {
            return loadedCargoMedicineSupplySource != null &&
                loadedCargoMedicineSupplySource.HasAnyLoadedMedicine(shuttleHost);
        }

        internal bool TryFindDoctorInventoryMedicine(
            Pawn doctor,
            Pawn patient,
            MedicalBayTreatmentQueryService queryService,
            out Medicine medicine,
            out string failReason)
        {
            return queryService.TryFindDoctorInventoryMedicine(
                doctor,
                patient,
                out medicine,
                out failReason);
        }

        internal bool TryFindSingleStackDoctorInventoryMedicine(
            Pawn doctor,
            Pawn patient,
            MedicalBayTreatmentQueryService queryService,
            out Medicine medicine,
            out string failReason)
        {
            return queryService.TryFindSingleStackDoctorInventoryMedicine(
                doctor,
                patient,
                out medicine,
                out failReason);
        }

        internal bool CanFindAnyAllowedMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            Pawn patient,
            MedicalBayTreatmentQueryService queryService,
            ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource,
            out string failReason)
        {
            Medicine inventoryMedicine;
            string inventoryFailReason;
            if (this.TryFindDoctorInventoryMedicine(
                doctor,
                patient,
                queryService,
                out inventoryMedicine,
                out inventoryFailReason))
            {
                failReason = null;
                return true;
            }

            string cargoFailReason = null;
            if (loadedCargoMedicineSupplySource != null &&
                loadedCargoMedicineSupplySource.CanProvideLoadedMedicineForPatient(
                    shuttleHost,
                    patient,
                    out cargoFailReason))
            {
                failReason = null;
                return true;
            }

            failReason = this.BuildAnyAllowedMedicineUnavailableReason(
                inventoryFailReason,
                cargoFailReason);
            return false;
        }

        internal string BuildAnyAllowedMedicineUnavailableReason(
            string inventoryFailReason,
            string cargoFailReason)
        {
            string noAllowedDoctorMedicine = "CT_Shuttle_MedicalBay_NoAllowedDoctorMedicine".Translate().ToString();
            string noAllowedLoadedCargoMedicine = "CT_Shuttle_MedicalBay_NoAllowedLoadedCargoMedicine".Translate().ToString();
            if (inventoryFailReason == noAllowedDoctorMedicine ||
                cargoFailReason == noAllowedLoadedCargoMedicine)
            {
                return "CT_Shuttle_MedicalBay_NoAllowedMedicine".Translate().ToString();
            }

            return "CT_Shuttle_MedicalBay_NoAvailableMedicine".Translate().ToString();
        }
    }
}
