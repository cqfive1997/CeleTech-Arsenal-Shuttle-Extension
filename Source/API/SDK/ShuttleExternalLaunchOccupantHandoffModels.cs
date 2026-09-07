using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public enum ShuttleExternalLaunchOccupantHandoffKind
    {
        Unknown,
        Quote,
        Handoff
    }

    public enum ShuttleExternalLaunchOccupantHandoffFailureReason
    {
        None,
        Unavailable,
        InvalidRequest,
        InvalidSource,
        HostUnavailable,
        RuntimeBlocked,
        LaunchTransferActive,
        CargoTransferActive,
        QueuedLoadActive,
        OccupantUnavailable,
        OccupantDestroyed,
        OccupantDead,
        OccupantNotColonist,
        OccupantAlreadyLoaded,
        OccupantOwnedByInternalHolder,
        OccupantSpawned,
        OccupantNotHeld,
        CargoUnavailable,
        InsufficientCargoCapacity,
        HandoffTargetUnavailable,
        HandoffAddFailed,
        InternalError
    }

    public sealed class ShuttleExternalLaunchOccupantHandoffSource
    {
        public ShuttleExternalLaunchOccupantHandoffSource(
            string ownerPackageId,
            string localRequesterKey,
            string moduleInstanceId,
            string reasonKey)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalRequesterKey = localRequesterKey;
            this.ModuleInstanceId = moduleInstanceId;
            this.ReasonKey = reasonKey;
        }

        public string OwnerPackageId { get; private set; }
        public string LocalRequesterKey { get; private set; }
        public string ModuleInstanceId { get; private set; }
        public string ReasonKey { get; private set; }
    }

    public sealed class ShuttleExternalLaunchOccupantHandoffRequest
    {
        /// <summary>
        /// Requests a one-way transfer from the caller-owned holder into the shuttle launch payload.
        /// The pawn must already be held by a source holder and must not be a pawn owned by one
        /// of this shuttle's internal holders.
        /// </summary>
        public ShuttleExternalLaunchOccupantHandoffRequest(
            ShuttleExternalLaunchOccupantHandoffSource source,
            Pawn pawn)
        {
            this.Source = source;
            this.Pawn = pawn;
        }

        public ShuttleExternalLaunchOccupantHandoffSource Source { get; private set; }
        public Pawn Pawn { get; private set; }
    }

    public sealed class ShuttleExternalLaunchOccupantHandoffResult
    {
        public ShuttleExternalLaunchOccupantHandoffResult(
            bool available,
            bool success,
            bool dryRun,
            ShuttleExternalLaunchOccupantHandoffKind kind,
            ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            string message,
            int pawnThingIdNumber,
            string pawnThingId,
            string pawnLabel,
            float affectedMassKg,
            ShuttleExternalLaunchOccupantHandoffSource source,
            int ticksGame,
            string diagnostics)
        {
            this.Available = available;
            this.Success = success;
            this.DryRun = dryRun;
            this.Kind = kind;
            this.FailureReason = failureReason;
            this.Message = message;
            this.PawnThingIdNumber = pawnThingIdNumber;
            this.PawnThingId = pawnThingId;
            this.PawnLabel = pawnLabel;
            this.AffectedMassKg = affectedMassKg;
            this.Source = source;
            this.TicksGame = ticksGame;
            this.Diagnostics = diagnostics;
        }

        public bool Available { get; private set; }
        public bool Success { get; private set; }
        public bool DryRun { get; private set; }
        public ShuttleExternalLaunchOccupantHandoffKind Kind { get; private set; }
        public ShuttleExternalLaunchOccupantHandoffFailureReason FailureReason { get; private set; }
        public string Message { get; private set; }
        public int PawnThingIdNumber { get; private set; }
        public string PawnThingId { get; private set; }
        public string PawnLabel { get; private set; }
        public float AffectedMassKg { get; private set; }
        public ShuttleExternalLaunchOccupantHandoffSource Source { get; private set; }
        public int TicksGame { get; private set; }
        public string Diagnostics { get; private set; }
    }
}
