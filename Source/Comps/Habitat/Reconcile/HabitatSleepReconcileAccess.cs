using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatSleepReconcileAccess
    {
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;

        internal HabitatSleepReconcileAccess(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants)
        {
            this.habitatHeldThings = habitatHeldThings;
            this.sleepingOccupants = sleepingOccupants;
        }

        internal int SleepingRecordCount
        {
            get
            {
                return this.sleepingOccupants.Count;
            }
        }

        internal ShuttleHabitatOccupantRecord GetSleepingRecord(int index)
        {
            return this.sleepingOccupants[index];
        }

        internal void RemoveSleepingRecordAt(int index)
        {
            this.sleepingOccupants.RemoveAt(index);
        }

        internal bool IsHeldPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsHeldPawn(
                this.habitatHeldThings,
                pawn);
        }
    }
}
