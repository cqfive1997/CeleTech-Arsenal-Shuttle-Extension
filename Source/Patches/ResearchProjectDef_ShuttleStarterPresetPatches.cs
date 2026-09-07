using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using HarmonyLib;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(ResearchProjectDef), "get_LabelCap")]
    internal static class ResearchProjectDef_ShuttleStarterPresetLabelPatch
    {
        private static void Postfix(
            ResearchProjectDef __instance,
            ref TaggedString __result)
        {
            TaggedString label;
            if (ShuttleStarterResearchTreePolicy.TryGetBasicLabel(
                __instance,
                out label))
            {
                __result = label;
            }
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "get_Description")]
    internal static class ResearchProjectDef_ShuttleStarterPresetDescriptionPatch
    {
        private static void Postfix(
            ResearchProjectDef __instance,
            ref string __result)
        {
            string description;
            if (ShuttleStarterResearchTreePolicy.TryGetBasicDescription(
                __instance,
                out description))
            {
                __result = description;
            }
        }
    }
}
