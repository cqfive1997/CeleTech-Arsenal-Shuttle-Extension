using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld.Planet;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal ShuttleLaunchTargetingContext BuildLaunchTargetingContext()
        {
            ShuttleControllerUIPort uiPort = new ShuttleControllerUIPort(this);
            return new ShuttleLaunchTargetingContext(
                this.shuttleHost,
                uiPort,
                this.GetProfileForRead,
                this.GetLaunchRuntimeState,
                this.BuildLaunchCargoSnapshot);
        }

        internal ShuttleLaunchExecutionInput BuildLaunchExecutionInput(
            PlanetTile destinationTile,
            TransportersArrivalAction arrivalAction)
        {
            // Rebuild all launch inputs at confirmation time. The player may have changed cargo,
            // modules, charge, or cooldown after opening the world targeter.
            ShuttleProfile launchProfile = this.BuildLaunchProfile();
            ShuttleCargoSnapshot launchCargoSnapshot = this.BuildLaunchCargoSnapshot();
            return this.launchInputFactory.Build(
                this.shuttleHost,
                destinationTile,
                arrivalAction,
                this.flightEnergyCalculator,
                launchProfile,
                this.BuildAssemblyReadinessIssues(launchProfile, launchCargoSnapshot),
                this.BuildLaunchAssemblySnapshot(),
                this.GetLaunchRuntimeState(),
                launchCargoSnapshot,
                this.profileRevision);
        }

        internal ShuttleLaunchResult RunDevMedicalBayPatientLaunchRollbackTest()
        {
            return this.electricLaunchService.ExecuteDevMedicalBayPatientLaunchRollbackTest(
                this.shuttleHost,
                this.BuildLaunchProfile(),
                this.GetLaunchRuntimeState());
        }

        internal ShuttleLaunchAssemblySnapshot BuildLaunchAssemblySnapshot()
        {
            return ShuttleLaunchAssemblySnapshot.FromAssemblyState(this.assemblyState);
        }

        internal ShuttleProfile BuildLaunchProfile()
        {
            return this.ReconcileProfileToHost();
        }

        internal ShuttleRuntimeState GetLaunchRuntimeState()
        {
            this.EnsureRuntimeState();
            return this.runtimeState;
        }

        internal void NotifyArrived()
        {
            this.EnsureRuntimeState();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            this.runtimeState.Launch.LastArrivalTick = ticksGame;
            ShuttleProfile currentProfile = this.ReconcileProfileToHost();
            this.moduleRuntimeCoordinator.NotifyArrived(
                this.shuttleHost,
                this.assemblyState,
                currentProfile,
                this.runtimeState,
                this.powerSystem,
                ticksGame,
                this.BuildCargoResourceBroker(currentProfile));
            this.InvalidateCargoInventorySnapshotCaches();
            this.MarkRefrigeratedCargoRegistryDirty();
            this.ReconcileRefrigeratedCargoRegistryIfNeeded(ticksGame);
        }

        internal ShuttleCargoSnapshot BuildLaunchCargoSnapshot()
        {
            return this.BuildCargoSnapshot();
        }
    }
}
