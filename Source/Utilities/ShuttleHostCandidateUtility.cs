using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal static class ShuttleHostCandidateUtility
    {
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";

        private static ThingDef cachedModularShuttleHostDef;

        internal static ThingDef GetModularShuttleHostDef()
        {
            if (cachedModularShuttleHostDef == null)
            {
                cachedModularShuttleHostDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            }

            return cachedModularShuttleHostDef;
        }

        internal static List<Thing> GetModularShuttleHosts(Map map)
        {
            // Returned list is owned by map.listerThings; callers must not mutate it.
            ThingDef def = GetModularShuttleHostDef();
            if (map == null || map.listerThings == null || def == null)
            {
                return new List<Thing>();
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            return things ?? new List<Thing>();
        }
    }
}
