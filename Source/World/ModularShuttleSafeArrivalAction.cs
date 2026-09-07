using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    /// <summary>
    /// Serializable arrival decorator that preserves the selected action while
    /// enforcing one invariant: no modular shuttle payload may remain in the
    /// ActiveTransporterInfo objects discarded by TravellingTransporters.
    /// </summary>
    public sealed class ModularShuttleSafeArrivalAction : TransportersArrivalAction
    {
        private TransportersArrivalAction innerAction;

        public ModularShuttleSafeArrivalAction()
        {
        }

        private ModularShuttleSafeArrivalAction(TransportersArrivalAction innerAction)
        {
            this.innerAction = innerAction;
        }

        public override bool GeneratesMap
        {
            get { return this.innerAction != null && this.innerAction.GeneratesMap; }
        }

        internal static TransportersArrivalAction Wrap(
            TransportersArrivalAction arrivalAction,
            Thing shuttle)
        {
            if (!ModularShuttleVisitSiteArrivalUtility.IsModularShuttle(shuttle) ||
                arrivalAction is ModularShuttleSafeArrivalAction)
            {
                return arrivalAction;
            }

            return new ModularShuttleSafeArrivalAction(arrivalAction);
        }

        internal bool TargetsSpecificCellMapParent(MapParent mapParent)
        {
            ModularShuttleLandInSpecificCellArrivalAction specificCellAction =
                this.innerAction as ModularShuttleLandInSpecificCellArrivalAction;
            return specificCellAction != null && specificCellAction.TargetsMapParent(mapParent);
        }

        internal static TransportersArrivalAction Wrap(
            TransportersArrivalAction arrivalAction,
            TravellingTransporters travellingTransporters)
        {
            if (arrivalAction is ModularShuttleSafeArrivalAction ||
                !ModularShuttleVisitSiteArrivalUtility.ContainsModularShuttle(travellingTransporters))
            {
                return arrivalAction;
            }

            return new ModularShuttleSafeArrivalAction(arrivalAction);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.innerAction, "innerAction");
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

            return destinationTile.Valid;
        }

        public override bool ShouldUseLongEvent(
            List<ActiveTransporterInfo> pods,
            PlanetTile tile)
        {
            if (this.innerAction != null && this.innerAction.ShouldUseLongEvent(pods, tile))
            {
                return true;
            }

            return ModularShuttleArrivalRecoveryService.DestinationMayNeedMapGeneration(tile);
        }

        public override void Arrived(
            List<ActiveTransporterInfo> transporters,
            PlanetTile tile)
        {
            HashSet<Caravan> caravansBeforeArrival =
                ModularShuttleArrivalRecoveryService.CapturePlayerCaravans();
            Exception arrivalException = null;
            if (this.innerAction != null)
            {
                bool innerActionValid = false;
                try
                {
                    innerActionValid = this.innerAction.StillValid(transporters, tile);
                    if (innerActionValid)
                    {
                        this.innerAction.Arrived(transporters, tile);
                    }
                }
                catch (Exception exception)
                {
                    arrivalException = exception;
                    Log.Error("[CeleTech Shuttle] Wrapped shuttle arrival action failed; attempting payload recovery. " +
                        "action=" + this.innerAction.GetType().FullName + " exception=" + exception);
                }

                if (!innerActionValid && arrivalException == null)
                {
                    Log.Warning("[CeleTech Shuttle] Wrapped shuttle arrival action became invalid; " +
                        "attempting payload recovery. action=" + this.innerAction.GetType().FullName +
                        " tile=" + (tile.Valid ? tile.ToString() : "invalid"));
                }
            }

            if (!ModularShuttleArrivalRecoveryService.HasUnresolvedPayload(transporters))
            {
                return;
            }

            string failureReason;
            if (ModularShuttleArrivalRecoveryService.TryRecoverUnresolvedPayload(
                transporters,
                tile,
                caravansBeforeArrival,
                out failureReason))
            {
                return;
            }

            Log.Error("[CeleTech Shuttle] Modular shuttle arrival recovery failed. " +
                "innerAction=" +
                (this.innerAction != null ? this.innerAction.GetType().FullName : "null") +
                " originalException=" +
                (arrivalException != null ? arrivalException.GetType().Name : "none") +
                " reason=" +
                (failureReason ?? "unknown"));
            Messages.Message(
                "CT_Shuttle_ArrivalRecoveryFailed".Translate(),
                new GlobalTargetInfo(tile),
                MessageTypeDefOf.NegativeEvent,
                true);
        }
    }
}
