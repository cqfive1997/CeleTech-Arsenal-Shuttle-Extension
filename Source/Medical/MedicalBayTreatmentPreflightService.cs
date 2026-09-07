using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayTreatmentPreflightService
    {
        internal bool CanUseShuttleForTreatment(
            ThingWithComps shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            ShuttleProfile profile;
            MedicalBayProfile medicalBay;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayProfile(shuttleHost, out profile, out medicalBay))
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (medicalBay.MedicalPatientSlots <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_NoSlots".Translate().ToString();
                return false;
            }

            if (!MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttleHost))
            {
                failReason = "CT_Shuttle_MedicalBay_TreatmentUnpowered".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanUseContainedPatient(
            CompShuttleMedicalBayOccupancy occupancy,
            Pawn patient,
            out string failReason)
        {
            failReason = null;
            if (patient == null || patient.Destroyed || patient.Dead)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (occupancy == null || !occupancy.ContainsPatient(patient) || patient.Spawned)
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanUseContainedPatientForTreatment(
            CompShuttleMedicalBayOccupancy occupancy,
            Pawn patient,
            out string failReason)
        {
            failReason = null;
            if (patient == null || patient.Destroyed || patient.Dead)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (occupancy == null || !occupancy.ContainsPatient(patient) || patient.Spawned)
            {
                failReason = "CT_Shuttle_MedicalBay_PatientNotHeld".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanUseDoctorForSpike(
            Pawn doctor,
            ThingWithComps shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (doctor == null ||
                doctor.Destroyed ||
                doctor.Dead ||
                !doctor.Spawned ||
                doctor.Map == null ||
                doctor.Downed)
            {
                failReason = "Medical Bay TendUtility test failed: doctor is unavailable.";
                return false;
            }

            if (!this.CanManipulate(doctor))
            {
                failReason = "Medical Bay TendUtility test failed: doctor cannot manipulate.";
                return false;
            }

            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null || doctor.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!doctor.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool CanUseDoctorForTreatment(
            Pawn doctor,
            ThingWithComps shuttleHost,
            out string failReason)
        {
            return MedicalBayAdmissionValidator.CanUseDoctorForMedicalBayTending(
                doctor,
                shuttleHost,
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
            if (queryService.TryFindDoctorInventoryMedicine(
                doctor,
                patient,
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

        internal bool CanManipulate(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        }

        private string BuildAnyAllowedMedicineUnavailableReason(
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
