using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayTreatmentContextBuilder
    {
        internal bool TryBuildTreatmentContext(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireReservation,
            MedicalBayTreatmentQueryService queryService,
            MedicalBayTreatmentPreflightService preflightService,
            out MedicalBayTreatmentContext context,
            out bool releaseTreatmentReservation,
            out string failReason)
        {
            context = default(MedicalBayTreatmentContext);
            releaseTreatmentReservation = false;
            failReason = null;

            CompShuttleMedicalBayOccupancy occupancy;
            if (!queryService.TryGetOccupancy(shuttleHost, out occupancy, out failReason))
            {
                return false;
            }

            if (!preflightService.CanUseShuttleForTreatment(shuttleHost, out failReason))
            {
                return false;
            }

            Pawn patient = queryService.FindContainedPatientByThingID(occupancy, patientThingID);
            if (!preflightService.CanUseContainedPatientForTreatment(occupancy, patient, out failReason))
            {
                return false;
            }

            if (!preflightService.CanUseDoctorForTreatment(doctor, shuttleHost, out failReason))
            {
                return false;
            }

            MedicalBayTendState tendState = queryService.ReadTendState(patient);
            if (tendState.UntendedTendableCount <= 0 ||
                !queryService.PatientHasVanillaTendNeed(patient))
            {
                context = new MedicalBayTreatmentContext(occupancy, patient, tendState);
                releaseTreatmentReservation = true;
                failReason = "CT_Shuttle_MedicalBay_NoTendableHediffs".Translate().ToString();
                return false;
            }

            int doctorThingID = doctor != null ? doctor.thingIDNumber : -1;
            int reservationDoctorThingID = occupancy.GetTreatmentReservationDoctorThingID(patientThingID);
            if (requireReservation)
            {
                if (reservationDoctorThingID != doctorThingID)
                {
                    failReason = "CT_Shuttle_MedicalBay_TreatmentReserved".Translate().ToString();
                    return false;
                }
            }
            else if (reservationDoctorThingID > 0 && reservationDoctorThingID != doctorThingID)
            {
                failReason = "CT_Shuttle_MedicalBay_TreatmentReserved".Translate().ToString();
                return false;
            }

            context = new MedicalBayTreatmentContext(occupancy, patient, tendState);
            return true;
        }
    }

    internal struct MedicalBayTreatmentContext
    {
        internal MedicalBayTreatmentContext(
            CompShuttleMedicalBayOccupancy occupancy,
            Pawn patient,
            MedicalBayTendState tendState)
        {
            this.Occupancy = occupancy;
            this.Patient = patient;
            this.TendState = tendState;
        }

        internal readonly CompShuttleMedicalBayOccupancy Occupancy;
        internal readonly Pawn Patient;
        internal readonly MedicalBayTendState TendState;
    }
}
