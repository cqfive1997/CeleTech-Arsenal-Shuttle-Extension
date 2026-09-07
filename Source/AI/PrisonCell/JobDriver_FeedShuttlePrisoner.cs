using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.PrisonCell
{
    public sealed class JobDriver_FeedShuttlePrisoner : JobDriver
    {
        private readonly PrisonCellFeedingValidator validator =
            new PrisonCellFeedingValidator();
        private readonly ShuttlePrisonerFeedingService feedingService =
            new ShuttlePrisonerFeedingService();

        private Thing ShuttleHost
        {
            get { return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null; }
        }

        private int PrisonerThingIDNumber
        {
            // job.count intentionally stores the held prisoner thingID because
            // the prisoner is inside a holder and is not a spawned job target.
            get { return this.job != null ? this.job.count : -1; }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            string failReason;
            Pawn prisoner = this.FindHeldPrisoner();
            if (!this.validator.CanAssignFeedJob(
                this.ShuttleHost as ThingWithComps,
                this.pawn,
                prisoner,
                out failReason))
            {
                return false;
            }

            return this.pawn.Reserve(this.ShuttleHost, this.job, 1, 1, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !this.CanStillFeed());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !this.CanStillFeed());

            yield return this.MakeFeedPrisonerToil();
        }

        private Toil MakeFeedPrisonerToil()
        {
            Toil toil = ToilMaker.MakeToil("FeedShuttlePrisonerInCell");
            toil.initAction = delegate
            {
                string failReason;
                if (!this.feedingService.TryFeedHeldPrisoner(
                    this.ShuttleHost as ThingWithComps,
                    this.pawn,
                    this.PrisonerThingIDNumber,
                    out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillFeed()
        {
            string failReason;
            return this.validator.CanFeedHeldPrisoner(
                this.ShuttleHost as ThingWithComps,
                this.pawn,
                this.FindHeldPrisoner(),
                out failReason);
        }

        private Pawn FindHeldPrisoner()
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            CompShuttlePrisonCellOccupancy occupancy = shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            return occupancy != null
                ? occupancy.FindHeldPrisonerByThingID(this.PrisonerThingIDNumber)
                : null;
        }
    }
}
