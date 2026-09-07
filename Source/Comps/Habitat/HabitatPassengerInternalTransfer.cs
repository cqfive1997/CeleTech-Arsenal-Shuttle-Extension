using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatPassengerInternalTransfer
    {
        internal static ShuttlePassengerInternalTransferResult TryTransfer(
            ThingWithComps host,
            Pawn pawn,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            failureReason = null;
            CompModularShuttleCore core = host != null
                ? host.TryGetComp<CompModularShuttleCore>()
                : null;
            if (core == null)
            {
                failureReason = "Shuttle core is unavailable.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            return core.TryTransferCompletedPassengerToCockpit(
                pawn,
                source,
                out failureReason);
        }
    }
}
