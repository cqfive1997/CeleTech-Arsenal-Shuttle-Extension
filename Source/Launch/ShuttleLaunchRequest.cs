using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    public sealed class ShuttleLaunchRequest
    {
        public ShuttleLaunchRequest(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            IReadOnlyList<ProfileBuildIssue> assemblyReadinessIssues,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoSnapshot cargoSnapshot,
            PlanetTile destinationTile,
            TransportersArrivalAction arrivalAction,
            int distanceTiles,
            float rangeDistanceFactor,
            int cooldownTicks,
            ThingDef leavingSkyfallerDef,
            ThingDef activeTransporterDef,
            WorldObjectDef worldObjectDef)
        {
            this.Host = host;
            this.Profile = profile;
            this.AssemblySnapshot = assemblySnapshot;
            this.AssemblyReadinessIssues = assemblyReadinessIssues;
            this.RuntimeState = runtimeState;
            this.CargoSnapshot = cargoSnapshot;
            this.DestinationTile = destinationTile;
            this.ArrivalAction = arrivalAction;
            this.DistanceTiles = distanceTiles;
            this.RangeDistanceFactor = rangeDistanceFactor;
            this.CooldownTicks = cooldownTicks;
            this.LeavingSkyfallerDef = leavingSkyfallerDef;
            this.ActiveTransporterDef = activeTransporterDef;
            this.WorldObjectDef = worldObjectDef;
        }

        public ThingWithComps Host { get; private set; }
        public ShuttleProfile Profile { get; private set; }
        public ShuttleLaunchAssemblySnapshot AssemblySnapshot { get; private set; }
        public IReadOnlyList<ProfileBuildIssue> AssemblyReadinessIssues { get; private set; }
        public ShuttleRuntimeState RuntimeState { get; private set; }
        public ShuttleCargoSnapshot CargoSnapshot { get; private set; }
        public PlanetTile DestinationTile { get; private set; }
        public TransportersArrivalAction ArrivalAction { get; private set; }
        public int DistanceTiles { get; private set; }
        public float RangeDistanceFactor { get; private set; }
        public int CooldownTicks { get; private set; }
        public ThingDef LeavingSkyfallerDef { get; private set; }
        public ThingDef ActiveTransporterDef { get; private set; }
        public WorldObjectDef WorldObjectDef { get; private set; }
    }
}
