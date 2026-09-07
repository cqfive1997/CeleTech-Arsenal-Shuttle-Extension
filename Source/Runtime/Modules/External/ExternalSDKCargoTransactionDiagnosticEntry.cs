using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionDiagnosticEntry
    {
        internal ExternalSDKCargoTransactionDiagnosticEntry(
            int sequenceId,
            int ticksGame,
            string hostStableId,
            ShuttleExternalCargoTransactionKind kind,
            bool dryRun,
            bool success,
            bool available,
            ShuttleExternalCargoTransactionFailureReason failureReason,
            string ownerPackageId,
            string localRequesterKey,
            string moduleInstanceId,
            string reasonKey,
            string itemDefName,
            int requestedCount,
            int matchedCount,
            int affectedCount,
            int affectedStackCount,
            float affectedMassKg,
            string message)
        {
            this.SequenceId = sequenceId;
            this.TicksGame = ticksGame;
            this.HostStableId = hostStableId;
            this.Kind = kind;
            this.DryRun = dryRun;
            this.Success = success;
            this.Available = available;
            this.FailureReason = failureReason;
            this.OwnerPackageId = ownerPackageId;
            this.LocalRequesterKey = localRequesterKey;
            this.ModuleInstanceId = moduleInstanceId;
            this.ReasonKey = reasonKey;
            this.ItemDefName = itemDefName;
            this.RequestedCount = requestedCount;
            this.MatchedCount = matchedCount;
            this.AffectedCount = affectedCount;
            this.AffectedStackCount = affectedStackCount;
            this.AffectedMassKg = affectedMassKg;
            this.Message = message;
        }

        internal int SequenceId { get; private set; }
        internal int TicksGame { get; private set; }
        internal string HostStableId { get; private set; }
        internal ShuttleExternalCargoTransactionKind Kind { get; private set; }
        internal bool DryRun { get; private set; }
        internal bool Success { get; private set; }
        internal bool Available { get; private set; }
        internal ShuttleExternalCargoTransactionFailureReason FailureReason { get; private set; }
        internal string OwnerPackageId { get; private set; }
        internal string LocalRequesterKey { get; private set; }
        internal string ModuleInstanceId { get; private set; }
        internal string ReasonKey { get; private set; }
        internal string ItemDefName { get; private set; }
        internal int RequestedCount { get; private set; }
        internal int MatchedCount { get; private set; }
        internal int AffectedCount { get; private set; }
        internal int AffectedStackCount { get; private set; }
        internal float AffectedMassKg { get; private set; }
        internal string Message { get; private set; }

        internal ShuttleExternalCargoTransactionDiagnosticRecord ToPublicRecord()
        {
            return new ShuttleExternalCargoTransactionDiagnosticRecord(
                this.SequenceId,
                this.TicksGame,
                this.HostStableId,
                this.Kind,
                this.DryRun,
                this.Success,
                this.Available,
                this.FailureReason,
                this.OwnerPackageId,
                this.LocalRequesterKey,
                this.ModuleInstanceId,
                this.ReasonKey,
                this.ItemDefName,
                this.RequestedCount,
                this.MatchedCount,
                this.AffectedCount,
                this.AffectedStackCount,
                this.AffectedMassKg,
                this.Message);
        }
    }
}
