using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.MedicalBay
{
    public sealed class JobDriver_CarryPatientToMedicalBay : JobDriver
    {
        private readonly ShuttleMedicalBayOccupancyService occupancyService =
            new ShuttleMedicalBayOccupancyService();
        private bool admittedToMedicalBay;

        private Pawn Patient
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Pawn : null;
            }
        }

        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.B).Thing : null;
            }
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.ReleaseAdmissionReservationOnExit);
            this.AddFinishAction(this.TryDropCarriedPatientOnFailedExit);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            string failReason;
            if (!MedicalBayAdmissionValidator.CanCarryPatientToMedicalBay(
                this.pawn,
                this.Patient,
                this.ShuttleHost,
                out failReason,
                this.pawn))
            {
                return false;
            }

            ShuttleProfile profile;
            MedicalBayProfile medicalBay;
            if (!MedicalBayAdmissionValidator.TryGetPoweredMedicalBayProfile(
                this.ShuttleHost,
                out profile,
                out medicalBay))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy) ||
                !occupancy.TryReserveAdmission(this.pawn, this.Patient, out failReason))
            {
                return false;
            }

            bool reserved = this.pawn.Reserve(this.Patient, this.job, 1, -1, null, errorOnFailed, false) &&
                this.pawn.Reserve(this.ShuttleHost, this.job, Max(1, medicalBay.MedicalPatientSlots), 1, null, errorOnFailed, false);
            if (reserved)
            {
                return true;
            }

            occupancy.ReleaseAdmissionReservation(this.Patient);
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !this.CanStillCarryToPatient());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !this.CanStillAdmitCarriedPatient());

            yield return this.MakeAdmitCarriedPatientToil();
        }

        private Toil MakeAdmitCarriedPatientToil()
        {
            Toil toil = ToilMaker.MakeToil("AdmitCarriedPatientToMedicalBay");
            toil.initAction = delegate
            {
                ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
                string failReason;
                if (shuttleWithComps == null ||
                    !this.occupancyService.TryAdmitCarriedPatient(
                        shuttleWithComps,
                this.pawn,
                this.Patient,
                out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.admittedToMedicalBay = true;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillCarryToPatient()
        {
            string failReason;
            return MedicalBayAdmissionValidator.CanContinueCarryAdmission(
                this.pawn,
                this.Patient,
                this.ShuttleHost,
                out failReason);
        }

        private bool CanStillAdmitCarriedPatient()
        {
            string failReason;
            return MedicalBayAdmissionValidator.CanContinueCarryAdmission(
                this.pawn,
                this.Patient,
                this.ShuttleHost,
                out failReason);
        }

        private void TryDropCarriedPatientOnFailedExit(JobCondition condition)
        {
            Pawn patient = this.Patient;
            if (this.admittedToMedicalBay || !MedicalBayAdmissionValidator.IsCarrierCarryingPatient(this.pawn, patient))
            {
                return;
            }

            if (this.IsPatientAlreadyInMedicalBay(patient))
            {
                return;
            }

            Map map = this.pawn != null ? this.pawn.Map : null;
            if (map == null)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] Could not drop carried Medical Bay patient during job cleanup because carrier has no map. Patient remains in carryTracker. condition=" + condition);
                }

                return;
            }

            IntVec3 dropCell = this.GetCleanupDropCell(map);
            Thing resultingThing;
            if (!this.pawn.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Near, out resultingThing) &&
                Prefs.DevMode)
            {
                Log.Warning("[CeleTech Shuttle] Failed to drop carried Medical Bay patient during job cleanup. Patient remains in carryTracker. condition=" + condition);
            }
        }

        private bool IsPatientAlreadyInMedicalBay(Pawn patient)
        {
            CompShuttleMedicalBayOccupancy occupancy;
            return MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy) &&
                occupancy != null &&
                occupancy.ContainsPatient(patient);
        }

        private void ReleaseAdmissionReservationOnExit(JobCondition condition)
        {
            CompShuttleMedicalBayOccupancy occupancy;
            if (MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy))
            {
                occupancy.ReleaseAdmissionReservation(this.Patient);
            }
        }

        private IntVec3 GetCleanupDropCell(Map map)
        {
            Thing shuttleHost = this.ShuttleHost;
            if (shuttleHost != null)
            {
                IntVec3 interactionCell = shuttleHost.InteractionCell;
                if (interactionCell.IsValid && interactionCell.InBounds(map))
                {
                    return interactionCell;
                }
            }

            return this.pawn != null && this.pawn.Position.IsValid
                ? this.pawn.Position
                : IntVec3.Invalid;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
