using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    /// <summary>
    /// Default world-transfer factory for the modular shuttle.
    /// It only creates and annotates skyfallers; validation and energy spending happen before this seam.
    /// </summary>
    public sealed class DefaultShuttleSkyfallerFactory : IShuttleSkyfallerFactory
    {
        public FlyShipLeaving CreateLeavingSkyfaller(ShuttleLaunchPayload payload, ActiveTransporter activeTransporter)
        {
            if (payload == null || activeTransporter == null || payload.LeavingSkyfallerDef == null)
            {
                return null;
            }

            FlyShipLeaving leaving;
            try
            {
                leaving = SkyfallerMaker.MakeSkyfaller(payload.LeavingSkyfallerDef, activeTransporter) as FlyShipLeaving;
            }
            catch (Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Leaving skyfaller creation failed in factory. " +
                    BuildExceptionDetails(exception));
                throw;
            }

            if (leaving == null)
            {
                return null;
            }

            leaving.groupID = payload.GroupID;
            leaving.destinationTile = payload.DestinationTile;
            TransportersArrivalAction wrappedArrivalAction =
                ModularShuttleVisitSiteArrivalUtility.WrapVisitSiteArrivalForModularShuttle(
                payload.ArrivalAction,
                payload.Host);
            leaving.arrivalAction = ModularShuttleSafeArrivalAction.Wrap(
                wrappedArrivalAction,
                payload.Host);
            leaving.worldObjectDef = payload.WorldObjectDef;
            return leaving;
        }

        private static string BuildExceptionDetails(Exception exception)
        {
            if (exception == null)
            {
                return "exception=<null>";
            }

            string details = "exception=" + exception.GetType().FullName +
                ": " + (exception.Message ?? "<null>");
            Exception inner = exception.InnerException;
            int depth = 0;
            while (inner != null && depth < 5)
            {
                details += "\ninner[" + depth.ToString() + "]=" +
                    inner.GetType().FullName + ": " +
                    (inner.Message ?? "<null>");
                inner = inner.InnerException;
                depth++;
            }

            return details + "\nstack=" + exception;
        }
    }
}
