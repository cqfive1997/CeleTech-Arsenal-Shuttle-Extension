using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    public sealed class ModularShuttleLandInSpecificCellArrivalAction : TransportersArrivalAction
    {
        private MapParent mapParent;
        private IntVec3 cell;
        private Rot4 rotation;

        public ModularShuttleLandInSpecificCellArrivalAction()
        {
        }

        public ModularShuttleLandInSpecificCellArrivalAction(MapParent mapParent, IntVec3 cell, Rot4 rotation)
        {
            this.mapParent = mapParent;
            this.cell = cell;
            this.rotation = rotation;
        }

        public override bool GeneratesMap
        {
            get
            {
                return this.mapParent != null && !this.mapParent.HasMap;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.mapParent, "mapParent");
            Scribe_Values.Look(ref this.cell, "cell", default(IntVec3));
            Scribe_Values.Look(ref this.rotation, "rotation", default(Rot4));
        }

        public override FloatMenuAcceptanceReport StillValid(
            IEnumerable<IThingHolder> pods,
            PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport baseReport = base.StillValid(pods, destinationTile);
            if (!baseReport)
            {
                return baseReport;
            }

            if (this.mapParent != null && this.mapParent.Tile != destinationTile)
            {
                return false;
            }

            if (this.mapParent == null || !this.mapParent.Spawned || this.mapParent.EnterCooldownBlocksEntering())
            {
                return false;
            }

            if (this.mapParent.HasMap)
            {
                return TransportersArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, this.mapParent);
            }

            return this.mapParent.Tile.Valid &&
                this.mapParent.MapGeneratorDef != null &&
                (this.mapParent.MapGeneratorDef != MapGeneratorDefOf.Space ||
                    this.mapParent is SpaceMapParent);
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return this.mapParent != null && !this.mapParent.HasMap;
        }

        internal bool TargetsMapParent(MapParent candidate)
        {
            return candidate != null && this.mapParent == candidate;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            if (transporters == null || transporters.Count == 0 || this.mapParent == null)
            {
                Log.Error("[CeleTech Shuttle] Modular shuttle specific-cell arrival had no transporter or map.");
                return;
            }

            if (transporters.Count > 1)
            {
                Log.Error("[CeleTech Shuttle] Modular shuttle specific-cell arrival received multiple transporters.");
            }

            ActiveTransporterInfo transporter = transporters.FirstOrDefault();
            if (transporter == null)
            {
                Log.Error("[CeleTech Shuttle] Modular shuttle specific-cell arrival had a null transporter.");
                return;
            }

            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);
            Map map = this.GetOrGenerateLandingMap();
            if (map == null)
            {
                Log.Error("[CeleTech Shuttle] Modular shuttle specific-cell arrival could not get or generate a map.");
                return;
            }

            IntVec3 landingCell;
            if (!ModularShuttleVisitSiteArrivalUtility.TryEnsureLandingCellInBounds(
                transporter,
                map,
                this.cell,
                out landingCell))
            {
                Log.Error("[CeleTech Shuttle] Modular shuttle specific-cell arrival had no in-bounds landing cell.");
                return;
            }

            ModularShuttleVisitSiteArrivalUtility.ClearCrushablePlantsForLanding(
                transporter,
                map,
                landingCell,
                this.rotation);
            TransportersArrivalActionUtility.DropShuttle(
                transporter,
                map,
                landingCell,
                new Rot4?(this.rotation),
                null);
            ModularShuttleLandingMapHoldService.NotifyLandingCompleted(this.mapParent);
            Messages.Message("MessageShuttleArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion, true);
        }

        private Map GetOrGenerateLandingMap()
        {
            if (this.mapParent == null)
            {
                return null;
            }

            if (this.mapParent.HasMap)
            {
                return this.mapParent.Map;
            }

            return GetOrGenerateMapUtility.GetOrGenerateMap(
                this.mapParent.Tile,
                ResolveMapSize(this.mapParent),
                this.mapParent is SpaceMapParent ? this.mapParent.def : null,
                null,
                false);
        }

        internal static IntVec3 ResolveMapSize(MapParent mapParent)
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
    }
}
