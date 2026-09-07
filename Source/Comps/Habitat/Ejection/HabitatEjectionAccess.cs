using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatEjectionAccess
    {
        private readonly CompShuttleHabitatOccupancy owner;
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;
        private readonly Action ensureInitialized;

        internal HabitatEjectionAccess(
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

        internal int SleepingRecordCount
        {
            get
            {
                return this.sleepingOccupants.Count;
            }
        }

        internal int DiningRecordCount
        {
            get
            {
                return this.diningOccupants.Count;
            }
        }

        internal int JoyRecordCount
        {
            get
            {
                return this.joyOccupants.Count;
            }
        }

        internal bool HasAnyOccupants
        {
            get
            {
                return HabitatOccupancyQueryService.CountHeldPawns(this.habitatHeldThings) > 0;
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ensureInitialized != null)
            {
                this.ensureInitialized();
            }
        }

        internal ShuttleHabitatOccupantRecord GetSleepingRecord(int index)
        {
            return this.sleepingOccupants[index];
        }

        internal ShuttleHabitatDiningOccupantRecord GetDiningRecord(int index)
        {
            return this.diningOccupants[index];
        }

        internal ShuttleHabitatJoyOccupantRecord GetJoyRecord(int index)
        {
            return this.joyOccupants[index];
        }

        internal void RemoveSleepingRecord(ShuttleHabitatOccupantRecord record)
        {
            this.sleepingOccupants.Remove(record);
        }

        internal void ReconcileSleepingRecords()
        {
            HabitatOccupancyReconciler.ReconcileRecordsToContainedPawns(
                this.CreateReconcileAccess());
        }

        internal void ReconcileDiningRecords()
        {
            HabitatOccupancyReconciler.ReconcileDiningRecordsToContainedThings(
                this.CreateReconcileAccess());
        }

        internal void ReconcileJoyRecords()
        {
            HabitatOccupancyReconciler.ReconcileJoyRecordsToContainedPawns(
                this.CreateReconcileAccess());
        }

        internal bool TryAbortDining(ShuttleHabitatDiningOccupantRecord record, Map map)
        {
            return HabitatDiningOccupancyService.TryAbortDining(
                this.CreateDiningAccess(),
                record,
                map);
        }

        internal bool TryAbortJoy(ShuttleHabitatJoyOccupantRecord record, Map map)
        {
            return HabitatJoyOccupancyService.TryAbortJoy(
                this.CreateJoyAccess(),
                record,
                map);
        }

        internal Pawn FindHeldPawnByThingID(int pawnThingID)
        {
            return HabitatOccupancyQueryService.FindHeldPawnByThingID(
                this.habitatHeldThings,
                pawnThingID);
        }

        internal bool IsContainedSleepingPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedPawn(
                this.habitatHeldThings,
                this.sleepingOccupants,
                pawn);
        }

        internal bool IsContainedDiningPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedDiningPawn(
                this.habitatHeldThings,
                this.diningOccupants,
                pawn);
        }

        internal bool IsContainedJoyPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedJoyPawn(
                this.habitatHeldThings,
                this.joyOccupants,
                pawn);
        }

        internal bool IsHeldPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsHeldPawn(
                this.habitatHeldThings,
                pawn);
        }

        internal bool TryDropPawn(Pawn pawn, Map map, out Thing resultingThing)
        {
            return this.habitatHeldThings.TryDrop(
                pawn,
                this.GetEjectCell(map),
                map,
                ThingPlaceMode.Near,
                out resultingThing,
                null,
                null);
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

        internal bool TryGetSleepHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(this.host, out profile, out habitat) &&
                habitat != null &&
                habitat.SupportsSleep &&
                habitat.SleepSlots > 0;
        }

        private HabitatDiningOccupancyAccess CreateDiningAccess()
        {
            this.EnsureInitialized();
            return new HabitatDiningOccupancyAccess(
                this.host,
                this.habitatHeldThings,
                this.diningOccupants,
                this.ensureInitialized,
                null,
                this.TryDropDiningPawn);
        }

        private HabitatJoyOccupancyAccess CreateJoyAccess()
        {
            this.EnsureInitialized();
            return new HabitatJoyOccupancyAccess(
                this.host,
                this.habitatHeldThings,
                this.joyOccupants,
                this.ensureInitialized,
                null,
                this.TryDropJoyPawn);
        }

        private bool TryDropDiningPawn(ShuttleHabitatDiningOccupantRecord record, Map map)
        {
            return HabitatEjectionService.TryDropDiningPawn(this, record, map);
        }

        private bool TryDropJoyPawn(
            ShuttleHabitatJoyOccupantRecord record,
            Map map,
            out Pawn droppedPawn)
        {
            return HabitatEjectionService.TryDropJoyPawn(this, record, map, out droppedPawn);
        }

        private HabitatOccupancyReconcileAccess CreateReconcileAccess()
        {
            return new HabitatOccupancyReconcileAccess(
                this.owner,
                this.host,
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants,
                this.ensureInitialized);
        }

        private IntVec3 GetEjectCell(Map map)
        {
            return ShuttleHabitatEjectDropUtility.GetEjectCell(this.host, map);
        }
    }
}
