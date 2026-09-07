using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Trade
{
    internal static class ShuttleRefrigeratedCargoTradeBridge
    {
        internal static IEnumerable<Thing> AppendCaravanRefrigeratedCargo(
            IEnumerable<Thing> original,
            Pawn playerNegotiator)
        {
            List<Thing> result = CopyDistinct(original);
            HashSet<Thing> seen = new HashSet<Thing>(result);
            Caravan caravan = playerNegotiator != null
                ? playerNegotiator.GetCaravan()
                : null;
            if (caravan == null || caravan.PawnsListForReading == null)
            {
                return result;
            }

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
            {
                Pawn pawn = pawns[pawnIndex];
                ThingOwner<Thing> inventory =
                    pawn != null && pawn.inventory != null
                        ? pawn.inventory.innerContainer
                        : null;
                if (inventory == null)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < inventory.Count; itemIndex++)
                {
                    Thing candidate = inventory[itemIndex];
                    if (!ModularShuttleCaravanUtility.IsModularShuttleThing(candidate))
                    {
                        continue;
                    }

                    ThingWithComps shuttle = candidate.GetInnerIfMinified() as ThingWithComps;
                    AddRefrigeratedCargo(shuttle, result, seen);
                }
            }

            return result;
        }

        internal static IEnumerable<Thing> AppendReachableMapRefrigeratedCargo(
            IEnumerable<Thing> original,
            Pawn traderPawn)
        {
            List<Thing> result = CopyDistinct(original);
            HashSet<Thing> seen = new HashSet<Thing>(result);
            Map map = traderPawn != null ? traderPawn.Map : null;
            List<Thing> shuttles = ShuttleHostCandidateUtility.GetModularShuttleHosts(map);
            for (int i = 0; i < shuttles.Count; i++)
            {
                ThingWithComps shuttle = shuttles[i] as ThingWithComps;
                if (!IsPlayerShuttleOnMap(shuttle, map) ||
                    shuttle.Position.Fogged(map) ||
                    !CanTraderReachShuttle(traderPawn, shuttle))
                {
                    continue;
                }

                AddRefrigeratedCargo(shuttle, result, seen);
            }

            return result;
        }

        internal static IEnumerable<Thing> AppendOrbitalRefrigeratedCargo(
            IEnumerable<Thing> original,
            Map map)
        {
            List<Thing> result = CopyDistinct(original);
            HashSet<Thing> seen = new HashSet<Thing>(result);
            HashSet<IntVec3> tradeableCells = BuildPoweredTradeableCells(map);
            if (tradeableCells.Count == 0)
            {
                return result;
            }

            List<Thing> shuttles = ShuttleHostCandidateUtility.GetModularShuttleHosts(map);
            for (int i = 0; i < shuttles.Count; i++)
            {
                ThingWithComps shuttle = shuttles[i] as ThingWithComps;
                if (!IsPlayerShuttleOnMap(shuttle, map) ||
                    !OccupiedRectOverlaps(shuttle, tradeableCells))
                {
                    continue;
                }

                AddRefrigeratedCargo(shuttle, result, seen);
            }

            return result;
        }

        internal static bool TryResolveRefrigeratedCargoHost(
            Thing thing,
            out ThingWithComps shuttle)
        {
            shuttle = null;
            RefrigeratedCargoRecord record = thing != null
                ? thing.ParentHolder as RefrigeratedCargoRecord
                : null;
            CompShuttleRefrigeratedCargoRegistry registry =
                record != null
                    ? record.ParentHolder as CompShuttleRefrigeratedCargoRegistry
                    : null;
            shuttle = registry != null ? registry.parent : null;
            return shuttle != null &&
                ModularShuttleCaravanUtility.IsModularShuttleThing(shuttle);
        }

        internal static bool IsInSellablePositionForCurrentMapTrade(Thing thing)
        {
            ThingWithComps shuttle;
            if (!TryResolveRefrigeratedCargoHost(thing, out shuttle))
            {
                return false;
            }

            Pawn traderPawn = TradeSession.trader as Pawn;
            if (traderPawn != null)
            {
                Map map = traderPawn.Map;
                return IsPlayerShuttleOnMap(shuttle, map) &&
                    !shuttle.Position.Fogged(map) &&
                    CanTraderReachShuttle(traderPawn, shuttle);
            }

            TradeShip tradeShip = TradeSession.trader as TradeShip;
            if (tradeShip == null)
            {
                return false;
            }

            Map tradeMap = tradeShip.Map;
            return IsPlayerShuttleOnMap(shuttle, tradeMap) &&
                OccupiedRectOverlaps(
                    shuttle,
                    BuildPoweredTradeableCells(tradeMap));
        }

        internal static void NotifyRefrigeratedCargoSold(ThingWithComps shuttle)
        {
            CompModularShuttleCore core =
                shuttle != null ? shuttle.TryGetComp<CompModularShuttleCore>() : null;
            if (core != null)
            {
                core.NotifyRefrigeratedCargoTradeChanged();
            }
        }

        private static List<Thing> CopyDistinct(IEnumerable<Thing> original)
        {
            List<Thing> result = new List<Thing>();
            if (original == null)
            {
                return result;
            }

            HashSet<Thing> seen = new HashSet<Thing>();
            foreach (Thing thing in original)
            {
                if (thing != null && seen.Add(thing))
                {
                    result.Add(thing);
                }
            }

            return result;
        }

        private static void AddRefrigeratedCargo(
            ThingWithComps shuttle,
            List<Thing> result,
            HashSet<Thing> seen)
        {
            CompShuttleRefrigeratedCargoRegistry registry =
                shuttle != null
                    ? shuttle.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                    : null;
            IReadOnlyList<RefrigeratedCargoRecord> records =
                registry != null ? registry.Records : null;
            if (records == null)
            {
                return;
            }

            for (int recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                RefrigeratedCargoRecord record = records[recordIndex];
                ThingOwner contents =
                    record != null ? record.GetDirectlyHeldThings() : null;
                if (contents == null)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < contents.Count; itemIndex++)
                {
                    Thing thing = contents[itemIndex];
                    if (thing == null ||
                        thing.Destroyed ||
                        thing.stackCount <= 0 ||
                        !seen.Add(thing))
                    {
                        continue;
                    }

                    result.Add(thing);
                }
            }
        }

        private static bool IsPlayerShuttleOnMap(ThingWithComps shuttle, Map map)
        {
            return shuttle != null &&
                !shuttle.Destroyed &&
                shuttle.Spawned &&
                shuttle.Map == map &&
                shuttle.Faction == Faction.OfPlayer;
        }

        private static bool CanTraderReachShuttle(Pawn traderPawn, ThingWithComps shuttle)
        {
            Map map = traderPawn != null ? traderPawn.Map : null;
            return map != null &&
                shuttle != null &&
                shuttle.MapHeld == map &&
                map.reachability.CanReach(
                    traderPawn.Position,
                    shuttle,
                    PathEndMode.Touch,
                    TraverseMode.PassDoors,
                    Danger.Some);
        }

        private static HashSet<IntVec3> BuildPoweredTradeableCells(Map map)
        {
            HashSet<IntVec3> cells = new HashSet<IntVec3>();
            if (map == null)
            {
                return cells;
            }

            foreach (Building_OrbitalTradeBeacon beacon in
                Building_OrbitalTradeBeacon.AllPowered(map))
            {
                if (beacon == null)
                {
                    continue;
                }

                foreach (IntVec3 cell in beacon.TradeableCells)
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        private static bool OccupiedRectOverlaps(
            ThingWithComps shuttle,
            HashSet<IntVec3> tradeableCells)
        {
            if (shuttle == null || tradeableCells == null || tradeableCells.Count == 0)
            {
                return false;
            }

            foreach (IntVec3 cell in shuttle.OccupiedRect())
            {
                if (tradeableCells.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
