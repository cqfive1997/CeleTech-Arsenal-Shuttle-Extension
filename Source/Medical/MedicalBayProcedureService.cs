using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayProcedureService
    {
        internal const int DefaultTendProcedureWorkTicks = 600;

        private readonly MedicalBayTreatmentService treatmentService =
            new MedicalBayTreatmentService();
        private readonly VirtualMedicalBaySurgeryService surgeryService =
            new VirtualMedicalBaySurgeryService();

        internal bool CanStartProcedure(
            ThingWithComps shuttle,
            Pawn doctor,
            int patientThingID,
            out string reason)
        {
            reason = null;
            CompShuttleMedicalBayOccupancy patientOccupancy;
            CompShuttleMedicalProcedureOccupancy procedureOccupancy;
            MedicalBayProcedureRuntimeState procedureState;
            if (!this.TryGetProcedureDependencies(
                shuttle,
                out patientOccupancy,
                out procedureOccupancy,
                out procedureState,
                out reason))
            {
                return false;
            }

            if (procedureState.HasActiveProcedure || procedureOccupancy.HasActiveDoctors)
            {
                reason = "CT_Shuttle_MedicalProcedure_AlreadyActive".Translate().ToString();
                return false;
            }

            if (this.FindPatientByThingID(patientOccupancy, patientThingID) == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_NoPatient".Translate().ToString();
                return false;
            }

            if (!MedicalBayAdmissionValidator.CanUseDoctorForMedicalBayTending(
                doctor,
                shuttle,
                out reason))
            {
                return false;
            }

            if (!procedureOccupancy.CanEnterDoctor(doctor, out reason))
            {
                return false;
            }

            return true;
        }

        internal bool TryStartProcedure(
            ThingWithComps shuttle,
            Pawn doctor,
            int patientThingID,
            string procedureType,
            bool useAvailableMedicine,
            out MedicalBayProcedureRecord record,
            out string reason)
        {
            return this.TryStartProcedure(
                shuttle,
                doctor,
                patientThingID,
                procedureType,
                useAvailableMedicine,
                0,
                string.Empty,
                -1,
                out record,
                out reason);
        }

        internal bool TryStartTendProcedure(
            ThingWithComps shuttle,
            Pawn doctor,
            int patientThingID,
            bool useAvailableMedicine,
            out MedicalBayProcedureRecord record,
            out string reason)
        {
            record = null;
            reason = null;
            if (this.treatmentService == null)
            {
                reason = "CT_Shuttle_MedicalBay_TreatmentFailed".Translate(string.Empty).ToString();
                return false;
            }

            bool canTend = useAvailableMedicine
                ? this.treatmentService.CanTendContainedPatientWithAnyAllowedMedicine(
                    shuttle,
                    doctor,
                    patientThingID,
                    out reason)
                : this.treatmentService.CanTendContainedPatientNoMedicine(
                    shuttle,
                    doctor,
                    patientThingID,
                    out reason);
            if (!canTend)
            {
                return false;
            }

            return this.TryStartProcedure(
                shuttle,
                doctor,
                patientThingID,
                MedicalBayProcedureRecord.TypeTend,
                useAvailableMedicine,
                DefaultTendProcedureWorkTicks,
                string.Empty,
                -1,
                out record,
                out reason);
        }

        internal bool TryStartSurgeryProcedure(
            ThingWithComps shuttle,
            Pawn doctor,
            int patientThingID,
            string recipeDefName,
            int bodyPartIndex,
            out MedicalBayProcedureRecord record,
            out string reason)
        {
            record = null;
            reason = null;
            if (this.surgeryService == null)
            {
                reason = "CT_Shuttle_MedicalSurgery_RecipeWorkerUnsupported".Translate().ToString();
                return false;
            }

            if (!this.CanStartProcedure(shuttle, doctor, patientThingID, out reason))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            Pawn patient = this.FindPatientByThingID(patientOccupancy, patientThingID);
            RecipeDef recipeDef = this.surgeryService.ResolveRecipeDef(recipeDefName);
            if (!this.surgeryService.CanApplySurgery(
                shuttle,
                doctor,
                patient,
                recipeDef,
                bodyPartIndex,
                out reason))
            {
                return false;
            }

            return this.TryStartProcedure(
                shuttle,
                doctor,
                patientThingID,
                MedicalBayProcedureRecord.TypeSurgery,
                true,
                this.surgeryService.GetSurgeryWorkTicks(recipeDef, patient),
                recipeDef.defName,
                bodyPartIndex,
                out record,
                out reason);
        }

        private bool TryStartProcedure(
            ThingWithComps shuttle,
            Pawn doctor,
            int patientThingID,
            string procedureType,
            bool useAvailableMedicine,
            int workTicksTotal,
            string recipeDefName,
            int bodyPartIndex,
            out MedicalBayProcedureRecord record,
            out string reason)
        {
            record = null;
            reason = null;
            if (!this.CanStartProcedure(shuttle, doctor, patientThingID, out reason))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy patientOccupancy;
            CompShuttleMedicalProcedureOccupancy procedureOccupancy;
            MedicalBayProcedureRuntimeState procedureState;
            if (!this.TryGetProcedureDependencies(
                shuttle,
                out patientOccupancy,
                out procedureOccupancy,
                out procedureState,
                out reason))
            {
                return false;
            }

            record = procedureState.CreateProcedure(
                procedureType,
                doctor.thingIDNumber,
                patientThingID,
                useAvailableMedicine,
                recipeDefName ?? string.Empty,
                bodyPartIndex,
                Max(0, workTicksTotal));
            if (record == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_AlreadyActive".Translate().ToString();
                return false;
            }

            if (!procedureOccupancy.TryEnterDoctor(doctor, out reason))
            {
                procedureState.CancelProcedure(record.ProcedureID);
                record = null;
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "CT_Shuttle_MedicalProcedure_DoctorEnterFailed".Translate().ToString();
                }

                return false;
            }

            record.MarkInProgress();
            return true;
        }

        internal bool TryValidateTendProcedureParticipants(
            ThingWithComps shuttle,
            MedicalBayProcedureRecord record,
            out string reason)
        {
            reason = null;
            if (record == null || record.ProcedureType != MedicalBayProcedureRecord.TypeTend)
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            if (procedureOccupancy == null || !procedureOccupancy.ContainsDoctor(record.DoctorThingID))
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            if (this.FindPatientByThingID(patientOccupancy, record.PatientThingID) == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_NoPatient".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool TryValidateSurgeryProcedureParticipants(
            ThingWithComps shuttle,
            MedicalBayProcedureRecord record,
            out string reason)
        {
            reason = null;
            if (record == null || record.ProcedureType != MedicalBayProcedureRecord.TypeSurgery)
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            if (procedureOccupancy == null || !procedureOccupancy.ContainsDoctor(record.DoctorThingID))
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            if (this.FindPatientByThingID(patientOccupancy, record.PatientThingID) == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_NoPatient".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool TryCompleteTendProcedure(
            ThingWithComps shuttle,
            MedicalBayProcedureRecord record,
            out string reason)
        {
            reason = null;
            if (record == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            MedicalBayProcedureRuntimeState procedureState;
            string procedureStateReason;
            if (!this.TryGetProcedureState(shuttle, out procedureState, out procedureStateReason))
            {
                reason = procedureStateReason;
                return false;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            Pawn doctor = procedureOccupancy != null
                ? procedureOccupancy.GetDoctorByThingID(record.DoctorThingID)
                : null;
            Pawn patient = this.FindPatientByThingID(patientOccupancy, record.PatientThingID);
            if (patient == null)
            {
                string exitReason;
                if (doctor != null &&
                    !this.TryExitProcedureDoctor(shuttle, procedureOccupancy, doctor, out exitReason))
                {
                    procedureState.MarkRecoveryRequired(record.ProcedureID);
                    this.ReleaseTreatmentReservation(patientOccupancy, record);
                    reason = string.IsNullOrEmpty(exitReason)
                        ? "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString()
                        : exitReason;
                    return false;
                }

                procedureState.MarkFailed(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                reason = "CT_Shuttle_MedicalProcedure_NoPatient".Translate().ToString();
                return false;
            }

            if (doctor == null ||
                !this.TryExitProcedureDoctor(shuttle, procedureOccupancy, doctor, out reason))
            {
                procedureState.MarkRecoveryRequired(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                }

                return false;
            }

            int untendedTendableHediffCount = this.treatmentService.CountUntendedTendableHediffs(
                shuttle,
                record.PatientThingID,
                out reason);
            if (untendedTendableHediffCount <= 0)
            {
                if (reason == "CT_Shuttle_MedicalBay_NoTendableHediffs".Translate().ToString())
                {
                    this.CompleteTendProcedure(procedureState, patientOccupancy, record, null);
                    return true;
                }

                procedureState.MarkFailed(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                return false;
            }

            bool success;
            string resultNotice = null;
            if (record.UseAvailableMedicine)
            {
                success = this.treatmentService.TryTendContainedPatientWithAnyAllowedMedicineForProcedure(
                    shuttle,
                    doctor,
                    record.PatientThingID,
                    out resultNotice,
                    out reason);
            }
            else
            {
                success = this.treatmentService.TryTendContainedPatientNoMedicineForProcedure(
                    shuttle,
                    doctor,
                    record.PatientThingID,
                    out reason);
            }

            if (!success)
            {
                procedureState.MarkFailed(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                this.ShowTendFailureMessage(record, reason, untendedTendableHediffCount);
                return false;
            }

            int remainingTendableHediffCount = this.treatmentService.CountUntendedTendableHediffs(
                shuttle,
                record.PatientThingID,
                out reason);
            if (remainingTendableHediffCount <= 0)
            {
                if (!string.IsNullOrEmpty(reason) &&
                    reason != "CT_Shuttle_MedicalBay_NoTendableHediffs".Translate().ToString())
                {
                    procedureState.MarkFailed(record.ProcedureID);
                    this.ReleaseTreatmentReservation(patientOccupancy, record);
                    return false;
                }

                this.CompleteTendProcedure(procedureState, patientOccupancy, record, resultNotice);
                return true;
            }

            string continueReason;
            bool canContinue = record.UseAvailableMedicine
                ? this.treatmentService.CanTendContainedPatientWithAnyAllowedMedicine(
                    shuttle,
                    doctor,
                    record.PatientThingID,
                    out continueReason)
                : this.treatmentService.CanTendContainedPatientNoMedicine(
                    shuttle,
                    doctor,
                    record.PatientThingID,
                    out continueReason);
            if (!canContinue)
            {
                procedureState.MarkFailed(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                this.ShowTendFailureMessage(record, continueReason, remainingTendableHediffCount);
                reason = continueReason;
                return false;
            }

            if (!procedureOccupancy.TryEnterDoctor(doctor, out reason))
            {
                if (this.IsDoctorSafelyOnMap(doctor, shuttle))
                {
                    procedureState.MarkFailed(record.ProcedureID);
                }
                else
                {
                    procedureState.MarkRecoveryRequired(record.ProcedureID);
                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = "CT_Shuttle_MedicalProcedure_DoctorEnterFailed".Translate().ToString();
                    }
                }

                this.ReleaseTreatmentReservation(patientOccupancy, record);
                return false;
            }

            record.MarkTendCycleCompleted(remainingTendableHediffCount);
            record.ResetWorkForNextCycle(DefaultTendProcedureWorkTicks);
            Messages.Message(
                "CT_Shuttle_MedicalProcedure_TendCycleCompleted"
                    .Translate(remainingTendableHediffCount)
                    .ToString(),
                MessageTypeDefOf.NeutralEvent,
                false);
            reason = null;
            return true;
        }

        private void CompleteTendProcedure(
            MedicalBayProcedureRuntimeState procedureState,
            CompShuttleMedicalBayOccupancy patientOccupancy,
            MedicalBayProcedureRecord record,
            string resultNotice)
        {
            if (procedureState == null || record == null)
            {
                return;
            }

            procedureState.MarkCompleted(record.ProcedureID);
            this.ReleaseTreatmentReservation(patientOccupancy, record);
            string message = "CT_Shuttle_MedicalProcedure_TendFullyCompleted".Translate().ToString();
            if (!string.IsNullOrEmpty(resultNotice))
            {
                message = message + " " + resultNotice;
            }

            Messages.Message(message, MessageTypeDefOf.PositiveEvent, false);
        }

        internal bool TryCompleteSurgeryProcedure(
            ThingWithComps shuttle,
            MedicalBayProcedureRecord record,
            out string reason)
        {
            reason = null;
            if (record == null || record.ProcedureType != MedicalBayProcedureRecord.TypeSurgery)
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            MedicalBayProcedureRuntimeState procedureState;
            string procedureStateReason;
            if (!this.TryGetProcedureState(shuttle, out procedureState, out procedureStateReason))
            {
                reason = procedureStateReason;
                return false;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            Pawn doctor = procedureOccupancy != null
                ? procedureOccupancy.GetDoctorByThingID(record.DoctorThingID)
                : null;
            Pawn patient = this.FindPatientByThingID(patientOccupancy, record.PatientThingID);
            if (patient == null)
            {
                string exitReason;
                if (doctor != null &&
                    !this.TryExitProcedureDoctor(shuttle, procedureOccupancy, doctor, out exitReason))
                {
                    procedureState.MarkRecoveryRequired(record.ProcedureID);
                    this.ReleaseTreatmentReservation(patientOccupancy, record);
                    reason = string.IsNullOrEmpty(exitReason)
                        ? "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString()
                        : exitReason;
                    return false;
                }

                procedureState.MarkFailed(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                reason = "CT_Shuttle_MedicalProcedure_NoPatient".Translate().ToString();
                return false;
            }

            if (doctor == null ||
                !this.TryExitProcedureDoctor(shuttle, procedureOccupancy, doctor, out reason))
            {
                procedureState.MarkRecoveryRequired(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                }

                return false;
            }

            RecipeDef recipeDef = this.surgeryService != null
                ? this.surgeryService.ResolveRecipeDef(record.RecipeDefName)
                : null;
            string notice = null;
            bool success = this.surgeryService != null &&
                this.surgeryService.TryApplySurgery(
                    shuttle,
                    doctor,
                    patient,
                    recipeDef,
                    record.BodyPartIndex,
                    out notice,
                    out reason);
            this.ReleaseTreatmentReservation(patientOccupancy, record);
            if (!success)
            {
                procedureState.MarkFailed(record.ProcedureID);
                Messages.Message(
                    "CT_Shuttle_MedicalSurgery_Failed".Translate().ToString() +
                        (!string.IsNullOrEmpty(reason) ? ": " + reason : string.Empty),
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            procedureState.MarkCompleted(record.ProcedureID);
            Messages.Message(
                !string.IsNullOrEmpty(notice)
                    ? notice
                    : "CT_Shuttle_MedicalSurgery_Completed".Translate().ToString(),
                MessageTypeDefOf.PositiveEvent,
                false);
            reason = null;
            return true;
        }

        private void ShowTendFailureMessage(
            MedicalBayProcedureRecord record,
            string failureReason,
            int remainingTendableHediffCount)
        {
            string message = failureReason;
            if (record != null &&
                record.UseAvailableMedicine &&
                this.IsMedicineUnavailableReason(failureReason))
            {
                message = "CT_Shuttle_MedicalProcedure_MedicineDepleted".Translate().ToString();
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "CT_Shuttle_MedicalBay_TreatmentFailed".Translate(string.Empty).ToString();
            }

            if (remainingTendableHediffCount > 0)
            {
                message = message + " " +
                    "CT_Shuttle_MedicalProcedure_RemainingWounds"
                        .Translate(remainingTendableHediffCount)
                        .ToString();
            }

            Messages.Message(
                "CT_Shuttle_MedicalProcedure_TendFailed".Translate(message).ToString(),
                MessageTypeDefOf.RejectInput,
                false);
        }

        private bool IsDoctorSafelyOnMap(Pawn doctor, ThingWithComps shuttle)
        {
            if (doctor == null || doctor.Destroyed || doctor.Dead || !doctor.Spawned || doctor.Map == null)
            {
                return false;
            }

            return shuttle == null || shuttle.Map == null || doctor.Map == shuttle.Map;
        }

        private bool IsMedicineUnavailableReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return true;
            }

            return reason == "CT_Shuttle_MedicalBay_NoDoctorMedicine".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_DoctorInventoryMedicineUnavailable".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_NoLoadedCargoMedicine".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_LoadedCargoMedicineUnavailable".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_NoAvailableMedicine".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_NoAllowedDoctorMedicine".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_NoAllowedLoadedCargoMedicine".Translate().ToString() ||
                reason == "CT_Shuttle_MedicalBay_NoAllowedMedicine".Translate().ToString();
        }

        internal bool TryFailProcedureAndExitDoctor(
            ThingWithComps shuttle,
            MedicalBayProcedureRecord record,
            string failureReason)
        {
            if (record == null)
            {
                return false;
            }

            MedicalBayProcedureRuntimeState procedureState;
            string reason;
            if (!this.TryGetProcedureState(shuttle, out procedureState, out reason))
            {
                return false;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            Pawn doctor = procedureOccupancy != null
                ? procedureOccupancy.GetDoctorByThingID(record.DoctorThingID)
                : null;
            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            if (doctor != null &&
                !this.TryExitProcedureDoctor(shuttle, procedureOccupancy, doctor, out reason))
            {
                procedureState.MarkRecoveryRequired(record.ProcedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                return false;
            }

            procedureState.MarkFailed(record.ProcedureID);
            this.ReleaseTreatmentReservation(patientOccupancy, record);
            if (!string.IsNullOrEmpty(failureReason))
            {
                Messages.Message(
                    "CT_Shuttle_MedicalProcedure_TendFailed".Translate(failureReason).ToString(),
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            return true;
        }

        internal bool TryCancelProcedure(
            ThingWithComps shuttle,
            int procedureID,
            out string reason)
        {
            reason = null;
            MedicalBayProcedureRuntimeState procedureState;
            if (!this.TryGetProcedureState(shuttle, out procedureState, out reason))
            {
                return false;
            }

            MedicalBayProcedureRecord record;
            if (!procedureState.TryGetProcedure(procedureID, out record) || record == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;
            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            Pawn doctor = procedureOccupancy != null
                ? procedureOccupancy.GetDoctorByThingID(record.DoctorThingID)
                : null;
            if (doctor != null &&
                (procedureOccupancy == null ||
                    !procedureOccupancy.TryExitDoctor(doctor, this.GetPreferredDoctorExitCell(shuttle), out reason)))
            {
                procedureState.MarkRecoveryRequired(procedureID);
                this.ReleaseTreatmentReservation(patientOccupancy, record);
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                }

                return false;
            }

            procedureState.CancelProcedure(procedureID);
            this.ReleaseTreatmentReservation(patientOccupancy, record);
            return true;
        }

        internal bool TryExitAllProcedureDoctors(ThingWithComps shuttle, out string reason)
        {
            reason = null;
            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            if (procedureOccupancy == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            return procedureOccupancy.TryExitAllDoctors(
                this.GetPreferredDoctorExitCell(shuttle),
                out reason);
        }

        internal bool TryCancelAllProceduresAndExitDoctors(ThingWithComps shuttle, out string reason)
        {
            reason = null;
            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            MedicalBayProcedureRuntimeState procedureState;
            string procedureStateReason;
            bool hasProcedureState = this.TryGetProcedureState(shuttle, out procedureState, out procedureStateReason);
            CompShuttleMedicalBayOccupancy patientOccupancy =
                shuttle != null ? shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>() : null;

            if (procedureOccupancy != null && procedureOccupancy.HasActiveDoctors)
            {
                if (!procedureOccupancy.TryExitAllDoctors(this.GetPreferredDoctorExitCell(shuttle), out reason))
                {
                    if (hasProcedureState)
                    {
                        this.MarkAllActiveProceduresRecoveryRequired(procedureState, patientOccupancy);
                    }

                    if (string.IsNullOrEmpty(reason))
                    {
                        reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                    }

                    return false;
                }
            }
            else if (procedureOccupancy == null && !hasProcedureState)
            {
                reason = procedureStateReason ?? "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            if (hasProcedureState)
            {
                this.CancelAllActiveProcedures(procedureState, patientOccupancy);
            }

            return true;
        }

        internal bool HasActiveProcedure(ThingWithComps shuttle)
        {
            if (shuttle == null)
            {
                return false;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>();
            if (procedureOccupancy != null && procedureOccupancy.HasActiveDoctors)
            {
                return true;
            }

            MedicalBayProcedureRuntimeState procedureState;
            string reason;
            return this.TryGetProcedureState(shuttle, out procedureState, out reason) &&
                procedureState.HasActiveProcedure;
        }

        private bool TryGetProcedureDependencies(
            ThingWithComps shuttle,
            out CompShuttleMedicalBayOccupancy patientOccupancy,
            out CompShuttleMedicalProcedureOccupancy procedureOccupancy,
            out MedicalBayProcedureRuntimeState procedureState,
            out string reason)
        {
            patientOccupancy = null;
            procedureOccupancy = null;
            procedureState = null;
            reason = null;
            if (shuttle == null || shuttle.Destroyed)
            {
                reason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            ShuttleProfile profile;
            MedicalBayProfile medicalBay;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayProfile(shuttle, out profile, out medicalBay))
            {
                reason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttle))
            {
                reason = "CT_Shuttle_MedicalBay_Unpowered".Translate().ToString();
                return false;
            }

            patientOccupancy = shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>();
            procedureOccupancy = shuttle.TryGetComp<CompShuttleMedicalProcedureOccupancy>();
            if (patientOccupancy == null || procedureOccupancy == null)
            {
                reason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!this.TryGetProcedureState(shuttle, out procedureState, out reason))
            {
                return false;
            }

            return true;
        }

        private bool TryGetProcedureState(
            ThingWithComps shuttle,
            out MedicalBayProcedureRuntimeState procedureState,
            out string reason)
        {
            procedureState = null;
            reason = null;
            ShuttleController controller;
            if (!MedicalBayAdmissionValidator.TryGetShuttleController(shuttle, out controller) ||
                controller == null)
            {
                reason = "CT_Shuttle_Error_CoreUnavailable".Translate().ToString();
                return false;
            }

            ShuttleRuntimeState runtimeState = controller.GetLaunchRuntimeState();
            procedureState = runtimeState != null ? runtimeState.MedicalProcedures : null;
            if (procedureState == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_RecoveryRequired".Translate().ToString();
                return false;
            }

            return true;
        }

        private Pawn FindPatientByThingID(
            CompShuttleMedicalBayOccupancy occupancy,
            int patientThingID)
        {
            if (occupancy == null || patientThingID <= 0)
            {
                return null;
            }

            List<Pawn> patients = occupancy.GetHeldPatientsForReading();
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

        private bool TryExitProcedureDoctor(
            ThingWithComps shuttle,
            CompShuttleMedicalProcedureOccupancy procedureOccupancy,
            Pawn doctor,
            out string reason)
        {
            reason = null;
            if (doctor == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            if (procedureOccupancy == null)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            return procedureOccupancy.TryExitDoctor(
                doctor,
                this.GetPreferredDoctorExitCell(shuttle),
                out reason);
        }

        private void ReleaseTreatmentReservation(
            CompShuttleMedicalBayOccupancy patientOccupancy,
            MedicalBayProcedureRecord record)
        {
            if (patientOccupancy == null || record == null)
            {
                return;
            }

            patientOccupancy.ReleaseTreatmentReservation(null, record.PatientThingID);
        }

        private void CancelAllActiveProcedures(
            MedicalBayProcedureRuntimeState procedureState,
            CompShuttleMedicalBayOccupancy patientOccupancy)
        {
            if (procedureState == null)
            {
                return;
            }

            IReadOnlyList<MedicalBayProcedureRecord> records = procedureState.ActiveProcedures;
            for (int i = 0; i < records.Count; i++)
            {
                MedicalBayProcedureRecord record = records[i];
                if (record != null && record.IsActive)
                {
                    procedureState.CancelProcedure(record.ProcedureID);
                    this.ReleaseTreatmentReservation(patientOccupancy, record);
                }
            }
        }

        private void MarkAllActiveProceduresRecoveryRequired(
            MedicalBayProcedureRuntimeState procedureState,
            CompShuttleMedicalBayOccupancy patientOccupancy)
        {
            if (procedureState == null)
            {
                return;
            }

            IReadOnlyList<MedicalBayProcedureRecord> records = procedureState.ActiveProcedures;
            for (int i = 0; i < records.Count; i++)
            {
                MedicalBayProcedureRecord record = records[i];
                if (record != null && record.IsActive)
                {
                    procedureState.MarkRecoveryRequired(record.ProcedureID);
                    this.ReleaseTreatmentReservation(patientOccupancy, record);
                }
            }
        }

        private IntVec3 GetPreferredDoctorExitCell(ThingWithComps shuttle)
        {
            if (shuttle == null)
            {
                return IntVec3.Invalid;
            }

            return shuttle.InteractionCell.IsValid ? shuttle.InteractionCell : shuttle.Position;
        }

        private static int Max(int left, int right)
        {
            return left > right ? left : right;
        }
    }
}
