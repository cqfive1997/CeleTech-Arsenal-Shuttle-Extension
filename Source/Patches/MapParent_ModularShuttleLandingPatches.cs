using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.GetShuttleFloatMenuOptions))]
    internal static class MapParent_ModularShuttleGetShuttleFloatMenuOptionsPatch
    {
        private static bool Prefix(
            MapParent __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ref IEnumerable<FloatMenuOption> __result)
        {
            ThingWithComps shuttle;
            if (!ModularShuttleExistingMapLandingTargeter.TryResolveModularShuttle(pods, out shuttle))
            {
                return true;
            }

            __result = ModularShuttleExistingMapLandingTargeter.GetFloatMenuOptions(
                __instance,
                pods,
                launchAction,
                shuttle);
            return false;
        }
    }

    [HarmonyPatch(typeof(SpaceMapParent), nameof(SpaceMapParent.GetShuttleFloatMenuOptions))]
    internal static class SpaceMapParent_ModularShuttleGetShuttleFloatMenuOptionsPatch
    {
        private static bool Prefix(
            SpaceMapParent __instance,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ref IEnumerable<FloatMenuOption> __result)
        {
            ThingWithComps shuttle;
            if (!ModularShuttleExistingMapLandingTargeter.TryResolveModularShuttle(pods, out shuttle))
            {
                return true;
            }

            __result = ModularShuttleExistingMapLandingTargeter.GetFloatMenuOptions(
                __instance,
                pods,
                launchAction,
                shuttle);
            return false;
        }
    }
}
