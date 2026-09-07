using System;
using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Trade;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(
        typeof(Settlement_TraderTracker),
        nameof(Settlement_TraderTracker.ColonyThingsWillingToBuy))]
    internal static class SettlementTraderRefrigeratedCargoPatch
    {
        private static void Postfix(
            Pawn playerNegotiator,
            ref IEnumerable<Thing> __result)
        {
            __result = ShuttleRefrigeratedCargoTradeBridge
                .AppendCaravanRefrigeratedCargo(__result, playerNegotiator);
        }
    }

    [HarmonyPatch(
        typeof(Caravan_TraderTracker),
        nameof(Caravan_TraderTracker.ColonyThingsWillingToBuy))]
    internal static class CaravanTraderRefrigeratedCargoPatch
    {
        private static void Postfix(
            Pawn playerNegotiator,
            ref IEnumerable<Thing> __result)
        {
            __result = ShuttleRefrigeratedCargoTradeBridge
                .AppendCaravanRefrigeratedCargo(__result, playerNegotiator);
        }
    }

    [HarmonyPatch(
        typeof(Pawn_TraderTracker),
        nameof(Pawn_TraderTracker.ColonyThingsWillingToBuy))]
    internal static class PawnTraderRefrigeratedCargoPatch
    {
        private static void Postfix(
            Pawn ___pawn,
            ref IEnumerable<Thing> __result)
        {
            __result = ShuttleRefrigeratedCargoTradeBridge
                .AppendReachableMapRefrigeratedCargo(__result, ___pawn);
        }
    }

    [HarmonyPatch(
        typeof(TradeShip),
        nameof(TradeShip.ColonyThingsWillingToBuy))]
    internal static class TradeShipRefrigeratedCargoPatch
    {
        private static void Postfix(
            TradeShip __instance,
            ref IEnumerable<Thing> __result)
        {
            __result = ShuttleRefrigeratedCargoTradeBridge
                .AppendOrbitalRefrigeratedCargo(
                    __result,
                    __instance != null ? __instance.Map : null);
        }
    }

    [HarmonyPatch(typeof(TradeDeal), "InSellablePosition")]
    internal static class TradeDealRefrigeratedCargoPositionPatch
    {
        private static bool Prefix(
            Thing t,
            ref string reason,
            ref bool __result)
        {
            if (!ShuttleRefrigeratedCargoTradeBridge
                .IsInSellablePositionForCurrentMapTrade(t))
            {
                return true;
            }

            reason = null;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class RefrigeratedCargoCompletedSalePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] parameters =
            {
                typeof(Thing),
                typeof(int),
                typeof(Pawn)
            };

            yield return AccessTools.Method(
                typeof(Settlement_TraderTracker),
                nameof(Settlement_TraderTracker.GiveSoldThingToTrader),
                parameters);
            yield return AccessTools.Method(
                typeof(Caravan_TraderTracker),
                nameof(Caravan_TraderTracker.GiveSoldThingToTrader),
                parameters);
            yield return AccessTools.Method(
                typeof(Pawn_TraderTracker),
                nameof(Pawn_TraderTracker.GiveSoldThingToTrader),
                parameters);
            yield return AccessTools.Method(
                typeof(TradeShip),
                nameof(TradeShip.GiveSoldThingToTrader),
                parameters);
        }

        private static void Prefix(
            Thing toGive,
            out ThingWithComps __state)
        {
            ShuttleRefrigeratedCargoTradeBridge.TryResolveRefrigeratedCargoHost(
                toGive,
                out __state);
        }

        private static void Postfix(ThingWithComps __state)
        {
            if (__state != null)
            {
                ShuttleRefrigeratedCargoTradeBridge.NotifyRefrigeratedCargoSold(
                    __state);
            }
        }
    }
}
