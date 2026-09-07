using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayTreatmentEligibilityService
    {
        internal bool CanTendWithoutMedicine(
            MedicalBayTreatmentContext context,
            out string failReason)
        {
            failReason = null;
            return context.Patient != null && context.Occupancy != null;
        }

        internal bool CanContinueTending(
            ThingWithComps shuttleHost,
            Pawn doctor,
            MedicalBayTreatmentContext context,
            bool requireMedicine,
            MedicalBayMedicineSourceResolver medicineSourceResolver,
            MedicalBayTreatmentQueryService queryService,
            ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource,
            out int untendedTendableHediffCount,
            out string failReason)
        {
            failReason = null;
            untendedTendableHediffCount = context.TendState.UntendedTendableCount;
            if (!requireMedicine)
            {
                return true;
            }

            return this.CanTendWithAnyAllowedMedicine(
                shuttleHost,
                doctor,
                context,
                medicineSourceResolver,
                queryService,
                loadedCargoMedicineSupplySource,
                out failReason);
        }

        internal bool CanTendWithDoctorInventoryMedicine(
            Pawn doctor,
            MedicalBayTreatmentContext context,
            MedicalBayMedicineSourceResolver medicineSourceResolver,
            MedicalBayTreatmentQueryService queryService,
            out string failReason)
        {
            Medicine medicine;
            return medicineSourceResolver.TryFindDoctorInventoryMedicine(
                doctor,
                context.Patient,
                queryService,
                out medicine,
                out failReason);
        }

        internal bool CanTendWithAnyAllowedMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            MedicalBayTreatmentContext context,
            MedicalBayMedicineSourceResolver medicineSourceResolver,
            MedicalBayTreatmentQueryService queryService,
            ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource,
            out string failReason)
        {
            return medicineSourceResolver.CanFindAnyAllowedMedicine(
                shuttleHost,
                doctor,
                context.Patient,
                queryService,
                loadedCargoMedicineSupplySource,
                out failReason);
        }
    }
}
