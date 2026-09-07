using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.MechCharging
{
    public sealed class JobDriver_ShuttleMechCharge : JobDriver
    {
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
            if (!MechChargerAdmissionValidator.CanUseShuttleMechChargerForCharging(
                this.pawn,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            MechChargerProfile mechCharger;
            if (!MechChargerAdmissionValidator.TryGetPoweredMechChargerProfile(
                shuttleHost,
                out profile,
                out mechCharger))
            {
                return false;
            }

            CompShuttleMechChargerOccupancy occupancy;
            if (!MechChargerAdmissionValidator.TryGetMechChargerOccupancy(shuttleHost, out occupancy) ||
                !occupancy.TryReserveCharging(this.pawn, this.pawn, out failReason))
            {
                return false;
            }

            if (this.pawn.Reserve(
                shuttleHost,
                this.job,
                Max(1, mechCharger.MechChargeSlots),
                1,
                null,
                errorOnFailed,
                false))
            {
                return true;
            }

            occupancy.ReleaseChargingReservation(this.pawn);
            return false;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.ReleaseChargingReservationOnExit);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !this.CanStillEnterMechCharger())
                .FailOn(() => !this.HasAvailableSlotForCurrentJob());

            yield return this.MakeEnterMechChargerToil();
        }

        private Toil MakeEnterMechChargerToil()
        {
            Toil toil = ToilMaker.MakeToil("EnterShuttleMechCharger");
            toil.initAction = delegate
            {
                CompShuttleMechChargerOccupancy occupancy = this.GetMechChargerOccupancy();
                string failReason;
                if (occupancy == null || !occupancy.TryEnterForCharging(this.pawn, out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillEnterMechCharger()
        {
            string failReason;
            return MechChargerAdmissionValidator.CanUseShuttleMechChargerForCharging(
                this.pawn,
                this.ShuttleHost,
                out failReason,
                this.pawn);
        }

        private bool HasAvailableSlotForCurrentJob()
        {
            MechChargerProfile mechCharger;
            if (!this.TryGetCurrentMechCharger(out mechCharger))
            {
                return false;
            }

            int currentUsers = MechChargerAdmissionValidator.CountCurrentShuttleMechChargeUsers(
                this.ShuttleHost,
                this.pawn,
                this.pawn);
            return currentUsers < mechCharger.MechChargeSlots;
        }

        private bool TryGetCurrentMechCharger(out MechChargerProfile mechCharger)
        {
            mechCharger = null;
            ShuttleProfile profile;
            return MechChargerAdmissionValidator.TryGetPoweredMechChargerProfile(
                this.ShuttleHost,
                out profile,
                out mechCharger);
        }

        private CompShuttleMechChargerOccupancy GetMechChargerOccupancy()
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            return shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleMechChargerOccupancy>()
                : null;
        }

        private void ReleaseChargingReservationOnExit(JobCondition condition)
        {
            CompShuttleMechChargerOccupancy occupancy = this.GetMechChargerOccupancy();
            if (occupancy == null)
            {
                return;
            }

            occupancy.ReleaseChargingReservation(this.pawn);
            if (this.pawn == null || !occupancy.ContainsChargingMech(this.pawn))
            {
                return;
            }

            if (condition == JobCondition.Succeeded && occupancy.IsChargingComplete(this.pawn))
            {
                occupancy.TryEjectChargingMech(this.pawn, out string unusedFailureReason);
                return;
            }

            if (condition != JobCondition.Succeeded)
            {
                occupancy.TryEjectChargingMech(this.pawn, out string unusedFailureReason);
            }
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
