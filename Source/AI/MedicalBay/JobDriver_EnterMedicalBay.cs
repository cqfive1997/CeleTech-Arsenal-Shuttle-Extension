using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.MedicalBay
{
    public sealed class JobDriver_EnterMedicalBay : JobDriver
    {
        private readonly ShuttleMedicalBayOccupancyService occupancyService =
            new ShuttleMedicalBayOccupancyService();

        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing shuttleHost = this.ShuttleHost;
            string failReason;
            if (!MedicalBayAdmissionValidator.CanUseMedicalBayForSelfAdmit(this.pawn, shuttleHost, out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            MedicalBayProfile medicalBay;
            if (!MedicalBayAdmissionValidator.TryGetPoweredMedicalBayProfile(shuttleHost, out profile, out medicalBay))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                !occupancy.TryReserveAdmission(this.pawn, this.pawn, out failReason))
            {
                return false;
            }

            if (this.pawn.Reserve(shuttleHost, this.job, Max(1, medicalBay.MedicalPatientSlots), 1, null, errorOnFailed, false))
            {
                return true;
            }

            occupancy.ReleaseAdmissionReservation(this.pawn);
            return false;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.ReleaseAdmissionReservationOnExit);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !this.CanStillSelfAdmit());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return this.MakeEnterMedicalBayToil();
        }

        private Toil MakeEnterMedicalBayToil()
        {
            Toil toil = ToilMaker.MakeToil("EnterMedicalBay");
            toil.initAction = delegate
            {
                ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
                string failReason;
                if (shuttleWithComps == null ||
                    !this.occupancyService.TryAdmitSelfPatient(shuttleWithComps, this.pawn, out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillSelfAdmit()
        {
            string failReason;
            return MedicalBayAdmissionValidator.CanContinueSelfAdmission(
                this.pawn,
                this.ShuttleHost,
                out failReason);
        }

        private void ReleaseAdmissionReservationOnExit(JobCondition condition)
        {
            CompShuttleMedicalBayOccupancy occupancy;
            if (MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(this.ShuttleHost, out occupancy))
            {
                occupancy.ReleaseAdmissionReservation(this.pawn);
            }
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
