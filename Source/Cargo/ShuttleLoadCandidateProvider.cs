using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Read-only candidate discovery. Vanilla remains authoritative for ordinary
    /// candidates; this provider only augments known compatibility gaps.
    /// </summary>
    internal sealed class ShuttleLoadCandidateProvider
    {
        internal List<Pawn> GetPassengerCandidates(
            List<CompTransporter> transporters,
            Map map)
        {
            List<Pawn> candidates = new List<Pawn>();
            HashSet<int> seenThingIDs = new HashSet<int>();
            if (transporters == null || transporters.Count == 0 || map == null)
            {
                return candidates;
            }

            foreach (Pawn pawn in TransporterUtility.AllSendablePawns(transporters, map))
            {
                AddUniquePawn(pawn, candidates, seenThingIDs);
            }

            IReadOnlyList<Pawn> spawnedPawns = map.mapPawns != null
                ? map.mapPawns.AllPawnsSpawned
                : null;
            if (spawnedPawns == null)
            {
                return candidates;
            }

            CompShuttle shuttle = transporters[0].parent.TryGetComp<CompShuttle>();
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn pawn = spawnedPawns[i];
                if (!IsCompatibleColonyPrisoner(pawn, map) ||
                    (shuttle != null && !shuttle.IsRequired(pawn) && !shuttle.IsAllowed(pawn)) ||
                    !CanAnyLoaderReach(pawn, transporters[0].parent, map))
                {
                    continue;
                }

                AddUniquePawn(pawn, candidates, seenThingIDs);
            }

            return candidates;
        }

        internal List<Thing> GetCargoCandidates(
            List<CompTransporter> transporters,
            Map map)
        {
            List<Thing> candidates = new List<Thing>();
            HashSet<int> seenThingIDs = new HashSet<int>();
            if (transporters == null || transporters.Count == 0 || map == null)
            {
                return candidates;
            }

            foreach (Thing thing in TransporterUtility.AllSendableItems(transporters, map))
            {
                AddUniqueThing(thing, candidates, seenThingIDs);
            }

            List<Thing> corpses = map.listerThings != null
                ? map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                : null;
            if (corpses == null)
            {
                return candidates;
            }

            CompShuttle shuttle = transporters[0].parent.TryGetComp<CompShuttle>();
            for (int i = 0; i < corpses.Count; i++)
            {
                Thing corpse = corpses[i];
                if (!IsEligibleCorpse(corpse, map) ||
                    (shuttle != null && !shuttle.IsRequired(corpse) && !shuttle.IsAllowed(corpse)) ||
                    !CanAnyLoaderReach(corpse, transporters[0].parent, map))
                {
                    continue;
                }

                AddUniqueThing(corpse, candidates, seenThingIDs);
            }

            return candidates;
        }

        private static bool IsCompatibleColonyPrisoner(Pawn pawn, Map map)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.Spawned &&
                pawn.Map == map &&
                pawn.IsPrisonerOfColony &&
                !pawn.InMentalState &&
                pawn.RaceProps != null &&
                pawn.RaceProps.allowedOnCaravan &&
                !pawn.IsQuestHelper() &&
                !pawn.IsQuestLodger();
        }

        private static bool IsEligibleCorpse(Thing thing, Map map)
        {
            Corpse corpse = thing as Corpse;
            return corpse != null &&
                !corpse.Destroyed &&
                corpse.Spawned &&
                corpse.Map == map &&
                !corpse.IsForbidden(Faction.OfPlayer);
        }

        private static bool CanAnyLoaderReach(Thing target, Thing destination, Map map)
        {
            if (target == null || destination == null || map == null || map.mapPawns == null)
            {
                return false;
            }

            List<Pawn> loaders = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < loaders.Count; i++)
            {
                Pawn loader = loaders[i];
                if (loader != null &&
                    !loader.Destroyed &&
                    !loader.Dead &&
                    !loader.Downed &&
                    loader.CanReach(
                        target,
                        PathEndMode.ClosestTouch,
                        Danger.Deadly,
                        false,
                        false,
                        TraverseMode.ByPawn) &&
                    loader.CanReach(
                        destination,
                        PathEndMode.Touch,
                        Danger.Deadly,
                        false,
                        false,
                        TraverseMode.ByPawn))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUniquePawn(
            Pawn pawn,
            List<Pawn> candidates,
            HashSet<int> seenThingIDs)
        {
            if (pawn == null || candidates == null || seenThingIDs == null)
            {
                return;
            }

            if (seenThingIDs.Add(pawn.thingIDNumber))
            {
                candidates.Add(pawn);
            }
        }

        private static void AddUniqueThing(
            Thing thing,
            List<Thing> candidates,
            HashSet<int> seenThingIDs)
        {
            if (thing == null || candidates == null || seenThingIDs == null)
            {
                return;
            }

            if (seenThingIDs.Add(thing.thingIDNumber))
            {
                candidates.Add(thing);
            }
        }
    }
}
