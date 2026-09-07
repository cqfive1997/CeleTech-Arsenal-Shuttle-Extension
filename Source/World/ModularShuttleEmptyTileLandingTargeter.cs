using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleEmptyTileLandingTargeter
    {
        private const string TemporaryLandingSiteDefName = "CT_ModularShuttleTemporaryLandingSite";

        internal static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
            PlanetTile tile,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ThingWithComps shuttle)
        {
            if (launchAction == null || shuttle == null)
            {
                yield break;
            }

            string failureReason;
            if (!CanOpenEmptyTileLandingMap(tile, out failureReason))
            {
                yield break;
            }

            yield return new FloatMenuOption(
                "CT_Shuttle_Launch_OpenEmptyTileLandingMap".Translate(),
                delegate
                {
                    OpenEmptyTileLandingMap(tile, launchAction, shuttle);
                },
                MenuOptionPriority.Default,
                null,
                null,
                0f,
                null,
                null,
                true,
                0);
        }

        private static bool CanOpenEmptyTileLandingMap(PlanetTile tile, out string failureReason)
        {
            failureReason = null;
            if (!tile.Valid || Find.World.Impassable(tile))
            {
                failureReason = "CT_Shuttle_Launch_Failed_DestinationNotLaunchable".Translate().ToString();
                return false;
            }

            if (Current.Game == null ||
                Find.WorldObjects == null ||
                Find.WorldObjects.AnyWorldObjectAt(tile) ||
                Find.WorldObjects.AnyMapParentAt(tile) ||
                Current.Game.FindMap(tile) != null ||
                !SettleInEmptyTileUtility.CanCreateMapAt(tile, false))
            {
                failureReason = "CT_Shuttle_Launch_Failed_EmptyTileLandingMapUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private static void OpenEmptyTileLandingMap(
            PlanetTile tile,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ThingWithComps shuttle)
        {
            string failureReason;
            if (!CanOpenEmptyTileLandingMap(tile, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    Messages.Message(failureReason, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            WorldObjectDef landingSiteDef =
                DefDatabase<WorldObjectDef>.GetNamedSilentFail(TemporaryLandingSiteDefName);
            if (landingSiteDef == null)
            {
                Messages.Message(
                    "CT_Shuttle_Launch_Failed_EmptyTileLandingSiteDefUnavailable".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            ModularShuttleTemporaryLandingSite landingSite =
                WorldObjectMaker.MakeWorldObject(landingSiteDef) as ModularShuttleTemporaryLandingSite;
            if (landingSite == null)
            {
                Messages.Message(
                    "CT_Shuttle_Launch_Failed_EmptyTileLandingSiteCreateFailed".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            landingSite.Tile = tile;
            landingSite.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(landingSite);
            GenerateMapAndBeginTargeting(landingSite, launchAction, shuttle);
        }

        private static void GenerateMapAndBeginTargeting(
            ModularShuttleTemporaryLandingSite landingSite,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ThingWithComps shuttle)
        {
            Map generatedMap = null;
            bool generationFailed = false;
            LongEventHandler.QueueLongEvent(
                delegate
                {
                    generatedMap = GetOrGenerateMapUtility.GetOrGenerateMap(
                        landingSite.Tile,
                        ResolveMapSize(landingSite),
                        null,
                        null,
                        false);
                    generationFailed = generatedMap == null;
                    if (!generationFailed)
                    {
                        ModularShuttleLandingMapHoldService.RegisterGeneratedSelectionMap(landingSite);
                    }
                },
                "GeneratingMapForNewEncounter",
                false,
                delegate(Exception exception)
                {
                    generationFailed = true;
                    Log.Error(
                        "[CeleTech Shuttle] Could not open empty-tile landing map for modular shuttle landing selection. " +
                        (exception != null ? exception.ToString() : "<unknown exception>"));
                },
                true,
                false,
                delegate
                {
                    if (generationFailed || generatedMap == null || !landingSite.HasMap)
                    {
                        CleanupFailedLandingSite(landingSite);
                        Messages.Message(
                            "CT_Shuttle_Launch_Failed_TargetMapGeneration".Translate(),
                            MessageTypeDefOf.RejectInput,
                            false);
                        return;
                    }

                    ModularShuttleExistingMapLandingTargeter.BeginTargeting(
                        landingSite,
                        launchAction,
                        shuttle);
                });
        }

        private static void CleanupFailedLandingSite(ModularShuttleTemporaryLandingSite landingSite)
        {
            if (landingSite != null && !landingSite.Destroyed)
            {
                landingSite.Destroy();
            }
        }

        private static IntVec3 ResolveMapSize(MapParent mapParent)
        {
            if (mapParent != null &&
                mapParent.def != null &&
                mapParent.def.overrideMapSize != null)
            {
                return mapParent.def.overrideMapSize.Value;
            }

            return Find.World.info.initialMapSize;
        }
    }
}
