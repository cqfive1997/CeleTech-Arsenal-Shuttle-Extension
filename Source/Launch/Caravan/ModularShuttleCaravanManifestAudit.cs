using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ModularShuttleCaravanManifestAudit
    {
        internal static bool TryValidateTransitManifestReachability(
            Caravan caravan,
            ThingWithComps shuttle,
            out string failureReason)
        {
            failureReason = null;
            CompShuttleHolderLaunchTransferState state = shuttle != null
                ? shuttle.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            if (state == null || !state.HasActiveManifest)
            {
                return true;
            }

            HashSet<int> reachableThingIDs = BuildReachableThingIDs(caravan, shuttle);
            ShuttleHolderLaunchManifest manifest = state.Manifest;
            if (manifest == null || manifest.Entries == null)
            {
                failureReason = "holder transfer manifest is unavailable";
                return false;
            }

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = manifest.Entries[i];
                if (entry == null)
                {
                    failureReason = "null holder transfer manifest entry at index=" + i;
                    return false;
                }

                if (entry.ThingID > 0 && !reachableThingIDs.Contains(entry.ThingID))
                {
                    failureReason = "holder transfer manifest thing is not reachable from caravan or shuttle. " +
                        entry.DumpForDebug();
                    return false;
                }

                if (entry.FoodThingID > 0 &&
                    entry.FoodThingID != entry.ThingID &&
                    !reachableThingIDs.Contains(entry.FoodThingID))
                {
                    failureReason = "holder transfer manifest food thing is not reachable from caravan or shuttle. " +
                        entry.DumpForDebug();
                    return false;
                }
            }

            return true;
        }

        private static HashSet<int> BuildReachableThingIDs(Caravan caravan, ThingWithComps shuttle)
        {
            HashSet<int> result = new HashSet<int>();
            AddRecursiveThings(caravan, result);
            AddShuttleTransferStateThings(shuttle, result);
            AddThing(shuttle, result);

            if (caravan != null && caravan.PawnsListForReading != null)
            {
                List<Pawn> pawns = caravan.PawnsListForReading;
                for (int i = 0; i < pawns.Count; i++)
                {
                    AddThing(pawns[i], result);
                }
            }

            return result;
        }

        private static void AddShuttleTransferStateThings(ThingWithComps shuttle, HashSet<int> result)
        {
            CompShuttleHolderLaunchTransferState state = shuttle != null
                ? shuttle.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            if (state == null)
            {
                return;
            }

            AddRecursiveThings(state, result);
        }

        private static void AddRecursiveThings(IThingHolder holder, HashSet<int> result)
        {
            if (holder == null || result == null)
            {
                return;
            }

            List<Thing> things = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(holder, things, true, null);
            for (int i = 0; i < things.Count; i++)
            {
                AddThing(things[i], result);
            }
        }

        private static void AddThing(Thing thing, HashSet<int> result)
        {
            if (thing == null || result == null || thing.thingIDNumber <= 0)
            {
                return;
            }

            result.Add(thing.thingIDNumber);
        }
    }
}
