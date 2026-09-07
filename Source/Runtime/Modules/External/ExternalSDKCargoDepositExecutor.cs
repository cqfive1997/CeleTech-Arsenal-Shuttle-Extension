using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoDepositExecutor
    {
        private readonly ExternalSDKCargoDepositThingFactory thingFactory =
            new ExternalSDKCargoDepositThingFactory();

        internal bool TryDeposit(
            ShuttleController controller,
            ExternalSDKCargoDepositPlan plan,
            out int affectedCount,
            out int affectedStackCount,
            out float affectedMassKg,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            affectedCount = 0;
            affectedStackCount = 0;
            affectedMassKg = 0f;
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (controller == null || plan == null || plan.Targets == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "deposit plan is unavailable";
                return false;
            }

            if (plan.AffectedCount <= 0 || plan.Targets.Count == 0)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "deposit plan has no affected cargo";
                return false;
            }

            List<ExternalSDKCargoCreatedRecord> createdRecords;
            if (!this.thingFactory.TryCreate(
                    plan,
                    out createdRecords,
                    out failureReason,
                    out message))
            {
                return false;
            }

            List<ShuttleCargoLooseDepositEntry> depositEntries =
                new List<ShuttleCargoLooseDepositEntry>();
            for (int i = 0; i < createdRecords.Count; i++)
            {
                ExternalSDKCargoCreatedRecord record = createdRecords[i];
                ExternalSDKCargoDepositTarget target =
                    record != null ? record.Target : null;
                if (target == null ||
                    target.TransporterIndex < 0 ||
                    record.Thing == null ||
                    record.Thing.Destroyed ||
                    record.Thing.stackCount <= 0)
                {
                    this.thingFactory.DestroyCreated(createdRecords);
                    failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                    message = "deposit target became unavailable";
                    return false;
                }

                depositEntries.Add(new ShuttleCargoLooseDepositEntry(
                    target.TransporterIndex,
                    record.Thing));
            }

            IShuttleCargoResourceBroker cargoBroker =
                controller.GetCargoResourceBrokerForInternalTransactions();
            if (cargoBroker == null)
            {
                this.thingFactory.DestroyCreated(createdRecords);
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoUnavailable;
                message = "cargo transaction service is unavailable";
                return false;
            }

            int depositedCount;
            string depositFailure;
            if (!cargoBroker.TryDepositLooseThings(
                    depositEntries,
                    ShuttleCargoAccessRequirement.None,
                    "external SDK cargo deposit",
                    out depositedCount,
                    out depositFailure))
            {
                this.thingFactory.DestroyCreated(createdRecords);
                failureReason = !string.IsNullOrEmpty(depositFailure) &&
                    depositFailure.IndexOf(
                        "cleanup failed",
                        StringComparison.OrdinalIgnoreCase) >= 0
                    ? ShuttleExternalCargoTransactionFailureReason.CleanupFailed
                    : ShuttleExternalCargoTransactionFailureReason.DepositAddFailed;
                message = depositFailure ?? "normal cargo rejected deposit";
                return false;
            }

            affectedCount = depositedCount;
            if (affectedCount != plan.AffectedCount)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.PartialApplyRejected;
                message = "deposited count did not match deposit plan";
                return false;
            }

            affectedStackCount = createdRecords.Count;
            affectedMassKg = plan.AffectedMassKg;
            return true;
        }
    }
}
