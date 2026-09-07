namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public enum ShuttleExternalCargoTransactionKind
    {
        Unknown,
        QuoteConsume,
        Consume,
        QuoteDeposit,
        Deposit
    }

    public enum ShuttleExternalCargoTransactionFailureReason
    {
        None,
        Unavailable,
        InvalidRequest,
        InvalidSource,
        InvalidDef,
        InvalidQuantity,
        CargoUnavailable,
        InsufficientItems,
        UnsupportedCargoKind,
        QueuedCargoExcluded,
        BlockedCargoExcluded,
        RefrigeratedCargoExcluded,
        PawnCargoExcluded,
        CorpseCargoExcluded,
        HostUnavailable,
        RuntimeBlocked,
        LaunchTransferActive,
        CargoTransferActive,
        InternalError,
        PartialApplyRejected,
        RollbackFailed,
        DepositUnsupported,
        InvalidStuff,
        InvalidQuality,
        UnsupportedThingDef,
        UnsupportedStuff,
        UnsupportedQuality,
        StackLimitExceeded,
        InsufficientCargoCapacity,
        CargoFilterRejected,
        ThingCreationFailed,
        DepositTargetUnavailable,
        DepositAddFailed,
        CleanupFailed
    }

    public sealed class ShuttleExternalCargoTransactionSource
    {
        public ShuttleExternalCargoTransactionSource(
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

    public sealed class ShuttleExternalCargoConsumeRequest
    {
        public ShuttleExternalCargoConsumeRequest(
            ShuttleExternalCargoTransactionSource source,
            string itemDefName,
            int count)
            : this(
                source,
                itemDefName,
                null,
                null,
                null,
                count,
                0f,
                false,
                true)
        {
        }

        public ShuttleExternalCargoConsumeRequest(
            ShuttleExternalCargoTransactionSource source,
            string itemDefName,
            string categoryDefName,
            string stuffDefName,
            string quality,
            int count,
            float maxMassKg,
            bool allowPartial,
            bool requireExactCount)
        {
            this.Source = source;
            this.ItemDefName = itemDefName;
            this.CategoryDefName = categoryDefName;
            this.StuffDefName = stuffDefName;
            this.Quality = quality;
            this.Count = count;
            this.MaxMassKg = maxMassKg;
            this.AllowPartial = allowPartial;
            this.RequireExactCount = requireExactCount;
        }

        public ShuttleExternalCargoTransactionSource Source { get; private set; }
        public string ItemDefName { get; private set; }
        public string CategoryDefName { get; private set; }
        public string StuffDefName { get; private set; }
        public string Quality { get; private set; }
        public int Count { get; private set; }
        public float MaxMassKg { get; private set; }
        public bool AllowPartial { get; private set; }
        public bool RequireExactCount { get; private set; }
    }

    public sealed class ShuttleExternalCargoDepositRequest
    {
        public ShuttleExternalCargoDepositRequest(
            ShuttleExternalCargoTransactionSource source,
            string itemDefName,
            int count)
            : this(
                source,
                itemDefName,
                count,
                null,
                null,
                0f,
                true,
                false)
        {
        }

        public ShuttleExternalCargoDepositRequest(
            ShuttleExternalCargoTransactionSource source,
            string itemDefName,
            int count,
            string stuffDefName,
            string quality,
            float maxMassKg,
            bool requireExactCount,
            bool allowMerge)
        {
            this.Source = source;
            this.ItemDefName = itemDefName;
            this.Count = count;
            this.StuffDefName = stuffDefName;
            this.Quality = quality;
            this.MaxMassKg = maxMassKg;
            this.RequireExactCount = requireExactCount;
            this.AllowMerge = allowMerge;
        }

        public ShuttleExternalCargoTransactionSource Source { get; private set; }
        public string ItemDefName { get; private set; }
        public int Count { get; private set; }
        public string StuffDefName { get; private set; }
        public string Quality { get; private set; }
        public float MaxMassKg { get; private set; }
        public bool RequireExactCount { get; private set; }
        public bool AllowMerge { get; private set; }
    }

    public sealed class ShuttleExternalCargoTransactionResult
    {
        public ShuttleExternalCargoTransactionResult(
            bool available,
            bool success,
            bool dryRun,
            ShuttleExternalCargoTransactionKind kind,
            ShuttleExternalCargoTransactionFailureReason failureReason,
            string message,
            int requestedCount,
            int matchedCount,
            int affectedCount,
            int affectedStackCount,
            float affectedMassKg,
            string itemDefName,
            ShuttleExternalCargoTransactionSource source,
            int ticksGame,
            string diagnostics)
        {
            this.Available = available;
            this.Success = success;
            this.DryRun = dryRun;
            this.Kind = kind;
            this.FailureReason = failureReason;
            this.Message = message;
            this.RequestedCount = requestedCount;
            this.MatchedCount = matchedCount;
            this.AffectedCount = affectedCount;
            this.AffectedStackCount = affectedStackCount;
            this.AffectedMassKg = affectedMassKg;
            this.ItemDefName = itemDefName;
            this.Source = source;
            this.TicksGame = ticksGame;
            this.Diagnostics = diagnostics;
        }

        public bool Available { get; private set; }
        public bool Success { get; private set; }
        public bool DryRun { get; private set; }
        public ShuttleExternalCargoTransactionKind Kind { get; private set; }
        public ShuttleExternalCargoTransactionFailureReason FailureReason { get; private set; }
        public string Message { get; private set; }
        public int RequestedCount { get; private set; }
        public int MatchedCount { get; private set; }
        public int AffectedCount { get; private set; }
        public int AffectedStackCount { get; private set; }
        public float AffectedMassKg { get; private set; }
        public string ItemDefName { get; private set; }
        public ShuttleExternalCargoTransactionSource Source { get; private set; }
        public int TicksGame { get; private set; }
        public string Diagnostics { get; private set; }
    }
}
