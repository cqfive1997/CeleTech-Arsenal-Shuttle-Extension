using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatLaunchTransferManifestHelper
    {
        internal static string BuildHabitatActivityID(
            string prefix,
            Thing primary,
            Thing secondary,
            int exportTick,
            int recordIndex)
        {
            int primaryID = primary != null ? primary.thingIDNumber : -1;
            int secondaryID = secondary != null ? secondary.thingIDNumber : -1;
            return "M6.2A-" + prefix + "-" + exportTick + "-" + recordIndex + "-" + primaryID + "-" + secondaryID;
        }

        internal static string GetThingDefName(Thing thing)
        {
            return thing != null && thing.def != null ? thing.def.defName : string.Empty;
        }

        internal static string GetThingLabel(Thing thing)
        {
            return thing != null ? thing.LabelShortCap : string.Empty;
        }

        internal static int GetDiningFoodStackCount(ShuttleHolderLaunchManifestEntry foodEntry)
        {
            return ShuttleHabitatDiningFoodReservationUtility.GetFoodStackCount(foodEntry);
        }

        internal static ShuttleHolderLaunchManifestEntry FindDiningFoodEntryForActivity(
            List<ShuttleHolderLaunchManifestEntry> foodEntries,
            string activityID)
        {
            if (foodEntries == null || activityID.NullOrEmpty())
            {
                return null;
            }

            for (int i = 0; i < foodEntries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = foodEntries[i];
                if (entry != null && entry.ActivityID == activityID)
                {
                    return entry;
                }
            }

            return null;
        }

        internal static CompShuttleHolderLaunchTransferState GetHolderTransferState(Thing parent)
        {
            ThingWithComps host = parent as ThingWithComps;
            return host != null
                ? host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }
    }
}
