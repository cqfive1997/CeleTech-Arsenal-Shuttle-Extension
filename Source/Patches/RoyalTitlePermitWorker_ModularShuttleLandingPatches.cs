using CeleTech.ShuttleExtension.ModularShuttle.World;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(RoyalTitlePermitWorker_CallShuttle), nameof(RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere))]
    internal static class RoyalTitlePermitWorker_ModularShuttleCanLandHerePatch
    {
        private static void Postfix(
            LocalTargetInfo target,
            Map map,
            ThingDef shuttleDef,
            Rot4? rot,
            ref AcceptanceReport __result)
        {
            if (__result.Accepted ||
                !ModularShuttleVisitSiteArrivalUtility.IsModularShuttleDef(shuttleDef))
            {
                return;
            }

            Rot4 rotation = rot ?? shuttleDef.defaultPlacingRot;
            if (ModularShuttleLandingCompatibilityPolicy.CanLandWithCompatibleOverlays(
                target,
                map,
                shuttleDef,
                rotation))
            {
                __result = AcceptanceReport.WasAccepted;
            }
        }
    }
}
