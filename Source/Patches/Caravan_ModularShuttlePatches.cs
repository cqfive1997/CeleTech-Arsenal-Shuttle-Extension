using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
    internal static class Caravan_ModularShuttleGetGizmosPatch
    {
        private static void Postfix(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendLaunchGizmo(__result, __instance);
        }

        private static IEnumerable<Gizmo> AppendLaunchGizmo(IEnumerable<Gizmo> source, Caravan caravan)
        {
            if (source != null)
            {
                foreach (Gizmo gizmo in source)
                {
                    yield return gizmo;
                }
            }

            if (caravan == null ||
                !caravan.IsPlayerControlled ||
                Find.WorldSelector == null ||
                Find.WorldSelector.NumSelectedObjects != 1)
            {
                yield break;
            }

            ThingWithComps shuttle;
            if (!ModularShuttleCaravanUtility.TryFindHeldModularShuttleCached(caravan, out shuttle))
            {
                yield break;
            }

            yield return ModularShuttleCaravanLaunchService.CreateLaunchGizmo(caravan, shuttle);
        }
    }

    [HarmonyPatch(typeof(Caravan), "get_CantMove")]
    internal static class Caravan_ModularShuttleCantMovePatch
    {
        private static void Postfix(Caravan __instance, ref bool __result)
        {
            if (__result || __instance == null || !__instance.IsPlayerControlled)
            {
                return;
            }

            ThingWithComps shuttle;
            if (ModularShuttleCaravanUtility.TryFindHeldModularShuttleCached(__instance, out shuttle))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(
        typeof(TransportersArrivalAction_FormCaravan),
        nameof(TransportersArrivalAction_FormCaravan.Arrived))]
    internal static class TransportersArrivalAction_FormCaravan_ModularShuttleCachePatch
    {
        private static void Postfix()
        {
            ModularShuttleCaravanUtility.ClearCachedHeldModularShuttle();
        }
    }
}
