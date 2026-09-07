using System;
using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(BuildableDef), "get_CostList")]
    internal static class BuildableDef_ShuttleStarterPresetCostListPatch
    {
        private static void Postfix(
            BuildableDef __instance,
            ref List<ThingDefCountClass> __result)
        {
            try
            {
                if (ShuttleStarterPresetBuildPolicy.AppliesTo(__instance))
                {
                    __result = ShuttleStarterPresetBuildPolicy
                        .BuildEffectiveCostList(__result);
                }
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Starter preset cost adaptation failed: " + exception,
                    0x43A17F11);
            }
        }
    }

    [HarmonyPatch(typeof(BuildableDef), "get_IsResearchFinished")]
    internal static class BuildableDef_ShuttleStarterPresetResearchPatch
    {
        private static void Postfix(BuildableDef __instance, ref bool __result)
        {
            try
            {
                if (__result && ShuttleStarterPresetBuildPolicy.AppliesTo(__instance))
                {
                    __result = ShuttleStarterPresetBuildPolicy
                        .ArePackageResearchPrerequisitesFinished();
                }
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Starter preset research adaptation failed: " + exception,
                    0x43A17F12);
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    internal static class StatExtension_ShuttleStarterPresetWorkPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(StatExtension),
                nameof(StatExtension.GetStatValueAbstract),
                new Type[]
                {
                    typeof(BuildableDef),
                    typeof(StatDef),
                    typeof(ThingDef)
                });
        }

        private static void Postfix(
            BuildableDef def,
            StatDef stat,
            ref float __result)
        {
            try
            {
                if (stat == StatDefOf.WorkToBuild &&
                    ShuttleStarterPresetBuildPolicy.IsHost(def))
                {
                    __result = ShuttleStarterPresetBuildPolicy
                        .BuildEffectiveWorkAmount(__result);
                }
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Starter preset work adaptation failed: " + exception,
                    0x43A17F13);
            }
        }
    }
}
