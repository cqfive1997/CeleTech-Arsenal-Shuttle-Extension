using System;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal delegate bool MedicalBayTreatmentContextFactory(
        ThingWithComps shuttleHost,
        Pawn doctor,
        int patientThingID,
        bool requireReservation,
        out MedicalBayTreatmentContext context,
        out string failReason);

    internal sealed class MedicalBayTreatmentExecutor
    {
        private readonly MedicalBayTreatmentQueryService queryService;
        private readonly MedicalBayTreatmentPreflightService preflightService;
        private readonly MedicalBayMedicineSourceResolver medicineSourceResolver;
        private readonly MedicalBayTreatmentDiagnostics diagnostics;
        private readonly ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource;
        private readonly MedicalBayTreatmentContextFactory buildTreatmentContext;

        internal MedicalBayTreatmentExecutor(
            MedicalBayTreatmentQueryService queryService,
            MedicalBayTreatmentPreflightService preflightService,
            MedicalBayMedicineSourceResolver medicineSourceResolver,
            MedicalBayTreatmentDiagnostics diagnostics,
            ShuttleLoadedCargoMedicineSupplySource loadedCargoMedicineSupplySource,
            MedicalBayTreatmentContextFactory buildTreatmentContext)
        {
            this.queryService = queryService;
            this.preflightService = preflightService;
            this.medicineSourceResolver = medicineSourceResolver;
            this.diagnostics = diagnostics;
            this.loadedCargoMedicineSupplySource = loadedCargoMedicineSupplySource;
            this.buildTreatmentContext = buildTreatmentContext;
        }

        internal bool TryTendContainedPatientNoMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireReservation,
            out string failReason)
        {
            MedicalBayTreatmentContext context;
            if (!this.buildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                requireReservation,
                out context,
                out failReason))
            {
                return false;
            }

            MedicalBayTendState before = context.TendState;
            try
            {
                TendUtility.DoTend(doctor, context.Patient, null);
            }
            catch (Exception exception)
            {
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed"
                    .Translate(exception.GetType().Name + ": " + exception.Message)
                    .ToString();
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (context.Patient.Destroyed ||
                context.Patient.Dead ||
                !context.Occupancy.ContainsPatient(context.Patient) ||
                context.Patient.Spawned)
            {
                failReason = "CT_Shuttle_MedicalBay_PatientNotHeld".Translate().ToString();
                return false;
            }

            MedicalBayTendState after = this.queryService.ReadTendState(context.Patient);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed"
                    .Translate("CT_Shuttle_MedicalBay_NoTendableHediffs".Translate())
                    .ToString();
                return false;
            }

            return true;
        }

        internal bool TryTendContainedPatientWithAnyAllowedMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool requireReservation,
            out string resultNotice,
            out string failReason)
        {
            resultNotice = null;
            MedicalBayTreatmentContext context;
            if (!this.buildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                requireReservation,
                out context,
                out failReason))
            {
                return false;
            }

            Medicine inventoryMedicine;
            string inventoryFailReason;
            if (this.medicineSourceResolver.TryFindDoctorInventoryMedicine(
                doctor,
                context.Patient,
                this.queryService,
                out inventoryMedicine,
                out inventoryFailReason))
            {
                return this.TryTendWithDoctorInventoryMedicine(
                    context,
                    doctor,
                    inventoryMedicine,
                    out resultNotice,
                    out failReason);
            }

            Medicine cargoMedicine;
            IShuttleMedicalSupplyWithdrawal withdrawal;
            string cargoFailReason = null;
            if (this.loadedCargoMedicineSupplySource != null &&
                this.loadedCargoMedicineSupplySource.TryTakeOneLoadedMedicineForPatient(
                    shuttleHost,
                    context.Patient,
                    out cargoMedicine,
                    out withdrawal,
                    out cargoFailReason))
            {
                return this.TryTendWithLoadedCargoMedicine(
                    context,
                    shuttleHost,
                    doctor,
                    cargoMedicine,
                    withdrawal,
                    out resultNotice,
                    out failReason);
            }

            failReason = this.medicineSourceResolver.BuildAnyAllowedMedicineUnavailableReason(
                inventoryFailReason,
                cargoFailReason);
            return false;
        }

        internal bool TryTendContainedPatientWithDoctorInventoryMedicineOrFallback(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            bool allowNoMedicineFallback,
            out string resultNotice,
            out string failReason)
        {
            resultNotice = null;
            MedicalBayTreatmentContext context;
            if (!this.buildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                true,
                out context,
                out failReason))
            {
                return false;
            }

            Medicine inventoryMedicine;
            bool hasInventoryMedicine = this.medicineSourceResolver.TryFindDoctorInventoryMedicine(
                doctor,
                context.Patient,
                this.queryService,
                out inventoryMedicine,
                out failReason);
            if (!hasInventoryMedicine &&
                failReason == "CT_Shuttle_MedicalBay_NoAllowedDoctorMedicine".Translate().ToString())
            {
                return false;
            }

            if (!hasInventoryMedicine && !allowNoMedicineFallback)
            {
                return false;
            }

            if (!hasInventoryMedicine)
            {
                return this.TryTendContainedPatientNoMedicine(
                    shuttleHost,
                    doctor,
                    patientThingID,
                    true,
                    out failReason);
            }

            return this.TryTendWithDoctorInventoryMedicine(
                context,
                doctor,
                inventoryMedicine,
                out resultNotice,
                out failReason);
        }

        internal bool TryTendContainedPatientNoMedicineForSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            failReason = null;

            CompShuttleMedicalBayOccupancy occupancy;
            if (!this.queryService.TryGetOccupancy(shuttleHost, out occupancy, out failReason))
            {
                return false;
            }

            Pawn patient = this.queryService.FindContainedPatientByThingID(occupancy, patientThingID);
            if (!this.preflightService.CanUseContainedPatient(occupancy, patient, out failReason))
            {
                return false;
            }

            if (!this.preflightService.CanUseDoctorForSpike(doctor, shuttleHost, out failReason))
            {
                return false;
            }

            if (!MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttleHost))
            {
                failReason = "CT_Shuttle_MedicalBay_Unpowered".Translate().ToString();
                return false;
            }

            MedicalBayTendState before = this.queryService.ReadTendState(patient);
            if (before.UntendedTendableCount <= 0)
            {
                failReason = "Medical Bay TendUtility test failed: contained patient has no untended tendable hediffs.";
                return false;
            }

            try
            {
                TendUtility.DoTend(doctor, patient, null);
            }
            catch (Exception exception)
            {
                failReason = "Medical Bay TendUtility test failed: " +
                    exception.GetType().Name +
                    ": " +
                    exception.Message;
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (patient.Destroyed || patient.Dead || !occupancy.ContainsPatient(patient) || patient.Spawned)
            {
                failReason = "Medical Bay TendUtility test failed: contained patient left the Medical Bay holder or became invalid.";
                return false;
            }

            MedicalBayTendState after = this.queryService.ReadTendState(patient);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                failReason = "Medical Bay TendUtility test failed: TendUtility.DoTend completed but tend state did not change.";
                return false;
            }

            return true;
        }

        internal bool TryTendContainedPatientWithDoctorInventoryMedicineForSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            MedicalBayTreatmentContext context;
            if (!this.buildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out context,
                out failReason))
            {
                return false;
            }

            Medicine medicine;
            if (!this.medicineSourceResolver.TryFindSingleStackDoctorInventoryMedicine(
                doctor,
                context.Patient,
                this.queryService,
                out medicine,
                out failReason))
            {
                return false;
            }

            MedicalBayTendState before = context.TendState;
            MedicineSpikeTrace trace = new MedicineSpikeTrace(medicine);
            try
            {
                TendUtility.DoTend(doctor, context.Patient, medicine);
            }
            catch (Exception exception)
            {
                trace.RecordAfter(medicine, context.Patient, context.Occupancy);
                this.TryRecoverUnconsumedMedicine(doctor, medicine, "[CeleTech Shuttle] MedicalBay inventory medicine test exception rollback");
                failReason = "Medical Bay inventory medicine tend test failed: " +
                    exception.GetType().Name +
                    ": " +
                    exception.Message +
                    " | " +
                    trace.ToLogString();
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            trace.RecordAfter(medicine, context.Patient, context.Occupancy);
            if (context.Patient.Destroyed ||
                context.Patient.Dead ||
                !context.Occupancy.ContainsPatient(context.Patient) ||
                context.Patient.Spawned)
            {
                this.TryRecoverUnconsumedMedicine(doctor, medicine, "[CeleTech Shuttle] MedicalBay inventory medicine test patient-invalid rollback");
                failReason = "Medical Bay inventory medicine tend test failed: contained patient left the Medical Bay holder or became invalid. " +
                    trace.ToLogString();
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            MedicalBayTendState after = this.queryService.ReadTendState(context.Patient);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                this.TryRecoverUnconsumedMedicine(doctor, medicine, "[CeleTech Shuttle] MedicalBay inventory medicine test no-state-change rollback");
                failReason = "Medical Bay inventory medicine tend test failed: TendUtility.DoTend completed but tend state did not change. " +
                    trace.ToLogString();
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (Prefs.DevMode)
            {
                Log.Message("[CeleTech Shuttle] MedicalBay inventory medicine tend test succeeded. " +
                    trace.ToLogString());
            }

            return true;
        }

        internal bool TryTendContainedPatientWithLoadedCargoMedicineForSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            out string failReason)
        {
            MedicalBayTreatmentContext context;
            if (!this.buildTreatmentContext(
                shuttleHost,
                doctor,
                patientThingID,
                false,
                out context,
                out failReason))
            {
                return false;
            }

            Medicine medicine;
            IShuttleMedicalSupplyWithdrawal withdrawal;
            if (this.loadedCargoMedicineSupplySource == null ||
                !this.loadedCargoMedicineSupplySource.TryTakeOneLoadedMedicineForPatient(
                    shuttleHost,
                    context.Patient,
                    out medicine,
                    out withdrawal,
                    out failReason))
            {
                return false;
            }

            MedicalBayTendState before = context.TendState;
            try
            {
                TendUtility.DoTend(doctor, context.Patient, medicine);
            }
            catch (Exception exception)
            {
                string rollbackNotice;
                if (withdrawal != null)
                {
                    withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                }
                else
                {
                    rollbackNotice = null;
                }

                failReason = "Medical Bay loaded cargo medicine tend test failed: " +
                    exception.GetType().Name +
                    ": " +
                    exception.Message +
                    " | " +
                    this.diagnostics.BuildLoadedCargoMedicineSpikeTrace(withdrawal, medicine, context.Patient, context.Occupancy);
                failReason = this.diagnostics.AppendNotice(failReason, rollbackNotice);
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (context.Patient.Destroyed ||
                context.Patient.Dead ||
                !context.Occupancy.ContainsPatient(context.Patient) ||
                context.Patient.Spawned)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "Medical Bay loaded cargo medicine tend test failed: contained patient left the Medical Bay holder or became invalid. " +
                    this.diagnostics.BuildLoadedCargoMedicineSpikeTrace(withdrawal, medicine, context.Patient, context.Occupancy);
                failReason = this.diagnostics.AppendNotice(failReason, rollbackNotice);
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            MedicalBayTendState after = this.queryService.ReadTendState(context.Patient);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "Medical Bay loaded cargo medicine tend test failed: TendUtility.DoTend completed but tend state did not change. " +
                    this.diagnostics.BuildLoadedCargoMedicineSpikeTrace(withdrawal, medicine, context.Patient, context.Occupancy);
                failReason = this.diagnostics.AppendNotice(failReason, rollbackNotice);
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            withdrawal.CommitConsumed();
            string trace = this.diagnostics.BuildLoadedCargoMedicineSpikeTrace(
                withdrawal,
                medicine,
                context.Patient,
                context.Occupancy);
            if (Prefs.DevMode)
            {
                if (medicine != null &&
                    !medicine.Destroyed &&
                    medicine.stackCount > 0 &&
                    medicine.holdingOwner == null &&
                    !medicine.Spawned)
                {
                    Log.Warning("[CeleTech Shuttle] MedicalBay loaded cargo medicine tend test succeeded but medicine remained alive, unheld, and unspawned. " +
                        trace);
                }
                else
                {
                    Log.Message("[CeleTech Shuttle] MedicalBay loaded cargo medicine tend test succeeded. " +
                        trace);
                }
            }

            failReason = null;
            return true;
        }

        private bool TryTendWithDoctorInventoryMedicine(
            MedicalBayTreatmentContext context,
            Pawn doctor,
            Medicine inventoryMedicine,
            out string resultNotice,
            out string failReason)
        {
            resultNotice = null;
            Medicine medicineForTend;
            bool splitMedicineFromStack;
            if (!this.TryTakeOneDoctorInventoryMedicine(
                doctor,
                inventoryMedicine,
                out medicineForTend,
                out splitMedicineFromStack,
                out failReason))
            {
                return false;
            }

            MedicalBayTendState before = context.TendState;
            try
            {
                TendUtility.DoTend(doctor, context.Patient, medicineForTend);
            }
            catch (Exception exception)
            {
                string recoveryNotice = this.TryRecoverUnconsumedMedicine(
                    doctor,
                    medicineForTend,
                    "[CeleTech Shuttle] MedicalBay doctor inventory medicine rollback after Tend exception");
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed"
                    .Translate(exception.GetType().Name + ": " + exception.Message)
                    .ToString();
                failReason = this.diagnostics.AppendNotice(failReason, recoveryNotice);
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (context.Patient.Destroyed ||
                context.Patient.Dead ||
                !context.Occupancy.ContainsPatient(context.Patient) ||
                context.Patient.Spawned)
            {
                string recoveryNotice = this.TryRecoverUnconsumedMedicine(
                    doctor,
                    medicineForTend,
                    "[CeleTech Shuttle] MedicalBay doctor inventory medicine rollback after patient invalidation");
                failReason = "CT_Shuttle_MedicalBay_PatientNotHeld".Translate().ToString();
                failReason = this.diagnostics.AppendNotice(failReason, recoveryNotice);
                return false;
            }

            MedicalBayTendState after = this.queryService.ReadTendState(context.Patient);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                string recoveryNotice = this.TryRecoverUnconsumedMedicine(
                    doctor,
                    medicineForTend,
                    "[CeleTech Shuttle] MedicalBay doctor inventory medicine rollback after no tend-state change");
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed"
                    .Translate("CT_Shuttle_MedicalBay_NoTendableHediffs".Translate())
                    .ToString();
                failReason = this.diagnostics.AppendNotice(failReason, recoveryNotice);
                return false;
            }

            if (splitMedicineFromStack)
            {
                this.RecoverOrphanedSplitMedicineAfterSuccessfulTend(
                    doctor,
                    medicineForTend,
                    "[CeleTech Shuttle] MedicalBay doctor inventory medicine success orphan guard");
            }

            resultNotice = medicineForTend != null
                ? "CT_Shuttle_MedicalBay_MedicineConsumed".Translate(this.diagnostics.GetMedicineLabel(medicineForTend)).ToString()
                : "CT_Shuttle_MedicalBay_NoMedicineTendNotice".Translate().ToString();
            return true;
        }

        private bool TryTendWithLoadedCargoMedicine(
            MedicalBayTreatmentContext context,
            ThingWithComps shuttleHost,
            Pawn doctor,
            Medicine medicine,
            IShuttleMedicalSupplyWithdrawal withdrawal,
            out string resultNotice,
            out string failReason)
        {
            resultNotice = null;
            if (withdrawal == null || !this.queryService.IsUsableMedicine(medicine))
            {
                failReason = "CT_Shuttle_MedicalBay_LoadedCargoMedicineUnavailable".Translate().ToString();
                return false;
            }

            MedicalBayTendState before = context.TendState;
            try
            {
                TendUtility.DoTend(doctor, context.Patient, medicine);
            }
            catch (Exception exception)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed"
                    .Translate(exception.GetType().Name + ": " + exception.Message)
                    .ToString();
                failReason = this.diagnostics.AppendNotice(failReason, rollbackNotice);
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (context.Patient.Destroyed ||
                context.Patient.Dead ||
                !context.Occupancy.ContainsPatient(context.Patient) ||
                context.Patient.Spawned)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "CT_Shuttle_MedicalBay_PatientNotHeld".Translate().ToString();
                failReason = this.diagnostics.AppendNotice(failReason, rollbackNotice);
                return false;
            }

            MedicalBayTendState after = this.queryService.ReadTendState(context.Patient);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "CT_Shuttle_MedicalBay_TreatmentFailed"
                    .Translate("CT_Shuttle_MedicalBay_NoTendableHediffs".Translate())
                    .ToString();
                failReason = this.diagnostics.AppendNotice(failReason, rollbackNotice);
                return false;
            }

            if (medicine != null &&
                !medicine.Destroyed &&
                medicine.stackCount > 0 &&
                medicine.holdingOwner == null &&
                !medicine.Spawned)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] MedicalBay loaded cargo medicine Tend succeeded but medicine remained alive, unheld, and unspawned; recovering it through the cargo withdrawal fallback.");
                }

                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                resultNotice = rollbackNotice;
                failReason = null;
                return true;
            }

            withdrawal.CommitConsumed();
            resultNotice = "CT_Shuttle_MedicalBay_MedicineConsumed".Translate(this.diagnostics.GetMedicineLabel(medicine)).ToString();
            failReason = null;
            return true;
        }

        private bool TryTakeOneDoctorInventoryMedicine(
            Pawn doctor,
            Medicine inventoryMedicine,
            out Medicine medicineForTend,
            out bool splitMedicineFromStack,
            out string failReason)
        {
            medicineForTend = null;
            splitMedicineFromStack = false;
            failReason = null;
            if (!this.queryService.IsUsableMedicine(inventoryMedicine) ||
                doctor == null ||
                doctor.inventory == null ||
                doctor.inventory.innerContainer == null)
            {
                failReason = "CT_Shuttle_MedicalBay_DoctorInventoryMedicineUnavailable".Translate().ToString();
                return false;
            }

            if (inventoryMedicine.stackCount == 1)
            {
                medicineForTend = inventoryMedicine;
                return true;
            }

            medicineForTend = inventoryMedicine.SplitOff(1) as Medicine;
            splitMedicineFromStack = true;
            if (!this.queryService.IsUsableMedicine(medicineForTend) || medicineForTend.stackCount != 1)
            {
                this.TryRecoverUnconsumedMedicine(
                    doctor,
                    medicineForTend,
                    "[CeleTech Shuttle] MedicalBay doctor inventory medicine rollback after failed Take");
                failReason = "CT_Shuttle_MedicalBay_DoctorInventoryMedicineUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private void RecoverOrphanedSplitMedicineAfterSuccessfulTend(
            Pawn doctor,
            Medicine medicine,
            string logPrefix)
        {
            if (medicine == null ||
                medicine.Destroyed ||
                medicine.stackCount <= 0 ||
                medicine.holdingOwner != null ||
                medicine.Spawned)
            {
                return;
            }

            if (Prefs.DevMode)
            {
                Log.Warning(logPrefix + ": split medicine remained alive, unheld, and unspawned after successful Tend; recovering it without changing normal vanilla consumption.");
            }

            this.TryRecoverUnconsumedMedicine(doctor, medicine, logPrefix);
        }

        private string TryRecoverUnconsumedMedicine(
            Pawn doctor,
            Medicine medicine,
            string logPrefix)
        {
            if (medicine == null || medicine.Destroyed || medicine.stackCount <= 0)
            {
                return null;
            }

            if (doctor != null &&
                doctor.inventory != null &&
                doctor.inventory.innerContainer != null)
            {
                if (medicine.holdingOwner == doctor.inventory.innerContainer)
                {
                    return "CT_Shuttle_MedicalBay_MedicineReturned".Translate().ToString();
                }

                if (doctor.inventory.innerContainer.TryAddOrTransfer(medicine, false))
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message(logPrefix + ": returned unconsumed medicine to doctor inventory.");
                    }

                    return "CT_Shuttle_MedicalBay_MedicineReturned".Translate().ToString();
                }
            }

            if (doctor != null &&
                doctor.Spawned &&
                doctor.Map != null &&
                GenPlace.TryPlaceThing(medicine, doctor.Position, doctor.Map, ThingPlaceMode.Near))
            {
                if (Prefs.DevMode)
                {
                    Log.Warning(logPrefix + ": dropped unconsumed medicine near doctor because inventory restore failed.");
                }

                return "CT_Shuttle_MedicalBay_MedicineDroppedOnFailure".Translate().ToString();
            }

            if (Prefs.DevMode)
            {
                Log.Warning(logPrefix + ": could not restore or drop unconsumed medicine; leaving it in its current container/state.");
            }

            return null;
        }
    }
}
