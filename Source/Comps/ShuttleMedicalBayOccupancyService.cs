using Verse;
using Verse.AI;
using RimWorld;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Narrow command/service boundary for Medical Bay occupancy operations.
    /// UI and command handlers use this instead of touching holders or records.
    /// </summary>
    internal sealed class ShuttleMedicalBayOccupancyService
    {
        internal bool TryAdmitSelfPatientByThingID(
            ThingWithComps shuttleHost,
            int patientThingID,
            out string failReason)
        {
            failReason = null;
            if (shuttleHost == null || shuttleHost.Map == null || patientThingID <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            Pawn patient = this.FindSpawnedPawnByThingID(shuttleHost.Map, patientThingID);
            if (patient == null)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            return this.TryAdmitSelfPatient(shuttleHost, patient, out failReason);
        }

        internal bool TryAssignSelfAdmissionJobByThingID(
            ThingWithComps shuttleHost,
            int patientThingID,
            out string failReason)
        {
            failReason = null;
            if (shuttleHost == null || shuttleHost.Map == null || patientThingID <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            Pawn patient = this.FindSpawnedPawnByThingID(shuttleHost.Map, patientThingID);
            if (patient == null)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            return this.TryAssignSelfAdmissionJob(shuttleHost, patient, out failReason);
        }

        internal bool TryAssignSelfAdmissionJob(
            ThingWithComps shuttleHost,
            Pawn patient,
            out string failReason)
        {
            failReason = null;
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.EnterMedicalBayJobDefName);
            if (jobDef == null)
            {
                failReason = "CT_Shuttle_MedicalBay_AdmitFailed".Translate().ToString();
                return false;
            }

            if (this.HasActiveAdmissionOrder(shuttleHost, patient))
            {
                failReason = "CT_Shuttle_MedicalBay_AdmissionAlreadyAssigned".Translate().ToString();
                return false;
            }

            if (patient == null ||
                patient.jobs == null ||
                shuttleHost == null ||
                !MedicalBayAdmissionValidator.CanUseMedicalBayForSelfAdmit(
                    patient,
                    shuttleHost,
                    out failReason))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!occupancy.TryReserveAdmission(patient, patient, out failReason))
            {
                return false;
            }

            // V2 SelfEnter uses the same job path as V1, but the job is assigned from the
            // command/service boundary instead of the UI. The job driver performs final
            // validation and moves the pawn into the Medical Bay holder only after arrival.
            Job job = JobMaker.MakeJob(jobDef, shuttleHost);
            if (patient.jobs.TryTakeOrderedJob(job))
            {
                return true;
            }

            occupancy.ReleaseAdmissionReservation(patient);
            failReason = "CT_Shuttle_MedicalBay_AdmitFailed".Translate().ToString();
            return false;
        }

        internal bool TryAssignCarryAdmissionJobByThingID(
            ThingWithComps shuttleHost,
            int patientThingID,
            int carrierThingID,
            out string failReason)
        {
            failReason = null;
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                patientThingID <= 0 ||
                carrierThingID <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            Pawn carrier = this.FindSpawnedPawnByThingID(shuttleHost.Map, carrierThingID);
            if (carrier == null)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            Pawn patient = this.FindSpawnedPawnByThingID(shuttleHost.Map, patientThingID);
            if (patient == null && carrier.carryTracker != null)
            {
                Thing carriedThing = carrier.carryTracker.CarriedThing;
                Pawn carriedPawn = carriedThing as Pawn;
                if (carriedPawn != null && carriedPawn.thingIDNumber == patientThingID)
                {
                    patient = carriedPawn;
                }
            }

            if (patient == null)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            return this.TryAssignCarryAdmissionJob(shuttleHost, carrier, patient, out failReason);
        }

        internal bool TryAssignCarryAdmissionJob(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn patient,
            out string failReason)
        {
            failReason = null;
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.CarryPatientToMedicalBayJobDefName);
            if (jobDef == null)
            {
                failReason = "CT_Shuttle_MedicalBay_CarriedAdmitFailed".Translate().ToString();
                return false;
            }

            if (this.HasActiveAdmissionOrder(shuttleHost, patient))
            {
                failReason = "CT_Shuttle_MedicalBay_AdmissionAlreadyAssigned".Translate().ToString();
                return false;
            }

            if (carrier == null ||
                carrier.jobs == null ||
                patient == null ||
                shuttleHost == null ||
                !MedicalBayAdmissionValidator.CanCarryPatientToMedicalBay(
                    carrier,
                    patient,
                    shuttleHost,
                    out failReason,
                    carrier))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!occupancy.TryReserveAdmission(carrier, patient, out failReason))
            {
                return false;
            }

            // V2 NeedsCarry uses the existing Medical Bay carry job path, but assignment
            // happens only through this command/service boundary. The job driver performs
            // final validation and admits the patient to the holder after arrival.
            Job job = JobMaker.MakeJob(jobDef, patient, shuttleHost);
            job.count = 1;
            if (carrier.jobs.TryTakeOrderedJob(job))
            {
                return true;
            }

            occupancy.ReleaseAdmissionReservation(patient);
            failReason = "CT_Shuttle_MedicalBay_CarriedAdmitFailed".Translate().ToString();
            return false;
        }

        private bool HasActiveAdmissionOrder(ThingWithComps shuttleHost, Pawn patient)
        {
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null ||
                patient == null)
            {
                return false;
            }

            JobDef selfJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.EnterMedicalBayJobDefName);
            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.CarryPatientToMedicalBayJobDefName);
            System.Collections.Generic.IReadOnlyList<Pawn> pawns =
                shuttleHost.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; pawns != null && i < pawns.Count; i++)
            {
                Pawn actor = pawns[i];
                Job currentJob = actor != null ? actor.CurJob : null;
                if (currentJob == null)
                {
                    continue;
                }

                if (currentJob.def == selfJobDef &&
                    actor == patient &&
                    currentJob.GetTarget(TargetIndex.A).Thing == shuttleHost)
                {
                    return true;
                }

                if (currentJob.def == carryJobDef &&
                    currentJob.GetTarget(TargetIndex.A).Pawn == patient &&
                    currentJob.GetTarget(TargetIndex.B).Thing == shuttleHost)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryAdmitSelfPatient(
            ThingWithComps shuttleHost,
            Pawn patient,
            out string failReason)
        {
            failReason = null;

            if (shuttleHost == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            return occupancy.TryAdmitSelfPatient(patient, out failReason);
        }

        internal bool TryAdmitCarriedPatient(
            ThingWithComps shuttleHost,
            Pawn carrier,
            Pawn patient,
            out string failReason)
        {
            failReason = null;

            if (shuttleHost == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            return occupancy.TryAdmitCarriedPatient(carrier, patient, out failReason);
        }

        internal bool TryEjectMedicalBayPatients(
            ThingWithComps shuttleHost,
            int patientThingID,
            out bool hadPatients,
            out string failReason)
        {
            hadPatients = false;
            failReason = null;

            if (shuttleHost == null)
            {
                failReason = "CT_Shuttle_MedicalBay_EjectFailed".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null || !occupancy.HasPatients)
            {
                return true;
            }

            if (shuttleHost.Map == null)
            {
                failReason = "CT_Shuttle_MedicalBay_EjectFailed".Translate().ToString();
                return false;
            }

            hadPatients = true;
            if (patientThingID > 0)
            {
                Pawn patient = this.FindPatientByThingID(occupancy, patientThingID);
                if (patient == null)
                {
                    failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                    return false;
                }

                return occupancy.TryEjectPatient(patient, out failReason);
            }

            return occupancy.TryEjectAllPatients(out failReason);
        }

        private Pawn FindPatientByThingID(CompShuttleMedicalBayOccupancy occupancy, int patientThingID)
        {
            if (occupancy == null || patientThingID <= 0)
            {
                return null;
            }

            System.Collections.Generic.List<Pawn> patients = occupancy.HeldPatients;
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient != null && patient.thingIDNumber == patientThingID)
                {
                    return patient;
                }
            }

            return null;
        }

        private Pawn FindSpawnedPawnByThingID(Map map, int patientThingID)
        {
            if (map == null || map.mapPawns == null || patientThingID <= 0)
            {
                return null;
            }

            System.Collections.Generic.IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == patientThingID)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
