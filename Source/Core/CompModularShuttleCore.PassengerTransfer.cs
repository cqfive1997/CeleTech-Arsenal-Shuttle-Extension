using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class CompModularShuttleCore
    {
        internal ShuttlePassengerInternalTransferResult TryTransferCompletedPassengerToCockpit(
            Pawn pawn,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            this.EnsureController();
            if (this.controller == null)
            {
                failureReason = "Shuttle controller is unavailable.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            return this.controller.TryTransferCompletedPassengerToCockpit(
                pawn,
                source,
                out failureReason);
        }
    }
}
