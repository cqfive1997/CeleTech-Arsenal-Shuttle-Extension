using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Centralized Medical Bay treatment boundary. Formal gameplay paths may use vanilla
    /// no-medicine tending or one allowed real Medicine Thing per Tend round from doctor
    /// inventory first, then loaded cargo medicine; no caller should modify
    /// contained-patient hediffs directly.
    /// </summary>
    internal sealed class MedicalBayTreatmentService
    {
        private readonly MedicalBayTreatmentQueryService queryService =
            new MedicalBayTreatmentQueryService();
        private readonly MedicalBayTreatmentPreflightService preflightService =
            new MedicalBayTreatmentPreflightService();
        private readonly MedicalBayTreatmentContextBuilder treatmentContextBuilder =
            new MedicalBayTreatmentContextBuilder();
        private readonly MedicalBayMedicineSourceResolver medicineSourceResolver =
            new MedicalBayMedicineSourceResolver();
        private readonly MedicalBayTreatmentEligibilityService eligibilityService =
            new MedicalBayTreatmentEligibilityService();
        private readonly MedicalBayTreatmentDiagnostics diagnostics =
            new MedicalBayTreatmentDiagnostics();
        private readonly ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource =
            new ShuttleLoadedCargoMedicineSupplySource();
        private readonly MedicalBayTreatmentExecutor treatmentExecutor;

        internal MedicalBayTreatmentService()
        {
            this.treatmentExecutor = new MedicalBayTreatmentExecutor(
                this.queryService,
                this.preflightService,
                this.medicineSourceResolver,
                this.diagnostics,
                this.loadedCargoMedicineSupplySource,
                this.TryBuildTreatmentContext);
        }

        internal bool CanTendContainedPatientNoMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            MedicalBayTreatmentContext context;
            return this.TryBuildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out context,
                out failReason) &&
                this.eligibilityService.CanTendWithoutMedicine(
                    context,
                    out failReason);
        }

        internal bool TryAssignTendJobByThingID(
            ThingWithComps shuttleHost,
            int patientThingID,
            int doctorThingID,
            bool useAvailableMedicine,
            out string failReason)
        {
            failReason = null;
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                patientThingID <= 0 ||
                doctorThingID <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            Pawn doctor = this.queryService.FindSpawnedPawnByThingID(shuttleHost.Map, doctorThingID);
            if (doctor == null)
            {
                failReason = "CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString();
                return false;
            }

            return this.TryAssignTendJob(
                shuttleHost,
                doctor,
                patientThingID,
                useAvailableMedicine,
                out failReason);
        }

        internal bool TryAssignTendJob(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool useAvailableMedicine,
            out string failReason)
        {
            failReason = null;
            string jobDefName = useAvailableMedicine
                ? MedicalBayAdmissionValidator.TendMedicalBayPatientWithMedicineJobDefName
                : MedicalBayAdmissionValidator.TendMedicalBayPatientJobDefName;
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(jobDefName);
            if (jobDef == null)
            {
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed".Translate(
                    useAvailableMedicine
                        ? "CT_Shuttle_MedicalBay_TendPatientWithMedicineJobLabel".Translate()
                        : "CT_Shuttle_MedicalBay_TendPatientJobLabel".Translate()).ToString();
                return false;
            }

            if (this.HasActiveTendOrder(shuttleHost, patientThingID))
            {
                failReason = "CT_Shuttle_MedicalBay_TreatmentAlreadyAssigned".Translate().ToString();
                return false;
            }

            bool canTend = useAvailableMedicine
                ? this.CanTendContainedPatientWithAnyAllowedMedicine(
                    shuttleHost,
                    doctor,
                    patientThingID,
                    out failReason)
                : this.CanTendContainedPatientNoMedicine(
                    shuttleHost,
                    doctor,
                    patientThingID,
                    out failReason);
            if (doctor == null || doctor.jobs == null || !canTend)
            {
                return false;
            }

            // V2 treatment starts the same contained-patient tend job as V1, but the UI
            // reaches it only through TreatMedicalBayPatientCommand. The job driver performs
            // treatment reservation and calls TendUtility from the MedicalBay service path.
            Job job = JobMaker.MakeJob(jobDef, shuttleHost);
            job.count = patientThingID;
            if (doctor.jobs.TryTakeOrderedJob(job))
            {
                return true;
            }

            failReason = "CT_Shuttle_MedicalBay_TreatmentFailed".Translate(
                useAvailableMedicine
                    ? "CT_Shuttle_MedicalBay_TendPatientWithMedicineJobLabel".Translate()
                    : "CT_Shuttle_MedicalBay_TendPatientJobLabel".Translate()).ToString();
            return false;
        }

        private bool HasActiveTendOrder(ThingWithComps shuttleHost, int patientThingID)
        {
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null ||
                patientThingID <= 0)
            {
                return false;
            }

            JobDef noMedicineJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.TendMedicalBayPatientJobDefName);
            JobDef medicineJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.TendMedicalBayPatientWithMedicineJobDefName);
            System.Collections.Generic.IReadOnlyList<Pawn> pawns =
                shuttleHost.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; pawns != null && i < pawns.Count; i++)
            {
                Job currentJob = pawns[i] != null ? pawns[i].CurJob : null;
                if (currentJob != null &&
                    (currentJob.def == noMedicineJobDef || currentJob.def == medicineJobDef) &&
                    currentJob.count == patientThingID &&
                    currentJob.GetTarget(TargetIndex.A).Thing == shuttleHost)
                {
                    return true;
                }
            }

            return false;
        }

        internal int CountUntendedTendableHediffs(
            ThingWithComps shuttleHost,
            int patientThingID,
            out string failReason)
        {
            return this.queryService.CountUntendedTendableHediffs(
                shuttleHost,
                patientThingID,
                this.preflightService,
                out failReason);
        }

        internal bool PatientNeedsTend(
            ThingWithComps shuttleHost,
            int patientThingID,
            out int untendedTendableHediffCount,
            out string failReason)
        {
            return this.queryService.PatientNeedsTend(
                shuttleHost,
                patientThingID,
                this.preflightService,
                out untendedTendableHediffCount,
                out failReason);
        }

        internal bool CanContinueTendingContainedPatient(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireMedicine,
            out int untendedTendableHediffCount,
            out string failReason)
        {
            untendedTendableHediffCount = 0;

            MedicalBayTreatmentContext context;
            if (!this.TryBuildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                true,
                out context,
                out failReason))
            {
                return false;
            }

            return this.eligibilityService.CanContinueTending(
                shuttleHost,
                doctor,
                context,
                requireMedicine,
                this.medicineSourceResolver,
                this.queryService,
                this.loadedCargoMedicineSupplySource,
                out untendedTendableHediffCount,
                out failReason);
        }

        internal bool TryTendContainedPatientNoMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            return this.TryTendContainedPatientNoMedicine(
                shuttleHost,
                doctor,
                patientThingID,
                true,
                out failReason);
        }

        internal bool TryTendContainedPatientNoMedicineForProcedure(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            return this.TryTendContainedPatientNoMedicine(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out failReason);
        }

        private bool TryTendContainedPatientNoMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireReservation,
            out string failReason)
        {
            return this.treatmentExecutor.TryTendContainedPatientNoMedicine(
                shuttleHost,
                doctor,
                patientThingID,
                requireReservation,
                out failReason);
        }

        internal bool HasAnyDoctorInventoryMedicine(Pawn doctor)
        {
            return this.medicineSourceResolver.HasAnyDoctorInventoryMedicine(
                doctor,
                this.queryService);
        }

        internal bool CanTendContainedPatientWithDoctorInventoryMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            MedicalBayTreatmentContext context;
            if (!this.TryBuildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out context,
                out failReason))
            {
                return false;
            }

            if (!this.eligibilityService.CanTendWithDoctorInventoryMedicine(
                doctor,
                context,
                this.medicineSourceResolver,
                this.queryService,
                out failReason))
            {
                return false;
            }

            return true;
        }

        internal bool CanTendContainedPatientWithAnyAllowedMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            MedicalBayTreatmentContext context;
            if (!this.TryBuildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out context,
                out failReason))
            {
                return false;
            }

            return this.eligibilityService.CanTendWithAnyAllowedMedicine(
                shuttleHost,
                doctor,
                context,
                this.medicineSourceResolver,
                this.queryService,
                this.loadedCargoMedicineSupplySource,
                out failReason);
        }

        internal bool TryTendContainedPatientWithAnyAllowedMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string resultNotice,
            out string failReason)
        {
            return this.TryTendContainedPatientWithAnyAllowedMedicine(
                shuttleHost,
                doctor,
                patientThingID,
                true,
                out resultNotice,
                out failReason);
        }

        internal bool TryTendContainedPatientWithAnyAllowedMedicineForProcedure(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string resultNotice,
            out string failReason)
        {
            return this.TryTendContainedPatientWithAnyAllowedMedicine(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out resultNotice,
                out failReason);
        }

        private bool TryTendContainedPatientWithAnyAllowedMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireReservation,
            out string resultNotice,
            out string failReason)
        {
            return this.treatmentExecutor.TryTendContainedPatientWithAnyAllowedMedicine(
                shuttleHost,
                doctor,
                patientThingID,
                requireReservation,
                out resultNotice,
                out failReason);
        }

        internal bool TryTendContainedPatientWithDoctorInventoryMedicineOrFallback(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool allowNoMedicineFallback,
            out string resultNotice,
            out string failReason)
        {
            return this.treatmentExecutor.TryTendContainedPatientWithDoctorInventoryMedicineOrFallback(
                shuttleHost,
                doctor,
                patientThingID,
                allowNoMedicineFallback,
                out resultNotice,
                out failReason);
        }

        internal bool TryTendContainedPatientNoMedicineForSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            return this.treatmentExecutor.TryTendContainedPatientNoMedicineForSpike(
                shuttleHost,
                doctor,
                patientThingID,
                out failReason);
        }

        internal bool TryTendContainedPatientWithDoctorInventoryMedicineForSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            return this.treatmentExecutor.TryTendContainedPatientWithDoctorInventoryMedicineForSpike(
                shuttleHost,
                doctor,
                patientThingID,
                out failReason);
        }

        internal bool TryTendContainedPatientWithLoadedCargoMedicineForSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            return this.treatmentExecutor.TryTendContainedPatientWithLoadedCargoMedicineForSpike(
                shuttleHost,
                doctor,
                patientThingID,
                out failReason);
        }

        private bool TryBuildTreatmentContext(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireReservation,
            out MedicalBayTreatmentContext context,
            out string failReason)
        {
            bool releaseTreatmentReservation;
            if (this.treatmentContextBuilder.TryBuildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                requireReservation,
                this.queryService,
                this.preflightService,
                out context,
                out releaseTreatmentReservation,
                out failReason))
            {
                return true;
            }

            if (releaseTreatmentReservation && context.Occupancy != null)
            {
                context.Occupancy.ReleaseTreatmentReservation(null, patientThingID);
            }

            context = default(MedicalBayTreatmentContext);
            return false;
        }

        internal bool HasAnyLoadedCargoMedicine(ThingWithComps shuttleHost)
        {
            return this.medicineSourceResolver.HasAnyLoadedCargoMedicine(
                shuttleHost,
                this.loadedCargoMedicineSupplySource);
        }
    }
}
