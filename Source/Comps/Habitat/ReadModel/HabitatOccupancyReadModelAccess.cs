using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatOccupancyReadModelAccess
    {
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatOccupantRecord> sleepingOccupants;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;
        private readonly List<ShuttleHabitatJoyOccupantRecord> joyOccupants;

        internal HabitatOccupancyReadModelAccess(
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

        internal List<Pawn> GetSleepingOccupantsForReading()
        {
            return HabitatOccupancyQueryService.GetSleepingOccupantsForReading(
                this.habitatHeldThings,
                this.sleepingOccupants);
        }

        internal List<Pawn> GetDiningOccupantsForReading()
        {
            return HabitatOccupancyQueryService.GetDiningOccupantsForReading(
                this.habitatHeldThings,
                this.diningOccupants);
        }

        internal IReadOnlyList<Pawn> GetJoyOccupantsForReading()
        {
            return HabitatOccupancyQueryService.GetJoyOccupantsForReading(
                this.habitatHeldThings,
                this.joyOccupants);
        }

        internal List<Pawn> GetOccupantsForReading()
        {
            return HabitatOccupancyQueryService.GetOccupantsForReading(
                this.habitatHeldThings,
                this.sleepingOccupants,
                this.diningOccupants,
                this.joyOccupants);
        }
    }
}
