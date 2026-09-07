using System;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver))]
    internal static class GameEnder_CheckOrUpdateGameOver_ShuttleHeldColonistPatch
    {
        private static void Postfix(GameEnder __instance)
        {
            try
            {
                if (!ShuttleHeldColonistUtility.AnyLivingFreeColonistHeldInOnMapShuttle())
                {
                    return;
                }

                // Do not short-circuit CheckOrUpdateGameOver. Vanilla and other mods may update
                // game-over countdown state or run side effects there; this postfix only corrects
                // the final gameEnding flag for colonists hidden inside shuttle-owned holders.
                if (__instance.gameEnding)
                {
                    __instance.gameEnding = false;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Failed to include shuttle-held colonists in game-over check: " + ex,
                    91342191);
            }
        }
    }

    [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.GameEndTick))]
    internal static class GameEnder_GameEndTick_ShuttleHeldColonistPatch
    {
        private static void Prefix(GameEnder __instance)
        {
            try
            {
                if (!__instance.gameEnding ||
                    !ShuttleHeldColonistUtility.AnyLivingFreeColonistHeldInOnMapShuttle())
                {
                    return;
                }

                __instance.gameEnding = false;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Failed to clear game-over countdown for shuttle-held colonists: " + ex,
                    91342192);
            }
        }
    }
}
