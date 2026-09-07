using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatLaunchTransferValidationAccess
    {
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;

        internal HabitatLaunchTransferValidationAccess(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants)
        {
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
            this.diningOccupants = diningOccupants;
            this.joyOccupants = joyOccupants;
        }

        internal int SleepingRecordCount
        {
            get
            {
                return this.sleepingOccupants != null ? this.sleepingOccupants.Count : 0;
            }
        }

        internal int DiningRecordCount
        {
            get
            {
                return this.diningOccupants != null ? this.diningOccupants.Count : 0;
            }
        }

        internal int JoyRecordCount
        {
            get
            {
                return this.joyOccupants != null ? this.joyOccupants.Count : 0;
            }
        }

        internal int HeldThingCount
        {
            get
            {
                return this.habitatHeldThings != null ? this.habitatHeldThings.Count : 0;
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

        internal Thing GetHeldThing(int index)
        {
            return this.habitatHeldThings[index];
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

        internal bool IsContainedDiningFood(Thing food)
        {
            return HabitatOccupancyQueryService.IsContainedDiningFood(
                this.habitatHeldThings,
                food);
        }

        internal bool IsContainedJoyPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsContainedJoyPawn(
                this.habitatHeldThings,
                this.joyOccupants,
                pawn);
        }

        internal int CountHeldPawns()
        {
            if (this.habitatHeldThings == null)
            {
                return 0;
            }

            return HabitatOccupancyQueryService.CountHeldPawns(this.habitatHeldThings);
        }

        internal Thing FindUnassignedDiningFood()
        {
            if (this.habitatHeldThings == null || this.diningOccupants == null)
            {
                return null;
            }

            return HabitatOccupancyQueryService.FindUnassignedDiningFood(
                this.habitatHeldThings,
                this.diningOccupants);
        }
    }
}
