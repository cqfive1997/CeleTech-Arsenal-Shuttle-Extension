using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    public sealed class JobDriver_HabitatRest : JobDriver
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
            if (shuttleHost == null || !shuttleHost.Spawned)
            {
                return false;
            }

            string failReason;
            if (!HabitatUtility.CanUseHabitatForSleep(this.pawn, shuttleHost, out failReason))
            {
                return false;
            }

            if (!HabitatUtility.HasAvailableSleepSlot(this.pawn, shuttleHost, out failReason) &&
                !this.HasAvailableSleepSlotForCurrentJob())
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!HabitatUtility.TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                return false;
            }

            return this.pawn.Reserve(shuttleHost, this.job, habitat.SleepSlots, 0, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !this.CanStillUseHabitatForSleep());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !this.CanStillUseHabitatForSleep())
                .FailOn(() => !this.HasAvailableSleepSlotForCurrentJob());

            yield return this.MakeEnterHabitatToil();
        }

        private Toil MakeEnterHabitatToil()
        {
            Toil toil = ToilMaker.MakeToil("EnterHabitatRest");
            toil.initAction = delegate
            {
                CompShuttleHabitatOccupancy occupancy = this.GetHabitatOccupancy();
                string failReason;
                if (occupancy == null || !occupancy.TryEnterForSleep(this.pawn, out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillUseHabitatForSleep()
        {
            string failReason;
            return HabitatUtility.CanUseHabitatForSleep(this.pawn, this.ShuttleHost, out failReason);
        }

        private bool HasAvailableSleepSlotForCurrentJob()
        {
            HabitatProfile habitat;
            if (!this.TryGetCurrentHabitat(out habitat))
            {
                return false;
            }

            int currentUsers = HabitatUtility.CountCurrentHabitatSleepUsers(this.ShuttleHost, this.pawn);
            return currentUsers < habitat.SleepSlots;
        }

        private bool TryGetCurrentHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(
                this.ShuttleHost,
                out profile,
                out habitat);
        }

        private CompShuttleHabitatOccupancy GetHabitatOccupancy()
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            return shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
        }
    }
}
