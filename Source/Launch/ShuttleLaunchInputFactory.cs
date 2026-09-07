using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    /// <summary>
    /// Builds the detached confirmation-time launch request and quote from already-selected inputs.
    /// It owns launch input assembly, not launch validation or transaction side effects.
    /// </summary>
    internal sealed class ShuttleLaunchInputFactory
    {
        private const string LeavingSkyfallerDefName = "CT_ModularShuttleLeaving";
        private const string WorldObjectDefName = "CT_ModularShuttleWorldObject";

        internal ShuttleLaunchExecutionInput Build(
            ThingWithComps host,
            PlanetTile destinationTile,
            TransportersArrivalAction arrivalAction,
            IShuttleFlightEnergyCalculator flightEnergyCalculator,
            ShuttleProfile launchProfile,
            IReadOnlyList<ProfileBuildIssue> assemblyReadinessIssues,
            ShuttleLaunchAssemblySnapshot launchAssemblySnapshot,
            ShuttleRuntimeState launchRuntimeState,
            ShuttleCargoSnapshot launchCargoSnapshot,
            int profileRevision)
        {
            int distanceTiles = this.GetLaunchDistanceTiles(host, destinationTile);
            float rangeDistanceFactor = this.GetLaunchRangeDistanceFactor(destinationTile);
            ShuttleFlightEnergyQuote quote = flightEnergyCalculator.Quote(
                launchProfile,
                launchRuntimeState,
                launchCargoSnapshot,
                distanceTiles,
                rangeDistanceFactor);

            ShuttleLaunchRequest request = new ShuttleLaunchRequest(
                host,
                launchProfile,
                launchAssemblySnapshot,
                assemblyReadinessIssues,
                launchRuntimeState,
                launchCargoSnapshot,
                destinationTile,
                arrivalAction,
                distanceTiles,
                rangeDistanceFactor,
                this.GetLaunchCooldownTicks(launchProfile),
                DefDatabase<ThingDef>.GetNamedSilentFail(LeavingSkyfallerDefName),
                ThingDefOf.ActiveDropPod,
                DefDatabase<WorldObjectDef>.GetNamedSilentFail(WorldObjectDefName));

            return new ShuttleLaunchExecutionInput(request, quote, profileRevision);
        }

        private int GetLaunchCooldownTicks(ShuttleProfile launchProfile)
        {
            if (launchProfile != null && launchProfile.Flight != null && launchProfile.Flight.LaunchCooldownTicks >= 0)
            {
                return launchProfile.Flight.LaunchCooldownTicks;
            }

            return ShuttleProfileTuningDef.FallbackLaunchCooldownTicks;
        }

        private int GetLaunchDistanceTiles(ThingWithComps host, PlanetTile destinationTile)
        {
            if (host == null || !host.Tile.Valid || !destinationTile.Valid)
            {
                return 0;
            }

            return Find.WorldGrid.TraversalDistanceBetween(host.Tile, destinationTile, true, int.MaxValue, true);
        }

        private float GetLaunchRangeDistanceFactor(PlanetTile destinationTile)
        {
            if (!destinationTile.Valid || destinationTile.Layer == null || destinationTile.Layer.Def == null)
            {
                return 1f;
            }

            return destinationTile.Layer.Def.rangeDistanceFactor;
        }
    }
}
