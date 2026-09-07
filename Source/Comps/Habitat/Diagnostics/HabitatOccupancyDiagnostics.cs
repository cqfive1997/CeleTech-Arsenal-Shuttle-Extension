using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatOccupancyDiagnostics
    {
        internal static string DescribeDiningFoodRollbackState(
            ShuttleHabitatDiningOccupantRecord record,
            bool isContainedDiningFood)
        {
            Thing food = record != null ? record.Food : null;
            return ShuttleHabitatDebugFormatter.DescribeDiningFoodRollbackState(
                food,
                isContainedDiningFood);
        }

        internal static string DescribeDiningStopState(
            ShuttleHabitatDiningOccupantRecord record,
            bool isContainedDiningPawn,
            string diningFoodRollbackState)
        {
            Pawn pawn = record != null ? record.Pawn : null;
            return ShuttleHabitatDebugFormatter.DescribeDiningStopState(
                pawn,
                isContainedDiningPawn,
                diningFoodRollbackState);
        }

        internal static string DescribeDiningFoodRollbackState(
            HabitatOccupancyDiagnosticsAccess access,
            ShuttleHabitatDiningOccupantRecord record)
        {
            Thing food = record != null ? record.Food : null;
            return DescribeDiningFoodRollbackState(
                record,
                access != null && access.IsContainedDiningFood(food));
        }

        internal static string DescribeDiningStopState(
            HabitatOccupancyDiagnosticsAccess access,
            ShuttleHabitatDiningOccupantRecord record)
        {
            Pawn pawn = record != null ? record.Pawn : null;
            return DescribeDiningStopState(
                record,
                access != null && access.IsContainedDiningPawn(pawn),
                DescribeDiningFoodRollbackState(access, record));
        }

        internal static string GetPawnHolderTransferFailureReason(
            PawnHolderTransferResult transferResult,
            string defaultReason)
        {
            if (transferResult != null && !string.IsNullOrEmpty(transferResult.FailureReason))
            {
                return transferResult.FailureReason;
            }

            return defaultReason;
        }

        internal static void HandleFailedPawnHolderTransfer(
            HabitatOccupancyDiagnosticsAccess access,
            Pawn pawn,
            PawnHolderTransferResult transferResult,
            string operation)
        {
            if (transferResult == null)
            {
                Log.Error("[CeleTech Shuttle] Habitat pawn holder transfer returned null result. operation=" +
                    (operation ?? "null"));
                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.FatalOwnerless)
            {
                Log.Error("[CeleTech Shuttle] Habitat pawn holder transfer fatal. operation=" +
                    (operation ?? "null") +
                    " context=" +
                    (transferResult.DebugContext ?? "null") +
                    " reason=" +
                    (transferResult.FailureReason ?? "null"));
                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.RecoveredToEmergencyOwner)
            {
                Log.Warning("[CeleTech Shuttle] Habitat pawn holder transfer recovered pawn to emergency owner; no Habitat record was created. operation=" +
                    (operation ?? "null") +
                    " context=" +
                    (transferResult.DebugContext ?? "null") +
                    " reason=" +
                    (transferResult.FailureReason ?? "null"));
                if (transferResult.WasSelected && access != null)
                {
                    access.SelectHostIfAvailable();
                }

                return;
            }

            if (transferResult.Status == PawnHolderTransferStatus.RecoveredToMap &&
                transferResult.WasSelected &&
                pawn != null &&
                pawn.Spawned)
            {
                Find.Selector.Select(pawn, false, false);
            }
        }

        internal static void LogDiningRecordPreservedWithoutMap(
            ShuttleHabitatDiningOccupantRecord record,
            bool pawnMissing,
            bool foodMissing)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Warning(
                HabitatOccupancyDebugFormatter.FormatDiningRecordPreservedWithoutMap(
                    record,
                    pawnMissing,
                    foodMissing));
        }

        internal static void LogOrphanDiningFoodPreservedWithoutMap(Thing food)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Warning(
                HabitatOccupancyDebugFormatter.FormatOrphanDiningFoodPreservedWithoutMap(
                    food));
        }

        internal static void LogUnknownHeldPawnPreservedWithoutMap(Pawn pawn)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Warning(
                HabitatOccupancyDebugFormatter.FormatUnknownHeldPawnPreservedWithoutMap(
                    pawn));
        }

        internal static void LogJoyRecordClearedWithoutMap(
            ShuttleHabitatJoyOccupantRecord record,
            bool pawnDead,
            bool joyKindMissing)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            Log.Warning(
                HabitatOccupancyDebugFormatter.FormatJoyRecordClearedWithoutMap(
                    record,
                    pawnDead,
                    joyKindMissing));
        }
    }
}
