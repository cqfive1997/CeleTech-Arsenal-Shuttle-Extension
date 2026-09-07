using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleExistingMapLandingTargeter
    {
        internal static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
            MapParent mapParent,
            IEnumerable<IThingHolder> pods,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ThingWithComps shuttle)
        {
            if (mapParent == null ||
                shuttle == null ||
                launchAction == null)
            {
                yield break;
            }

            if (TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, mapParent))
            {
                yield return new FloatMenuOption(
                    "LandInExistingMap".Translate(mapParent.Label),
                    delegate
                    {
                        BeginTargeting(mapParent, launchAction, shuttle);
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

            string failureReason;
            if (CanOpenMapForLandingSelection(mapParent, out failureReason))
            {
                yield return new FloatMenuOption(
                    "CT_Shuttle_Launch_OpenMapAndChooseLandingCell".Translate(mapParent.Label),
                    delegate
                    {
                        OpenMapAndBeginTargeting(mapParent, launchAction, shuttle);
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
        }

        internal static bool TryResolveModularShuttle(
            IEnumerable<IThingHolder> pods,
            out ThingWithComps shuttle)
        {
            shuttle = null;
            if (pods == null)
            {
                return false;
            }

            foreach (IThingHolder holder in pods)
            {
                CompTransporter transporter = holder as CompTransporter;
                if (transporter != null)
                {
                    shuttle = transporter.parent as ThingWithComps;
                    if (ModularShuttleVisitSiteArrivalUtility.IsModularShuttle(shuttle))
                    {
                        return true;
                    }
                }

                ActiveTransporterInfo activeTransporter = holder as ActiveTransporterInfo;
                if (activeTransporter != null)
                {
                    shuttle = activeTransporter.GetShuttle() as ThingWithComps;
                    if (ModularShuttleVisitSiteArrivalUtility.IsModularShuttle(shuttle))
                    {
                        return true;
                    }
                }

                Caravan caravan = holder as Caravan;
                if (caravan != null &&
                    ModularShuttleCaravanUtility.TryFindHeldModularShuttleCached(caravan, out shuttle))
                {
                    return true;
                }
            }

            shuttle = null;
            return false;
        }

        internal static void BeginTargeting(
            MapParent mapParent,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ThingWithComps shuttle)
        {
            if (mapParent == null || !mapParent.HasMap || shuttle == null)
            {
                return;
            }

            ModularShuttleLandingMapHoldService.RefreshTransientHold(mapParent);
            Map map = mapParent.Map;
            if (Find.WorldTargeter != null && Find.WorldTargeter.IsTargeting)
            {
                Find.WorldTargeter.StopTargeting();
            }

            Current.Game.CurrentMap = map;
            CameraJumper.TryHideWorld();

            ThingDef shuttleDef = shuttle.def ?? ThingDefOf.Shuttle;
            Rot4 selectedRotation = shuttleDef.defaultPlacingRot;
            TargetingParameters targetingParameters = TargetingParameters.ForCell();
            Find.Targeter.BeginTargeting(
                targetingParameters,
                delegate(LocalTargetInfo target)
                {
                    ModularShuttleLandingMapHoldService.RefreshTransientHold(mapParent);
                    launchAction(
                        mapParent.Tile,
                        new ModularShuttleLandInSpecificCellArrivalAction(
                            mapParent,
                            target.Cell,
                            selectedRotation));
                },
                delegate(LocalTargetInfo target)
                {
                    ModularShuttleVisitSiteArrivalUtility.DrawShuttleGhostAllowingCrushablePlants(
                        target,
                        map,
                        shuttleDef,
                        selectedRotation);
                },
                delegate(LocalTargetInfo target)
                {
                    AcceptanceReport report =
                        ModularShuttleVisitSiteArrivalUtility.ShuttleCanLandHereAllowingCrushablePlants(
                            target,
                            map,
                            shuttleDef,
                            selectedRotation);
                    if (!report.Accepted)
                    {
                        Messages.Message(
                            report.Reason,
                            new LookTargets(target.Cell, map),
                            MessageTypeDefOf.RejectInput,
                            false);
                    }

                    return report.Accepted;
                },
                null,
                null,
                CompLaunchable.TargeterMouseAttachment,
                true,
                delegate(LocalTargetInfo target)
                {
                    ModularShuttleLandingMapHoldService.RefreshTransientHold(mapParent);
                    if (!shuttleDef.rotatable)
                    {
                        return;
                    }

                    if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
                    {
                        selectedRotation = selectedRotation.Rotated(RotationDirection.Clockwise);
                    }

                    if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
                    {
                        selectedRotation = selectedRotation.Rotated(RotationDirection.Counterclockwise);
                    }
                },
                null);
        }

        private static bool CanOpenMapForLandingSelection(MapParent mapParent, out string failureReason)
        {
            failureReason = null;
            if (mapParent == null || !mapParent.Spawned)
            {
                failureReason = "CT_Shuttle_Launch_Failed_TargetMapUnavailable".Translate().ToString();
                return false;
            }

            if (mapParent.HasMap)
            {
                return false;
            }

            if (mapParent.EnterCooldownBlocksEntering())
            {
                failureReason = "MessageEnterCooldownBlocksEntering".Translate(
                    mapParent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true, false)).ToString();
                return false;
            }

            if (!mapParent.Tile.Valid ||
                mapParent.MapGeneratorDef == null)
            {
                failureReason = "CT_Shuttle_Launch_Failed_TargetMapUnavailable".Translate().ToString();
                return false;
            }

            if (mapParent.MapGeneratorDef == MapGeneratorDefOf.Space &&
                !(mapParent is SpaceMapParent))
            {
                failureReason = "CT_Shuttle_Launch_Failed_TargetMapUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private static void OpenMapAndBeginTargeting(
            MapParent mapParent,
            Action<PlanetTile, TransportersArrivalAction> launchAction,
            ThingWithComps shuttle)
        {
            if (mapParent == null || launchAction == null || shuttle == null)
            {
                return;
            }

            if (mapParent.HasMap)
            {
                BeginTargeting(mapParent, launchAction, shuttle);
                return;
            }

            string failureReason;
            if (!CanOpenMapForLandingSelection(mapParent, out failureReason))
            {
                if (!failureReason.NullOrEmpty())
                {
                    Messages.Message(failureReason, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            Map generatedMap = null;
            bool generationFailed = false;
            LongEventHandler.QueueLongEvent(
                delegate
                {
                    generatedMap = GetOrGenerateMapUtility.GetOrGenerateMap(
                        mapParent.Tile,
                        ResolveMapSize(mapParent),
                        ResolveWorldObjectDefForGeneration(mapParent),
                        null,
                        false);
                    generationFailed = generatedMap == null;
                    if (!generationFailed)
                    {
                        ModularShuttleLandingMapHoldService.RegisterGeneratedSelectionMap(mapParent);
                    }
                },
                "GeneratingMapForNewEncounter",
                false,
                delegate(Exception exception)
                {
                    generationFailed = true;
                    Log.Error(
                        "[CeleTech Shuttle] Could not open target map for modular shuttle landing selection. " +
                        (exception != null ? exception.ToString() : "<unknown exception>"));
                },
                true,
                false,
                delegate
                {
                    if (generationFailed || generatedMap == null || !mapParent.HasMap)
                    {
                        Messages.Message(
                            "CT_Shuttle_Launch_Failed_TargetMapGeneration".Translate(),
                            MessageTypeDefOf.RejectInput,
                            false);
                        return;
                    }

                    BeginTargeting(mapParent, launchAction, shuttle);
                });
        }

        private static IntVec3 ResolveMapSize(MapParent mapParent)
        {
            Site site = mapParent as Site;
            if (site != null)
            {
                return site.PreferredMapSize;
            }

            if (mapParent != null &&
                mapParent.def != null &&
                mapParent.def.overrideMapSize != null)
            {
                return mapParent.def.overrideMapSize.Value;
            }

            return Find.World.info.initialMapSize;
        }

        private static WorldObjectDef ResolveWorldObjectDefForGeneration(MapParent mapParent)
        {
            return mapParent is SpaceMapParent ? mapParent.def : null;
        }
    }
}
