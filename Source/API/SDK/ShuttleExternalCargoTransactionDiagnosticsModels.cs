using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public sealed class ShuttleExternalCargoTransactionDiagnosticsSnapshot
    {
        public ShuttleExternalCargoTransactionDiagnosticsSnapshot(
            bool available,
            string unavailableReason,
            int revision,
            int ticksGame,
            int totalAttemptCount,
            int totalSuccessCount,
            int totalFailureCount,
            int quoteConsumeAttemptCount,
            int consumeAttemptCount,
            int quoteDepositAttemptCount,
            int depositAttemptCount,
            IEnumerable<ShuttleExternalCargoTransactionDiagnosticRecord> recentRecords,
            IEnumerable<ShuttleExternalCargoTransactionFailureSummary> failureSummaries,
            IEnumerable<ShuttleExternalCargoTransactionSourceSummary> sourceSummaries,
            int maxRecentRecords,
            bool recordsTruncated)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.Revision = revision;
            this.TicksGame = ticksGame;
            this.TotalAttemptCount = totalAttemptCount;
            this.TotalSuccessCount = totalSuccessCount;
            this.TotalFailureCount = totalFailureCount;
            this.QuoteConsumeAttemptCount = quoteConsumeAttemptCount;
            this.ConsumeAttemptCount = consumeAttemptCount;
            this.QuoteDepositAttemptCount = quoteDepositAttemptCount;
            this.DepositAttemptCount = depositAttemptCount;
            this.RecentRecords = ShuttleExternalSDKCollections.Copy(recentRecords);
            this.FailureSummaries = ShuttleExternalSDKCollections.Copy(failureSummaries);
            this.SourceSummaries = ShuttleExternalSDKCollections.Copy(sourceSummaries);
            this.MaxRecentRecords = maxRecentRecords;
            this.RecordsTruncated = recordsTruncated;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public int Revision { get; private set; }
        public int TicksGame { get; private set; }
        public int TotalAttemptCount { get; private set; }
        public int TotalSuccessCount { get; private set; }
        public int TotalFailureCount { get; private set; }
        public int QuoteConsumeAttemptCount { get; private set; }
        public int ConsumeAttemptCount { get; private set; }
        public int QuoteDepositAttemptCount { get; private set; }
        public int DepositAttemptCount { get; private set; }
        public IReadOnlyList<ShuttleExternalCargoTransactionDiagnosticRecord> RecentRecords { get; private set; }
        public IReadOnlyList<ShuttleExternalCargoTransactionFailureSummary> FailureSummaries { get; private set; }
        public IReadOnlyList<ShuttleExternalCargoTransactionSourceSummary> SourceSummaries { get; private set; }
        public int MaxRecentRecords { get; private set; }
        public bool RecordsTruncated { get; private set; }
    }

    public sealed class ShuttleExternalCargoTransactionDiagnosticRecord
    {
        public ShuttleExternalCargoTransactionDiagnosticRecord(
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

        public int SequenceId { get; private set; }
        public int TicksGame { get; private set; }
        public string HostStableId { get; private set; }
        public ShuttleExternalCargoTransactionKind Kind { get; private set; }
        public bool DryRun { get; private set; }
        public bool Success { get; private set; }
        public bool Available { get; private set; }
        public ShuttleExternalCargoTransactionFailureReason FailureReason { get; private set; }
        public string OwnerPackageId { get; private set; }
        public string LocalRequesterKey { get; private set; }
        public string ModuleInstanceId { get; private set; }
        public string ReasonKey { get; private set; }
        public string ItemDefName { get; private set; }
        public int RequestedCount { get; private set; }
        public int MatchedCount { get; private set; }
        public int AffectedCount { get; private set; }
        public int AffectedStackCount { get; private set; }
        public float AffectedMassKg { get; private set; }
        public string Message { get; private set; }
    }

    public sealed class ShuttleExternalCargoTransactionFailureSummary
    {
        public ShuttleExternalCargoTransactionFailureSummary(
            ShuttleExternalCargoTransactionFailureReason failureReason,
            int count)
        {
            this.FailureReason = failureReason;
            this.Count = count;
        }

        public ShuttleExternalCargoTransactionFailureReason FailureReason { get; private set; }
        public int Count { get; private set; }
    }

    public sealed class ShuttleExternalCargoTransactionSourceSummary
    {
        public ShuttleExternalCargoTransactionSourceSummary(
            string ownerPackageId,
            string localRequesterKey,
            int attemptCount,
            int successCount,
            int failureCount)
        {
            this.OwnerPackageId = ownerPackageId;
            this.LocalRequesterKey = localRequesterKey;
            this.AttemptCount = attemptCount;
            this.SuccessCount = successCount;
            this.FailureCount = failureCount;
        }

        public string OwnerPackageId { get; private set; }
        public string LocalRequesterKey { get; private set; }
        public int AttemptCount { get; private set; }
        public int SuccessCount { get; private set; }
        public int FailureCount { get; private set; }
    }
}
