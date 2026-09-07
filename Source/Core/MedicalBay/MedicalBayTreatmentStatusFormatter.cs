using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal sealed class MedicalBayTreatmentStatusFormatter
    {
        internal void ApplyActiveProcedure(
            ShuttleMedicalBayReadModel model,
            ThingWithComps shuttleHost,
            ShuttleRuntimeState runtimeState,
            CompShuttleMedicalBayOccupancy patientOccupancy)
        {
            if (model == null)
            {
                return;
            }

            MedicalBayProcedureRecord record = this.GetActiveProcedure(runtimeState);
            CompShuttleMedicalProcedureOccupancy procedureOccupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleMedicalProcedureOccupancy>()
                : null;
            Pawn doctor = this.GetProcedureDoctor(procedureOccupancy, record);
            ShuttleMedicalPatientReadModel patient = record != null
                ? this.GetPatientReadModelByThingID(model, record.PatientThingID)
                : null;

            if (record == null)
            {
                if (procedureOccupancy == null || !procedureOccupancy.HasActiveDoctors)
                {
                    model.HasActiveProcedure = false;
                    model.ActiveProcedureStatusLabel =
                        "CT_Shuttle_MedicalProcedure_Status_None".Translate().ToString();
                    return;
                }

                model.HasActiveProcedure = true;
                model.ActiveProcedureStatus = MedicalBayProcedureRecord.StatusRecoveryRequired;
                model.ActiveProcedureStatusLabel =
                    "CT_Shuttle_MedicalProcedure_Status_RecoveryRequired".Translate().ToString();
                model.ActiveProcedureDoctorThingID = doctor != null ? doctor.thingIDNumber : -1;
                model.ActiveProcedureDoctorLabel = this.GetPawnLabel(doctor, model.ActiveProcedureDoctorThingID);
                model.ActiveProcedureDoctorDisplayThing = doctor;
                model.ActiveProcedurePatientThingID = -1;
                model.ActiveProcedurePatientLabel = "-";
                model.ActiveProcedureTooltip = this.BuildProcedureTooltip(model);
                return;
            }

            model.HasActiveProcedure = true;
            model.ActiveProcedureType = record.ProcedureType;
            model.ActiveProcedureStatus = record.Status;
            model.ActiveProcedureID = record.ProcedureID;
            model.ActiveProcedureDoctorThingID = record.DoctorThingID;
            model.ActiveProcedurePatientThingID = record.PatientThingID;
            model.ActiveProcedureDoctorLabel = this.GetPawnLabel(doctor, record.DoctorThingID);
            model.ActiveProcedureDoctorDisplayThing = doctor;
            model.ActiveProcedurePatientLabel = patient != null
                ? patient.PawnLabel
                : this.GetPawnLabel(this.FindPatientByThingID(patientOccupancy, record.PatientThingID), record.PatientThingID);
            model.ActiveProcedureWorkTicksDone = Math.Max(0, record.WorkTicksDone);
            model.ActiveProcedureWorkTicksTotal = Math.Max(0, record.WorkTicksTotal);
            model.ActiveProcedureProgress = model.ActiveProcedureWorkTicksTotal > 0
                ? Clamp01((float)model.ActiveProcedureWorkTicksDone / model.ActiveProcedureWorkTicksTotal)
                : 0f;
            model.ActiveProcedureTendCyclesCompleted = Math.Max(0, record.TendCyclesCompleted);
            model.ActiveProcedureLastKnownRemainingTendableCount =
                Math.Max(0, record.LastKnownRemainingTendableCount);
            model.ActiveProcedureStatusLabel = this.GetProcedureStatusLabel(record);
            model.ActiveProcedureTooltip = this.BuildProcedureTooltip(model);

            if (patient != null)
            {
                patient.IsActiveProcedurePatient = true;
                patient.ActiveProcedureStatusLabel = model.ActiveProcedureStatusLabel;
                patient.ActiveProcedureTooltip = model.ActiveProcedureTooltip;
            }
        }

        private MedicalBayProcedureRecord GetActiveProcedure(ShuttleRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                return null;
            }

            runtimeState.EnsureInitialized();
            MedicalBayProcedureRuntimeState procedureState = runtimeState.MedicalProcedures;
            IReadOnlyList<MedicalBayProcedureRecord> records =
                procedureState != null ? procedureState.ActiveProcedures : null;
            if (records == null)
            {
                return null;
            }

            for (int i = 0; i < records.Count; i++)
            {
                MedicalBayProcedureRecord record = records[i];
                if (record != null && record.IsActive)
                {
                    return record;
                }
            }

            return null;
        }

        private Pawn GetProcedureDoctor(
            CompShuttleMedicalProcedureOccupancy procedureOccupancy,
            MedicalBayProcedureRecord record)
        {
            if (procedureOccupancy == null)
            {
                return null;
            }

            if (record != null && record.DoctorThingID > 0)
            {
                Pawn doctor = procedureOccupancy.GetDoctorByThingID(record.DoctorThingID);
                if (doctor != null)
                {
                    return doctor;
                }
            }

            IReadOnlyList<Pawn> doctors = procedureOccupancy.ActiveDoctors;
            return doctors != null && doctors.Count > 0 ? doctors[0] : null;
        }

        private ShuttleMedicalPatientReadModel GetPatientReadModelByThingID(
            ShuttleMedicalBayReadModel model,
            int patientThingID)
        {
            if (model == null || model.Patients == null || patientThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < model.Patients.Count; i++)
            {
                ShuttleMedicalPatientReadModel patient = model.Patients[i];
                if (patient != null && patient.PawnThingID == patientThingID)
                {
                    return patient;
                }
            }

            return null;
        }

        private Pawn FindPatientByThingID(
            CompShuttleMedicalBayOccupancy occupancy,
            int patientThingID)
        {
            if (occupancy == null || patientThingID <= 0)
            {
                return null;
            }

            IReadOnlyList<ShuttleMedicalPatientSnapshot> snapshots =
                occupancy.BuildPatientSnapshotsForReading();
            if (snapshots == null)
            {
                return null;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                ShuttleMedicalPatientSnapshot snapshot = snapshots[i];
                if (snapshot != null && snapshot.PawnThingID == patientThingID)
                {
                    return snapshot.Pawn;
                }
            }

            return null;
        }

        private string GetProcedureStatusLabel(MedicalBayProcedureRecord record)
        {
            if (record == null)
            {
                return "CT_Shuttle_MedicalProcedure_Status_None".Translate().ToString();
            }

            if (record.Status == MedicalBayProcedureRecord.StatusRecoveryRequired)
            {
                return "CT_Shuttle_MedicalProcedure_Status_RecoveryRequired".Translate().ToString();
            }

            if (record.ProcedureType == MedicalBayProcedureRecord.TypeSurgery)
            {
                return "CT_Shuttle_MedicalProcedure_Status_Surgery".Translate().ToString();
            }

            return "CT_Shuttle_MedicalProcedure_Status_Tending".Translate().ToString();
        }

        private string BuildProcedureTooltip(ShuttleMedicalBayReadModel model)
        {
            if (model == null || !model.HasActiveProcedure)
            {
                return "CT_Shuttle_MedicalProcedure_Status_None".Translate().ToString();
            }

            int percent = UnityEngine.Mathf.RoundToInt(Clamp01(model.ActiveProcedureProgress) * 100f);
            string tooltip = model.ActiveProcedureStatusLabel;
            if (!string.IsNullOrEmpty(model.ActiveProcedureDoctorLabel))
            {
                tooltip += "\n" + "CT_Shuttle_MedicalProcedure_Doctor"
                    .Translate(model.ActiveProcedureDoctorLabel)
                    .ToString();
            }

            if (!string.IsNullOrEmpty(model.ActiveProcedurePatientLabel))
            {
                tooltip += "\n" + "CT_Shuttle_MedicalProcedure_Patient"
                    .Translate(model.ActiveProcedurePatientLabel)
                    .ToString();
            }

            tooltip += "\n" + "CT_Shuttle_MedicalProcedure_Progress"
                .Translate(
                    model.ActiveProcedureWorkTicksDone,
                    model.ActiveProcedureWorkTicksTotal,
                    percent)
                .ToString();
            if (model.ActiveProcedureTendCyclesCompleted > 0)
            {
                tooltip += "\n" + "CT_Shuttle_MedicalProcedure_CyclesCompleted"
                    .Translate(model.ActiveProcedureTendCyclesCompleted)
                    .ToString();
            }

            if (model.ActiveProcedureLastKnownRemainingTendableCount > 0)
            {
                tooltip += "\n" + "CT_Shuttle_MedicalProcedure_RemainingWounds"
                    .Translate(model.ActiveProcedureLastKnownRemainingTendableCount)
                    .ToString();
            }

            return tooltip;
        }

        private string GetPawnLabel(Pawn pawn, int thingID)
        {
            if (pawn != null)
            {
                return pawn.LabelShortCap;
            }

            return thingID > 0 ? "#" + thingID.ToString() : "-";
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return -1f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }
}
