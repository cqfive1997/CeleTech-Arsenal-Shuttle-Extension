using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Internal passenger mutation seam used by held-passenger intent reconciliation and
    /// natural-completion transfer into the cockpit. The cockpit and cargo share the
    /// vanilla transporter holder, but remain separate domain concepts.
    /// It does not expand the public cargo backend contract.
    /// </summary>
    internal interface IShuttlePassengerBoardingBackend
    {
        bool ContainsPassenger(ThingWithComps host, Pawn pawn);

        bool EnsurePassengerQueued(
            ThingWithComps host,
            Pawn pawn,
            out bool queuedNow,
            out string failureReason);

        bool TryCancelPendingPassenger(
            ThingWithComps host,
            Pawn pawn,
            out bool canceled,
            out string failureReason);

        ShuttlePassengerInternalTransferResult TryTransferHeldPassengerToCockpit(
            ThingWithComps host,
            Pawn pawn,
            ThingOwner<Thing> source,
            out string failureReason);
    }
}
