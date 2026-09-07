using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal bool ContainsPatient(Pawn pawn)
        {
            this.EnsureInitialized();
            return SharedPatientRecordService.ContainsPatient(
                this.medicalHeldThings,
                this.patientRecords,
                pawn);
        }

        internal List<Pawn> GetHeldPatientsForReading()
        {
            this.EnsureInitialized();
            return SharedPatientRecordService.GetHeldPatientsForReading(
                this.medicalHeldThings,
                this.patientRecords);
        }

        internal List<ShuttleMedicalPatientSnapshot> BuildPatientSnapshotsForReading()
        {
            this.EnsureInitialized();
            this.ReconcilePatientRecordsToHeldPawns();
            return SharedPatientRecordService.BuildPatientSnapshotsForReading(
                this.medicalHeldThings,
                this.patientRecords);
        }

        private bool TryValidateNoDuplicatePatientRecordsForHeldPawns(out string failureReason)
        {
            return SharedPatientRecordService.TryValidateNoDuplicatePatientRecordsForHeldPawns(
                this.medicalHeldThings,
                this.patientRecords,
                out failureReason);
        }

        private void ReconcilePatientRecordsToHeldPawns()
        {
            MedicalBayOccupancyReconciler.ReconcilePatientRecordsToHeldPawns(this);
        }

        private void RemoveInvalidPatientRecords()
        {
            MedicalBayOccupancyReconciler.RemoveInvalidPatientRecords(this);
        }

        private int CountPatientRecordsForThingID(int patientThingID)
        {
            return SharedPatientRecordService.CountPatientRecordsForThingID(
                this.patientRecords,
                patientThingID);
        }

        private void RemovePatientRecord(Pawn pawn)
        {
            int patientThingID = pawn != null ? pawn.thingIDNumber : -1;
            this.RemovePatientRecord(patientThingID, pawn);
        }

        private void RemovePatientRecord(int patientThingID)
        {
            this.RemovePatientRecord(patientThingID, null);
        }

        private void RemovePatientRecord(int patientThingID, Pawn pawn)
        {
            SharedPatientRecordService.RemovePatientRecord(
                this.patientRecords,
                patientThingID,
                pawn,
                this.ReleaseAllReservationsForPatient);
        }

        private bool IsContainedPatient(Pawn pawn)
        {
            return SharedPatientRecordService.ContainsPatient(
                this.medicalHeldThings,
                this.patientRecords,
                pawn);
        }

        private bool IsHeldPawn(Pawn pawn)
        {
            return SharedPatientRecordService.IsHeldPawn(
                this.medicalHeldThings,
                pawn);
        }

        private int CountValidPatientRecords()
        {
            return SharedPatientRecordService.CountValidPatientRecords(
                this.medicalHeldThings,
                this.patientRecords);
        }

        private Pawn FindHeldPatientByThingID(int patientThingID)
        {
            return SharedPatientRecordService.FindHeldPatientByThingID(
                this.medicalHeldThings,
                this.patientRecords,
                patientThingID);
        }

        private bool TryFindPatientRecord(
            Pawn pawn,
            out ShuttleMedicalPatientRecord record,
            out int recordIndex)
        {
            return SharedPatientRecordService.TryFindPatientRecord(
                this.medicalHeldThings,
                this.patientRecords,
                pawn,
                out record,
                out recordIndex);
        }
    }
}
