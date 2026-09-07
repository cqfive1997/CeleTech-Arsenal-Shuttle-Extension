using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    internal static class ModularShuttleLandingCompatibilityPolicy
    {
        internal static bool CanLandWithCompatibleOverlays(
            LocalTargetInfo target,
            Map map,
            ThingDef shuttleDef,
            Rot4 rotation)
        {
            if (map == null || shuttleDef == null || !target.IsValid)
            {
                return false;
            }

            foreach (IntVec3 cell in GenAdj.OccupiedRect(
                target.Cell,
                rotation,
                shuttleDef.Size))
            {
                if (!CanUseCell(cell, map, shuttleDef))
                {
                    return false;
                }
            }

            IntVec3 interactionCell = ThingUtility.InteractionCellWhenAt(
                shuttleDef,
                target.Cell,
                rotation,
                map);
            return CanUseCell(interactionCell, map, shuttleDef);
        }

        internal static bool CanCoexistBeneathShuttle(ThingDef shuttleDef, Thing thing)
        {
            if (shuttleDef == null || thing == null || thing.def == null)
            {
                return false;
            }

            ThingDef thingDef = thing.def;
            BuildingProperties building = thingDef.building;
            if (thingDef.category != ThingCategory.Building ||
                building == null ||
                building.isEdifice ||
                thingDef.passability == Traversability.Impassable ||
                thingDef.preventSkyfallersLandingOn)
            {
                return false;
            }

            // Match the actual landed-host spawn semantics. If GenSpawn would wipe the
            // underlying Thing, accepting the landing would silently destroy infrastructure
            // or artwork and is therefore not a compatibility-safe overlap.
            return !GenSpawn.SpawningWipes(shuttleDef, thingDef, false);
        }

        private static bool CanUseCell(IntVec3 cell, Map map, ThingDef shuttleDef)
        {
            if (!cell.InBounds(map) || cell.Fogged(map) || !cell.Walkable(map))
            {
                return false;
            }

            TerrainAffordanceDef affordance = ThingDefOf.Shuttle.terrainAffordanceNeeded;
            if (affordance != null && !cell.GetAffordances(map).Contains(affordance))
            {
                return false;
            }

            RoofDef roof = cell.GetRoof(map);
            if (roof != null && (roof.isNatural || roof.isThickRoof))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing is IActiveTransporter || thing is Skyfaller)
                {
                    return false;
                }

                if (thing.def.category == ThingCategory.Building &&
                    (thing.def.building == null || !thing.def.building.isPowerConduit) &&
                    !CanCoexistBeneathShuttle(shuttleDef, thing))
                {
                    return false;
                }

                PlantProperties plant = thing.def.plant;
                if (plant != null && plant.IsTree)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
