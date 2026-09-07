using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        // Treatment reservations lock a patient inside this holder for one doctor.
        // They are not RimWorld job reservations and are cleared aggressively when stale.
        internal bool TryReserveTreatment(Pawn doctor, int patientThingID, out string failReason)
        {
            failReason = null;
            this.EnsureInitialized();
            this.ClearStaleTreatmentReservations();

            ShuttleController controller;
            if (MedicalBayAdmissionValidator.TryGetShuttleController(this.parent, out controller) &&
                controller != null &&
                controller.HasActiveMedicalProcedure())
            {
                failReason = "CT_Shuttle_MedicalProcedure_AlreadyActive".Translate().ToString();
                return false;
            }

            if (!MedicalBayAdmissionValidator.CanUseDoctorForMedicalBayTending(
                doctor,
                this.parent,
                out failReason))
            {
                return false;
            }

            Pawn patient = this.FindHeldPatientByThingID(patientThingID);
            if (patient == null || patient.Destroyed || patient.Dead || patient.Spawned || !this.ContainsPatient(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientNotHeld".Translate().ToString();
                return false;
            }

            if (!this.PatientHasUntendedTendableHediff(patient))
            {
                this.ReleaseTreatmentReservation(null, patientThingID);
                failReason = "CT_Shuttle_MedicalBay_NoTendableHediffs".Translate().ToString();
                return false;
            }

            ShuttleMedicalTreatmentReservation existing = this.FindTreatmentReservation(patientThingID);
            if (existing != null)
            {
                if (existing.DoctorThingID == doctor.thingIDNumber)
                {
                    existing.ReservationTick = ShuttleTickUtility.TicksGameOrMinusOne();
                    return true;
                }

                failReason = "CT_Shuttle_MedicalBay_TreatmentReserved".Translate().ToString();
                return false;
            }

            this.treatmentReservations.Add(new ShuttleMedicalTreatmentReservation(
                patientThingID,
                doctor.thingIDNumber,
                ShuttleTickUtility.TicksGameOrMinusOne()));
            return true;
        }

        internal void ReleaseTreatmentReservation(Pawn doctor, int patientThingID)
        {
            this.EnsureInitialized();
            int doctorThingID = doctor != null ? doctor.thingIDNumber : -1;
            for (int i = this.treatmentReservations.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalTreatmentReservation reservation = this.treatmentReservations[i];
                if (reservation == null || reservation.PatientThingID != patientThingID)
                {
                    continue;
                }

                if (doctorThingID <= 0 || reservation.DoctorThingID == doctorThingID)
                {
                    this.treatmentReservations.RemoveAt(i);
                }
            }
        }

        internal bool IsPatientReservedForTreatment(int patientThingID)
        {
            return this.GetTreatmentReservationDoctorThingID(patientThingID) > 0;
        }

        internal bool TryReserveAdmission(Pawn actor, Pawn patient, out string failReason)
        {
            failReason = null;
            this.EnsureInitialized();
            this.ClearStaleAdmissionReservations();

            if (patient == null || patient.Destroyed || patient.Dead || patient.thingIDNumber <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (this.ContainsPatient(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            MedicalBayProfile medicalBay;
            if (!this.TryGetMedicalBayProfile(out medicalBay) ||
                medicalBay == null ||
                !medicalBay.HasMedicalBay)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (medicalBay.MedicalPatientSlots <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_NoSlots".Translate().ToString();
                return false;
            }

            int patientThingID = patient.thingIDNumber;
            ShuttleMedicalAdmissionReservation existing = this.FindAdmissionReservation(patientThingID);
            int actorThingID = actor != null && actor.thingIDNumber > 0
                ? actor.thingIDNumber
                : patientThingID;
            int currentTick = ShuttleTickUtility.TicksGameOrMinusOne();
            if (existing != null)
            {
                existing.ActorThingID = actorThingID;
                existing.ReservationTick = currentTick;
                return true;
            }

            if (this.PatientCount + this.CountAdmissionReservationsExcluding(patientThingID) >= medicalBay.MedicalPatientSlots)
            {
                failReason = "CT_Shuttle_MedicalBay_Full".Translate().ToString();
                return false;
            }

            this.admissionReservations.Add(new ShuttleMedicalAdmissionReservation(
                patientThingID,
                actorThingID,
                currentTick));
            return true;
        }

        internal void ReleaseAdmissionReservation(Pawn patient)
        {
            this.ReleaseAdmissionReservation(patient != null ? patient.thingIDNumber : -1);
        }

        internal void ReleaseAdmissionReservation(int patientThingID)
        {
            this.EnsureInitialized();
            if (patientThingID <= 0 || this.admissionReservations == null)
            {
                return;
            }

            for (int i = this.admissionReservations.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalAdmissionReservation reservation = this.admissionReservations[i];
                if (reservation == null || reservation.PatientThingID == patientThingID)
                {
                    this.admissionReservations.RemoveAt(i);
                }
            }
        }

        internal void ReleaseAllReservationsForPatient(int patientThingID)
        {
            if (patientThingID <= 0)
            {
                return;
            }

            this.ReleaseAdmissionReservation(patientThingID);
            this.ReleaseTreatmentReservation(null, patientThingID);
        }

        internal void ReleaseAllReservationsForPatients(IEnumerable<int> patientThingIDs)
        {
            if (patientThingIDs == null)
            {
                return;
            }

            foreach (int patientThingID in patientThingIDs)
            {
                this.ReleaseAllReservationsForPatient(patientThingID);
            }
        }

        internal int CountPendingAdmissionReservations(Pawn ignoredPatient)
        {
            this.EnsureInitialized();
            this.ClearStaleAdmissionReservations();
            return this.CountAdmissionReservationsExcluding(
                ignoredPatient != null ? ignoredPatient.thingIDNumber : -1);
        }

        internal bool HasAdmissionReservationForPatientID(int patientThingID)
        {
            this.EnsureInitialized();
            this.ClearStaleAdmissionReservations();
            return this.FindAdmissionReservation(patientThingID) != null;
        }

        internal int GetTreatmentReservationDoctorThingID(int patientThingID)
        {
            this.EnsureInitialized();
            this.ClearStaleTreatmentReservations();

            ShuttleMedicalTreatmentReservation reservation = this.FindTreatmentReservation(patientThingID);
            return reservation != null ? reservation.DoctorThingID : -1;
        }

        internal void ClearStaleTreatmentReservations()
        {
            this.ClearStaleTreatmentReservations(this.BuildSpawnedPawnLookup());
        }

        private void ClearStaleTreatmentReservations(Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (this.treatmentReservations == null || this.treatmentReservations.Count == 0)
            {
                return;
            }

            for (int i = this.treatmentReservations.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalTreatmentReservation reservation = this.treatmentReservations[i];
                if (reservation == null || this.IsTreatmentReservationStale(reservation, spawnedPawnsByThingID))
                {
                    this.treatmentReservations.RemoveAt(i);
                }
            }
        }

        internal void ClearStaleAdmissionReservations()
        {
            this.ClearStaleAdmissionReservations(this.BuildSpawnedPawnLookup());
        }

        private void ClearStaleAdmissionReservations(Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (this.admissionReservations == null || this.admissionReservations.Count == 0)
            {
                return;
            }

            for (int i = this.admissionReservations.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalAdmissionReservation reservation = this.admissionReservations[i];
                if (reservation == null || this.IsAdmissionReservationStale(reservation, spawnedPawnsByThingID))
                {
                    this.admissionReservations.RemoveAt(i);
                }
            }
        }

        private ShuttleMedicalTreatmentReservation FindTreatmentReservation(int patientThingID)
        {
            if (patientThingID <= 0 || this.treatmentReservations == null)
            {
                return null;
            }

            for (int i = 0; i < this.treatmentReservations.Count; i++)
            {
                ShuttleMedicalTreatmentReservation reservation = this.treatmentReservations[i];
                if (reservation != null && reservation.PatientThingID == patientThingID)
                {
                    return reservation;
                }
            }

            return null;
        }

        private ShuttleMedicalAdmissionReservation FindAdmissionReservation(int patientThingID)
        {
            if (patientThingID <= 0 || this.admissionReservations == null)
            {
                return null;
            }

            for (int i = 0; i < this.admissionReservations.Count; i++)
            {
                ShuttleMedicalAdmissionReservation reservation = this.admissionReservations[i];
                if (reservation != null && reservation.PatientThingID == patientThingID)
                {
                    return reservation;
                }
            }

            return null;
        }

        private int CountAdmissionReservationsExcluding(int ignoredPatientThingID)
        {
            if (this.admissionReservations == null || this.admissionReservations.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < this.admissionReservations.Count; i++)
            {
                ShuttleMedicalAdmissionReservation reservation = this.admissionReservations[i];
                if (reservation != null &&
                    reservation.PatientThingID > 0 &&
                    reservation.PatientThingID != ignoredPatientThingID)
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsTreatmentReservationStale(
            ShuttleMedicalTreatmentReservation reservation,
            Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (reservation == null ||
                reservation.PatientThingID <= 0 ||
                reservation.DoctorThingID <= 0)
            {
                return true;
            }

            Pawn patient = this.FindHeldPatientByThingID(reservation.PatientThingID);
            if (patient == null || patient.Destroyed || patient.Dead || patient.Spawned || !this.ContainsPatient(patient))
            {
                return true;
            }

            if (!this.PatientHasUntendedTendableHediff(patient))
            {
                return true;
            }

            Pawn doctor = this.FindSpawnedPawnByThingID(
                reservation.DoctorThingID,
                spawnedPawnsByThingID);
            string failReason;
            if (!MedicalBayAdmissionValidator.CanUseDoctorForMedicalBayTending(
                doctor,
                this.parent,
                out failReason))
            {
                return true;
            }

            if (this.IsDoctorCurrentJobTreatingPatient(doctor, reservation.PatientThingID))
            {
                return false;
            }

            return !this.IsRecentTreatmentReservation(reservation);
        }

        private bool IsAdmissionReservationStale(
            ShuttleMedicalAdmissionReservation reservation,
            Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (reservation == null || reservation.PatientThingID <= 0)
            {
                return true;
            }

            Pawn heldPatient = this.FindHeldPatientByThingID(reservation.PatientThingID);
            if (heldPatient != null)
            {
                return true;
            }

            Pawn patient = this.FindSpawnedPawnByThingID(
                reservation.PatientThingID,
                spawnedPawnsByThingID);
            if (patient == null || patient.Destroyed || patient.Dead)
            {
                return true;
            }

            if (this.IsCurrentAdmissionJobForReservation(reservation, spawnedPawnsByThingID))
            {
                return false;
            }

            return !this.IsRecentAdmissionReservation(reservation);
        }

        private bool IsRecentTreatmentReservation(ShuttleMedicalTreatmentReservation reservation)
        {
            int currentTick = ShuttleTickUtility.TicksGameOrMinusOne();
            return currentTick >= 0 &&
                reservation != null &&
                reservation.ReservationTick >= 0 &&
                currentTick - reservation.ReservationTick <= 250;
        }

        private bool IsRecentAdmissionReservation(ShuttleMedicalAdmissionReservation reservation)
        {
            int currentTick = ShuttleTickUtility.TicksGameOrMinusOne();
            return currentTick >= 0 &&
                reservation != null &&
                reservation.ReservationTick >= 0 &&
                currentTick - reservation.ReservationTick <= 250;
        }

        private Pawn FindSpawnedDoctorByThingID(int doctorThingID)
        {
            return this.FindSpawnedPawnByThingID(doctorThingID);
        }

        private Pawn FindSpawnedPawnByThingID(int pawnThingID)
        {
            return this.FindSpawnedPawnByThingID(pawnThingID, null);
        }

        private Pawn FindSpawnedPawnByThingID(
            int pawnThingID,
            Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (pawnThingID <= 0)
            {
                return null;
            }

            if (spawnedPawnsByThingID != null)
            {
                Pawn foundPawn;
                if (spawnedPawnsByThingID.TryGetValue(pawnThingID, out foundPawn))
                {
                    return foundPawn;
                }

                return null;
            }

            Map map = this.parent != null ? this.parent.Map : null;
            IReadOnlyList<Pawn> pawns = map != null && map.mapPawns != null
                ? map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    return pawn;
                }
            }

            return null;
        }

        private bool IsDoctorCurrentJobTreatingPatient(Pawn doctor, int patientThingID)
        {
            if (doctor == null || doctor.CurJob == null || patientThingID <= 0)
            {
                return false;
            }

            JobDef tendJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.TendMedicalBayPatientJobDefName);
            JobDef tendWithMedicineJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.TendMedicalBayPatientWithMedicineJobDefName);
            bool isMedicalBayTendJob =
                (tendJobDef != null && doctor.CurJob.def == tendJobDef) ||
                (tendWithMedicineJobDef != null && doctor.CurJob.def == tendWithMedicineJobDef);
            return isMedicalBayTendJob &&
                doctor.CurJob.count == patientThingID &&
                this.TargetReferences(doctor.CurJob.GetTarget(TargetIndex.A), this.parent);
        }

        private bool IsCurrentAdmissionJobForReservation(
            ShuttleMedicalAdmissionReservation reservation)
        {
            return this.IsCurrentAdmissionJobForReservation(reservation, null);
        }

        private bool IsCurrentAdmissionJobForReservation(
            ShuttleMedicalAdmissionReservation reservation,
            Dictionary<int, Pawn> spawnedPawnsByThingID)
        {
            if (reservation == null || reservation.PatientThingID <= 0)
            {
                return false;
            }

            Pawn actor = this.FindSpawnedPawnByThingID(
                reservation.ActorThingID > 0 ? reservation.ActorThingID : reservation.PatientThingID,
                spawnedPawnsByThingID);
            Job job = actor != null ? actor.CurJob : null;
            if (job == null)
            {
                return false;
            }

            JobDef enterJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.EnterMedicalBayJobDefName);
            if (enterJobDef != null &&
                job.def == enterJobDef &&
                actor.thingIDNumber == reservation.PatientThingID &&
                this.TargetReferences(job.GetTarget(TargetIndex.A), this.parent))
            {
                return true;
            }

            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.CarryPatientToMedicalBayJobDefName);
            return carryJobDef != null &&
                job.def == carryJobDef &&
                this.TargetReferences(job.GetTarget(TargetIndex.B), this.parent) &&
                this.TargetPawnThingID(job.GetTarget(TargetIndex.A)) == reservation.PatientThingID;
        }

        private bool TargetReferences(LocalTargetInfo target, Thing thing)
        {
            return thing != null && target.IsValid && target.Thing == thing;
        }

        private int TargetPawnThingID(LocalTargetInfo target)
        {
            Pawn pawn = target.IsValid ? target.Thing as Pawn : null;
            return pawn != null ? pawn.thingIDNumber : -1;
        }

        private Dictionary<int, Pawn> BuildSpawnedPawnLookup()
        {
            Dictionary<int, Pawn> lookup = new Dictionary<int, Pawn>();
            Map map = this.parent != null ? this.parent.Map : null;
            IReadOnlyList<Pawn> pawns = map != null && map.mapPawns != null
                ? map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return lookup;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed && pawn.thingIDNumber > 0)
                {
                    lookup[pawn.thingIDNumber] = pawn;
                }
            }

            return lookup;
        }
    }
}
