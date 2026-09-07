using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatDiningOccupancyAccess
    {
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly Action ensureInitialized;
        private readonly Action reconcileDiningRecordsToContainedThings;
        private readonly HabitatDiningPawnDropDelegate tryDropDiningPawn;

        internal HabitatDiningOccupancyAccess(
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            Action ensureInitialized,
            Action reconcileDiningRecordsToContainedThings,
            HabitatDiningPawnDropDelegate tryDropDiningPawn)
        {
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.diningOccupants = diningOccupants;
            this.ensureInitialized = ensureInitialized;
            this.reconcileDiningRecordsToContainedThings = reconcileDiningRecordsToContainedThings;
            this.tryDropDiningPawn = tryDropDiningPawn;
        }

        internal ThingWithComps Host
        {
            get
            {
                return this.host;
            }
        }

        internal Map HostMap
        {
            get
            {
                return this.host != null ? this.host.Map : null;
            }
        }

        internal bool IsSpawnedHost
        {
            get
            {
                return this.host != null && this.host.Spawned;
            }
        }

        internal int DiningRecordCount
        {
            get
            {
                return this.diningOccupants.Count;
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal bool TryGetDiningHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.host, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsDining &&
                habitat.DiningSlots > 0 &&
                habitat.AllowsInventoryFood;
        }

        internal bool TryGetAnyDiningHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.host, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsDining &&
                habitat.DiningSlots > 0 &&
                (habitat.AllowsInventoryFood || habitat.AllowsCargoFoodWithdrawal);
        }

        internal bool TryGetCargoDiningHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.host, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsDining &&
                habitat.DiningSlots > 0 &&
                habitat.AllowsCargoFoodWithdrawal;
        }

        internal bool ContainsDiningPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return this.IsContainedDiningPawn(pawn);
        }

        internal bool IsContainedDiningPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedDiningPawn(
                this.habitatHeldThings,
                this.diningOccupants,
                pawn);
        }

        internal bool IsContainedDiningFood(Thing food)
        {
            return HabitatOccupancyQueryService.IsContainedDiningFood(
                this.habitatHeldThings,
                food);
        }

        internal ShuttleHabitatDiningOccupantRecord GetDiningRecord(int index)
        {
            return this.diningOccupants[index];
        }

        internal ShuttleHabitatDiningOccupantRecord AddDiningRecord(
            Pawn pawn,
            Thing food,
            int diningStartTick,
            int chewTicksTotal)
        {
            ShuttleHabitatDiningOccupantRecord record =
                new ShuttleHabitatDiningOccupantRecord(
                    pawn,
                    food,
                    diningStartTick,
                    chewTicksTotal);
            this.diningOccupants.Add(record);
            return record;
        }

        internal void RemoveDiningRecordAt(int index)
        {
            this.diningOccupants.RemoveAt(index);
        }

        internal void RemoveDiningRecord(ShuttleHabitatDiningOccupantRecord record)
        {
            this.diningOccupants.Remove(record);
        }

        internal void ReconcileDiningRecordsToContainedThings()
        {
            if (this.reconcileDiningRecordsToContainedThings != null)
            {
                this.reconcileDiningRecordsToContainedThings();
            }
        }

        internal bool TryDropDiningPawn(
            ShuttleHabitatDiningOccupantRecord record,
            Map map)
        {
            return this.tryDropDiningPawn != null &&
                this.tryDropDiningPawn(record, map);
        }

        internal ShuttlePassengerInternalTransferResult TryTransferCompletedPassenger(
            Pawn pawn,
            out string failureReason)
        {
            return HabitatPassengerInternalTransfer.TryTransfer(
                this.host,
                pawn,
                this.habitatHeldThings,
                out failureReason);
        }

        internal bool TryGetDiningFoodForPawn(Pawn pawn, out Thing food)
        {
            food = null;
            if (pawn == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return HabitatOccupancyQueryService.TryGetDiningFoodForPawn(
                this.habitatHeldThings,
                this.diningOccupants,
                pawn,
                out food);
        }

        internal bool TryTakeDiningFoodFromInventory(
            Pawn pawn,
            Thing inventoryFood,
            int count,
            out Thing diningMeal)
        {
            diningMeal = null;
            if (pawn == null ||
                pawn.inventory == null ||
                pawn.inventory.innerContainer == null ||
                inventoryFood == null ||
                inventoryFood.Destroyed ||
                !pawn.inventory.Contains(inventoryFood) ||
                !HabitatIngestUtility.IsUsableHabitatFood(pawn, inventoryFood))
            {
                return false;
            }

            int takeCount = count > 0 ? count : 1;
            if (takeCount > inventoryFood.stackCount)
            {
                takeCount = inventoryFood.stackCount;
            }

            Thing takenFood = pawn.inventory.innerContainer.Take(inventoryFood, takeCount);
            if (takenFood == null)
            {
                return false;
            }

            this.EnsureInitialized();
            if (!this.habitatHeldThings.TryAdd(takenFood, false))
            {
                this.RestoreDiningFoodToInventoryOrDrop(pawn, takenFood, pawn.Map);
                return false;
            }

            diningMeal = takenFood;
            return true;
        }

        internal void RestoreDiningFoodToInventoryOrDrop(
            Pawn pawn,
            Thing food,
            Map map)
        {
            if (food == null || food.Destroyed)
            {
                return;
            }

            if (pawn != null && pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                if (pawn.inventory.innerContainer.TryAddOrTransfer(food, false))
                {
                    return;
                }
            }

            CompShuttleHolderLaunchTransferState transferState = this.GetHolderTransferState();
            if (transferState != null)
            {
                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason;
                ThingOwner preferredOwner = pawn != null && pawn.inventory != null
                    ? pawn.inventory.innerContainer
                    : null;
                if (transferState.TryRecoverTransferThing(
                    food,
                    "Habitat dining food restore to inventory failed.",
                    preferredOwner,
                    this.habitatHeldThings,
                    map,
                    map != null ? this.GetEjectCell(map) : IntVec3.Invalid,
                    out recoveryStatus,
                    out recoveryFailureReason))
                {
                    if (recoveryStatus != ShuttleTransferRecoveryStatus.RecoveredToPreferredOwner)
                    {
                        Log.Warning("[CeleTech Shuttle] Habitat dining food restore used emergency recovery. food=" +
                            food +
                            " status=" +
                            recoveryStatus +
                            " note=" +
                            (recoveryFailureReason ?? "null"));
                    }

                    return;
                }

                Log.Error("[CeleTech Shuttle] Habitat dining food emergency recovery failed. food=" +
                    food +
                    " recovery=" +
                    (recoveryFailureReason ?? "null"));
            }

            if (map != null)
            {
                Thing resultingThing;
                if (this.IsContainedDiningFood(food))
                {
                    this.habitatHeldThings.TryDrop(
                        food,
                        this.GetEjectCell(map),
                        map,
                        ThingPlaceMode.Near,
                        out resultingThing,
                        null,
                        null);
                    return;
                }

                if (!ShuttleHabitatEjectDropUtility.TryPlaceThingNear(
                    food,
                    this.GetEjectCell(map),
                    map))
                {
                    Log.Error("[CeleTech Shuttle] Habitat dining food restore/drop failed after inventory rollback and emergency recovery failed. food=" +
                        food +
                        " map=" +
                        map);
                }
            }
        }

        internal bool TryResolveDiningStopPair(
            ShuttleHabitatDiningOccupantRecord record,
            Map map,
            string context)
        {
            if (!this.TryResolveDiningFoodForStop(record, map, context))
            {
                return false;
            }

            bool pawnResolved = this.IsDiningPawnSafelyOutOfHabitat(record);
            if (!pawnResolved)
            {
                string pawnRecoveryFailure;
                pawnResolved = this.TryRecoverDiningPawnForStopPath(
                    record,
                    map,
                    context,
                    out pawnRecoveryFailure);
                if (!pawnResolved)
                {
                    Log.Error("[CeleTech Shuttle] " +
                        (context ?? "Habitat dining stop") +
                        " resolved dining food but failed to resolve dining pawn; dining record preserved. pawnRecovery=" +
                        (pawnRecoveryFailure ?? "null") +
                        " " +
                        this.DescribeDiningStopState(record));
                    return false;
                }
            }

            return true;
        }

        internal bool TryResolveDiningFoodForStop(
            ShuttleHabitatDiningOccupantRecord record,
            Map map,
            string context)
        {
            if (record == null)
            {
                return false;
            }

            Pawn pawn = record.Pawn;
            Thing food = record.Food;
            if (this.IsContainedDiningFood(food))
            {
                if (map == null)
                {
                    Log.Warning("[CeleTech Shuttle] " +
                        (context ?? "Habitat dining stop") +
                        " could not resolve dining food because no map is available; dining record preserved. " +
                        this.DescribeDiningStopState(record));
                    return false;
                }

                this.RestoreDiningFoodToInventoryOrDrop(pawn, food, map);
            }

            if (this.IsDiningFoodSafelyOutAfterCargoCommitFailure(record))
            {
                return true;
            }

            Log.Error("[CeleTech Shuttle] " +
                (context ?? "Habitat dining stop") +
                " failed because dining food is not safely resolved; dining record preserved. " +
                this.DescribeDiningStopState(record));
            return false;
        }

        internal bool TryRecoverDiningPawnAfterCargoCommitFailure(
            ShuttleHabitatDiningOccupantRecord record,
            Map map,
            out string failureReason)
        {
            return this.TryRecoverDiningPawnForStopPath(
                record,
                map,
                "Habitat cargo dining commit failure",
                out failureReason);
        }

        internal bool IsDiningFoodSafelyOutAfterCargoCommitFailure(
            ShuttleHabitatDiningOccupantRecord record)
        {
            Thing food = record != null ? record.Food : null;
            if (this.IsContainedDiningFood(food))
            {
                return false;
            }

            return food == null ||
                food.Destroyed ||
                food.Spawned ||
                food.holdingOwner != null;
        }

        internal string DescribeDiningFoodRollbackState(
            ShuttleHabitatDiningOccupantRecord record)
        {
            return HabitatOccupancyDiagnostics.DescribeDiningFoodRollbackState(
                record,
                this.IsContainedDiningFood(record != null ? record.Food : null));
        }

        internal string DescribeDiningStopState(
            ShuttleHabitatDiningOccupantRecord record)
        {
            return HabitatOccupancyDiagnostics.DescribeDiningStopState(
                record,
                this.IsContainedDiningPawn(record != null ? record.Pawn : null),
                this.DescribeDiningFoodRollbackState(record));
        }

        internal PawnHolderTransferResult TryMovePawnIntoHabitatHolder(
            Pawn pawn,
            Map map,
            string operation)
        {
            CompShuttleHolderLaunchTransferState transferState = this.GetHolderTransferState();
            return ShuttlePawnHolderTransferUtility.TryMoveSpawnedPawnIntoHolderSafely(
                pawn,
                this.habitatHeldThings,
                map,
                this.GetEjectCell(map),
                transferState != null ? transferState.EmergencyRecoveryThings : null,
                operation);
        }

        internal void HandleFailedPawnHolderTransfer(
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
                if (transferResult.WasSelected && this.host != null)
                {
                    Find.Selector.Select(this.host, false, false);
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

        internal string GetPawnHolderTransferFailureReason(
            PawnHolderTransferResult transferResult,
            string defaultReason)
        {
            return HabitatOccupancyDiagnostics.GetPawnHolderTransferFailureReason(
                transferResult,
                defaultReason);
        }

        private bool TryRecoverDiningPawnForStopPath(
            ShuttleHabitatDiningOccupantRecord record,
            Map map,
            string context,
            out string failureReason)
        {
            failureReason = null;
            if (record == null)
            {
                failureReason = "dining record is null";
                return false;
            }

            Pawn pawn = record.Pawn;
            if (pawn == null || pawn.Destroyed)
            {
                failureReason = "dining pawn is null or destroyed";
                return false;
            }

            this.EnsureInitialized();
            if (!this.IsHeldPawn(pawn))
            {
                if (pawn.Spawned || pawn.holdingOwner != null)
                {
                    return true;
                }

                failureReason = "dining pawn is not in Habitat holder, map, or a confirmed owner. pawn=" + pawn;
                return false;
            }

            if (this.TryDropDiningPawn(record, map) &&
                !this.IsHeldPawn(pawn) &&
                (pawn.Spawned || pawn.holdingOwner != null))
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState transferState = this.GetHolderTransferState();
            if (transferState == null)
            {
                failureReason = "holder transfer state unavailable after TryDropDiningPawn failed. pawn=" + pawn;
                return false;
            }

            ShuttleTransferRecoveryStatus recoveryStatus;
            string recoveryFailureReason;
            if (transferState.TryRecoverTransferThing(
                pawn,
                context ?? "Habitat dining stop pawn recovery.",
                null,
                null,
                map,
                map != null ? this.GetEjectCell(map) : IntVec3.Invalid,
                out recoveryStatus,
                out recoveryFailureReason))
            {
                if (this.IsHeldPawn(pawn))
                {
                    failureReason = "emergency recovery reported success but pawn remains in Habitat holder. status=" +
                        recoveryStatus +
                        " pawn=" +
                        pawn;
                    return false;
                }

                Log.Warning("[CeleTech Shuttle] Habitat dining pawn recovered outside Habitat holder. context=" +
                    (context ?? "null") +
                    " status=" +
                    recoveryStatus +
                    " pawn=" +
                    pawn);
                return true;
            }

            failureReason = "TryDropDiningPawn and emergency recovery failed. pawn=" +
                pawn +
                " recovery=" +
                (recoveryFailureReason ?? "null");
            return false;
        }

        private bool IsDiningPawnSafelyOutOfHabitat(
            ShuttleHabitatDiningOccupantRecord record)
        {
            Pawn pawn = record != null ? record.Pawn : null;
            if (this.IsContainedDiningPawn(pawn))
            {
                return false;
            }

            return pawn == null ||
                pawn.Destroyed ||
                pawn.Spawned ||
                pawn.holdingOwner != null;
        }

        private bool IsHeldPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsHeldPawn(this.habitatHeldThings, pawn);
        }

        private IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.host, map);
        }

        private CompShuttleHolderLaunchTransferState GetHolderTransferState()
        {
            return this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
        }
    }
}
