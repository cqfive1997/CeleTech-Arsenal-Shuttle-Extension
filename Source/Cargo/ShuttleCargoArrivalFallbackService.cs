using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal static class ShuttleCargoArrivalFallbackService
    {
        // TODO: Delegate to cargo backend destination selection once arrival fallback has a backend-level API.
        // This service intentionally wraps vanilla CompTransporter only as a narrow last-resort fallback boundary.
        internal static bool TryGetNormalCargoFallbackOwner(
            ThingWithComps shuttleHost,
            out ThingOwner fallbackOwner,
            out string failureReason)
        {
            fallbackOwner = null;
            failureReason = null;

            if (shuttleHost == null)
            {
                failureReason = "[CeleTech Shuttle] Arrival cargo fallback failed: shuttle host is unavailable.";
                return false;
            }

            CompTransporter transporter = shuttleHost.TryGetComp<CompTransporter>();
            if (transporter == null)
            {
                failureReason = "[CeleTech Shuttle] Arrival cargo fallback failed: CompTransporter is unavailable.";
                return false;
            }

            fallbackOwner = transporter.GetDirectlyHeldThings();
            if (fallbackOwner == null)
            {
                failureReason = "[CeleTech Shuttle] Arrival cargo fallback failed: normal cargo holder is unavailable.";
                return false;
            }

            return true;
        }
    }
}
