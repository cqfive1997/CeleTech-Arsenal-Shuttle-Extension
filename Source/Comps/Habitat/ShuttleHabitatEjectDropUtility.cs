using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class ShuttleHabitatEjectDropUtility
    {
        internal static IntVec3 GetEjectCell(Thing parent, Map map)
        {
            if (parent == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 cell = parent.InteractionCell;
            if (map != null && cell.IsValid && cell.InBounds(map))
            {
                return cell;
            }

            return parent.Position;
        }

        internal static bool TryPlaceThingNear(Thing thing, IntVec3 cell, Map map)
        {
            return GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
        }
    }
}
