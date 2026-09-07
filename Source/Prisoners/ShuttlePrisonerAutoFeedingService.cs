using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Automatic scheduler for existing Prison Cell feed jobs. It never consumes
    /// food or changes prisoner needs directly; actual feeding remains behind
    /// ShuttlePrisonerFeedingService and JobDriver_FeedShuttlePrisoner.
    /// </summary>
    internal sealed class ShuttlePrisonerAutoFeedingService
    {
        private const int AutoFeedCheckIntervalTicks = 250;
        private const int RetryCooldownTicks = 2500;
        private const int NoFoodCooldownTicks = 5000;
        private const float AutoFeedThreshold = 0.35f;
        private const float EmergencyFeedThreshold = 0.20f;

        private readonly PrisonCellFeedingValidator validator =
            new PrisonCellFeedingValidator();
        private readonly ShuttlePrisonerFeedingService feedingService =
            new ShuttlePrisonerFeedingService();

        private int lastAutoFeedCheckTick = -1;

        internal void Tick(
            ThingWithComps shuttleHost,
            CompShuttlePrisonCellOccupancy occupancy,
            int ticksGame)
        {
            if (ticksGame < 0)
            {
                return;
            }

            if (this.lastAutoFeedCheckTick < 0)
            {
                this.lastAutoFeedCheckTick = ticksGame;
                return;
            }

            int elapsedTicks = ticksGame - this.lastAutoFeedCheckTick;
            if (elapsedTicks <= 0)
            {
                this.lastAutoFeedCheckTick = ticksGame;
                return;
            }

            if (elapsedTicks < AutoFeedCheckIntervalTicks)
            {
                return;
            }

            this.lastAutoFeedCheckTick = ticksGame;

            if (shuttleHost == null ||
                shuttleHost.Destroyed ||
                shuttleHost.Map == null ||
                occupancy == null ||
                !occupancy.HasPrisoners)
            {
                return;
            }

            CompShuttleHolderLaunchTransferState transferState =
                shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (transferState != null && transferState.HasAnyActiveOrRecoveryTransfer)
            {
                return;
            }

            IReadOnlyList<Pawn> prisoners = occupancy.HeldPrisonersForReading;
            if (prisoners == null || prisoners.Count == 0)
            {
                return;
            }

            for (int i = 0; i < prisoners.Count; i++)
            {
                this.TryScheduleFeedForPrisoner(
                    shuttleHost,
                    occupancy,
                    prisoners[i],
                    ticksGame);
            }
        }

        private void TryScheduleFeedForPrisoner(
            ThingWithComps shuttleHost,
            CompShuttlePrisonCellOccupancy occupancy,
            Pawn prisoner,
            int ticksGame)
        {
            if (!this.NeedsAutoFeed(prisoner) ||
                !occupancy.ContainsPrisoner(prisoner))
            {
                return;
            }

            int prisonerThingIDNumber = prisoner.thingIDNumber;
            if (prisonerThingIDNumber <= 0 ||
                this.HasActiveFeedJobForPrisoner(shuttleHost.Map, prisonerThingIDNumber))
            {
                return;
            }

            ShuttlePrisonerRecord record =
                occupancy.FindPrisonerRecordForInternalUse(prisonerThingIDNumber);
            if (record == null)
            {
                // Missing record means occupancy/reconcile state is not trustworthy enough
                // to schedule work. Fail closed instead of creating an uncooldowned job loop.
                return;
            }

            if (this.IsOnCooldown(shuttleHost, record, prisoner, ticksGame))
            {
                return;
            }

            string failReason;
            Pawn feeder = this.FindFeeder(
                shuttleHost,
                prisoner,
                out failReason);
            if (feeder == null)
            {
                this.RecordAutoFeedFailure(record, ticksGame, failReason);
                return;
            }

            record.MarkAutoFeedAttempt(ticksGame);

            if (this.feedingService.TryAssignFeedPrisonerJob(
                shuttleHost,
                feeder,
                prisonerThingIDNumber,
                out failReason))
            {
                // Success here means the scheduler assigned a feed job. The job
                // later performs the actual feeding through the existing service.
                record.MarkAutoFeedSuccess(ticksGame);

                return;
            }

            this.RecordAutoFeedFailure(record, ticksGame, failReason);
        }

        private bool NeedsAutoFeed(Pawn prisoner)
        {
            return prisoner != null &&
                !prisoner.Destroyed &&
                !prisoner.Dead &&
                prisoner.RaceProps != null &&
                prisoner.RaceProps.Humanlike &&
                prisoner.needs != null &&
                prisoner.needs.food != null &&
                prisoner.needs.food.CurLevelPercentage < AutoFeedThreshold;
        }

        private bool IsOnCooldown(
            ThingWithComps shuttleHost,
            ShuttlePrisonerRecord record,
            Pawn prisoner,
            int ticksGame)
        {
            if (record == null || record.LastAutoFeedAttemptTick < 0)
            {
                return false;
            }

            int elapsedTicks = ticksGame - record.LastAutoFeedAttemptTick;
            if (elapsedTicks < 0)
            {
                return false;
            }

            if (this.IsNoFoodFailure(record.LastAutoFeedFailureReason) &&
                this.validator.HasFoodAvailable(shuttleHost, prisoner))
            {
                return false;
            }

            int cooldownTicks = this.GetRetryCooldownTicks(record, prisoner);
            return elapsedTicks < cooldownTicks;
        }

        private int GetRetryCooldownTicks(
            ShuttlePrisonerRecord record,
            Pawn prisoner)
        {
            if (record != null && this.IsNoFoodFailure(record.LastAutoFeedFailureReason))
            {
                return NoFoodCooldownTicks;
            }

            if (prisoner != null &&
                prisoner.needs != null &&
                prisoner.needs.food != null &&
                prisoner.needs.food.CurLevelPercentage < EmergencyFeedThreshold)
            {
                return RetryCooldownTicks / 2;
            }

            return RetryCooldownTicks;
        }

        private Pawn FindFeeder(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            Map map = shuttleHost != null ? shuttleHost.Map : null;
            if (map == null || map.mapPawns == null)
            {
                failReason = "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
                return null;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                failReason = "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
                return null;
            }

            string firstFailure = null;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn feeder = pawns[i];
                if (!this.IsColonistCandidate(feeder))
                {
                    continue;
                }

                string localFailure;
                if (this.validator.CanAssignFeedJob(
                    shuttleHost,
                    feeder,
                    prisoner,
                    out localFailure))
                {
                    failReason = null;
                    return feeder;
                }

                if (string.IsNullOrEmpty(firstFailure))
                {
                    firstFailure = localFailure;
                }
            }

            failReason = !string.IsNullOrEmpty(firstFailure)
                ? firstFailure
                : "CT_Shuttle_PrisonCell_NoFeeder".Translate().ToString();
            return null;
        }

        private bool HasActiveFeedJobForPrisoner(
            Map map,
            int prisonerThingIDNumber)
        {
            if (map == null || map.mapPawns == null || prisonerThingIDNumber <= 0)
            {
                return false;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!this.IsColonistCandidate(pawn) ||
                    pawn.CurJob == null ||
                    pawn.CurJob.def == null ||
                    pawn.CurJob.def.defName != PrisonCellFeedingValidator.FeedPrisonerJobDefName ||
                    pawn.CurJob.count != prisonerThingIDNumber)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool IsColonistCandidate(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.Spawned &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.IsColonist;
        }

        private void RecordAutoFeedFailure(
            ShuttlePrisonerRecord record,
            int ticksGame,
            string failReason)
        {
            if (record == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(failReason))
            {
                failReason = "CT_Shuttle_PrisonCell_FeedFailed".Translate().ToString();
            }

            record.MarkAutoFeedAttempt(ticksGame);
            record.MarkAutoFeedFailure(ticksGame, failReason);
        }

        private bool IsNoFoodFailure(string failReason)
        {
            if (string.IsNullOrEmpty(failReason))
            {
                return false;
            }

            return failReason == "CT_Shuttle_PrisonCell_NoFood".Translate().ToString() ||
                failReason == "CT_Shuttle_PrisonCell_NoFood";
        }
    }
}
