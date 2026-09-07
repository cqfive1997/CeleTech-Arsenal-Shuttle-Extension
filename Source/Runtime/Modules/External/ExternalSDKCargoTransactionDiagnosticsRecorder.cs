using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionDiagnosticsRecorder
    {
        internal const int MaxRecentRecords = 128;
        private const int MaxSourceSummaries = 64;
        private const int MaxSourceFieldLength = 128;
        private const int MaxMessageLength = 512;

        private static readonly ExternalSDKCargoTransactionDiagnosticsRecorder instance =
            new ExternalSDKCargoTransactionDiagnosticsRecorder();

        private readonly object syncRoot = new object();
        private readonly List<ExternalSDKCargoTransactionDiagnosticEntry> recentEntries =
            new List<ExternalSDKCargoTransactionDiagnosticEntry>();
        private readonly Dictionary<ShuttleExternalCargoTransactionFailureReason, int> failureCounts =
            new Dictionary<ShuttleExternalCargoTransactionFailureReason, int>();
        private readonly List<SourceCounter> sourceCounters = new List<SourceCounter>();

        private int nextSequenceId;
        private int revision;
        private int totalAttemptCount;
        private int totalSuccessCount;
        private int totalFailureCount;
        private int quoteConsumeAttemptCount;
        private int consumeAttemptCount;
        private int quoteDepositAttemptCount;
        private int depositAttemptCount;
        private bool recordsTruncated;

        private ExternalSDKCargoTransactionDiagnosticsRecorder()
        {
        }

        internal static ExternalSDKCargoTransactionDiagnosticsRecorder Instance
        {
            get
            {
                return instance;
            }
        }

        internal void RecordSafe(
            ShuttleController controller,
            ShuttleExternalCargoTransactionResult result)
        {
            try
            {
                this.Record(controller, result);
            }
            catch
            {
                // Diagnostics must never change cargo transaction behavior.
            }
        }

        internal bool TryGetSnapshot(
            out ShuttleExternalCargoTransactionDiagnosticsSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                snapshot = this.BuildSnapshot();
                return true;
            }
            catch
            {
                snapshot = BuildUnavailableSnapshot("cargo transaction diagnostics unavailable");
                return false;
            }
        }

        internal ShuttleExternalCargoTransactionDiagnosticsSnapshot BuildUnavailableSnapshotForRead(
            string unavailableReason)
        {
            return BuildUnavailableSnapshot(unavailableReason);
        }

        private static ShuttleExternalCargoTransactionDiagnosticsSnapshot BuildUnavailableSnapshot(
            string unavailableReason)
        {
            return new ShuttleExternalCargoTransactionDiagnosticsSnapshot(
                false,
                Clamp(unavailableReason, MaxMessageLength),
                0,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                new List<ShuttleExternalCargoTransactionDiagnosticRecord>(),
                new List<ShuttleExternalCargoTransactionFailureSummary>(),
                new List<ShuttleExternalCargoTransactionSourceSummary>(),
                MaxRecentRecords,
                false);
        }

        private void Record(
            ShuttleController controller,
            ShuttleExternalCargoTransactionResult result)
        {
            ExternalSDKCargoTransactionDiagnosticEntry entry =
                this.BuildEntry(controller, result);

            lock (this.syncRoot)
            {
                this.revision++;
                this.totalAttemptCount++;
                if (entry.Success)
                {
                    this.totalSuccessCount++;
                }
                else
                {
                    this.totalFailureCount++;
                    this.AddFailure(entry.FailureReason);
                }

                this.AddKindAttempt(entry.Kind);
                this.AddSource(entry);
                if (this.recentEntries.Count >= MaxRecentRecords)
                {
                    this.recentEntries.RemoveAt(0);
                    this.recordsTruncated = true;
                }

                this.recentEntries.Add(entry);
            }
        }

        private ExternalSDKCargoTransactionDiagnosticEntry BuildEntry(
            ShuttleController controller,
            ShuttleExternalCargoTransactionResult result)
        {
            ShuttleExternalCargoTransactionSource source =
                result != null ? result.Source : null;
            return new ExternalSDKCargoTransactionDiagnosticEntry(
                this.NextSequenceId(),
                result != null
                    ? result.TicksGame
                    : ShuttleTickUtility.TicksGameOrMinusOne(),
                this.BuildHostStableId(controller),
                result != null
                    ? result.Kind
                    : ShuttleExternalCargoTransactionKind.Unknown,
                result != null && result.DryRun,
                result != null && result.Success,
                result != null && result.Available,
                result != null
                    ? result.FailureReason
                    : ShuttleExternalCargoTransactionFailureReason.InternalError,
                ClampSource(source != null ? source.OwnerPackageId : null),
                ClampSource(source != null ? source.LocalRequesterKey : null),
                ClampSource(source != null ? source.ModuleInstanceId : null),
                ClampSource(source != null ? source.ReasonKey : null),
                ClampSource(result != null ? result.ItemDefName : null),
                result != null ? result.RequestedCount : 0,
                result != null ? result.MatchedCount : 0,
                result != null ? result.AffectedCount : 0,
                result != null ? result.AffectedStackCount : 0,
                result != null ? result.AffectedMassKg : 0f,
                Clamp(result != null ? result.Message : null, MaxMessageLength));
        }

        private ShuttleExternalCargoTransactionDiagnosticsSnapshot BuildSnapshot()
        {
            lock (this.syncRoot)
            {
                List<ShuttleExternalCargoTransactionDiagnosticRecord> recentRecords =
                    new List<ShuttleExternalCargoTransactionDiagnosticRecord>();
                for (int i = 0; i < this.recentEntries.Count; i++)
                {
                    ExternalSDKCargoTransactionDiagnosticEntry entry =
                        this.recentEntries[i];
                    if (entry != null)
                    {
                        recentRecords.Add(entry.ToPublicRecord());
                    }
                }

                List<ShuttleExternalCargoTransactionFailureSummary> failureSummaries =
                    this.BuildFailureSummaries();
                List<ShuttleExternalCargoTransactionSourceSummary> sourceSummaries =
                    this.BuildSourceSummaries();

                return new ShuttleExternalCargoTransactionDiagnosticsSnapshot(
                    true,
                    null,
                    this.revision,
                    ShuttleTickUtility.TicksGameOrMinusOne(),
                    this.totalAttemptCount,
                    this.totalSuccessCount,
                    this.totalFailureCount,
                    this.quoteConsumeAttemptCount,
                    this.consumeAttemptCount,
                    this.quoteDepositAttemptCount,
                    this.depositAttemptCount,
                    recentRecords,
                    failureSummaries,
                    sourceSummaries,
                    MaxRecentRecords,
                    this.recordsTruncated);
            }
        }

        private List<ShuttleExternalCargoTransactionFailureSummary> BuildFailureSummaries()
        {
            List<ShuttleExternalCargoTransactionFailureSummary> summaries =
                new List<ShuttleExternalCargoTransactionFailureSummary>();
            Array values = Enum.GetValues(typeof(ShuttleExternalCargoTransactionFailureReason));
            for (int i = 0; i < values.Length; i++)
            {
                ShuttleExternalCargoTransactionFailureReason reason =
                    (ShuttleExternalCargoTransactionFailureReason)values.GetValue(i);
                int count;
                if (reason != ShuttleExternalCargoTransactionFailureReason.None &&
                    this.failureCounts.TryGetValue(reason, out count) &&
                    count > 0)
                {
                    summaries.Add(new ShuttleExternalCargoTransactionFailureSummary(
                        reason,
                        count));
                }
            }

            return summaries;
        }

        private List<ShuttleExternalCargoTransactionSourceSummary> BuildSourceSummaries()
        {
            List<ShuttleExternalCargoTransactionSourceSummary> summaries =
                new List<ShuttleExternalCargoTransactionSourceSummary>();
            for (int i = 0; i < this.sourceCounters.Count; i++)
            {
                SourceCounter counter = this.sourceCounters[i];
                if (counter != null)
                {
                    summaries.Add(counter.ToPublicSummary());
                }
            }

            return summaries;
        }

        private void AddFailure(
            ShuttleExternalCargoTransactionFailureReason failureReason)
        {
            if (failureReason == ShuttleExternalCargoTransactionFailureReason.None)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InternalError;
            }

            int count;
            this.failureCounts.TryGetValue(failureReason, out count);
            this.failureCounts[failureReason] = count + 1;
        }

        private void AddKindAttempt(ShuttleExternalCargoTransactionKind kind)
        {
            if (kind == ShuttleExternalCargoTransactionKind.QuoteConsume)
            {
                this.quoteConsumeAttemptCount++;
            }
            else if (kind == ShuttleExternalCargoTransactionKind.Consume)
            {
                this.consumeAttemptCount++;
            }
            else if (kind == ShuttleExternalCargoTransactionKind.QuoteDeposit)
            {
                this.quoteDepositAttemptCount++;
            }
            else if (kind == ShuttleExternalCargoTransactionKind.Deposit)
            {
                this.depositAttemptCount++;
            }
        }

        private void AddSource(ExternalSDKCargoTransactionDiagnosticEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            SourceCounter counter = this.FindSourceCounter(
                entry.OwnerPackageId,
                entry.LocalRequesterKey);
            if (counter == null)
            {
                if (this.sourceCounters.Count >= MaxSourceSummaries)
                {
                    return;
                }

                counter = new SourceCounter(
                    entry.OwnerPackageId,
                    entry.LocalRequesterKey);
                this.sourceCounters.Add(counter);
            }

            counter.Record(entry.Success);
        }

        private SourceCounter FindSourceCounter(
            string ownerPackageId,
            string localRequesterKey)
        {
            for (int i = 0; i < this.sourceCounters.Count; i++)
            {
                SourceCounter counter = this.sourceCounters[i];
                if (counter != null &&
                    counter.Matches(ownerPackageId, localRequesterKey))
                {
                    return counter;
                }
            }

            return null;
        }

        private int NextSequenceId()
        {
            lock (this.syncRoot)
            {
                unchecked
                {
                    this.nextSequenceId++;
                    if (this.nextSequenceId <= 0)
                    {
                        this.nextSequenceId = 1;
                    }
                }

                return this.nextSequenceId;
            }
        }

        private string BuildHostStableId(ShuttleController controller)
        {
            try
            {
                if (controller != null && controller.ShuttleHost != null)
                {
                    return "thingID:" + controller.ShuttleHost.thingIDNumber;
                }
            }
            catch
            {
            }

            return null;
        }

        private static string ClampSource(string value)
        {
            return Clamp(value, MaxSourceFieldLength);
        }

        private static string Clamp(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Trim();
            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, maxLength);
        }

        private sealed class SourceCounter
        {
            private readonly string ownerPackageId;
            private readonly string localRequesterKey;
            private int attemptCount;
            private int successCount;
            private int failureCount;

            internal SourceCounter(string ownerPackageId, string localRequesterKey)
            {
                this.ownerPackageId = ownerPackageId;
                this.localRequesterKey = localRequesterKey;
            }

            internal bool Matches(string ownerPackageId, string localRequesterKey)
            {
                return this.ownerPackageId == ownerPackageId &&
                    this.localRequesterKey == localRequesterKey;
            }

            internal void Record(bool success)
            {
                this.attemptCount++;
                if (success)
                {
                    this.successCount++;
                }
                else
                {
                    this.failureCount++;
                }
            }

            internal ShuttleExternalCargoTransactionSourceSummary ToPublicSummary()
            {
                return new ShuttleExternalCargoTransactionSourceSummary(
                    this.ownerPackageId,
                    this.localRequesterKey,
                    this.attemptCount,
                    this.successCount,
                    this.failureCount);
            }
        }
    }
}
