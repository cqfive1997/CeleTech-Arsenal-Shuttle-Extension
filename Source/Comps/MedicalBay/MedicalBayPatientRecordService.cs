using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Stateless helper for keeping Medical Bay patient records aligned with the holder.
    /// It does not own ThingOwner state, Scribe fields, UI, or admission/ejection commands.
    /// </summary>
    internal sealed class MedicalBayPatientRecordService
    {
        internal void ReconcilePatientRecordsToHeldPawns(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords,
            Action<int> releaseReservationsForPatient,
            int ticksGame)
        {
            this.RemoveInvalidPatientRecords(
                medicalHeldThings,
                patientRecords,
                releaseReservationsForPatient);

            if (medicalHeldThings == null || patientRecords == null)
            {
                return;
            }

            for (int i = 0; i < medicalHeldThings.Count; i++)
            {
                Pawn pawn = medicalHeldThings[i] as Pawn;
                if (pawn != null && !this.HasRecordFor(patientRecords, pawn))
                {
                    patientRecords.Add(new ShuttleMedicalPatientRecord(
                        pawn,
                        "Recovered",
                        ticksGame,
                        string.Empty,
                        string.Empty));
                }
            }
        }

        internal void RemoveInvalidPatientRecords(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords,
            Action<int> releaseReservationsForPatient)
        {
            if (patientRecords == null)
            {
                return;
            }

            HashSet<int> seenPatientThingIDs = new HashSet<int>();
            for (int i = patientRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalPatientRecord record = patientRecords[i];
                if (record == null)
                {
                    patientRecords.RemoveAt(i);
                    continue;
                }

                record.Sanitize();
                if (record.PawnThingID > 0 && !seenPatientThingIDs.Add(record.PawnThingID))
                {
                    this.ReleaseReservations(releaseReservationsForPatient, record.PawnThingID);
                    patientRecords.RemoveAt(i);
                    continue;
                }

                Pawn pawn = record.Pawn;
                if (pawn == null || !this.IsHeldPawn(medicalHeldThings, pawn))
                {
                    this.ReleaseReservations(releaseReservationsForPatient, record.PawnThingID);
                    patientRecords.RemoveAt(i);
                }
            }
        }

        internal bool ContainsPatient(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords,
            Pawn pawn)
        {
            return pawn != null &&
                this.IsHeldPawn(medicalHeldThings, pawn) &&
                this.HasRecordFor(patientRecords, pawn);
        }

        internal List<Pawn> GetHeldPatientsForReading(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords)
        {
            List<Pawn> patients = new List<Pawn>();
            if (patientRecords != null)
            {
                for (int i = 0; i < patientRecords.Count; i++)
                {
                    ShuttleMedicalPatientRecord record = patientRecords[i];
                    Pawn pawn = record != null ? record.Pawn : null;
                    if (pawn != null &&
                        this.IsHeldPawn(medicalHeldThings, pawn) &&
                        !patients.Contains(pawn))
                    {
                        patients.Add(pawn);
                    }
                }
            }

            if (medicalHeldThings != null)
            {
                for (int i = 0; i < medicalHeldThings.Count; i++)
                {
                    Pawn pawn = medicalHeldThings[i] as Pawn;
                    if (pawn != null && !patients.Contains(pawn))
                    {
                        patients.Add(pawn);
                    }
                }
            }

            return patients;
        }

        internal List<ShuttleMedicalPatientSnapshot> BuildPatientSnapshotsForReading(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords)
        {
            List<ShuttleMedicalPatientSnapshot> snapshots =
                new List<ShuttleMedicalPatientSnapshot>();
            if (patientRecords == null)
            {
                return snapshots;
            }

            for (int i = 0; i < patientRecords.Count; i++)
            {
                ShuttleMedicalPatientRecord record = patientRecords[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && this.IsHeldPawn(medicalHeldThings, pawn))
                {
                    snapshots.Add(new ShuttleMedicalPatientSnapshot(record, pawn));
                }
            }

            return snapshots;
        }

        internal bool TryValidateNoDuplicatePatientRecordsForHeldPawns(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords,
            out string failureReason)
        {
            failureReason = null;
            if (patientRecords == null || patientRecords.Count <= 1)
            {
                return true;
            }

            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < patientRecords.Count; i++)
            {
                ShuttleMedicalPatientRecord record = patientRecords[i];
                if (record == null)
                {
                    continue;
                }

                record.Sanitize();
                if (record.PawnThingID <= 0)
                {
                    continue;
                }

                Pawn pawn = record.Pawn;
                if (pawn == null || !this.IsHeldPawn(medicalHeldThings, pawn))
                {
                    continue;
                }

                if (!seen.Add(record.PawnThingID))
                {
                    failureReason = "[CeleTech Shuttle] MedicalBay patient export refused duplicate live patient record for thingID=" +
                        record.PawnThingID +
                        ".";
                    return false;
                }
            }

            return true;
        }

        internal bool TryFindPatientRecord(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords,
            Pawn pawn,
            out ShuttleMedicalPatientRecord record,
            out int recordIndex)
        {
            record = null;
            recordIndex = -1;
            if (pawn == null || patientRecords == null)
            {
                return false;
            }

            for (int i = 0; i < patientRecords.Count; i++)
            {
                ShuttleMedicalPatientRecord candidate = patientRecords[i];
                if (candidate != null &&
                    candidate.Pawn == pawn &&
                    this.IsHeldPawn(medicalHeldThings, pawn))
                {
                    record = candidate;
                    recordIndex = i;
                    return true;
                }
            }

            return false;
        }

        internal int CountPatientRecordsForThingID(
            List<ShuttleMedicalPatientRecord> patientRecords,
            int patientThingID)
        {
            if (patientThingID <= 0 || patientRecords == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < patientRecords.Count; i++)
            {
                ShuttleMedicalPatientRecord record = patientRecords[i];
                if (record != null)
                {
                    record.Sanitize();
                }

                if (record != null && record.PawnThingID == patientThingID)
                {
                    count++;
                }
            }

            return count;
        }

        internal void RemovePatientRecord(
            List<ShuttleMedicalPatientRecord> patientRecords,
            int patientThingID,
            Pawn pawn,
            Action<int> releaseReservationsForPatient)
        {
            if (patientRecords == null)
            {
                this.ReleaseReservations(releaseReservationsForPatient, patientThingID);
                return;
            }

            if (patientThingID <= 0 && pawn != null)
            {
                patientThingID = pawn.thingIDNumber;
            }

            for (int i = patientRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalPatientRecord record = patientRecords[i];
                bool matchesPawn = pawn != null && record != null && record.Pawn == pawn;
                bool matchesThingID = patientThingID > 0 && record != null && record.PawnThingID == patientThingID;
                if (record == null || matchesPawn || matchesThingID)
                {
                    if (record != null && patientThingID <= 0)
                    {
                        patientThingID = record.PawnThingID;
                    }

                    if (record != null)
                    {
                        record.StopAdmission();
                    }

                    patientRecords.RemoveAt(i);
                }
            }

            this.ReleaseReservations(releaseReservationsForPatient, patientThingID);
        }

        internal bool IsHeldPawn(
            ThingOwner<Thing> medicalHeldThings,
            Pawn pawn)
        {
            return pawn != null &&
                medicalHeldThings != null &&
                medicalHeldThings.Contains(pawn);
        }

        internal int CountValidPatientRecords(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords)
        {
            List<Pawn> countedPawns = new List<Pawn>();
            if (patientRecords != null)
            {
                for (int i = 0; i < patientRecords.Count; i++)
                {
                    ShuttleMedicalPatientRecord record = patientRecords[i];
                    if (record != null &&
                        this.IsHeldPawn(medicalHeldThings, record.Pawn) &&
                        !countedPawns.Contains(record.Pawn))
                    {
                        countedPawns.Add(record.Pawn);
                    }
                }
            }

            if (medicalHeldThings != null)
            {
                for (int i = 0; i < medicalHeldThings.Count; i++)
                {
                    Pawn pawn = medicalHeldThings[i] as Pawn;
                    if (pawn != null && !countedPawns.Contains(pawn))
                    {
                        countedPawns.Add(pawn);
                    }
                }
            }

            return countedPawns.Count;
        }

        internal Pawn FindHeldPatientByThingID(
            ThingOwner<Thing> medicalHeldThings,
            List<ShuttleMedicalPatientRecord> patientRecords,
            int patientThingID)
        {
            if (patientThingID <= 0)
            {
                return null;
            }

            List<Pawn> patients = this.GetHeldPatientsForReading(
                medicalHeldThings,
                patientRecords);
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

        private bool HasRecordFor(
            List<ShuttleMedicalPatientRecord> patientRecords,
            Pawn pawn)
        {
            if (pawn == null || patientRecords == null)
            {
                return false;
            }

            for (int i = 0; i < patientRecords.Count; i++)
            {
                ShuttleMedicalPatientRecord record = patientRecords[i];
                if (record != null && record.Pawn == pawn)
                {
                    return true;
                }
            }

            return false;
        }

        private void ReleaseReservations(
            Action<int> releaseReservationsForPatient,
            int patientThingID)
        {
            if (releaseReservationsForPatient != null)
            {
                releaseReservationsForPatient(patientThingID);
            }
        }
    }
}
