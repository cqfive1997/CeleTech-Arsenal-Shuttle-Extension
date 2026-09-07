using System;
using System.Collections.Generic;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleVisitSiteArrivalUtility
    {
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";
        private const string LogCategory = "ArrivalReflection";

        private static readonly FieldInfo VisitSiteSiteField =
            typeof(TransportersArrivalAction_VisitSite).GetField(
                "site",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo VisitSiteArrivalModeField =
            typeof(TransportersArrivalAction_VisitSite).GetField(
                "arrivalMode",
                BindingFlags.Instance | BindingFlags.NonPublic);

        internal static TransportersArrivalAction WrapVisitSiteArrivalForModularShuttle(
            TransportersArrivalAction arrivalAction,
            Thing shuttle)
        {
            if (!IsModularShuttle(shuttle))
            {
                return arrivalAction;
            }

            return WrapVisitSiteArrival(arrivalAction);
        }

        internal static TransportersArrivalAction WrapVisitSiteArrival(
            TransportersArrivalAction arrivalAction)
        {
            if (arrivalAction == null || arrivalAction is ModularShuttleVisitSiteArrivalAction)
            {
                return arrivalAction;
            }

            TransportersArrivalAction_VisitSite visitSite = arrivalAction as TransportersArrivalAction_VisitSite;
            if (visitSite == null)
            {
                return arrivalAction;
            }

            if (VisitSiteSiteField == null)
            {
                WarnMissingReflectionMemberOnce(
                    "MissingVisitSiteSiteField",
                    "TransportersArrivalAction_VisitSite.site",
                    "visit-site shuttle arrival wrapping");
                return arrivalAction;
            }

            if (VisitSiteArrivalModeField == null)
            {
                WarnMissingReflectionMemberOnce(
                    "MissingVisitSiteArrivalModeField",
                    "TransportersArrivalAction_VisitSite.arrivalMode",
                    "visit-site shuttle arrival wrapping");
                return arrivalAction;
            }

            Site site;
            PawnsArrivalModeDef arrivalMode;
            try
            {
                site = VisitSiteSiteField.GetValue(visitSite) as Site;
                arrivalMode = VisitSiteArrivalModeField.GetValue(visitSite) as PawnsArrivalModeDef;
            }
            catch (Exception exception)
            {
                ShuttleLog.WarnOnce(
                    LogCategory,
                    "VisitSiteFieldReadFailed",
                    "Could not read TransportersArrivalAction_VisitSite private fields for visit-site shuttle arrival wrapping. " +
                    "Fallback: the original RimWorld arrival action will be used. reason=" +
                    exception.GetType().Name +
                    ": " +
                    exception.Message);
                return arrivalAction;
            }

            if (site == null)
            {
                return arrivalAction;
            }

            return new ModularShuttleVisitSiteArrivalAction(site, arrivalMode);
        }

        private static void WarnMissingReflectionMemberOnce(string key, string memberName, string feature)
        {
            ShuttleLog.WarnOnce(
                LogCategory,
                key,
                "Could not find " + memberName + ". " +
                "Affected compatibility feature: " + feature + ". " +
                "Fallback: the original RimWorld arrival action will be used.");
        }

        internal static bool TryFindSafeModularShuttleLandingCell(
            ActiveTransporterInfo transporter,
            Map map,
            IntVec3 preferredCell,
            out IntVec3 landingCell)
        {
            landingCell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            Thing shuttle = transporter != null ? transporter.GetShuttle() : null;
            ThingDef shuttleDef = shuttle != null && shuttle.def != null
                ? shuttle.def
                : DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName) ?? ThingDefOf.Shuttle;
            Rot4 rotation = shuttle != null ? shuttle.Rotation : shuttleDef.defaultPlacingRot;

            if (IsAcceptedLandingCell(preferredCell, map, shuttleDef, rotation))
            {
                landingCell = preferredCell;
                return true;
            }

            if (preferredCell.IsValid &&
                CellFinder.TryFindRandomCellNear(
                    preferredCell,
                    map,
                    30,
                    c => IsAcceptedLandingCell(c, map, shuttleDef, rotation),
                    out landingCell))
            {
                return true;
            }

            IntVec3 bestShuttleCell = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
            if (IsAcceptedLandingCell(bestShuttleCell, map, shuttleDef, rotation))
            {
                landingCell = bestShuttleCell;
                return true;
            }

            if (CellFinder.TryFindRandomCell(
                map,
                c => IsAcceptedLandingCell(c, map, shuttleDef, rotation),
                out landingCell))
            {
                return true;
            }

            if (TryFindInBoundsFootprintCell(preferredCell, map, shuttleDef, rotation, out landingCell))
            {
                return false;
            }

            TryFindAnyInBoundsCell(map, out landingCell);
            return false;
        }

        internal static bool TryEnsureLandingCellInBounds(
            ActiveTransporterInfo transporter,
            Map map,
            IntVec3 candidate,
            out IntVec3 landingCell)
        {
            landingCell = candidate;
            if (map == null)
            {
                landingCell = IntVec3.Invalid;
                return false;
            }

            Thing shuttle = transporter != null ? transporter.GetShuttle() : null;
            ThingDef shuttleDef = shuttle != null && shuttle.def != null
                ? shuttle.def
                : DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName) ?? ThingDefOf.Shuttle;
            Rot4 rotation = shuttle != null ? shuttle.Rotation : shuttleDef.defaultPlacingRot;
            if (candidate.IsValid &&
                candidate.InBounds(map) &&
                GenAdj.OccupiedRect(
                    candidate,
                    rotation,
                    ResolveLandingClearanceSize(shuttleDef)).InBounds(map))
            {
                return true;
            }

            IntVec3 center = GetClampedMapCenter(map);
            if (TryFindInBoundsFootprintCell(center, map, shuttleDef, rotation, out landingCell))
            {
                return true;
            }

            return TryFindAnyInBoundsCell(map, out landingCell);
        }

        internal static void ClearCrushablePlantsForLanding(
            ActiveTransporterInfo transporter,
            Map map,
            IntVec3 landingCell)
        {
            if (map == null || !landingCell.IsValid || !landingCell.InBounds(map))
            {
                return;
            }

            Thing shuttle = transporter != null ? transporter.GetShuttle() : null;
            ThingDef shuttleDef = shuttle != null && shuttle.def != null
                ? shuttle.def
                : DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName) ?? ThingDefOf.Shuttle;
            Rot4 rotation = shuttle != null ? shuttle.Rotation : shuttleDef.defaultPlacingRot;
            ClearCrushablePlantsForLanding(map, landingCell, shuttleDef, rotation);
        }

        internal static void ClearCrushablePlantsForLanding(
            ActiveTransporterInfo transporter,
            Map map,
            IntVec3 landingCell,
            Rot4 rotation)
        {
            if (map == null || !landingCell.IsValid || !landingCell.InBounds(map))
            {
                return;
            }

            Thing shuttle = transporter != null ? transporter.GetShuttle() : null;
            ThingDef shuttleDef = shuttle != null && shuttle.def != null
                ? shuttle.def
                : DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName) ?? ThingDefOf.Shuttle;
            ClearCrushablePlantsForLanding(map, landingCell, shuttleDef, rotation);
        }

        private static void ClearCrushablePlantsForLanding(
            Map map,
            IntVec3 landingCell,
            ThingDef shuttleDef,
            Rot4 rotation)
        {
            if (map == null || !landingCell.IsValid || !landingCell.InBounds(map) || shuttleDef == null)
            {
                return;
            }

            CellRect footprint = GenAdj.OccupiedRect(
                landingCell,
                rotation,
                ResolveLandingClearanceSize(shuttleDef));
            if (!footprint.InBounds(map))
            {
                return;
            }

            foreach (IntVec3 cell in footprint.Cells)
            {
                ClearCrushablePlantAt(map, cell);
            }

            ClearCrushablePlantAt(
                map,
                ThingUtility.InteractionCellWhenAt(shuttleDef, landingCell, rotation, map));
        }

        private static void ClearCrushablePlantAt(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map))
            {
                return;
            }

            Plant plant = cell.GetPlant(map);
            if (plant != null && !plant.Destroyed)
            {
                plant.Destroy(DestroyMode.KillFinalize);
            }
        }

        internal static Thing DropModularShuttleAtSafeCell(
            List<ActiveTransporterInfo> transporters,
            Map map,
            IntVec3 preferredCell)
        {
            if (transporters == null || transporters.Count == 0 || map == null)
            {
                return null;
            }

            ActiveTransporterInfo transporter = transporters[0];
            IntVec3 landingCell;
            bool foundSafeCell = TryFindSafeModularShuttleLandingCell(
                transporter,
                map,
                preferredCell,
                out landingCell);
            if (!TryEnsureLandingCellInBounds(
                transporter,
                map,
                landingCell,
                out landingCell))
            {
                Log.Error("[CeleTech Shuttle] Could not find any in-bounds modular shuttle landing cell. map=" + map);
                return null;
            }

            if (!foundSafeCell && Prefs.DevMode)
            {
                Log.Warning(
                    "[CeleTech Shuttle] Could not find a fully accepted modular shuttle landing cell; using an in-bounds fallback. " +
                    "preferredCell=" +
                    preferredCell +
                    " fallbackCell=" +
                    landingCell +
                    " map=" +
                    map);
            }

            Thing shuttle = transporter.GetShuttle();
            ThingDef shuttleDef = shuttle != null && shuttle.def != null
                ? shuttle.def
                : DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName) ?? ThingDefOf.Shuttle;
            Rot4 rotation = shuttle != null ? shuttle.Rotation : shuttleDef.defaultPlacingRot;

            ClearCrushablePlantsForLanding(map, landingCell, shuttleDef, rotation);
            return TransportersArrivalActionUtility.DropShuttle(
                transporter,
                map,
                landingCell,
                new Rot4?(rotation));
        }

        internal static bool ContainsModularShuttle(TravellingTransporters travellingTransporters)
        {
            if (travellingTransporters == null)
            {
                return false;
            }

            List<IThingHolder> holders = new List<IThingHolder>();
            travellingTransporters.GetChildHolders(holders);
            for (int i = 0; i < holders.Count; i++)
            {
                ActiveTransporterInfo transporter = holders[i] as ActiveTransporterInfo;
                if (transporter != null && IsModularShuttle(transporter.GetShuttle()))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsModularShuttle(Thing shuttle)
        {
            ThingWithComps shuttleWithComps = shuttle as ThingWithComps;
            return shuttleWithComps != null &&
                (shuttleWithComps.TryGetComp<CompModularShuttleCore>() != null ||
                    IsModularShuttleDef(shuttle.def));
        }

        internal static bool IsModularShuttleDef(ThingDef shuttleDef)
        {
            return shuttleDef != null &&
                (shuttleDef.defName == ModularShuttleHostDefName ||
                    shuttleDef.HasComp<CompModularShuttleCore>());
        }

        private static bool IsAcceptedLandingCell(IntVec3 cell, Map map, ThingDef shuttleDef, Rot4 rotation)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            CellRect footprint = GenAdj.OccupiedRect(
                cell,
                rotation,
                ResolveLandingClearanceSize(shuttleDef));
            if (!footprint.InBounds(map))
            {
                return false;
            }

            bool hasCrushablePlant;
            if (!CanUseLandingFootprintAfterCrushingPlants(
                    cell,
                    footprint,
                    map,
                    shuttleDef,
                    rotation,
                    out hasCrushablePlant))
            {
                return false;
            }

            if (RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(
                    cell,
                    map,
                    shuttleDef,
                    rotation).Accepted)
            {
                return true;
            }

            return hasCrushablePlant;
        }

        internal static AcceptanceReport ShuttleCanLandHereAllowingCrushablePlants(
            LocalTargetInfo target,
            Map map,
            ThingDef shuttleDef,
            Rot4 rotation)
        {
            AcceptanceReport vanillaReport =
                RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, map, shuttleDef, new Rot4?(rotation));
            if (map == null || shuttleDef == null || !target.IsValid)
            {
                return vanillaReport;
            }

            if (IsAcceptedLandingCell(target.Cell, map, shuttleDef, rotation))
            {
                return AcceptanceReport.WasAccepted;
            }

            if (vanillaReport.Accepted)
            {
                return false;
            }

            return vanillaReport;
        }

        internal static void DrawShuttleGhostAllowingCrushablePlants(
            LocalTargetInfo target,
            Map map,
            ThingDef shuttleDef,
            Rot4 rotation)
        {
            Color color = ShuttleCanLandHereAllowingCrushablePlants(target, map, shuttleDef, rotation).Accepted
                ? Designator_Place.CanPlaceColor
                : Designator_Place.CannotPlaceColor;
            GhostDrawer.DrawGhostThing(
                target.Cell,
                rotation,
                shuttleDef,
                shuttleDef.graphic,
                color,
                AltitudeLayer.Blueprint,
                null,
                true,
                null);
            Vector3 interactionCell =
                ThingUtility.InteractionCellWhenAt(shuttleDef, target.Cell, rotation, map)
                    .ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint);
            Graphics.DrawMesh(MeshPool.plane10, interactionCell, Quaternion.identity, GenDraw.InteractionCellMaterial, 0);
        }

        private static bool CanUseLandingFootprintAfterCrushingPlants(
            IntVec3 landingCell,
            CellRect footprint,
            Map map,
            ThingDef shuttleDef,
            Rot4 rotation,
            out bool hasCrushablePlant)
        {
            hasCrushablePlant = false;
            foreach (IntVec3 footprintCell in footprint.Cells)
            {
                if (!CanUseLandingCellAfterCrushingPlants(footprintCell, map, shuttleDef, ref hasCrushablePlant))
                {
                    return false;
                }
            }

            IntVec3 interactionCell = ThingUtility.InteractionCellWhenAt(shuttleDef, landingCell, rotation, map);
            if (!CanUseLandingCellAfterCrushingPlants(interactionCell, map, shuttleDef, ref hasCrushablePlant))
            {
                return false;
            }

            // Success means the full footprint has no non-crushable blocker.
            // The output flag separately decides whether a vanilla rejection is plant-only.
            return true;
        }

        private static bool CanUseLandingCellAfterCrushingPlants(
            IntVec3 cell,
            Map map,
            ThingDef shuttleDef,
            ref bool hasCrushablePlant)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            if (map.fogGrid != null && map.fogGrid.IsFogged(cell))
            {
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null || terrain.passability == Traversability.Impassable)
            {
                return false;
            }

            TerrainAffordanceDef terrainAffordanceNeeded = shuttleDef.terrainAffordanceNeeded ??
                ThingDefOf.Shuttle.terrainAffordanceNeeded;
            if (terrainAffordanceNeeded != null && !cell.GetAffordances(map).Contains(terrainAffordanceNeeded))
            {
                return false;
            }

            RoofDef roof = cell.GetRoof(map);
            if (roof != null && (roof.isNatural || roof.isThickRoof))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (IsCrushablePlantThing(thing))
                {
                    hasCrushablePlant = true;
                    continue;
                }

                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    return false;
                }

                if (BlocksPlantCrushLanding(shuttleDef, thing))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsCrushablePlantThing(Thing thing)
        {
            return thing is Plant ||
                (thing != null &&
                    thing.def != null &&
                    (thing.def.category == ThingCategory.Plant || thing.def.plant != null));
        }

        private static bool BlocksPlantCrushLanding(ThingDef shuttleDef, Thing thing)
        {
            if (thing == null || thing.def == null)
            {
                return false;
            }

            if (thing is IActiveTransporter || thing is Skyfaller)
            {
                return true;
            }

            if (thing.def.category == ThingCategory.Building)
            {
                if (thing.def.building != null && thing.def.building.isPowerConduit)
                {
                    return false;
                }

                return !ModularShuttleLandingCompatibilityPolicy.CanCoexistBeneathShuttle(
                    shuttleDef,
                    thing);
            }

            return thing.def.preventSkyfallersLandingOn ||
                thing.def.passability == Traversability.Impassable;
        }

        private static bool TryFindInBoundsFootprintCell(
            IntVec3 preferredCell,
            Map map,
            ThingDef shuttleDef,
            Rot4 rotation,
            out IntVec3 landingCell)
        {
            landingCell = IntVec3.Invalid;
            if (preferredCell.IsValid && preferredCell.InBounds(map))
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(preferredCell, 40f, true))
                {
                    if (cell.InBounds(map) &&
                        GenAdj.OccupiedRect(
                            cell,
                            rotation,
                            ResolveLandingClearanceSize(shuttleDef)).InBounds(map))
                    {
                        landingCell = cell;
                        return true;
                    }
                }
            }

            foreach (IntVec3 cell in map.AllCells)
            {
                if (GenAdj.OccupiedRect(
                    cell,
                    rotation,
                    ResolveLandingClearanceSize(shuttleDef)).InBounds(map))
                {
                    landingCell = cell;
                    return true;
                }
            }

            return false;
        }

        private static IntVec2 ResolveLandingClearanceSize(ThingDef shuttleDef)
        {
            return shuttleDef != null
                ? shuttleDef.Size
                : new IntVec2(1, 1);
        }

        private static bool TryFindAnyInBoundsCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 center = GetClampedMapCenter(map);
            if (center.IsValid && center.InBounds(map))
            {
                cell = center;
                return true;
            }

            foreach (IntVec3 mapCell in map.AllCells)
            {
                if (mapCell.InBounds(map))
                {
                    cell = mapCell;
                    return true;
                }
            }

            return false;
        }

        private static IntVec3 GetClampedMapCenter(Map map)
        {
            if (map == null || map.Size.x <= 0 || map.Size.z <= 0)
            {
                return IntVec3.Invalid;
            }

            return new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
        }
    }
}
