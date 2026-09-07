using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatDiningOccupancyService
    {
        private const int MinimumDiningChewTicks = 60;

            internal static bool TryEnterForDining(
                HabitatDiningOccupancyAccess access,
                Pawn pawn,
                Thing inventoryFood,
                int count,
                out string failReason)
            {
                failReason = null;
                if (pawn == null || access.Host == null || !access.IsSpawnedHost)
                {
                    failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                    return false;
                }

                HabitatProfile habitat;
                if (!access.TryGetDiningHabitat(out habitat))
                {
                    failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                    return false;
                }

                string useFailReason;
                if (!HabitatUtility.CanUseHabitatForDining(pawn, access.Host, out useFailReason))
                {
                    failReason = useFailReason;
                    return false;
                }

                int currentUsers = HabitatUtility.CountCurrentHabitatDiningUsers(access.Host, pawn);
                if (currentUsers >= habitat.DiningSlots)
                {
                    failReason = "CT_Shuttle_HabitatDiningSlotsFull".Translate().ToString();
                    return false;
                }

                Thing diningMeal;
                if (!TryTakeDiningFoodFromInventory(access, pawn, inventoryFood, count, out diningMeal))
                {
                    failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                    return false;
                }

                access.EnsureInitialized();
                Map map = pawn.Map;
                PawnHolderTransferResult transferResult =
                    access.TryMovePawnIntoHabitatHolder(
                    pawn,
                    map,
                    "Habitat inventory dining entry");
                if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
                {
                    RestoreDiningFoodToInventoryOrDrop(access, pawn, diningMeal, map);
                    access.HandleFailedPawnHolderTransfer(pawn, transferResult, "Habitat inventory dining entry");
                    failReason = access.GetPawnHolderTransferFailureReason(
                        transferResult,
                        "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString());
                    return false;
                }

                access.AddDiningRecord(
                    pawn,
                    diningMeal,
                    Find.TickManager.TicksGame,
                    ComputeDiningChewTicks(pawn, diningMeal));

                if (transferResult.WasSelected)
                {
                    Find.Selector.Select(access.Host, false, false);
                }

                return true;
            }

            internal static bool TryEnterForDiningFromCargoWithdrawal(
                HabitatDiningOccupancyAccess access,
                Pawn pawn,
                HabitatFoodWithdrawal withdrawal,
                out string failReason)
            {
                failReason = null;
                if (withdrawal == null)
                {
                    failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                    return false;
                }

                if (pawn == null ||
                    withdrawal.Food == null ||
                    withdrawal.Food.Destroyed ||
                    access.Host == null ||
                    !access.IsSpawnedHost)
                {
                    withdrawal.TryRollback();
                    failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                    return false;
                }

                HabitatProfile habitat;
                if (!access.TryGetCargoDiningHabitat(out habitat))
                {
                    withdrawal.TryRollback();
                    failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                    return false;
                }

                string useFailReason;
                if (!HabitatUtility.CanUseHabitatForDining(pawn, access.Host, out useFailReason))
                {
                    withdrawal.TryRollback();
                    failReason = useFailReason;
                    return false;
                }

                int currentUsers = HabitatUtility.CountCurrentHabitatDiningUsers(access.Host, pawn);
                if (currentUsers >= habitat.DiningSlots)
                {
                    withdrawal.TryRollback();
                    failReason = "CT_Shuttle_HabitatDiningSlotsFull".Translate().ToString();
                    return false;
                }

                access.EnsureInitialized();
                Thing diningMeal = withdrawal.Food;
                if (!access.IsContainedDiningFood(diningMeal))
                {
                    withdrawal.TryRollback();
                    failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                    return false;
                }

                Map map = pawn.Map;
                PawnHolderTransferResult transferResult =
                    access.TryMovePawnIntoHabitatHolder(
                    pawn,
                    map,
                    "Habitat cargo dining entry");
                if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
                {
                    withdrawal.TryRollback();
                    access.HandleFailedPawnHolderTransfer(pawn, transferResult, "Habitat cargo dining entry");
                    failReason = access.GetPawnHolderTransferFailureReason(
                        transferResult,
                        "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString());
                    return false;
                }

                ShuttleHabitatDiningOccupantRecord record = access.AddDiningRecord(
                    pawn,
                    diningMeal,
                    Find.TickManager.TicksGame,
                    ComputeDiningChewTicks(pawn, diningMeal));

                if (!withdrawal.TryCommit())
                {
                    string pawnRecoveryFailure;
                    bool pawnRecovered = access.TryRecoverDiningPawnAfterCargoCommitFailure(
                        record,
                        map,
                        out pawnRecoveryFailure);
                    bool foodRollbackSucceeded = withdrawal.TryRollback();
                    bool foodRollbackSafe = foodRollbackSucceeded ||
                        access.IsDiningFoodSafelyOutAfterCargoCommitFailure(record);
                    if (pawnRecovered && foodRollbackSafe)
                    {
                        if (!foodRollbackSucceeded)
                        {
                            Log.Warning("[CeleTech Shuttle] Habitat cargo dining commit failed; food rollback reported failure but food is no longer in the Habitat holder and has a safe owner. Removing dining record. " +
                                access.DescribeDiningFoodRollbackState(record));
                        }

                        access.RemoveDiningRecord(record);
                        record.StopDining();
                        if (transferResult.WasSelected)
                        {
                            if (pawn != null && pawn.Spawned)
                            {
                                Find.Selector.Select(pawn, false, false);
                            }
                            else if (access.Host != null)
                            {
                                Find.Selector.Select(access.Host, false, false);
                            }
                        }
                    }
                    else if (pawnRecovered)
                    {
                        Log.Error("[CeleTech Shuttle] Habitat cargo dining commit failed; pawn was recovered but food rollback failed. Dining record was preserved to avoid orphan holder food. " +
                            access.DescribeDiningFoodRollbackState(record));
                    }
                    else
                    {
                        Log.Error("[CeleTech Shuttle] Habitat cargo dining commit failed and pawn recovery failed; dining record was preserved to avoid orphan holder pawn. " +
                            (pawnRecoveryFailure ?? "null") +
                            " foodRollbackSucceeded=" +
                            foodRollbackSucceeded +
                            " " +
                            access.DescribeDiningFoodRollbackState(record));
                    }

                    failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                    return false;
                }

                if (transferResult.WasSelected)
                {
                    Find.Selector.Select(access.Host, false, false);
                }

                return true;
            }

            internal static void TickDiningOccupants(HabitatDiningOccupancyAccess access)
            {
                access.EnsureInitialized();
                access.ReconcileDiningRecordsToContainedThings();
                if (access.DiningRecordCount == 0)
                {
                    return;
                }

                HabitatProfile habitat;
                bool habitatAvailable = access.TryGetAnyDiningHabitat(out habitat);
                Map map = access.HostMap;

                access.ReconcileDiningRecordsToContainedThings();

                for (int i = access.DiningRecordCount - 1; i >= 0; i--)
                {
                    ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                    Pawn pawn = record != null ? record.Pawn : null;
                    Thing food = record != null ? record.Food : null;

                    if (!access.IsContainedDiningPawn(pawn))
                    {
                        if (TryAbortDining(access, record, map))
                        {
                            continue;
                        }

                        Log.Error("[CeleTech Shuttle] Habitat dining tick found dining pawn outside holder, but abort recovery did not fully resolve pawn/food pair; dining record was preserved. " +
                            access.DescribeDiningStopState(record));
                        continue;
                    }

                    if (!habitatAvailable ||
                        pawn.needs == null ||
                        pawn.needs.food == null ||
                        !access.IsContainedDiningFood(food))
                    {
                        TryAbortDining(access, record, map);
                        continue;
                    }

                    record.TickChew();
                    if (record.ChewTicksLeft <= 0)
                    {
                        TryCompleteDining(access, record, map, habitat);
                    }
                }
            }

            internal static bool TryCompleteDining(
                HabitatDiningOccupancyAccess access,
                ShuttleHabitatDiningOccupantRecord record,
                Map map,
                HabitatProfile habitat)
            {
                if (record == null)
                {
                    return false;
                }

                Pawn pawn = record.Pawn;
                Thing food = record.Food;
                if (!access.IsContainedDiningPawn(pawn))
                {
                    access.RemoveDiningRecord(record);
                    return true;
                }

                if (map == null)
                {
                    return false;
                }

                if (access.IsContainedDiningFood(food) && !record.IsFinalized)
                {
                    if (!HabitatIngestUtility.FinalizeHabitatIngest(pawn, food, habitat))
                    {
                        return TryAbortDining(access, record, map);
                    }

                    record.MarkFinalized();
                }

                if (!access.TryResolveDiningFoodForStop(
                    record,
                    map,
                    "Habitat dining completion"))
                {
                    return false;
                }

                string transferFailureReason;
                ShuttlePassengerInternalTransferResult transferResult =
                    access.TryTransferCompletedPassenger(
                        pawn,
                        out transferFailureReason);
                if (transferResult == ShuttlePassengerInternalTransferResult.UnsafeOwnerState)
                {
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] Habitat dining completion stopped because internal passenger transfer could not prove pawn ownership. pawn=" +
                            pawn +
                            " reason=" +
                            (transferFailureReason ?? "null"),
                        832301 + pawn.thingIDNumber);
                    return false;
                }

                if (transferResult == ShuttlePassengerInternalTransferResult.Transferred)
                {
                    record.StopDining();
                    access.RemoveDiningRecord(record);
                    return true;
                }

                if (!TryResolveDiningStopPair(access, record, map, "Habitat dining completion"))
                {
                    return false;
                }

                record.StopDining();
                access.RemoveDiningRecord(record);
                return true;
            }

            internal static bool TryAbortDining(
                HabitatDiningOccupancyAccess access,
                ShuttleHabitatDiningOccupantRecord record,
                Map map)
            {
                if (record == null)
                {
                    return false;
                }

                if (!TryResolveDiningStopPair(access, record, map, "Habitat dining abort"))
                {
                    return false;
                }

                record.StopDining();
                access.RemoveDiningRecord(record);
                return true;
            }

            internal static bool TryResolveDiningStopPair(
                HabitatDiningOccupancyAccess access,
                ShuttleHabitatDiningOccupantRecord record,
                Map map,
                string context)
            {
                return access.TryResolveDiningStopPair(record, map, context);
            }

            internal static bool TryTakeDiningFoodFromInventory(
                HabitatDiningOccupancyAccess access,
                Pawn pawn,
                Thing inventoryFood,
                int count,
                out Thing diningMeal)
            {
                return access.TryTakeDiningFoodFromInventory(
                    pawn,
                    inventoryFood,
                    count,
                    out diningMeal);
            }

            internal static void RestoreDiningFoodToInventoryOrDrop(
                HabitatDiningOccupancyAccess access,
                Pawn pawn,
                Thing food,
                Map map)
            {
                access.RestoreDiningFoodToInventoryOrDrop(
                    pawn,
                    food,
                    map);
            }

            internal static int ComputeDiningChewTicks(Pawn pawn, Thing food)
            {
                if (food == null || food.def == null || food.def.ingestible == null)
                {
                    return MinimumDiningChewTicks;
                }

                int baseTicks = food.def.ingestible.baseIngestTicks;
                if (baseTicks < MinimumDiningChewTicks)
                {
                    baseTicks = MinimumDiningChewTicks;
                }

                if (pawn != null && food.def.ingestible.useEatingSpeedStat)
                {
                    float eatingSpeed = pawn.GetStatValue(StatDefOf.EatingSpeed);
                    if (eatingSpeed > 0f)
                    {
                        baseTicks = (int)(baseTicks / eatingSpeed);
                    }
                }

                return baseTicks < 1 ? 1 : baseTicks;
            }
    }
}
