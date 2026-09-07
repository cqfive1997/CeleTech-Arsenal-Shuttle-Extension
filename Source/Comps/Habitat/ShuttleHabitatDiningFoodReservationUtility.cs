using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class ShuttleHabitatDiningFoodReservationUtility
    {
        internal static string GetFoodDefName(ShuttleHolderLaunchManifestEntry foodEntry)
        {
            if (foodEntry == null)
            {
                return null;
            }

            return !string.IsNullOrEmpty(foodEntry.FoodDefName)
                ? foodEntry.FoodDefName
                : foodEntry.DefName;
        }

        internal static int GetFoodStackCount(ShuttleHolderLaunchManifestEntry foodEntry)
        {
            if (foodEntry == null || foodEntry.FoodStackCount <= 0)
            {
                return 1;
            }

            return foodEntry.FoodStackCount;
        }

        internal static bool MatchesFoodManifest(ShuttleHolderLaunchManifestEntry foodEntry, Thing thing)
        {
            if (foodEntry == null || thing == null || thing.def == null)
            {
                return false;
            }

            string foodDefName = GetFoodDefName(foodEntry);
            return !string.IsNullOrEmpty(foodDefName) && thing.def.defName == foodDefName;
        }

        internal static int GetReservedFoodCount(
            Dictionary<int, int> reservedFoodCountsByThingID,
            Thing thing)
        {
            if (reservedFoodCountsByThingID == null || thing == null)
            {
                return 0;
            }

            int reservedCount;
            return reservedFoodCountsByThingID.TryGetValue(thing.thingIDNumber, out reservedCount)
                ? reservedCount
                : 0;
        }

        internal static void AddReservedFoodCount(
            Dictionary<int, int> reservedFoodCountsByThingID,
            Thing thing,
            int count)
        {
            if (reservedFoodCountsByThingID == null || thing == null || count <= 0)
            {
                return;
            }

            int current;
            reservedFoodCountsByThingID.TryGetValue(thing.thingIDNumber, out current);
            reservedFoodCountsByThingID[thing.thingIDNumber] = current + count;
        }

        internal static void TryFindFoodPlanCandidateInOwner(
            ShuttleHolderLaunchManifestEntry foodEntry,
            int requiredCount,
            ThingOwner source,
            Dictionary<int, int> reservedFoodCountsByThingID,
            HashSet<int> allowedThingIDs,
            bool allowAmbiguousPreferredMatches,
            ref Thing candidate,
            ref ThingOwner candidateOwner,
            ref bool ambiguous)
        {
            if (ambiguous || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                Thing thing = source[i];
                if (thing == null ||
                    thing.Destroyed ||
                    !MatchesFoodManifest(foodEntry, thing) ||
                    (allowedThingIDs != null && !allowedThingIDs.Contains(thing.thingIDNumber)))
                {
                    continue;
                }

                int availableCount = thing.stackCount - GetReservedFoodCount(
                    reservedFoodCountsByThingID,
                    thing);
                if (availableCount < requiredCount)
                {
                    continue;
                }

                if (candidate != null && candidate != thing && !allowAmbiguousPreferredMatches)
                {
                    ambiguous = true;
                    return;
                }

                if (candidate == null)
                {
                    candidate = thing;
                    candidateOwner = source;
                }
            }
        }

        internal static bool TryFindFoodStackCandidateInOwner(
            ShuttleHolderLaunchManifestEntry foodEntry,
            ThingOwner source,
            HashSet<int> whollyAllocatedThingIDs,
            HashSet<int> allowedThingIDs,
            out Thing candidate)
        {
            candidate = null;
            if (foodEntry == null || source == null)
            {
                return false;
            }

            string foodDefName = GetFoodDefName(foodEntry);
            if (string.IsNullOrEmpty(foodDefName))
            {
                return false;
            }

            int count = GetFoodStackCount(foodEntry);
            for (int i = 0; i < source.Count; i++)
            {
                Thing thing = source[i];
                if (thing == null ||
                    thing.Destroyed ||
                    thing.def == null ||
                    thing.def.defName != foodDefName ||
                    thing.stackCount < count ||
                    (whollyAllocatedThingIDs != null && whollyAllocatedThingIDs.Contains(thing.thingIDNumber)) ||
                    (allowedThingIDs != null && !allowedThingIDs.Contains(thing.thingIDNumber)))
                {
                    continue;
                }

                candidate = thing;
                return true;
            }

            return false;
        }
    }
}
