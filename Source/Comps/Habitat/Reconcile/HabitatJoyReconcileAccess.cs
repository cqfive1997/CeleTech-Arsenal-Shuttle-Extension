using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatJoyReconcileAccess
    {
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;

        internal HabitatJoyReconcileAccess(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants)
        {
            this.habitatHeldThings = habitatHeldThings;
            this.joyOccupants = joyOccupants;
        }

        internal int JoyRecordCount
        {
            get
            {
                return this.joyOccupants.Count;
            }
        }

        internal ShuttleHabitatJoyOccupantRecord GetJoyRecord(int index)
        {
            return this.joyOccupants[index];
        }

        internal void RemoveJoyRecordAt(int index)
        {
            this.joyOccupants.RemoveAt(index);
        }

        internal bool IsHeldPawn(Pawn pawn)
        {
            return HabitatOccupancyQueryService.IsHeldPawn(
                this.habitatHeldThings,
                pawn);
        }
    }
}
