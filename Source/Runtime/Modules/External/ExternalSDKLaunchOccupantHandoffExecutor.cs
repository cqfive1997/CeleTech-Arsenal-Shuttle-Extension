using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffExecutor
    {
        internal bool TryHandoff(
            ShuttleController controller,
            ExternalSDKLaunchOccupantHandoffPlan plan,
            out float affectedMassKg,
            out ShuttleExternalLaunchOccupantHandoffFailureReason failureReason,
            out string message)
        {
            affectedMassKg = 0f;
            failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.None;
            message = null;

            if (controller == null || plan == null || plan.Request == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InvalidRequest;
                message = "handoff plan is unavailable";
                return false;
            }

            Pawn pawn = plan.Request.Pawn;
            if (pawn == null || pawn.Destroyed)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantUnavailable;
                message = "pawn became unavailable before handoff";
                return false;
            }

            if (pawn.Spawned)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantSpawned;
                message = "pawn became spawned before handoff";
                return false;
            }

            if (pawn.holdingOwner == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantNotHeld;
                message = "pawn is no longer held by a source holder";
                return false;
            }

            if (plan.Request.SourceOwner != null && pawn.holdingOwner != plan.Request.SourceOwner)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantUnavailable;
                message = "pawn source holder changed before handoff";
                return false;
            }

            if (plan.TargetTransporter == null || plan.TargetContents == null)
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.HandoffTargetUnavailable;
                message = "handoff target is unavailable";
                return false;
            }

            if (plan.TargetContents.Contains(pawn))
            {
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.OccupantAlreadyLoaded;
                message = "pawn is already loaded in shuttle cargo";
                return false;
            }

            try
            {
                if (!plan.TargetContents.TryAddOrTransfer(pawn, false))
                {
                    this.TryRecoverOwnerlessPawnToSource(pawn, plan.Request.SourceOwner, "target rejected pawn");
                    failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.HandoffAddFailed;
                    message = "normal cargo holder rejected pawn";
                    return false;
                }
            }
            catch (Exception exception)
            {
                this.TryRecoverOwnerlessPawnToSource(pawn, plan.Request.SourceOwner, "handoff exception");
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.InternalError;
                message = "handoff transfer failed: " + exception.GetType().Name;
                return false;
            }

            if (!plan.TargetContents.Contains(pawn) || pawn.holdingOwner != plan.TargetContents)
            {
                this.TryRecoverOwnerlessPawnToSource(pawn, plan.Request.SourceOwner, "target postcondition failed");
                failureReason = ShuttleExternalLaunchOccupantHandoffFailureReason.HandoffAddFailed;
                message = "handoff target did not retain pawn";
                return false;
            }

            this.NotifyThingAddedSafe(plan, pawn);
            controller.InvalidateCargoInventorySnapshotCaches();
            affectedMassKg = plan.AffectedMassKg;
            return true;
        }

        private void NotifyThingAddedSafe(
            ExternalSDKLaunchOccupantHandoffPlan plan,
            Pawn pawn)
        {
            try
            {
                if (plan != null && plan.TargetTransporter != null && pawn != null)
                {
                    plan.TargetTransporter.Notify_ThingAdded(pawn);
                }
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "[CeleTech Shuttle] Launch occupant handoff Notify_ThingAdded failed for pawn " +
                    (pawn != null ? pawn : null) +
                    ": " +
                    exception);
            }
        }

        private void TryRecoverOwnerlessPawnToSource(
            Pawn pawn,
            ThingOwner sourceOwner,
            string context)
        {
            if (pawn == null || pawn.Destroyed || pawn.Spawned || pawn.holdingOwner != null)
            {
                return;
            }

            if (sourceOwner != null && sourceOwner.TryAddOrTransfer(pawn, false))
            {
                Log.Warning(
                    "[CeleTech Shuttle] Recovered ownerless launch handoff pawn to source holder. context=" +
                    (context ?? "null") +
                    " pawn=" +
                    pawn);
                return;
            }

            Log.Error(
                "[CeleTech Shuttle] Launch handoff left a pawn without a holder and recovery failed. context=" +
                (context ?? "null") +
                " pawn=" +
                pawn);
        }
    }
}
