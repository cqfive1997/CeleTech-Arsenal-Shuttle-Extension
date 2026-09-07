using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.Runtime
{
    public sealed class ShuttleExternalHostInfo
    {
        private readonly Thing unsafeLiveShuttleHost;
        private readonly Map unsafeLiveMap;

        public ShuttleExternalHostInfo(
            Thing shuttleHost,
            Map map,
            IReadOnlyList<IntVec3> occupiedCells,
            IReadOnlyList<IntVec3> adjacentExternalCells,
            int thingIdNumber,
            string thingId,
            string defName,
            string label,
            string factionDefName,
            bool spawned,
            int mapUniqueId,
            string tile,
            int positionX,
            int positionY,
            int positionZ,
            int rotationAsInt)
            : this(
                shuttleHost,
                map,
                occupiedCells,
                adjacentExternalCells,
                thingIdNumber,
                thingId,
                defName,
                label,
                factionDefName,
                spawned,
                mapUniqueId,
                tile,
                positionX,
                positionY,
                positionZ,
                rotationAsInt,
                true)
        {
        }

        public ShuttleExternalHostInfo(
            Thing shuttleHost,
            Map map,
            IReadOnlyList<IntVec3> occupiedCells,
            IReadOnlyList<IntVec3> adjacentExternalCells,
            int thingIdNumber,
            string thingId,
            string defName,
            string label,
            string factionDefName,
            bool spawned,
            int mapUniqueId,
            string tile,
            int positionX,
            int positionY,
            int positionZ,
            int rotationAsInt,
            bool playerColonistOnboardFacilityUseAllowed)
        {
            this.unsafeLiveShuttleHost = shuttleHost;
            this.unsafeLiveMap = map;
            this.OccupiedCells = occupiedCells ?? new List<IntVec3>();
            this.AdjacentExternalCells = adjacentExternalCells ?? new List<IntVec3>();
            this.ThingIdNumber = thingIdNumber;
            this.ThingId = thingId;
            this.DefName = defName;
            this.Label = label;
            this.FactionDefName = factionDefName;
            this.Spawned = spawned;
            this.MapUniqueId = mapUniqueId;
            this.Tile = tile;
            this.PositionX = positionX;
            this.PositionY = positionY;
            this.PositionZ = positionZ;
            this.RotationAsInt = rotationAsInt;
            this.PlayerColonistOnboardFacilityUseAllowed =
                playerColonistOnboardFacilityUseAllowed;
        }

        /// <summary>
        /// Unsafe live Verse reference retained for source compatibility. Stable external
        /// integrations should use the snapshot fields on this DTO instead.
        /// Third-party systems must not move, destroy, reparent, or mutate this object.
        /// </summary>
        [Obsolete("Unsafe live Verse reference; use snapshot fields instead.")]
        public Thing ShuttleHost
        {
            get { return this.unsafeLiveShuttleHost; }
        }

        /// <summary>
        /// Unsafe live Verse reference retained for source compatibility. Stable external
        /// integrations should use MapUniqueId, Tile, position, occupied cells, and adjacent
        /// cells instead.
        /// </summary>
        [Obsolete("Unsafe live Verse reference; use snapshot fields instead.")]
        public Map Map
        {
            get { return this.unsafeLiveMap; }
        }

        /// <summary>
        /// Explicit unsafe live shuttle host reference. Prefer snapshot fields for stable use.
        /// </summary>
        public Thing UnsafeLiveShuttleHost
        {
            get { return this.unsafeLiveShuttleHost; }
        }

        /// <summary>
        /// Explicit unsafe live map reference. Prefer snapshot fields for stable use.
        /// </summary>
        public Map UnsafeLiveMap
        {
            get { return this.unsafeLiveMap; }
        }

        public IReadOnlyList<IntVec3> OccupiedCells { get; private set; }
        public IReadOnlyList<IntVec3> AdjacentExternalCells { get; private set; }
        public int ThingIdNumber { get; private set; }
        public string ThingId { get; private set; }
        public string DefName { get; private set; }
        public string Label { get; private set; }
        public string FactionDefName { get; private set; }
        public bool Spawned { get; private set; }
        public int MapUniqueId { get; private set; }
        public string Tile { get; private set; }
        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public int PositionZ { get; private set; }
        public int RotationAsInt { get; private set; }

        /// <summary>
        /// Effective read-only permission for servicing player colonists through onboard
        /// facilities at the current host location. Non-colonist role policy remains with
        /// the consuming runtime.
        /// </summary>
        public bool PlayerColonistOnboardFacilityUseAllowed { get; private set; }
    }

    public interface IShuttleExternalHostReadPort
    {
        bool TryGetHostInfo(out ShuttleExternalHostInfo hostInfo);
    }
}
