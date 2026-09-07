using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using HarmonyLib;
using RimWorld.Planet;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch]
    internal static class MapParent_ModularShuttleLandingMapHoldShouldRemoveMapNowPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MapParent), nameof(MapParent.ShouldRemoveMapNow));
            yield return AccessTools.Method(typeof(Site), nameof(Site.ShouldRemoveMapNow));
            yield return AccessTools.Method(typeof(Settlement), nameof(Settlement.ShouldRemoveMapNow));
            yield return AccessTools.Method(typeof(SpaceMapParent), nameof(SpaceMapParent.ShouldRemoveMapNow));
            yield return AccessTools.Method(typeof(ModularShuttleTemporaryLandingSite), nameof(ModularShuttleTemporaryLandingSite.ShouldRemoveMapNow));
        }

        private static bool Prefix(
            MapParent __instance,
            ref bool alsoRemoveWorldObject,
            ref bool __result)
        {
            if (ModularShuttleLandingMapHoldService.ShouldPreventMapRemoval(__instance))
            {
                alsoRemoveWorldObject = false;
                __result = false;
                return false;
            }

            bool removeWorldObject;
            if (ModularShuttleLandingMapHoldService.ShouldCloseGeneratedSelectionMap(__instance, out removeWorldObject))
            {
                alsoRemoveWorldObject = removeWorldObject;
                __result = true;
                return false;
            }

            return true;
        }
    }
}
