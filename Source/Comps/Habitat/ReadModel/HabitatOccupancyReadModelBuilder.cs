using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatOccupancyReadModelBuilder
    {
        internal static List<Pawn> BuildSleepingOccupantsForReading(
            HabitatOccupancyReadModelAccess access)
        {
            if (access == null)
            {
                return new List<Pawn>();
            }

            return access.GetSleepingOccupantsForReading();
        }

        internal static List<Pawn> BuildDiningOccupantsForReading(
            HabitatOccupancyReadModelAccess access)
        {
            if (access == null)
            {
                return new List<Pawn>();
            }

            return access.GetDiningOccupantsForReading();
        }

        internal static IReadOnlyList<Pawn> BuildJoyOccupantsForReading(
            HabitatOccupancyReadModelAccess access)
        {
            if (access == null)
            {
                return new List<Pawn>();
            }

            return access.GetJoyOccupantsForReading();
        }

        internal static List<Pawn> BuildOccupantsForReading(
            HabitatOccupancyReadModelAccess access)
        {
            if (access == null)
            {
                return new List<Pawn>();
            }

            return access.GetOccupantsForReading();
        }
    }
}
