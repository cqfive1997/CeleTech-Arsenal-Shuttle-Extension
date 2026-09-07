using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ShuttleColdAutoTransferPlanner
    {
        private readonly ShuttleColdTransferQueryService queryService;
        private readonly ShuttleColdTransferPreflightService preflightService;
        private readonly ThingWithComps host;
        private readonly ShuttleProfile profile;

        internal ShuttleColdAutoTransferPlanner(
            ShuttleColdTransferQueryService queryService,
            ShuttleColdTransferPreflightService preflightService,
            ThingWithComps host,
            ShuttleProfile profile)
        {
            this.queryService = queryService;
            this.preflightService = preflightService;
            this.host = host;
            this.profile = profile;
        }

        internal ColdAutoTransferCandidateSnapshot BuildAutoTransferCandidateSnapshot(int ticksGame)
        {
            List<ColdAutoTransferCandidate> candidates = new List<ColdAutoTransferCandidate>();
            int scannedStackCount = 0;
            List<CompTransporter> transporters = this.queryService.ResolveTransporters();
            for (int transporterIndex = 0; transporterIndex < transporters.Count; transporterIndex++)
            {
                CompTransporter transporter = transporters[transporterIndex];
                ThingOwner contents = ShuttleColdTransferQueryService.GetContents(transporter);
                if (contents == null)
                {
                    continue;
                }

                for (int loadedIndex = 0; loadedIndex < contents.Count; loadedIndex++)
                {
                    Thing thing = contents[loadedIndex];
                    scannedStackCount++;
                    if (!ShuttleColdTransferMatcher.IsBasicAutoTransferCandidate(thing, contents, this.host))
                    {
                        continue;
                    }

                    candidates.Add(new ColdAutoTransferCandidate(
                        thing,
                        contents,
                        transporter,
                        transporterIndex,
                        loadedIndex,
                        thing.stackCount,
                        CargoDisplayUtility.GetThingMass(thing, thing.stackCount)));
                }
            }

            return new ColdAutoTransferCandidateSnapshot(
                ticksGame,
                candidates,
                scannedStackCount);
        }

        internal bool TryFindAutoTransferCandidate(
            ColdAutoTransferCandidateSnapshot snapshot,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            out ColdAutoTransferCandidate candidate,
            out int moveCount)
        {
            candidate = null;
            moveCount = 0;
            if (snapshot == null || snapshot.Candidates == null || moduleDef == null || record == null)
            {
                return false;
            }

            for (int i = 0; i < snapshot.Candidates.Count; i++)
            {
                ColdAutoTransferCandidate current = snapshot.Candidates[i];
                if (!this.IsAutoTransferCandidateStillAvailable(current))
                {
                    continue;
                }

                int candidateMoveCount;
                if (!this.TryGetAutoTransferMoveCount(
                    current.Thing,
                    moduleDef,
                    record,
                    effectiveAutoTransferFilter,
                    hasCustomAutoTransferFilter,
                    out candidateMoveCount))
                {
                    continue;
                }

                candidate = current;
                moveCount = candidateMoveCount;
                return true;
            }

            return false;
        }

        internal bool IsAutoTransferCandidateStillAvailable(ColdAutoTransferCandidate candidate)
        {
            if (candidate == null || candidate.Consumed)
            {
                return false;
            }

            Thing thing = candidate.Thing;
            if (!ShuttleColdTransferMatcher.IsBasicAutoTransferCandidate(thing, candidate.SourceOwner, this.host))
            {
                candidate.Consumed = true;
                return false;
            }

            return true;
        }

        internal bool TryGetAutoTransferMoveCount(
            Thing thing,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            RefrigeratedCargoRecord record,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            out int moveCount)
        {
            moveCount = 0;
            if (!ShuttleColdTransferMatcher.PassesAutoTransferSelection(
                thing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter))
            {
                return false;
            }

            int requestedCount = thing.stackCount;
            float fullStackMassKg = CargoDisplayUtility.GetThingMass(thing, requestedCount);
            string failureReason;
            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            if (!this.preflightService.HasColdCapacity(
                record,
                this.profile,
                registry,
                fullStackMassKg,
                out failureReason))
            {
                if (!moduleDef.allowPartialStackTransfer)
                {
                    return false;
                }

                requestedCount = this.preflightService.GetMaxPartialCountForColdCapacity(
                    thing,
                    record,
                    this.profile,
                    registry);
                if (requestedCount <= 0)
                {
                    return false;
                }
            }

            return this.preflightService.TryValidateMoveIntoCold(
                thing,
                moduleDef,
                effectiveAutoTransferFilter,
                hasCustomAutoTransferFilter,
                requestedCount,
                out moveCount,
                out failureReason);
        }
    }
}
