using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical
{
    internal sealed class MedicalBayProcedureRuntimeState : IExposable
    {
        private List<MedicalBayProcedureRecord> activeProcedures =
            new List<MedicalBayProcedureRecord>();
        private int nextProcedureID = 1;

        internal bool HasActiveProcedure
        {
            get
            {
                this.EnsureInitialized();
                for (int i = 0; i < this.activeProcedures.Count; i++)
                {
                    MedicalBayProcedureRecord record = this.activeProcedures[i];
                    if (record != null && record.IsActive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal IReadOnlyList<MedicalBayProcedureRecord> ActiveProcedures
        {
            get
            {
                this.EnsureInitialized();
                return this.activeProcedures;
            }
        }

        internal void EnsureInitialized()
        {
            if (this.activeProcedures == null)
            {
                this.activeProcedures = new List<MedicalBayProcedureRecord>();
            }

            if (this.nextProcedureID < 1)
            {
                this.nextProcedureID = 1;
            }
        }

        internal MedicalBayProcedureRecord CreateProcedure(
            string procedureType,
            int doctorThingID,
            int patientThingID,
            bool useAvailableMedicine,
            string recipeDefName,
            int bodyPartIndex,
            int workTicksTotal)
        {
            this.EnsureInitialized();
            if (this.HasActiveProcedure)
            {
                return null;
            }

            int procedureID = this.nextProcedureID;
            this.nextProcedureID++;
            MedicalBayProcedureRecord record = new MedicalBayProcedureRecord(
                procedureID,
                procedureType,
                doctorThingID,
                patientThingID,
                useAvailableMedicine,
                recipeDefName,
                bodyPartIndex,
                workTicksTotal);
            this.activeProcedures.Add(record);
            return record;
        }

        internal bool TryGetProcedure(int procedureID, out MedicalBayProcedureRecord record)
        {
            record = null;
            this.EnsureInitialized();
            if (procedureID <= 0)
            {
                return false;
            }

            for (int i = 0; i < this.activeProcedures.Count; i++)
            {
                MedicalBayProcedureRecord candidate = this.activeProcedures[i];
                if (candidate != null && candidate.ProcedureID == procedureID)
                {
                    record = candidate;
                    return true;
                }
            }

            return false;
        }

        internal void MarkCompleted(int procedureID)
        {
            MedicalBayProcedureRecord record;
            if (this.TryGetProcedure(procedureID, out record))
            {
                record.MarkCompleted();
            }
        }

        internal void MarkFailed(int procedureID)
        {
            MedicalBayProcedureRecord record;
            if (this.TryGetProcedure(procedureID, out record))
            {
                record.MarkFailed();
            }
        }

        internal void CancelProcedure(int procedureID)
        {
            MedicalBayProcedureRecord record;
            if (this.TryGetProcedure(procedureID, out record))
            {
                record.MarkCancelled();
            }
        }

        internal void MarkRecoveryRequired(int procedureID)
        {
            MedicalBayProcedureRecord record;
            if (this.TryGetProcedure(procedureID, out record))
            {
                record.MarkRecoveryRequired();
            }
        }

        internal bool HasProcedureForDoctor(int doctorThingID)
        {
            if (doctorThingID <= 0)
            {
                return false;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.activeProcedures.Count; i++)
            {
                MedicalBayProcedureRecord record = this.activeProcedures[i];
                if (record != null &&
                    record.IsActive &&
                    record.DoctorThingID == doctorThingID)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasProcedureForPatient(int patientThingID)
        {
            if (patientThingID <= 0)
            {
                return false;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.activeProcedures.Count; i++)
            {
                MedicalBayProcedureRecord record = this.activeProcedures[i];
                if (record != null &&
                    record.IsActive &&
                    record.PatientThingID == patientThingID)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasActiveProcedureForPatient(int patientThingID)
        {
            return this.HasProcedureForPatient(patientThingID);
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref this.activeProcedures,
                "activeProcedures",
                LookMode.Deep);
            Scribe_Values.Look(ref this.nextProcedureID, "nextProcedureID", 1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                for (int i = this.activeProcedures.Count - 1; i >= 0; i--)
                {
                    MedicalBayProcedureRecord record = this.activeProcedures[i];
                    if (record == null)
                    {
                        this.activeProcedures.RemoveAt(i);
                        continue;
                    }

                    record.SanitizePostLoad();
                }
            }
        }
    }
}
