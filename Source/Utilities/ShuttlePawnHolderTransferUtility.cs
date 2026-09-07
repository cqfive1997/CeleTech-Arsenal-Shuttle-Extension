using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal enum PawnHolderTransferStatus
    {
        MovedToDestination,
        RecoveredToEmergencyOwner,
        RecoveredToMap,
        FatalOwnerless
    }

    internal sealed class PawnHolderTransferResult
    {
        internal PawnHolderTransferResult(
            PawnHolderTransferStatus status,
            bool wasSelected,
            string failureReason,
            string debugContext)
        {
            this.Status = status;
            this.WasSelected = wasSelected;
            this.FailureReason = failureReason;
            this.DebugContext = debugContext;
        }

        internal PawnHolderTransferStatus Status { get; private set; }
        internal bool WasSelected { get; private set; }
        internal string FailureReason { get; private set; }
        internal string DebugContext { get; private set; }
    }

    internal static class ShuttlePawnHolderTransferUtility
    {
        internal static PawnHolderTransferResult TryMoveSpawnedPawnIntoHolderSafely(
            Pawn pawn,
            ThingOwner<Thing> destination,
            Map fallbackMap,
            IntVec3 fallbackCell,
            ThingOwner<Thing> emergencyRecoveryOwner,
            string debugContext)
        {
            bool wasSelected = false;
            string failureReason = null;
            bool added = false;
            bool caughtException = false;

            if (pawn == null)
            {
                failureReason = "Pawn holder transfer failed: pawn is null. context=" +
                    (debugContext ?? "null");
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.FatalOwnerless,
                    false,
                    failureReason,
                    debugContext);
            }

            if (pawn.Destroyed)
            {
                failureReason = "Pawn holder transfer failed: pawn is destroyed. context=" +
                    (debugContext ?? "null") +
                    " pawn=" +
                    pawn;
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.FatalOwnerless,
                    false,
                    failureReason,
                    debugContext);
            }

            try
            {
                if (destination == null)
                {
                    failureReason = "Pawn holder transfer failed: destination holder is unavailable. context=" +
                        (debugContext ?? "null") +
                        " pawn=" +
                        pawn;
                }
                else
                {
                    wasSelected = pawn.DeSpawnOrDeselect(DestroyMode.Vanish);
                    added = destination.TryAddOrTransfer(pawn, true);
                    if (!added)
                    {
                        failureReason = "Pawn holder transfer failed: destination rejected pawn. context=" +
                            (debugContext ?? "null") +
                            " pawn=" +
                            pawn;
                    }
                }
            }
            catch (System.Exception exception)
            {
                caughtException = true;
                failureReason = "Pawn holder transfer threw during critical section. context=" +
                    (debugContext ?? "null") +
                    " pawn=" +
                    pawn +
                    " exception=" +
                    exception;
                Log.Error("[CeleTech Shuttle] " + failureReason);
            }
            finally
            {
                if (!added)
                {
                    PawnHolderTransferResult recoveryResult = EnsurePawnRecovered(
                        pawn,
                        destination,
                        fallbackMap,
                        fallbackCell,
                        emergencyRecoveryOwner,
                        debugContext,
                        caughtException,
                        wasSelected,
                        failureReason);
                    failureReason = recoveryResult.FailureReason;
                }
            }

            return ClassifyFinalState(
                pawn,
                destination,
                emergencyRecoveryOwner,
                wasSelected,
                failureReason,
                debugContext);
        }

        private static PawnHolderTransferResult EnsurePawnRecovered(
            Pawn pawn,
            ThingOwner<Thing> destination,
            Map fallbackMap,
            IntVec3 fallbackCell,
            ThingOwner<Thing> emergencyRecoveryOwner,
            string debugContext,
            bool fromException,
            bool wasSelected,
            string priorFailureReason)
        {
            if (pawn == null)
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.FatalOwnerless,
                    wasSelected,
                    AppendFailure(priorFailureReason, "Pawn recovery failed: pawn is null."),
                    debugContext);
            }

            if (pawn.Destroyed)
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.FatalOwnerless,
                    wasSelected,
                    AppendFailure(priorFailureReason, "Pawn recovery failed: pawn is destroyed. pawn=" + pawn),
                    debugContext);
            }

            if (destination != null && destination.Contains(pawn))
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.MovedToDestination,
                    wasSelected,
                    priorFailureReason,
                    debugContext);
            }

            if (pawn.Spawned)
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.RecoveredToMap,
                    wasSelected,
                    priorFailureReason,
                    debugContext);
            }

            if (emergencyRecoveryOwner != null && emergencyRecoveryOwner.Contains(pawn))
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.RecoveredToEmergencyOwner,
                    wasSelected,
                    priorFailureReason,
                    debugContext);
            }

            if (pawn.holdingOwner != null)
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.FatalOwnerless,
                    wasSelected,
                    AppendFailure(
                        priorFailureReason,
                        "Pawn holder transfer recovery could not confirm a safe owner. context=" +
                            (debugContext ?? "null") +
                            " pawn=" +
                            pawn +
                            " holdingOwner=" +
                            pawn.holdingOwner),
                    debugContext);
            }

            if (fallbackMap != null &&
                fallbackCell.IsValid &&
                TryPlacePawnNear(pawn, fallbackCell, fallbackMap))
            {
                Log.Warning("[CeleTech Shuttle] Pawn holder transfer recovered ownerless pawn by respawning near shuttle. context=" +
                    (debugContext ?? "null") +
                    " fromException=" +
                    fromException +
                    " pawn=" +
                    pawn);
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.RecoveredToMap,
                    wasSelected,
                    priorFailureReason,
                    debugContext);
            }

            if (emergencyRecoveryOwner != null &&
                emergencyRecoveryOwner.TryAddOrTransfer(pawn, false))
            {
                Log.Warning("[CeleTech Shuttle] Pawn holder transfer recovered ownerless pawn into emergency recovery holder. context=" +
                    (debugContext ?? "null") +
                    " fromException=" +
                    fromException +
                    " pawn=" +
                    pawn);
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.RecoveredToEmergencyOwner,
                    wasSelected,
                    priorFailureReason,
                    debugContext);
            }

            string failureReason = "Pawn holder transfer recovery failed: pawn is unspawned and ownerless. context=" +
                (debugContext ?? "null") +
                " fromException=" +
                fromException +
                " pawn=" +
                pawn +
                " mapAvailable=" +
                (fallbackMap != null) +
                " fallbackCell=" +
                fallbackCell;
            Log.Error("[CeleTech Shuttle] " + failureReason);
            return new PawnHolderTransferResult(
                PawnHolderTransferStatus.FatalOwnerless,
                wasSelected,
                AppendFailure(priorFailureReason, failureReason),
                debugContext);
        }

        private static PawnHolderTransferResult ClassifyFinalState(
            Pawn pawn,
            ThingOwner<Thing> destination,
            ThingOwner<Thing> emergencyRecoveryOwner,
            bool wasSelected,
            string failureReason,
            string debugContext)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.FatalOwnerless,
                    wasSelected,
                    failureReason,
                    debugContext);
            }

            if (destination != null && destination.Contains(pawn))
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.MovedToDestination,
                    wasSelected,
                    failureReason,
                    debugContext);
            }

            if (emergencyRecoveryOwner != null && emergencyRecoveryOwner.Contains(pawn))
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.RecoveredToEmergencyOwner,
                    wasSelected,
                    failureReason,
                    debugContext);
            }

            if (pawn.Spawned)
            {
                return new PawnHolderTransferResult(
                    PawnHolderTransferStatus.RecoveredToMap,
                    wasSelected,
                    failureReason,
                    debugContext);
            }

            string finalFailureReason = failureReason;
            if (pawn.holdingOwner != null)
            {
                finalFailureReason = AppendFailure(
                    finalFailureReason,
                    "Pawn holder transfer final state could not confirm destination/map/emergency owner. context=" +
                        (debugContext ?? "null") +
                        " pawn=" +
                        pawn +
                        " holdingOwner=" +
                        pawn.holdingOwner);
            }
            else
            {
                finalFailureReason = AppendFailure(
                    finalFailureReason,
                    "Pawn holder transfer final state is ownerless. context=" +
                        (debugContext ?? "null") +
                        " pawn=" +
                        pawn);
            }

            Log.Error("[CeleTech Shuttle] " + finalFailureReason);
            return new PawnHolderTransferResult(
                PawnHolderTransferStatus.FatalOwnerless,
                wasSelected,
                finalFailureReason,
                debugContext);
        }

        private static bool TryPlacePawnNear(Pawn pawn, IntVec3 cell, Map map)
        {
            try
            {
                return GenPlace.TryPlaceThing(
                    pawn,
                    cell,
                    map,
                    ThingPlaceMode.Near);
            }
            catch (System.Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Pawn emergency map placement threw: " + exception);
                return false;
            }
        }

        private static string AppendFailure(string existing, string addition)
        {
            if (string.IsNullOrEmpty(existing))
            {
                return addition;
            }

            if (string.IsNullOrEmpty(addition))
            {
                return existing;
            }

            return existing + " recovery=" + addition;
        }
    }
}
