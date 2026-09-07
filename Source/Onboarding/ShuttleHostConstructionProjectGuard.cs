using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Onboarding
{
    internal static class ShuttleHostConstructionProjectGuard
    {
        internal static bool HasPendingHostConstruction()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; maps != null && i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map.listerThings == null)
                {
                    continue;
                }

                if (ContainsHostProject(
                        map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint)) ||
                    ContainsHostProject(
                        map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsHostProject(List<Thing> things)
        {
            for (int i = 0; things != null && i < things.Count; i++)
            {
                Thing thing = things[i];
                BuildableDef target = thing != null && thing.def != null
                    ? thing.def.entityDefToBuild
                    : null;
                if (target != null &&
                    target.defName == ShuttleStarterPresetBuildPolicy.HostDefName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
