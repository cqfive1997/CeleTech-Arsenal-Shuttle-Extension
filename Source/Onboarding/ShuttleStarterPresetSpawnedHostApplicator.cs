using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Onboarding
{
    internal static class ShuttleStarterPresetSpawnedHostApplicator
    {
        internal static void ApplyToSpawnedHosts()
        {
            if (!ShuttleStarterPresetBuildPolicy.IsBasicModeActive)
            {
                return;
            }

            ThingDef hostDef = DefDatabase<ThingDef>.GetNamedSilentFail(
                ShuttleStarterPresetBuildPolicy.HostDefName);
            List<Map> maps = Find.Maps;
            for (int mapIndex = 0; hostDef != null &&
                maps != null && mapIndex < maps.Count; mapIndex++)
            {
                Map map = maps[mapIndex];
                List<Thing> hosts = map != null && map.listerThings != null
                    ? map.listerThings.ThingsOfDef(hostDef)
                    : null;
                for (int hostIndex = 0; hosts != null && hostIndex < hosts.Count; hostIndex++)
                {
                    ThingWithComps host = hosts[hostIndex] as ThingWithComps;
                    CompModularShuttleCore core =
                        host != null ? host.TryGetComp<CompModularShuttleCore>() : null;
                    if (core != null)
                    {
                        core.TryApplyStarterPresetAfterSelection();
                    }
                }
            }
        }
    }
}
