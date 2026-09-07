using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal sealed class ShuttleHolderLaunchTransferTransaction
    {
        private readonly List<IShuttleHolderLaunchTransferParticipant> participants;
        private readonly List<IShuttleHolderLaunchTransferParticipant> exportedParticipants =
            new List<IShuttleHolderLaunchTransferParticipant>();

        internal ShuttleHolderLaunchTransferTransaction(
            List<IShuttleHolderLaunchTransferParticipant> participants)
        {
            this.participants = participants ?? new List<IShuttleHolderLaunchTransferParticipant>();
        }

        internal IReadOnlyList<IShuttleHolderLaunchTransferParticipant> Participants
        {
            get { return this.participants; }
        }

        internal IReadOnlyList<IShuttleHolderLaunchTransferParticipant> ExportedParticipants
        {
            get { return this.exportedParticipants; }
        }

        internal bool HasExportedParticipants
        {
            get { return this.exportedParticipants.Count > 0; }
        }

        internal bool WasParticipantExported(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < this.exportedParticipants.Count; i++)
            {
                IShuttleHolderLaunchTransferParticipant participant = this.exportedParticipants[i];
                if (participant != null && participant.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool CanUseAll(
            ThingWithComps host,
            TransportersArrivalAction arrivalAction,
            out string failureReason)
        {
            failureReason = null;
            this.LogParticipantOrder("validate", this.participants);
            for (int i = 0; i < this.participants.Count; i++)
            {
                IShuttleHolderLaunchTransferParticipant participant = this.participants[i];
                if (participant == null || !participant.NeedsTransfer(host))
                {
                    continue;
                }

                if (!participant.CanUse(host, arrivalAction, out failureReason))
                {
                    failureReason = "[CeleTech Shuttle] Holder transfer participant " +
                        participant.Key +
                        " refused launch: " +
                        (failureReason ?? "null");
                    return false;
                }
            }

            return true;
        }

        internal bool TryExportAll(
            ThingWithComps host,
            List<ShuttleLaunchCargoHandoff> handoffs,
            out ShuttleHolderLaunchTransferTransactionResult result)
        {
            result = ShuttleHolderLaunchTransferTransactionResult.Succeeded("No holder transfer participants required export.");
            this.exportedParticipants.Clear();
            this.LogParticipantOrder("export", this.participants);

            for (int i = 0; i < this.participants.Count; i++)
            {
                IShuttleHolderLaunchTransferParticipant participant = this.participants[i];
                if (participant == null || !participant.NeedsTransfer(host))
                {
                    continue;
                }

                string failureReason;
                if (participant.TryExport(host, handoffs, out failureReason))
                {
                    this.exportedParticipants.Add(participant);
                    if (Prefs.DevMode)
                    {
                        Log.Message("[CeleTech Shuttle] Holder transfer participant exported: " + participant.Key);
                    }

                    continue;
                }

                ShuttleHolderLaunchTransferParticipantRollbackResult failedRollback =
                    participant.TryRollback(
                        host,
                        handoffs,
                        host != null ? host.Map : null,
                        participant.Key + " export failure");
                string exportFailure = "[CeleTech Shuttle] Holder transfer participant export failed. key=" +
                    participant.Key +
                    " failure=" +
                    (failureReason ?? "null") +
                    " selfRollback=" +
                    (failedRollback != null ? failedRollback.Notice ?? "null" : "null");

                if (failedRollback != null && !failedRollback.HandoffsClear)
                {
                    result = ShuttleHolderLaunchTransferTransactionResult.Failed(
                        participant.Key,
                        false,
                        exportFailure);
                    return false;
                }

                ShuttleHolderLaunchTransferRollbackAggregate priorRollback =
                    this.RollbackExportedParticipants(
                    host,
                    handoffs,
                    host != null ? host.Map : null,
                    participant.Key + " export failure",
                    false);
                result = ShuttleHolderLaunchTransferTransactionResult.Failed(
                    participant.Key,
                    priorRollback.HandoffsClear,
                    exportFailure +
                        " priorRollbackSucceeded=" +
                        priorRollback.AllRollbackSucceeded +
                        " priorHandoffsClear=" +
                        priorRollback.HandoffsClear +
                        " priorRollback=" +
                        (priorRollback.Notice ?? "null"));
                return false;
            }

            result = ShuttleHolderLaunchTransferTransactionResult.Succeeded(
                "Exported holder transfer participants: " + this.DescribeExportedParticipants());
            return true;
        }

        internal bool TryRollbackExportedForLaunchFailure(
            ThingWithComps host,
            List<ShuttleLaunchCargoHandoff> handoffs,
            Map map,
            string context,
            out ShuttleHolderLaunchTransferTransactionResult result)
        {
            result = ShuttleHolderLaunchTransferTransactionResult.Succeeded("No holder transfer participants were exported.");
            this.LogParticipantOrder("rollback", this.exportedParticipants);
            for (int i = this.exportedParticipants.Count - 1; i >= 0; i--)
            {
                IShuttleHolderLaunchTransferParticipant participant = this.exportedParticipants[i];
                if (participant == null)
                {
                    continue;
                }

                ShuttleHolderLaunchTransferParticipantRollbackResult rollback =
                    participant.TryRollback(host, handoffs, map, context);
                string notice = rollback != null ? rollback.Notice : null;
                if (Prefs.DevMode && !string.IsNullOrEmpty(notice))
                {
                    Log.Warning("[CeleTech Shuttle] Holder transfer participant rolled back after " +
                        (context ?? "launch failure") +
                        ": key=" +
                        participant.Key +
                        " notice=" +
                        notice);
                }

                if (rollback != null && !rollback.HandoffsClear)
                {
                    result = ShuttleHolderLaunchTransferTransactionResult.Failed(
                        participant.Key,
                        false,
                        "[CeleTech Shuttle] Holder transfer rollback stopped before ordinary cargo rollback. key=" +
                            participant.Key +
                            " notice=" +
                            (notice ?? "null"));
                    return false;
                }
            }

            result = ShuttleHolderLaunchTransferTransactionResult.Succeeded(
                "Rolled back holder transfer participants after " +
                    (context ?? "launch failure") +
                    ": " +
                    this.DescribeExportedParticipants());
            return true;
        }

        private ShuttleHolderLaunchTransferRollbackAggregate RollbackExportedParticipants(
            ThingWithComps host,
            List<ShuttleLaunchCargoHandoff> handoffs,
            Map map,
            string context,
            bool stopOnBlockedHandoffs)
        {
            StringBuilder builder = new StringBuilder();
            bool allRollbackSucceeded = true;
            bool handoffsClear = true;
            for (int i = this.exportedParticipants.Count - 1; i >= 0; i--)
            {
                IShuttleHolderLaunchTransferParticipant participant = this.exportedParticipants[i];
                if (participant == null)
                {
                    continue;
                }

                ShuttleHolderLaunchTransferParticipantRollbackResult rollback =
                    participant.TryRollback(host, handoffs, map, context);
                if (rollback == null)
                {
                    allRollbackSucceeded = false;
                    handoffsClear = false;
                }
                else
                {
                    allRollbackSucceeded = allRollbackSucceeded && rollback.RollbackSucceeded;
                    handoffsClear = handoffsClear && rollback.HandoffsClear;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" ");
                }

                builder.Append(participant.Key);
                builder.Append("=");
                builder.Append(rollback != null ? rollback.Notice ?? "null" : "null");

                if (stopOnBlockedHandoffs && (rollback == null || !rollback.HandoffsClear))
                {
                    break;
                }
            }

            return new ShuttleHolderLaunchTransferRollbackAggregate(
                allRollbackSucceeded,
                handoffsClear,
                builder.ToString());
        }

        private string DescribeExportedParticipants()
        {
            return this.DescribeParticipants(this.exportedParticipants);
        }

        private void LogParticipantOrder(
            string phase,
            List<IShuttleHolderLaunchTransferParticipant> participantList)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Message("[CeleTech Shuttle] Holder transfer participant " +
                (phase ?? "phase") +
                " order: " +
                this.DescribeParticipants(participantList));
        }

        private string DescribeParticipants(List<IShuttleHolderLaunchTransferParticipant> participantList)
        {
            if (participantList == null || participantList.Count == 0)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < participantList.Count; i++)
            {
                IShuttleHolderLaunchTransferParticipant participant = participantList[i];
                if (participant == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(participant.Key);
            }

            return builder.Length > 0 ? builder.ToString() : "none";
        }
    }

    internal sealed class ShuttleHolderLaunchTransferTransactionResult
    {
        private ShuttleHolderLaunchTransferTransactionResult(
            bool success,
            string failedParticipantKey,
            bool handoffsClear,
            string message)
        {
            this.Success = success;
            this.FailedParticipantKey = failedParticipantKey;
            this.HandoffsClear = handoffsClear;
            this.Message = message;
        }

        internal bool Success { get; private set; }

        internal string FailedParticipantKey { get; private set; }

        internal bool HandoffsClear { get; private set; }

        internal string Message { get; private set; }

        internal static ShuttleHolderLaunchTransferTransactionResult Succeeded(string message)
        {
            return new ShuttleHolderLaunchTransferTransactionResult(
                true,
                null,
                true,
                message);
        }

        internal static ShuttleHolderLaunchTransferTransactionResult Failed(
            string failedParticipantKey,
            bool handoffsClear,
            string message)
        {
            return new ShuttleHolderLaunchTransferTransactionResult(
                false,
                failedParticipantKey,
                handoffsClear,
                message);
        }
    }

    internal sealed class ShuttleHolderLaunchTransferRollbackAggregate
    {
        internal ShuttleHolderLaunchTransferRollbackAggregate(
            bool allRollbackSucceeded,
            bool handoffsClear,
            string notice)
        {
            this.AllRollbackSucceeded = allRollbackSucceeded;
            this.HandoffsClear = handoffsClear;
            this.Notice = notice;
        }

        internal bool AllRollbackSucceeded { get; private set; }

        internal bool HandoffsClear { get; private set; }

        internal string Notice { get; private set; }
    }
}
