using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleArrivalRecoveryService
    {
        internal static bool HasUnresolvedPayload(List<ActiveTransporterInfo> transporters)
        {
            if (transporters == null)
            {
                return false;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo transporter = transporters[i];
                if (transporter != null &&
                    transporter.innerContainer != null &&
                    transporter.innerContainer.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool DestinationMayNeedMapGeneration(PlanetTile tile)
        {
            MapParent mapParent = FindMapParent(tile);
            return mapParent != null && !mapParent.HasMap && mapParent.MapGeneratorDef != null;
        }

        internal static HashSet<Caravan> CapturePlayerCaravans()
        {
            HashSet<Caravan> result = new HashSet<Caravan>();
            if (Find.WorldObjects == null)
            {
                return result;
            }

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan caravan = caravans[i];
                if (caravan != null && caravan.Spawned && caravan.IsPlayerControlled)
                {
                    result.Add(caravan);
                }
            }

            return result;
        }

        internal static bool TryRecoverUnresolvedPayload(
            List<ActiveTransporterInfo> transporters,
            PlanetTile tile,
            HashSet<Caravan> caravansBeforeArrival,
            out string failureReason)
        {
            failureReason = null;
            if (!HasUnresolvedPayload(transporters))
            {
                return true;
            }

            StringBuilder attemptFailures = new StringBuilder();
            try
            {
                Caravan caravan = FindRecoveryCaravan(tile, caravansBeforeArrival);
                if (caravan != null)
                {
                    new TransportersArrivalAction_GiveToCaravan(caravan).Arrived(transporters, tile);
                    if (!HasUnresolvedPayload(transporters))
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                AppendAttemptFailure(attemptFailures, "give-to-caravan", exception);
            }

            try
            {
                Map map = FindDestinationMap(tile) ?? TryGenerateDestinationMap(tile);
                if (map != null && TryDropRemainingToMap(transporters, map) &&
                    !HasUnresolvedPayload(transporters))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                AppendAttemptFailure(attemptFailures, "drop-to-map", exception);
            }

            try
            {
                if (TransportersArrivalAction_FormCaravan.CanFormCaravanAt(transporters, tile))
                {
                    new TransportersArrivalAction_FormCaravan("MessageShuttleArrived").Arrived(
                        transporters,
                        tile);
                    if (!HasUnresolvedPayload(transporters))
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                AppendAttemptFailure(attemptFailures, "form-caravan", exception);
            }

            failureReason = DescribeUnresolvedPayload(transporters);
            if (attemptFailures.Length > 0)
            {
                failureReason += ". recovery-attempt-errors=" + attemptFailures;
            }

            return false;
        }

        private static Caravan FindRecoveryCaravan(
            PlanetTile tile,
            HashSet<Caravan> caravansBeforeArrival)
        {
            if (Find.WorldObjects == null)
            {
                return null;
            }

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan caravan = caravans[i];
                if (caravan != null &&
                    caravan.Spawned &&
                    caravan.IsPlayerControlled &&
                    (caravansBeforeArrival == null || !caravansBeforeArrival.Contains(caravan)))
                {
                    return caravan;
                }
            }

            Caravan adjacent = null;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan caravan = caravans[i];
                if (caravan == null || !caravan.Spawned || !caravan.IsPlayerControlled)
                {
                    continue;
                }

                if (caravan.Tile == tile)
                {
                    return caravan;
                }

                if (adjacent == null && Find.WorldGrid.IsNeighborOrSame(caravan.Tile, tile))
                {
                    adjacent = caravan;
                }
            }

            return adjacent;
        }

        private static void AppendAttemptFailure(
            StringBuilder builder,
            string step,
            Exception exception)
        {
            if (builder == null)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append(step ?? "unknown-step");
            builder.Append('=');
            builder.Append(exception != null ? exception.GetType().Name : "unknown-exception");
            if (exception != null && !string.IsNullOrEmpty(exception.Message))
            {
                builder.Append(": ");
                builder.Append(exception.Message);
            }
        }

        private static Map FindDestinationMap(PlanetTile tile)
        {
            if (Find.Maps == null)
            {
                return null;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map != null && map.Tile == tile)
                {
                    return map;
                }
            }

            return null;
        }

        private static Map TryGenerateDestinationMap(PlanetTile tile)
        {
            MapParent mapParent = FindMapParent(tile);
            if (mapParent == null || mapParent.MapGeneratorDef == null)
            {
                return null;
            }

            return GetOrGenerateMapUtility.GetOrGenerateMap(
                mapParent.Tile,
                ModularShuttleLandInSpecificCellArrivalAction.ResolveMapSize(mapParent),
                mapParent is SpaceMapParent ? mapParent.def : null,
                null,
                false);
        }

        private static MapParent FindMapParent(PlanetTile tile)
        {
            if (!tile.Valid || Find.WorldObjects == null)
            {
                return null;
            }

            List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                MapParent mapParent = worldObjects[i] as MapParent;
                if (mapParent != null && mapParent.Spawned && mapParent.Tile == tile)
                {
                    return mapParent;
                }
            }

            return null;
        }

        private static bool TryDropRemainingToMap(
            List<ActiveTransporterInfo> transporters,
            Map map)
        {
            if (transporters == null || map == null)
            {
                return false;
            }

            bool attempted = false;
            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo transporter = transporters[i];
                if (transporter == null ||
                    transporter.innerContainer == null ||
                    transporter.innerContainer.Count == 0)
                {
                    continue;
                }

                attempted = true;
                Thing shuttle = transporter.GetShuttle();
                if (shuttle != null && transporter.innerContainer.Contains(shuttle))
                {
                    IntVec3 preferredCell = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
                    ModularShuttleVisitSiteArrivalUtility.DropModularShuttleAtSafeCell(
                        new List<ActiveTransporterInfo> { transporter },
                        map,
                        preferredCell);
                    continue;
                }

                IntVec3 near = shuttle != null && shuttle.Spawned && shuttle.Map == map
                    ? shuttle.Position
                    : map.Center;
                transporter.innerContainer.TryDropAll(
                    near,
                    map,
                    ThingPlaceMode.Near,
                    null,
                    null,
                    true);
            }

            return attempted;
        }

        private static string DescribeUnresolvedPayload(List<ActiveTransporterInfo> transporters)
        {
            StringBuilder builder = new StringBuilder();
            if (transporters == null)
            {
                return "transporters=null";
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo transporter = transporters[i];
                if (transporter == null || transporter.innerContainer == null)
                {
                    continue;
                }

                for (int j = 0; j < transporter.innerContainer.Count; j++)
                {
                    Thing thing = transporter.innerContainer[j];
                    if (builder.Length > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append("transporter[");
                    builder.Append(i);
                    builder.Append("] -> ");
                    builder.Append(thing != null ? thing.ToString() : "null");
                }
            }

            return builder.Length > 0 ? builder.ToString() : "no unresolved payload details";
        }
    }
}
