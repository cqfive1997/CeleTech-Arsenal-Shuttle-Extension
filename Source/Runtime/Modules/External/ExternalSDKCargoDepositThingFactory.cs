using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoDepositThingFactory
    {
        internal bool TryCreate(
            ExternalSDKCargoDepositPlan plan,
            out List<ExternalSDKCargoCreatedRecord> createdRecords,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            createdRecords = new List<ExternalSDKCargoCreatedRecord>();
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (plan == null || plan.Request == null || plan.Targets == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                message = "deposit plan is unavailable";
                return false;
            }

            for (int i = 0; i < plan.Targets.Count; i++)
            {
                ExternalSDKCargoDepositTarget target = plan.Targets[i];
                if (target == null || target.StackCount <= 0)
                {
                    this.DestroyCreated(createdRecords);
                    failureReason = ShuttleExternalCargoTransactionFailureReason.InvalidRequest;
                    message = "deposit target is unavailable";
                    return false;
                }

                Thing thing = ThingMaker.MakeThing(plan.Request.ThingDef);
                if (thing == null)
                {
                    this.DestroyCreated(createdRecords);
                    failureReason = ShuttleExternalCargoTransactionFailureReason.ThingCreationFailed;
                    message = "failed to create deposit item";
                    return false;
                }

                thing.stackCount = target.StackCount;
                createdRecords.Add(new ExternalSDKCargoCreatedRecord(target, thing));
            }

            return true;
        }

        internal void DestroyCreated(List<ExternalSDKCargoCreatedRecord> createdRecords)
        {
            if (createdRecords == null)
            {
                return;
            }

            for (int i = 0; i < createdRecords.Count; i++)
            {
                Thing thing = createdRecords[i] != null
                    ? createdRecords[i].Thing
                    : null;
                this.DestroyLooseThing(thing);
            }
        }

        internal void DestroyLooseThing(Thing thing)
        {
            if (thing != null &&
                !thing.Destroyed &&
                thing.holdingOwner == null &&
                !thing.Spawned)
            {
                thing.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
