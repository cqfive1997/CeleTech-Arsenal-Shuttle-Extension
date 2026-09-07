using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.MedicalBay
{
    public sealed class JobDriver_TendMedicalBayPatient : JobDriver
    {
        private readonly MedicalBayTreatmentService treatmentService =
            new MedicalBayTreatmentService();
        private bool loopFinished;
        private bool procedureStarted;

        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        private int PatientThingID
        {
            get
            {
                // The patient is contained in the Medical Bay holder and is not a map target.
                // M5D.1 stores the contained patient's thingID in job.count.
                return this.job != null ? this.job.count : -1;
            }
        }

        private bool UseAvailableMedicine
        {
            get
            {
                return this.job != null &&
                    this.job.def != null &&
                    this.job.def.defName == MedicalBayAdmissionValidator.TendMedicalBayPatientWithMedicineJobDefName;
            }
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.ReleaseTreatmentReservationOnFinish);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            string failReason;
            if (!this.CanTendForCurrentMode(
                shuttleWithComps,
                out failReason))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.TryReserveTreatment(this.pawn, this.PatientThingID, out failReason))
            {
                return false;
            }

            if (!this.pawn.Reserve(this.ShuttleHost, this.job, 1, -1, null, errorOnFailed, false))
            {
                occupancy.ReleaseTreatmentReservation(this.pawn, this.PatientThingID);
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !this.IsReservedForThisDoctor());

            yield return this.MakeStartMedicalBayTendProcedureToil();
        }

        private Toil MakeStartMedicalBayTendProcedureToil()
        {
            Toil toil = ToilMaker.MakeToil("StartMedicalBayTendProcedure");
            toil.initAction = delegate
            {
                if (this.loopFinished ||
                    !this.CheckContinueOrStop())
                {
                    return;
                }

                ShuttleController controller;
                string message = null;
                if (!MedicalBayAdmissionValidator.TryGetShuttleController(this.ShuttleHost, out controller) ||
                    controller == null ||
                    !controller.TryStartMedicalBayTendProcedureFromPawn(
                        this.pawn,
                        this.PatientThingID,
                        this.UseAvailableMedicine,
                        out message))
                {
                    this.StopTendLoop(message);
                    return;
                }

                this.procedureStarted = true;
                this.loopFinished = true;
                Messages.Message(
                    message ?? "CT_Shuttle_MedicalProcedure_TendStarted".Translate().ToString(),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        }

        private bool CheckContinueOrStop()
        {
            if (this.loopFinished)
            {
                return false;
            }

            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            int untendedTendableHediffCount;
            string failReason;
            if (this.treatmentService.CanContinueTendingContainedPatient(
                shuttleWithComps,
                this.pawn,
                this.PatientThingID,
                this.UseAvailableMedicine,
                out untendedTendableHediffCount,
                out failReason))
            {
                return true;
            }

            if (this.IsNoTendableReason(failReason))
            {
                this.loopFinished = true;
                Messages.Message(
                    "CT_Shuttle_MedicalBay_TendLoopCompleted".Translate().ToString(),
                    MessageTypeDefOf.PositiveEvent,
                    false);
                this.EndJobWith(JobCondition.Succeeded);
                return false;
            }

            this.StopTendLoop(this.BuildStopReason(failReason));
            return false;
        }

        private bool CanTendForCurrentMode(
            ThingWithComps shuttleWithComps,
            out string failReason)
        {
            if (this.UseAvailableMedicine)
            {
                return this.treatmentService.CanTendContainedPatientWithAnyAllowedMedicine(
                    shuttleWithComps,
                    this.pawn,
                    this.PatientThingID,
                    out failReason);
            }

            return this.treatmentService.CanTendContainedPatientNoMedicine(
                shuttleWithComps,
                this.pawn,
                this.PatientThingID,
                out failReason);
        }

        private bool IsReservedForThisDoctor()
        {
            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy) ||
                occupancy == null ||
                this.pawn == null)
            {
                return false;
            }

            return occupancy.GetTreatmentReservationDoctorThingID(this.PatientThingID) == this.pawn.thingIDNumber;
        }

        private void StopTendLoop(string reason)
        {
            if (this.loopFinished)
            {
                return;
            }

            this.loopFinished = true;
            string stopReason = string.IsNullOrEmpty(reason)
                ? "CT_Shuttle_MedicalBay_TreatmentFailed".Translate(string.Empty).ToString()
                : reason;
            Messages.Message(
                "CT_Shuttle_MedicalBay_TendLoopStopped".Translate(stopReason).ToString(),
                MessageTypeDefOf.RejectInput,
                false);
            this.EndJobWith(JobCondition.Incompletable);
        }

        private string BuildStopReason(string failReason)
        {
            string reason = failReason;
            if (this.UseAvailableMedicine &&
                (this.IsNoAvailableMedicineReason(failReason) || string.IsNullOrEmpty(failReason)))
            {
                reason = string.IsNullOrEmpty(failReason)
                    ? "CT_Shuttle_MedicalBay_AvailableMedicineDepleted".Translate().ToString()
                    : failReason;
            }

            if (!this.IsNoTendableReason(reason))
            {
                reason = string.IsNullOrEmpty(reason)
                    ? "CT_Shuttle_MedicalBay_RemainingWoundsUntended".Translate().ToString()
                    : reason +
                    " " +
                    "CT_Shuttle_MedicalBay_RemainingWoundsUntended".Translate().ToString();
            }

            return reason;
        }

        private bool IsNoTendableReason(string failReason)
        {
            return failReason == "CT_Shuttle_MedicalBay_NoTendableHediffs".Translate().ToString();
        }

        private bool IsNoAvailableMedicineReason(string failReason)
        {
            return failReason == "CT_Shuttle_MedicalBay_NoDoctorMedicine".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_DoctorInventoryMedicineUnavailable".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_NoLoadedCargoMedicine".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_LoadedCargoMedicineUnavailable".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_NoAvailableMedicine".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_NoAllowedDoctorMedicine".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_NoAllowedLoadedCargoMedicine".Translate().ToString() ||
                failReason == "CT_Shuttle_MedicalBay_NoAllowedMedicine".Translate().ToString();
        }

        private void ReleaseTreatmentReservationOnFinish(JobCondition condition)
        {
            if (this.procedureStarted)
            {
                return;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy) &&
                occupancy != null)
            {
                occupancy.ReleaseTreatmentReservation(this.pawn, this.PatientThingID);
            }
        }
    }
}
