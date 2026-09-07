using System;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal delegate bool TryCancelMedicalBayProcedureDelegate(int procedureID, out string message);
    internal delegate bool TryStartMedicalBaySurgeryProcedureDelegate(
        Pawn doctor,
        int patientThingID,
        string recipeDefName,
        int bodyPartIndex,
        out string message);

    /// <summary>
    /// Narrow command-handler view of the controller. Handlers receive the seams they need
    /// without owning durable shuttle state themselves.
    /// </summary>
    internal sealed class ShuttleCommandContext
    {
        private readonly Func<ShuttleProfile> getProfileForRead;
        private readonly Func<ShuttleRuntimeState> getRuntimeState;
        private readonly Func<ShuttleProfile> reconcileProfileToHost;
        private readonly Func<ShuttleCargoSnapshot> buildCargoSnapshot;
        private readonly Func<ShuttleLaunchTargetingContext> buildLaunchTargetingContext;
        private readonly Func<PlanetTile, TransportersArrivalAction, ShuttleLaunchExecutionInput> buildLaunchExecutionInput;
        private readonly Func<float, bool> trySetActiveSurfaceShieldRechargeSpeed;
        private readonly TryCancelMedicalBayProcedureDelegate tryCancelMedicalBayProcedure;
        private readonly TryStartMedicalBaySurgeryProcedureDelegate tryStartMedicalBaySurgeryProcedure;
        private readonly Action<ProfileDirtyReason> markProfileDirty;

        public ShuttleCommandContext(
            ThingWithComps host,
            Building_ModularShuttle buildingHost,
            ShuttleAssemblyState assemblyState,
            ShuttleAssemblyMutationController assemblyMutationController,
            IShuttleCargoBackend cargoBackend,
            IShuttleCommandExecutor commandExecutor,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            ShuttleWorldTargetingService worldTargetingService,
            ElectricShuttleLaunchService electricLaunchService,
            ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator,
            Func<ShuttleRuntimeState> getRuntimeState,
            IShuttleStoredEnergySink storedEnergySink,
            Func<ShuttleProfile> getProfileForRead,
            Func<ShuttleProfile> reconcileProfileToHost,
            Func<ShuttleCargoSnapshot> buildCargoSnapshot,
            Func<ShuttleLaunchTargetingContext> buildLaunchTargetingContext,
            Func<PlanetTile, TransportersArrivalAction, ShuttleLaunchExecutionInput> buildLaunchExecutionInput,
            Func<float, bool> trySetActiveSurfaceShieldRechargeSpeed,
            TryCancelMedicalBayProcedureDelegate tryCancelMedicalBayProcedure,
            TryStartMedicalBaySurgeryProcedureDelegate tryStartMedicalBaySurgeryProcedure,
            Action<ProfileDirtyReason> markProfileDirty)
        {
            this.Host = host;
            this.BuildingHost = buildingHost;
            this.AssemblyState = assemblyState;
            this.AssemblyMutationController = assemblyMutationController;
            this.CargoBackend = cargoBackend;
            this.CommandExecutor = commandExecutor;
            this.LoadCargoReadPort = loadCargoReadPort;
            this.WorldTargetingService = worldTargetingService;
            this.ElectricLaunchService = electricLaunchService;
            this.ModuleRuntimeCoordinator = moduleRuntimeCoordinator;
            this.StoredEnergySink = storedEnergySink;
            this.getRuntimeState = getRuntimeState;
            this.getProfileForRead = getProfileForRead;
            this.reconcileProfileToHost = reconcileProfileToHost;
            this.buildCargoSnapshot = buildCargoSnapshot;
            this.buildLaunchTargetingContext = buildLaunchTargetingContext;
            this.buildLaunchExecutionInput = buildLaunchExecutionInput;
            this.trySetActiveSurfaceShieldRechargeSpeed = trySetActiveSurfaceShieldRechargeSpeed;
            this.tryCancelMedicalBayProcedure = tryCancelMedicalBayProcedure;
            this.tryStartMedicalBaySurgeryProcedure = tryStartMedicalBaySurgeryProcedure;
            this.markProfileDirty = markProfileDirty;
        }

        public ThingWithComps Host { get; private set; }
        public Building_ModularShuttle BuildingHost { get; private set; }
        public ShuttleAssemblyState AssemblyState { get; private set; }
        public ShuttleAssemblyMutationController AssemblyMutationController { get; private set; }
        public IShuttleCargoBackend CargoBackend { get; private set; }
        public IShuttleCommandExecutor CommandExecutor { get; private set; }
        public IShuttleLoadCargoReadPort LoadCargoReadPort { get; private set; }
        public ShuttleWorldTargetingService WorldTargetingService { get; private set; }
        public ElectricShuttleLaunchService ElectricLaunchService { get; private set; }
        public ShuttleModuleRuntimeCoordinator ModuleRuntimeCoordinator { get; private set; }
        public IShuttleStoredEnergySink StoredEnergySink { get; private set; }

        public ShuttleProfile GetProfileForRead()
        {
            return this.getProfileForRead != null ? this.getProfileForRead() : null;
        }

        public ShuttleRuntimeState GetRuntimeState()
        {
            return this.getRuntimeState != null ? this.getRuntimeState() : null;
        }

        public ShuttleProfile ReconcileProfileToHost()
        {
            return this.reconcileProfileToHost != null ? this.reconcileProfileToHost() : null;
        }

        public ShuttleCargoSnapshot BuildCargoSnapshot()
        {
            return this.buildCargoSnapshot != null ? this.buildCargoSnapshot() : null;
        }

        public ShuttleLaunchTargetingContext BuildLaunchTargetingContext()
        {
            return this.buildLaunchTargetingContext != null ? this.buildLaunchTargetingContext() : null;
        }

        public ShuttleLaunchExecutionInput BuildLaunchExecutionInput(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            return this.buildLaunchExecutionInput != null
                ? this.buildLaunchExecutionInput(destinationTile, arrivalAction)
                : null;
        }

        public bool TrySetActiveSurfaceShieldRechargeSpeed(float multiplier)
        {
            return this.trySetActiveSurfaceShieldRechargeSpeed != null &&
                this.trySetActiveSurfaceShieldRechargeSpeed(multiplier);
        }

        public bool TryCancelMedicalBayProcedure(int procedureID, out string message)
        {
            message = null;
            return this.tryCancelMedicalBayProcedure != null &&
                this.tryCancelMedicalBayProcedure(procedureID, out message);
        }

        public bool TryStartMedicalBaySurgeryProcedure(
            Pawn doctor,
            int patientThingID,
            string recipeDefName,
            int bodyPartIndex,
            out string message)
        {
            message = null;
            return this.tryStartMedicalBaySurgeryProcedure != null &&
                this.tryStartMedicalBaySurgeryProcedure(
                    doctor,
                    patientThingID,
                    recipeDefName,
                    bodyPartIndex,
                    out message);
        }

        public void MarkProfileDirty(ProfileDirtyReason reason)
        {
            if (this.markProfileDirty != null)
            {
                this.markProfileDirty(reason);
            }
        }
    }
}
