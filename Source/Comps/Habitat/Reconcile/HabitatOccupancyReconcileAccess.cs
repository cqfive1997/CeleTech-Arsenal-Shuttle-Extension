using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatOccupancyReconcileAccess
    {
        private readonly CompShuttleHabitatOccupancy owner;
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly Action ensureInitialized;

        internal HabitatOccupancyReconcileAccess(
            CompShuttleHabitatOccupancy owner,
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Action ensureInitialized)
        {
            this.owner = owner;
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
            this.diningOccupants = diningOccupants;
            this.joyOccupants = joyOccupants;
            this.ensureInitialized = ensureInitialized;
        }

        internal Map HostMap
        {
            get
            {
                return this.host != null ? this.host.Map : null;
            }
        }

        internal int HeldThingCount
        {
            get
            {
                return this.habitatHeldThings.Count;
            }
        }

        internal int DiningRecordCount
        {
            get
            {
                return this.diningOccupants.Count;
            }
        }

        internal HabitatSleepReconcileAccess CreateSleepAccess()
        {
            return new HabitatSleepReconcileAccess(
                this.habitatHeldThings,
                this.sleepingOccupants);
        }

        internal HabitatJoyReconcileAccess CreateJoyAccess()
        {
            return new HabitatJoyReconcileAccess(
                this.habitatHeldThings,
                this.joyOccupants);
        }

        internal Thing GetHeldThing(int index)
        {
            return this.habitatHeldThings[index];
        }

        internal ShuttleHabitatDiningOccupantRecord GetDiningRecord(int index)
        {
            return this.diningOccupants[index];
        }

        internal void AddDiningRecord(
            Pawn pawn,
            Thing food,
            int startTick,
            int chewTicks)
        {
            this.diningOccupants.Add(new ShuttleHabitatDiningOccupantRecord(
                pawn,
                food,
                startTick,
                chewTicks));
        }

        internal void RemoveDiningRecordAt(int index)
        {
            this.diningOccupants.RemoveAt(index);
        }

        internal bool IsHeldPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsHeldPawn(
                this.habitatHeldThings,
                pawn);
        }

        internal bool IsContainedDiningFood(Thing food)
        {
            return HabitatOccupancyQueryService.IsContainedDiningFood(
                this.habitatHeldThings,
                food);
        }

        internal bool HasRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasRecordFor(
                this.sleepingOccupants,
                pawn);
        }

        internal bool HasDiningRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasDiningRecordFor(
                this.diningOccupants,
                pawn);
        }

        internal bool HasDiningRecordForFood(Thing food)
        {
            return HabitatOccupancyQueryService.HasDiningRecordForFood(
                this.diningOccupants,
                food);
        }

        internal bool HasJoyRecordFor(Pawn pawn)
        {
            return HabitatOccupancyQueryService.HasJoyRecordFor(
                this.joyOccupants,
                pawn);
        }

        internal Thing FindUnassignedDiningFood()
        {
            return HabitatOccupancyQueryService.FindUnassignedDiningFood(
                this.habitatHeldThings,
                this.diningOccupants);
        }

        internal int ComputeDiningChewTicks(Pawn pawn, Thing food)
        {
            return HabitatDiningOccupancyService.ComputeDiningChewTicks(pawn, food);
        }

        internal void LogDiningRecordPreservedWithoutMap(
            ShuttleHabitatDiningOccupantRecord record,
            bool pawnMissing,
            bool foodMissing)
        {
            HabitatOccupancyDiagnostics.LogDiningRecordPreservedWithoutMap(
                record,
                pawnMissing,
                foodMissing);
        }

        internal void LogOrphanDiningFoodPreservedWithoutMap(Thing food)
        {
            HabitatOccupancyDiagnostics.LogOrphanDiningFoodPreservedWithoutMap(food);
        }

        internal void LogUnknownHeldPawnPreservedWithoutMap(Pawn pawn)
        {
            HabitatOccupancyDiagnostics.LogUnknownHeldPawnPreservedWithoutMap(pawn);
        }

        internal void LogJoyRecordClearedWithoutMap(
            ShuttleHabitatJoyOccupantRecord record,
            bool pawnDead,
            bool joyKindMissing)
        {
            HabitatOccupancyDiagnostics.LogJoyRecordClearedWithoutMap(
                record,
                pawnDead,
                joyKindMissing);
        }

        internal string DescribeDiningStopState(ShuttleHabitatDiningOccupantRecord record)
        {
            return HabitatOccupancyDiagnostics.DescribeDiningStopState(
                this.CreateDiagnosticsAccess(),
                record);
        }

        internal bool TryResolveDiningStopPair(
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

        internal bool TryDropJoyPawn(
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

        internal bool TryDropHeldThing(
            Thing thing,
            Map map,
            out Thing resultingThing)
        {
            return this.habitatHeldThings.TryDrop(
                thing,
                this.GetEjectCell(map),
                map,
                ThingPlaceMode.Near,
                out resultingThing,
                null,
                null);
        }

        internal bool HasHabitatHolder()
        {
            return this.habitatHeldThings != null;
        }

        internal void RemoveHeldPawn(Pawn pawn)
        {
            this.habitatHeldThings.Remove(pawn);
        }

        private HabitatDiningOccupancyAccess CreateDiningAccess()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }

            return new HabitatDiningOccupancyAccess(
                this.host,
                this.habitatHeldThings,
                this.diningOccupants,
                this.ensureInitialized,
                null,
                this.TryDropDiningPawn);
        }

        private HabitatEjectionAccess CreateEjectionAccess()
        {
            return new HabitatEjectionAccess(
                this.owner,
                this.host,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.ensureInitialized);
        }

        private bool TryDropDiningPawn(ShuttleHabitatDiningOccupantRecord record, Map map)
        {
            return HabitatEjectionService.TryDropDiningPawn(
                this.CreateEjectionAccess(),
                record,
                map);
        }

        private HabitatOccupancyDiagnosticsAccess CreateDiagnosticsAccess()
        {
            return new HabitatOccupancyDiagnosticsAccess(
                this.host,
                this.habitatHeldThings,
                this.diningOccupants);
        }

        private IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.host, map);
        }
    }
}
