using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    public sealed class ModularShuttleVisitCaravanWorldObjectArrivalAction : TransportersArrivalAction
    {
        private static readonly List<Pawn> tmpPawns = new List<Pawn>();
        private static readonly List<Thing> tmpContainedThings = new List<Thing>();

        private WorldObject worldObject;

        public ModularShuttleVisitCaravanWorldObjectArrivalAction()
        {
        }

        public ModularShuttleVisitCaravanWorldObjectArrivalAction(WorldObject worldObject)
        {
            this.worldObject = worldObject;
        }

        public override bool GeneratesMap
        {
            get
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.worldObject, "worldObject");
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

            if (this.worldObject != null && this.worldObject.Tile != destinationTile)
            {
                return false;
            }

            return ModularShuttleCaravanWorldObjectCompatUtility.CanVisit(pods, this.worldObject);
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            if (this.worldObject == null)
            {
                Log.Error("[CeleTech Shuttle] Caravan-world-object arrival reached destination without a world object.");
                return;
            }

            tmpPawns.Clear();
            tmpContainedThings.Clear();

            try
            {
                CollectPawns(transporters);
                PlanetTile caravanTile;
                if (!GenWorldClosest.TryFindClosestPassableTile(tile, out caravanTile))
                {
                    caravanTile = tile;
                }

                Caravan caravan = CaravanMaker.MakeCaravan(tmpPawns, Faction.OfPlayer, caravanTile, true);
                Thing shuttle = TryTransferShuttleToCaravan(transporters, caravan);
                TransferRemainingContentsToCaravan(transporters, caravan);

                ThingWithComps shuttleWithComps = shuttle as ThingWithComps;
                if (ModularShuttleVisitSiteArrivalUtility.IsModularShuttle(shuttleWithComps))
                {
                    ModularShuttleCaravanWorldObjectArrivalRegistry.Register(caravan);
                }

                Messages.Message("MessageShuttleArrived".Translate(), caravan, MessageTypeDefOf.TaskCompletion, true);
                if (!ModularShuttleCaravanWorldObjectCompatUtility.TryNotifyCaravanArrived(this.worldObject, caravan))
                {
                    Log.Error("[CeleTech Shuttle] Caravan-world-object arrival could not notify target world object. def=" +
                        (this.worldObject.def != null ? this.worldObject.def.defName : "null"));
                }
            }
            finally
            {
                tmpPawns.Clear();
                tmpContainedThings.Clear();
            }
        }

        private static void CollectPawns(List<ActiveTransporterInfo> transporters)
        {
            if (transporters == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ThingOwner innerContainer = transporters[i].innerContainer;
                for (int j = innerContainer.Count - 1; j >= 0; j--)
                {
                    Pawn pawn = innerContainer[j] as Pawn;
                    if (pawn != null)
                    {
                        tmpPawns.Add(pawn);
                        innerContainer.Remove(pawn);
                    }
                }
            }
        }

        private static Thing TryTransferShuttleToCaravan(
            List<ActiveTransporterInfo> transporters,
            Caravan caravan)
        {
            if (transporters == null ||
                transporters.Count == 0 ||
                caravan == null ||
                !transporters.IsShuttle())
            {
                return null;
            }

            Thing shuttle = transporters[0].RemoveShuttle();
            if (shuttle != null)
            {
                CaravanInventoryUtility.GiveThing(caravan, shuttle);
                ModularShuttleCaravanUtility.ClearCachedHeldModularShuttle();
            }

            return shuttle;
        }

        private static void TransferRemainingContentsToCaravan(
            List<ActiveTransporterInfo> transporters,
            Caravan caravan)
        {
            if (transporters == null || caravan == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                tmpContainedThings.Clear();
                tmpContainedThings.AddRange(transporters[i].innerContainer);
                for (int j = 0; j < tmpContainedThings.Count; j++)
                {
                    Thing thing = tmpContainedThings[j];
                    transporters[i].innerContainer.Remove(thing);
                    CaravanInventoryUtility.GiveThing(caravan, thing);
                }
            }
        }
    }
}
