using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    public sealed class ModularShuttleVisitSiteArrivalAction : TransportersArrivalAction
    {
        private Site site;
        private PawnsArrivalModeDef arrivalMode;

        public ModularShuttleVisitSiteArrivalAction()
        {
        }

        public ModularShuttleVisitSiteArrivalAction(Site site, PawnsArrivalModeDef arrivalMode)
        {
            this.site = site;
            this.arrivalMode = arrivalMode ?? PawnsArrivalModeDefOf.EdgeDrop;
        }

        public override bool GeneratesMap
        {
            get
            {
                return true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.site, "site");
            Scribe_Defs.Look(ref this.arrivalMode, "arrivalMode");
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport baseReport = base.StillValid(pods, destinationTile);
            if (!baseReport)
            {
                return baseReport;
            }

            if (this.site != null && this.site.Tile != destinationTile)
            {
                return false;
            }

            return TransportersArrivalAction_VisitSite.CanVisit(pods, this.site);
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return this.site != null && !this.site.HasMap;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            if (this.site == null)
            {
                Log.Error("[CeleTech Shuttle] ModularShuttleVisitSiteArrivalAction arrived without a site; contents will fall back to vanilla arrival handling.");
                return;
            }

            Thing lookTarget = TransportersArrivalActionUtility.GetLookTarget(transporters);
            bool generatedMap = !this.site.HasMap;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(this.site.Tile, this.site.PreferredMapSize, null, null, false);
            if (generatedMap)
            {
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(
                    map.mapPawns.AllPawns,
                    "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural),
                    LetterDefOf.NeutralEvent,
                    true,
                    true);
            }

            if (this.site.Faction != null &&
                this.site.Faction != Faction.OfPlayer &&
                this.site.MainSitePartDef != null &&
                this.site.MainSitePartDef.considerEnteringAsAttack)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(
                    this.site.Faction,
                    Faction.OfPlayer.GoodwillToMakeHostile(this.site.Faction),
                    true,
                    true,
                    HistoryEventDefOf.AttackedSettlement,
                    null);
            }

            if (transporters != null && transporters.IsShuttle())
            {
                Messages.Message("MessageShuttleArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion, true);
                this.DropModularShuttleAtSafeCell(transporters, map);
                return;
            }

            Messages.Message("MessageTransportPodsArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion, true);
            (this.arrivalMode ?? PawnsArrivalModeDefOf.EdgeDrop).Worker.TravellingTransportersArrived(transporters, map);
        }

        private void DropModularShuttleAtSafeCell(List<ActiveTransporterInfo> transporters, Map map)
        {
            if (transporters == null || transporters.Count == 0 || map == null)
            {
                return;
            }

            IntVec3 preferredCell = this.GetPreferredLandingCell(transporters, map);
            ModularShuttleVisitSiteArrivalUtility.DropModularShuttleAtSafeCell(
                transporters,
                map,
                preferredCell);
        }

        private IntVec3 GetPreferredLandingCell(List<ActiveTransporterInfo> transporters, Map map)
        {
            PawnsArrivalModeDef mode = this.arrivalMode ?? PawnsArrivalModeDefOf.EdgeDrop;
            if (mode == PawnsArrivalModeDefOf.CenterDrop)
            {
                IntVec3 centerCell;
                if (DropCellFinder.TryFindRaidDropCenterClose(out centerCell, map, true, true, true))
                {
                    return centerCell;
                }
            }

            return DropCellFinder.FindRaidDropCenterDistant(map, false, !transporters.IsShuttle());
        }
    }
}
