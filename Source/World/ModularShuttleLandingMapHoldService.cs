using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleLandingMapHoldService
    {
        private const int TransientHoldTicks = 10000;

        private static readonly Dictionary<MapParent, int> transientHolds =
            new Dictionary<MapParent, int>();

        private static readonly HashSet<MapParent> generatedSelectionMaps = new HashSet<MapParent>();

        private static readonly List<MapParent> staleHolds = new List<MapParent>();

        internal static void RegisterGeneratedSelectionMap(MapParent mapParent)
        {
            if (mapParent == null || mapParent.Destroyed)
            {
                return;
            }

            generatedSelectionMaps.Add(mapParent);
            RefreshTransientHold(mapParent);
        }

        internal static void RefreshTransientHold(MapParent mapParent)
        {
            if (mapParent == null || mapParent.Destroyed)
            {
                return;
            }

            PurgeStaleTransientHolds();
            transientHolds[mapParent] = CurrentTick + TransientHoldTicks;
        }

        internal static bool ShouldPreventMapRemoval(MapParent mapParent)
        {
            if (mapParent == null || mapParent.Destroyed || !mapParent.HasMap)
            {
                return false;
            }

            PurgeStaleTransientHolds();

            if (ContainsSpawnedModularShuttle(mapParent.Map))
            {
                return true;
            }

            int expiryTick;
            if (transientHolds.TryGetValue(mapParent, out expiryTick) && expiryTick >= CurrentTick)
            {
                return true;
            }

            return HasInFlightSpecificCellArrival(mapParent);
        }

        internal static bool ContainsSpawnedModularShuttle(Map map)
        {
            if (map == null || map.listerThings == null)
            {
                return false;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null &&
                    !thing.Destroyed &&
                    thing.Spawned &&
                    ModularShuttleVisitSiteArrivalUtility.IsModularShuttle(thing))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool ShouldCloseGeneratedSelectionMap(
            MapParent mapParent,
            out bool alsoRemoveWorldObject)
        {
            alsoRemoveWorldObject = false;
            if (mapParent == null || mapParent.Destroyed || !mapParent.HasMap)
            {
                return false;
            }

            PurgeStaleTransientHolds();

            if (!generatedSelectionMaps.Contains(mapParent) ||
                HasActiveTransientHold(mapParent) ||
                HasInFlightSpecificCellArrival(mapParent))
            {
                return false;
            }

            generatedSelectionMaps.Remove(mapParent);
            transientHolds.Remove(mapParent);
            alsoRemoveWorldObject = mapParent is ModularShuttleTemporaryLandingSite;
            return true;
        }

        internal static void NotifyLandingCompleted(MapParent mapParent)
        {
            if (mapParent == null)
            {
                return;
            }

            generatedSelectionMaps.Remove(mapParent);
            transientHolds.Remove(mapParent);
        }

        private static bool HasActiveTransientHold(MapParent mapParent)
        {
            int expiryTick;
            return mapParent != null &&
                transientHolds.TryGetValue(mapParent, out expiryTick) &&
                expiryTick >= CurrentTick;
        }

        private static bool HasInFlightSpecificCellArrival(MapParent mapParent)
        {
            if (mapParent == null || Find.WorldObjects == null)
            {
                return false;
            }

            List<TravellingTransporters> travellingTransporters = Find.WorldObjects.TravellingTransporters;
            for (int i = 0; i < travellingTransporters.Count; i++)
            {
                TravellingTransporters travelling = travellingTransporters[i];
                if (travelling == null ||
                    travelling.Destroyed ||
                    travelling.destinationTile != mapParent.Tile)
                {
                    continue;
                }

                ModularShuttleLandInSpecificCellArrivalAction landingAction =
                    travelling.arrivalAction as ModularShuttleLandInSpecificCellArrivalAction;
                if (landingAction != null && landingAction.TargetsMapParent(mapParent))
                {
                    return true;
                }

                ModularShuttleSafeArrivalAction safeArrivalAction =
                    travelling.arrivalAction as ModularShuttleSafeArrivalAction;
                if (safeArrivalAction != null &&
                    safeArrivalAction.TargetsSpecificCellMapParent(mapParent))
                {
                    return true;
                }
            }

            return false;
        }

        private static void PurgeStaleTransientHolds()
        {
            if (transientHolds.Count == 0)
            {
                generatedSelectionMaps.RemoveWhere(mapParent => mapParent == null || mapParent.Destroyed);
                return;
            }

            staleHolds.Clear();
            int currentTick = CurrentTick;
            foreach (KeyValuePair<MapParent, int> hold in transientHolds)
            {
                if (hold.Key == null || hold.Key.Destroyed || hold.Value < currentTick)
                {
                    staleHolds.Add(hold.Key);
                }
            }

            for (int i = 0; i < staleHolds.Count; i++)
            {
                transientHolds.Remove(staleHolds[i]);
            }

            generatedSelectionMaps.RemoveWhere(mapParent => mapParent == null || mapParent.Destroyed);
            staleHolds.Clear();
        }

        private static int CurrentTick
        {
            get
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            }
        }
    }
}
