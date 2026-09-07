using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class HabitatOccupancyDiagnosticsAccess
    {
        private readonly ThingWithComps host;
        private readonly ThingOwner<Thing> habitatHeldThings;
        private readonly List<ShuttleHabitatDiningOccupantRecord> diningOccupants;

        internal HabitatOccupancyDiagnosticsAccess(
            ThingWithComps host,
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants)
        {
            this.host = host;
            this.habitatHeldThings = habitatHeldThings;
            this.diningOccupants = diningOccupants;
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

        internal void SelectHostIfAvailable()
        {
            if (this.host != null)
            {
                Find.Selector.Select(this.host, false, false);
            }
        }
    }
}
