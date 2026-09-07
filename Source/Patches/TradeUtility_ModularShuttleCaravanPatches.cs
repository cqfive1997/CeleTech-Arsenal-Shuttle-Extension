using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(TradeUtility), nameof(TradeUtility.PlayerSellableNow))]
    internal static class TradeUtility_ModularShuttleCaravanPatches
    {
        private static void Postfix(Thing t, ITrader trader, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            try
            {
                if (ModularShuttleCaravanUtility.IsModularShuttleThing(t))
                {
                    __result = false;
                }
            }
            catch (System.Exception exception)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Failed to filter modular shuttle from trade sellables: " + exception,
                    930184611);
            }
        }
    }
}
