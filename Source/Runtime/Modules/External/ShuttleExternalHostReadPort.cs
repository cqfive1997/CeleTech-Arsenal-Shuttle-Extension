using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ShuttleExternalHostReadPort : IShuttleExternalHostReadPort
    {
        private readonly ThingWithComps host;

        internal ShuttleExternalHostReadPort(ThingWithComps host)
        {
            this.host = host;
        }

        public bool TryGetHostInfo(out ShuttleExternalHostInfo hostInfo)
        {
            hostInfo = null;
            if (this.host == null)
            {
                return false;
            }

            Map map = this.host.Map;
            IntVec3 position = this.host.Position;
            IReadOnlyList<IntVec3> occupiedCells = this.BuildOccupiedCells(map);
            IReadOnlyList<IntVec3> adjacentExternalCells = this.BuildAdjacentExternalCells(map, occupiedCells);
            hostInfo = new ShuttleExternalHostInfo(
                this.host,
                map,
                occupiedCells,
                adjacentExternalCells,
                this.host.thingIDNumber,
                this.host.ThingID,
                this.host.def != null ? this.host.def.defName : null,
                this.host.LabelCap.ToString(),
                this.host.Faction != null && this.host.Faction.def != null
                    ? this.host.Faction.def.defName
                    : null,
                this.host.Spawned,
                map != null ? map.uniqueID : -1,
                this.host.Tile.ToString(),
                position.x,
                position.y,
                position.z,
                this.host.Rotation.AsInt,
                this.IsPlayerColonistOnboardFacilityUseAllowed(map));
            return true;
        }

        private bool IsPlayerColonistOnboardFacilityUseAllowed(Map map)
        {
            if (map == null || !map.IsPlayerHome)
            {
                return true;
            }

            ShuttleOtherSettings other = CeleTechShuttleMod.Settings != null
                ? CeleTechShuttleMod.Settings.Other
                : null;
            return other == null || other.AllowColonistOnboardDeviceUseAtPlayerHome;
        }

        private IReadOnlyList<IntVec3> BuildOccupiedCells(Map map)
        {
            List<IntVec3> cells = new List<IntVec3>();
            if (this.host == null || this.host.def == null)
            {
                return cells;
            }

            CellRect rect = GenAdj.OccupiedRect(
                this.host.Position,
                this.host.Rotation,
                this.host.def.Size);
            foreach (IntVec3 cell in rect)
            {
                if (map == null || cell.InBounds(map))
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        private IReadOnlyList<IntVec3> BuildAdjacentExternalCells(
            Map map,
            IReadOnlyList<IntVec3> occupiedCells)
        {
            List<IntVec3> adjacent = new List<IntVec3>();
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return adjacent;
            }

            HashSet<IntVec3> occupiedSet = new HashSet<IntVec3>(occupiedCells);
            HashSet<IntVec3> adjacentSet = new HashSet<IntVec3>();
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                IntVec3 occupied = occupiedCells[i];
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0)
                        {
                            continue;
                        }

                        IntVec3 candidate = new IntVec3(
                            occupied.x + dx,
                            occupied.y,
                            occupied.z + dz);
                        if (occupiedSet.Contains(candidate) || adjacentSet.Contains(candidate))
                        {
                            continue;
                        }

                        if (map != null && !candidate.InBounds(map))
                        {
                            continue;
                        }

                        adjacentSet.Add(candidate);
                        adjacent.Add(candidate);
                    }
                }
            }

            return adjacent;
        }
    }
}
