using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleHabitatOccupancy
    {
        // Sleep

        internal bool ContainsSleepingPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return this.IsContainedPawn(pawn);
        }

        // Sleep entry point used by Habitat jobs. The pawn is despawned into the
        // shuttle-owned holder so launch transfer can treat it as holder state,
        // not as map cargo.
        internal bool TryEnterForSleep(Pawn pawn, out string failReason)
        {
            return HabitatSleepOccupancyService.TryEnterForSleep(
                this.CreateSleepAccess(),
                pawn,
                out failReason);
        }

        private void TickSleepingOccupants()
        {
            HabitatSleepOccupancyService.TickSleepingOccupants(this.CreateSleepAccess());
        }

        private bool TryEject(ShuttleHabitatOccupantRecord record, Map map, bool completedRest)
        {
            return HabitatEjectionService.TryEject(
                this.CreateEjectionAccess(),
                record,
                map,
                completedRest);
        }

        private HabitatSleepOccupancyAccess CreateSleepAccess()
        {
            this.EnsureInitialized();
            return new HabitatSleepOccupancyAccess(
                this.parent,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.EnsureInitialized,
                this.ReconcileRecordsToContainedPawns,
                this.TryEject);
        }

        // Joy

        internal bool ContainsJoyPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            return this.CreateJoyAccess().ContainsJoyPawn(pawn);
        }

        // Recreation entry point. Joy pawns are held in the shared Habitat holder,
        // but their progress and kind stay in a joy-specific record.
        internal bool TryEnterForJoy(
            Pawn pawn,
            JoyKindDef joyKind,
            float joyGainRate,
            int maxJoyTicks,
            out string failReason)
        {
            return HabitatJoyOccupancyService.TryEnterForJoy(
                this.CreateJoyAccess(),
                pawn,
                joyKind,
                joyGainRate,
                maxJoyTicks,
                out failReason);
        }

        internal bool TryGetJoyKindForPawn(Pawn pawn, out JoyKindDef joyKind)
        {
            if (pawn == null)
            {
                joyKind = null;
                return false;
            }

            return this.CreateJoyAccess().TryGetJoyKindForPawn(pawn, out joyKind);
        }

        private void TickJoyOccupants()
        {
            HabitatJoyOccupancyService.TickJoyOccupants(this.CreateJoyAccess());
        }

        private bool TryCompleteJoy(
            ShuttleHabitatJoyOccupantRecord record,
            Map map,
            HabitatProfile habitat)
        {
            return HabitatJoyOccupancyService.TryCompleteJoy(
                this.CreateJoyAccess(),
                record,
                map,
                habitat);
        }

        private bool TryAbortJoy(ShuttleHabitatJoyOccupantRecord record, Map map)
        {
            return HabitatJoyOccupancyService.TryAbortJoy(
                this.CreateJoyAccess(),
                record,
                map);
        }

        private bool TryDropJoyPawn(
            ShuttleHabitatJoyOccupantRecord record,
            Map map,
            out Pawn droppedPawn)
        {
            return HabitatEjectionService.TryDropJoyPawn(
                this.CreateEjectionAccess(),
                record,
                map,
                out droppedPawn);
        }

        private bool TryGetJoyHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.parent, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsJoy &&
                habitat.JoySlots > 0 &&
                habitat.JoyKinds != null &&
                habitat.JoyKinds.Count > 0;
        }

        private HabitatJoyOccupancyAccess CreateJoyAccess()
        {
            this.EnsureInitialized();
            return new HabitatJoyOccupancyAccess(
                this.parent,
                this.habitatHeldThings,
                this.joyOccupants,
                this.EnsureInitialized,
                this.ReconcileJoyRecordsToContainedPawns,
                this.TryDropJoyPawn);
        }

        // Dining

        internal bool ContainsDiningPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            return this.CreateDiningAccess().ContainsDiningPawn(pawn);
        }

        // Dining from pawn inventory: the pawn and the exact meal Thing become a
        // paired record. Export/restore must keep them together.
        internal bool TryEnterForDining(Pawn pawn, Thing inventoryFood, int count, out string failReason)
        {
            return HabitatDiningOccupancyService.TryEnterForDining(
                this.CreateDiningAccess(),
                pawn,
                inventoryFood,
                count,
                out failReason);
        }

        internal bool TryGetDiningFoodDestinationForCargoWithdrawal(out ThingOwner<Thing> destination)
        {
            destination = null;
            if (this.parent == null || !this.parent.Spawned)
            {
                return false;
            }

            this.EnsureInitialized();
            // Legacy Habitat-internal bridge for the current cargo food transaction.
            // External job paths should use TryBeginDiningCargoFoodWithdrawal instead.
            destination = this.habitatHeldThings;
            return destination != null;
        }

        internal bool TryBeginDiningCargoFoodWithdrawal(
            Pawn pawn,
            HabitatProfile habitat,
            IShuttleHabitatFoodSource foodSource,
            out HabitatFoodWithdrawal withdrawal,
            out string failReason)
        {
            withdrawal = null;
            failReason = null;
            if (foodSource == null)
            {
                failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                return false;
            }

            ThingOwner<Thing> destination;
            if (!this.TryGetDiningFoodDestinationForCargoWithdrawal(out destination))
            {
                failReason = "CT_Shuttle_HabitatDiningUnavailable".Translate().ToString();
                return false;
            }

            if (!foodSource.TryBeginHabitatFoodWithdrawal(
                this.parent,
                pawn,
                habitat,
                destination,
                "Habitat dining cargo food",
                out withdrawal))
            {
                failReason = "CT_Shuttle_HabitatNoFood".Translate().ToString();
                return false;
            }

            return true;
        }

        // Dining from loaded cargo. The withdrawal object owns rollback until this
        // method has both the pawn and food safely represented as one dining record.
        internal bool TryEnterForDiningFromCargoWithdrawal(
            Pawn pawn,
            HabitatFoodWithdrawal withdrawal,
            out string failReason)
        {
            return HabitatDiningOccupancyService.TryEnterForDiningFromCargoWithdrawal(
                this.CreateDiningAccess(),
                pawn,
                withdrawal,
                out failReason);
        }

        internal bool TryGetDiningFoodForPawn(Pawn pawn, out Thing food)
        {
            if (pawn == null)
            {
                food = null;
                return false;
            }

            return this.CreateDiningAccess().TryGetDiningFoodForPawn(pawn, out food);
        }

        private void TickDiningOccupants()
        {
            HabitatDiningOccupancyService.TickDiningOccupants(this.CreateDiningAccess());
        }

        private bool TryCompleteDining(
            ShuttleHabitatDiningOccupantRecord record,
            Map map,
            HabitatProfile habitat)
        {
            return HabitatDiningOccupancyService.TryCompleteDining(
                this.CreateDiningAccess(),
                record,
                map,
                habitat);
        }

        private bool TryAbortDining(ShuttleHabitatDiningOccupantRecord record, Map map)
        {
            return HabitatDiningOccupancyService.TryAbortDining(
                this.CreateDiningAccess(),
                record,
                map);
        }

        private bool TryDropDiningPawn(ShuttleHabitatDiningOccupantRecord record, Map map)
        {
            return HabitatEjectionService.TryDropDiningPawn(
                this.CreateEjectionAccess(),
                record,
                map);
        }

        private bool TryResolveDiningStopPair(
            ShuttleHabitatDiningOccupantRecord record,
            Map map,
            string context)
        {
            return HabitatDiningOccupancyService.TryResolveDiningStopPair(
                this.CreateDiningAccess(),
                record,
                map,
                context);
        }

        private bool TryTakeDiningFoodFromInventory(
            Pawn pawn,
            Thing inventoryFood,
            int count,
            out Thing diningMeal)
        {
            return HabitatDiningOccupancyService.TryTakeDiningFoodFromInventory(
                this.CreateDiningAccess(),
                pawn,
                inventoryFood,
                count,
                out diningMeal);
        }

        private void RestoreDiningFoodToInventoryOrDrop(Pawn pawn, Thing food, Map map)
        {
            HabitatDiningOccupancyService.RestoreDiningFoodToInventoryOrDrop(
                this.CreateDiningAccess(),
                pawn,
                food,
                map);
        }

        private HabitatDiningOccupancyAccess CreateDiningAccess()
        {
            this.EnsureInitialized();
            return new HabitatDiningOccupancyAccess(
                this.parent,
                this.habitatHeldThings,
                this.diningOccupants,
                this.EnsureInitialized,
                this.ReconcileDiningRecordsToContainedThings,
                this.TryDropDiningPawn);
        }

        private bool TryGetDiningHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.parent, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsDining &&
                habitat.DiningSlots > 0 &&
                habitat.AllowsInventoryFood;
        }

        private bool TryGetAnyDiningHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.parent, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsDining &&
                habitat.DiningSlots > 0 &&
                (habitat.AllowsInventoryFood || habitat.AllowsCargoFoodWithdrawal);
        }

        private bool TryGetCargoDiningHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.parent, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsDining &&
                habitat.DiningSlots > 0 &&
                habitat.AllowsCargoFoodWithdrawal;
        }

        private bool HasDiningRecordForFood(Thing food)
        {
            return HabitatOccupancyQueryService.HasDiningRecordForFood(
                this.diningOccupants,
                food);
        }

        private int ComputeDiningChewTicks(Pawn pawn, Thing food)
        {
            return HabitatDiningOccupancyService.ComputeDiningChewTicks(pawn, food);
        }

        // ReadModel

        internal List<Pawn> GetSleepingOccupantsForReading()
        {
            return HabitatOccupancyReadModelBuilder.BuildSleepingOccupantsForReading(
                this.CreateReadModelAccess());
        }

        internal List<Pawn> GetDiningOccupantsForReading()
        {
            return HabitatOccupancyReadModelBuilder.BuildDiningOccupantsForReading(
                this.CreateReadModelAccess());
        }

        internal IReadOnlyList<Pawn> GetJoyOccupantsForReading()
        {
            return HabitatOccupancyReadModelBuilder.BuildJoyOccupantsForReading(
                this.CreateReadModelAccess());
        }

        internal List<Pawn> GetOccupantsForReading()
        {
            return HabitatOccupancyReadModelBuilder.BuildOccupantsForReading(
                this.CreateReadModelAccess());
        }

        private HabitatOccupancyReadModelAccess CreateReadModelAccess()
        {
            this.EnsureInitialized();
            return new HabitatOccupancyReadModelAccess(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants);
        }

        // Diagnostics

        private string DescribeDiningFoodRollbackState(ShuttleHabitatDiningOccupantRecord record)
        {
            return HabitatOccupancyDiagnostics.DescribeDiningFoodRollbackState(
                this.CreateDiagnosticsAccess(),
                record);
        }

        private string DescribeDiningStopState(ShuttleHabitatDiningOccupantRecord record)
        {
            return HabitatOccupancyDiagnostics.DescribeDiningStopState(
                this.CreateDiagnosticsAccess(),
                record);
        }

        private HabitatOccupancyDiagnosticsAccess CreateDiagnosticsAccess()
        {
            return new HabitatOccupancyDiagnosticsAccess(
                this.parent,
                this.habitatHeldThings,
                this.diningOccupants);
        }

        private void HandleFailedPawnHolderTransfer(
            Pawn pawn,
            PawnHolderTransferResult transferResult,
            string operation)
        {
            HabitatOccupancyDiagnostics.HandleFailedPawnHolderTransfer(
                this.CreateDiagnosticsAccess(),
                pawn,
                transferResult,
                operation);
        }

        private string GetPawnHolderTransferFailureReason(
            PawnHolderTransferResult transferResult,
            string defaultReason)
        {
            return HabitatOccupancyDiagnostics.GetPawnHolderTransferFailureReason(
                transferResult,
                defaultReason);
        }

        private void LogDiningRecordPreservedWithoutMap(
            ShuttleHabitatDiningOccupantRecord record,
            bool pawnMissing,
            bool foodMissing)
        {
            HabitatOccupancyDiagnostics.LogDiningRecordPreservedWithoutMap(
                record,
                pawnMissing,
                foodMissing);
        }

        private void LogOrphanDiningFoodPreservedWithoutMap(Thing food)
        {
            HabitatOccupancyDiagnostics.LogOrphanDiningFoodPreservedWithoutMap(food);
        }

        private void LogUnknownHeldPawnPreservedWithoutMap(Pawn pawn)
        {
            HabitatOccupancyDiagnostics.LogUnknownHeldPawnPreservedWithoutMap(pawn);
        }

        private void LogJoyRecordClearedWithoutMap(
            ShuttleHabitatJoyOccupantRecord record,
            bool pawnDead,
            bool joyKindMissing)
        {
            HabitatOccupancyDiagnostics.LogJoyRecordClearedWithoutMap(
                record,
                pawnDead,
                joyKindMissing);
        }

        // Reconcile

        private bool IsContainedPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedPawn(
                this.habitatHeldThings,
                this.sleepingOccupants,
                pawn);
        }

        private bool IsContainedDiningPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedDiningPawn(
                this.habitatHeldThings,
                this.diningOccupants,
                pawn);
        }

        private bool IsContainedJoyPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedJoyPawn(
                this.habitatHeldThings,
                this.joyOccupants,
                pawn);
        }

        private bool IsContainedDiningFood(Thing food)
        {
            return HabitatOccupancyQueryService.IsContainedDiningFood(
                this.habitatHeldThings,
                food);
        }

        private bool IsHeldThing(Thing thing)
        {
            return HabitatOccupancyQueryService.IsHeldThing(this.habitatHeldThings, thing);
        }

        private void ReconcileRecordsToContainedPawns()
        {
            HabitatOccupancyReconciler.ReconcileRecordsToContainedPawns(this.CreateReconcileAccess());
        }

        private void ReconcileJoyRecordsToContainedPawns()
        {
            HabitatOccupancyReconciler.ReconcileJoyRecordsToContainedPawns(this.CreateReconcileAccess());
        }

        private void ReconcileDiningRecordsToContainedThings()
        {
            HabitatOccupancyReconciler.ReconcileDiningRecordsToContainedThings(this.CreateReconcileAccess());
        }

        private HabitatOccupancyReconcileAccess CreateReconcileAccess()
        {
            return new HabitatOccupancyReconcileAccess(
                this,
                this.parent,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized);
        }

        private bool HasRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasRecordFor(this.sleepingOccupants, pawn);
        }

        private bool HasDiningRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasDiningRecordFor(this.diningOccupants, pawn);
        }

        private bool HasJoyRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasJoyRecordFor(this.joyOccupants, pawn);
        }

        private Thing FindUnassignedDiningFood()
        {
            return HabitatOccupancyQueryService.FindUnassignedDiningFood(
                this.habitatHeldThings,
                this.diningOccupants);
        }

        private int CountValidSleepingRecords()
        {
            return HabitatOccupancyQueryService.CountValidSleepingRecords(
                this.habitatHeldThings,
                this.sleepingOccupants);
        }

        private int CountValidDiningRecords()
        {
            return HabitatOccupancyQueryService.CountValidDiningRecords(
                this.habitatHeldThings,
                this.diningOccupants);
        }

        private int CountValidJoyRecords()
        {
            return HabitatOccupancyQueryService.CountValidJoyRecords(
                this.habitatHeldThings,
                this.joyOccupants);
        }

        private int CountHeldPawns()
        {
            return HabitatOccupancyQueryService.CountHeldPawns(this.habitatHeldThings);
        }

        // Ejection

        private HabitatEjectionAccess CreateEjectionAccess()
        {
            return new HabitatEjectionAccess(
                this,
                this.parent,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.EnsureInitialized);
        }
    }
}
