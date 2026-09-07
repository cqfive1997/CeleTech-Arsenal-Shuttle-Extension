using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Commands.External;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Current-stage core orchestrator.
    /// It owns durable assembly/runtime truth and non-persistent profile derivation.
    /// </summary>
    public sealed partial class ShuttleController :
        IExposable,
        IShuttleCommandExecutor,
        IShuttleExternalModuleUIReadPort
    {
        // Shared builder is stateless; keeping one instance avoids recreating it for every profile rebuild.
        private static readonly ShuttleProfileBuilder SharedProfileBuilder = new ShuttleProfileBuilder();
        private static readonly ShuttleAssemblyReadSnapshotBuilder SharedAssemblyReadSnapshotBuilder = new ShuttleAssemblyReadSnapshotBuilder();
        private static readonly ShuttleAssemblyReadinessValidator SharedAssemblyReadinessValidator = new ShuttleAssemblyReadinessValidator();
        private static readonly ShuttlePowerReadModelBuilder SharedPowerReadModelBuilder = new ShuttlePowerReadModelBuilder();
        private static readonly ShuttleWeaponBayReadModelBuilder SharedWeaponBayReadModelBuilder = new ShuttleWeaponBayReadModelBuilder();
        private static readonly ShuttleHabitatReadModelBuilder SharedHabitatReadModelBuilder = new ShuttleHabitatReadModelBuilder();
        private static readonly ShuttleMedicalBayReadModelBuilder SharedMedicalBayReadModelBuilder = new ShuttleMedicalBayReadModelBuilder();
        private static readonly ShuttleMechChargerReadModelBuilder SharedMechChargerReadModelBuilder = new ShuttleMechChargerReadModelBuilder();
        private static readonly ShuttlePrisonCellReadModelBuilder SharedPrisonCellReadModelBuilder = new ShuttlePrisonCellReadModelBuilder();
        private static readonly ShuttleAutoWorkTableReadModelBuilder SharedAutoWorkTableReadModelBuilder = new ShuttleAutoWorkTableReadModelBuilder();
        private static readonly ExternalModuleUIReadModelBuilder SharedExternalModuleUIReadModelBuilder =
            new ExternalModuleUIReadModelBuilder();
        private static readonly ShuttleSurfaceShieldRuntimeService SharedSurfaceShieldRuntimeService =
            new ShuttleSurfaceShieldRuntimeService();
        private static readonly ShuttleHullIntegrityService SharedHullIntegrityService =
            new ShuttleHullIntegrityService();
        private static readonly ShuttleHullRepairService SharedHullRepairService =
            new ShuttleHullRepairService();
        private static readonly MedicalBayProcedureService SharedMedicalBayProcedureService =
            new MedicalBayProcedureService();
        private static readonly MedicalBayProcedureRuntimeSystem SharedMedicalBayProcedureRuntimeSystem =
            new MedicalBayProcedureRuntimeSystem();
        private static readonly ShuttleProfileSyncService SharedProfileSyncService = new ShuttleProfileSyncService();
        private const int RefrigeratedCargoRegistryFallbackIntervalTicks = 250;
        private const int LoadCargoReadModelCacheRefreshTicks = 250;
        private const int LoadCargoReadModelSlowBuildLogThresholdMs = 20;
        private const string HabitatOccupiedIssueCode = "habitat-occupied";
        private const string HabitatJoyOccupiedIssueCode = "habitat-joy-occupied";
        private const string HabitatTransferInvalidStateIssueCode = "habitat-transfer-invalid-state";
        private const string MedicalBayOccupiedIssueCode = "medical-bay-occupied";
        private const string MechChargerOccupiedIssueCode = "mech-charger-occupied";
        private const string PrisonCellOccupiedIssueCode = "prison-cell-occupied";
        private const string HolderTransferManifestActiveIssueCode = "holder-transfer-manifest-active";
        private const string RefrigeratedCargoLaunchTransferUnavailableIssueCode = "refrigerated-cargo-launch-transfer-unavailable";
        private const string ActiveMedicalProcedureIssueCode = "active-medical-procedure";
        private const string LaunchCargoBackendUnavailableIssueCode = "launch-cargo-backend-unavailable";
        private const string LaunchCargoLoadingIncompleteIssueCode = "launch-cargo-loading-incomplete";
        private const string LaunchCargoUnloadingActiveIssueCode = "launch-cargo-unloading-active";
        private const string LaunchCargoCapacityUnavailableIssueCode = "launch-cargo-capacity-unavailable";
        private const string LaunchCargoMassExceedsCapacityIssueCode = "launch-cargo-mass-exceeds-capacity";
        private const string LaunchUnderRoofIssueCode = "launch-under-roof";
        private const string LaunchCooldownIssueCode = "launch-cooldown";
        private const string LaunchStoredEnergyInsufficientIssueCode = "launch-stored-energy-insufficient";
        private const string LaunchRangeUnavailableIssueCode = "launch-range-unavailable";
        private const string LaunchEnvironmentBlockedIssueCode = "launch-environment-blocked";
        private const string LaunchRuntimePreflightIssueCode = "launch-runtime-preflight";

        private readonly ShuttleControllerTickProfiler tickProfiler = new ShuttleControllerTickProfiler();
        private readonly List<ShuttleDeveloperDiagnosticModel> developerDiagnostics =
            new List<ShuttleDeveloperDiagnosticModel>();

        // Transient typed building host. This is rebound by CompModularShuttleCore after creation/load.
        private Building_ModularShuttle shuttleBuildingHost;

        // Transient real RimWorld host thing. Bridges use this to reach vanilla comps such as
        // CompTransporter and CompShuttle.
        private ThingWithComps shuttleHost;

        // Durable assembly truth. It stores topology and static player configuration.
        private ShuttleAssemblyState assemblyState = new ShuttleAssemblyState();

        // Durable live runtime truth. Runtime systems mutate buckets here; profile stays derived.
        private ShuttleRuntimeState runtimeState = new ShuttleRuntimeState();

        // Derived, non-persistent profile cache. It is rebuilt from AssemblyState whenever dirty.
        private ShuttleProfile profile;

        // Bitmask of the reasons that caused the current profile rebuild request.
        private ProfileDirtyReason pendingDirtyReasons = ProfileDirtyReason.None;

        // Dirty flag for the cached profile. Runtime cargo/launch changes should not toggle this.
        private bool profileDirty = true;

        // Monotonic profile revision for UI/debugging. It is intentionally not persisted.
        private int profileRevision;

        // Last profile revision pushed into runtime/external seams. These are non-persistent
        // cache guards; save/load replays sync from assembly/profile/runtime truth.
        private int lastRuntimeSyncProfileRevision = -1;
        private int lastRuntimeSyncDispatchFingerprint = -1;
        private int lastExternalSyncProfileRevision = -1;
        private int cachedAutoWorkTableInventoryTick = int.MinValue;
        private int cachedAutoWorkTableInventoryProfileRevision = -1;
        private ShuttleCargoInventorySnapshot cachedAutoWorkTableInventorySnapshot;
        private ShuttleLoadCargoReadModel cachedLoadCargoReadModel;
        private ThingWithComps cachedLoadCargoReadModelHost;
        private ShuttleAssemblyState cachedLoadCargoReadModelAssemblyState;
        private ShuttleRuntimeState cachedLoadCargoReadModelRuntimeState;
        private int cachedLoadCargoReadModelTick = int.MinValue;
        private int cachedLoadCargoReadModelProfileRevision = -1;
        private int cachedLoadCargoReadModelExternalMassRevision;
        private ShuttleRuntimeMassContributionSnapshot cachedExternalMassContributionSnapshot;
        private ThingWithComps cachedExternalMassContributionHost;
        private ShuttleAssemblyState cachedExternalMassContributionAssemblyState;
        private ShuttleRuntimeState cachedExternalMassContributionRuntimeState;
        private int cachedExternalMassContributionProfileRevision = -1;
        private int cachedExternalMassContributionTick = int.MinValue;
        private int loadCargoReadModelRequestCount;
        private int loadCargoReadModelBuildCount;
        private int loadCargoReadModelLastDevLogTick = int.MinValue;
        private ShuttleCommandContext cachedCommandContext;
        private ThingWithComps cachedCommandContextHost;
        private Building_ModularShuttle cachedCommandContextBuildingHost;
        private ShuttleAssemblyState cachedCommandContextAssemblyState;
        private ShuttleCargoResourceBroker cachedCargoResourceBroker;
        private ThingWithComps cachedCargoResourceBrokerHost;
        private ShuttleAssemblyState cachedCargoResourceBrokerAssemblyState;
        private ShuttleRuntimeState cachedCargoResourceBrokerRuntimeState;
        private ShuttleProfile cachedCargoResourceBrokerProfile;
        private int cachedCargoResourceBrokerProfileRevision = -1;
        private ShuttleCargoColdTransferService cachedRefrigeratedCargoTransferService;
        private ThingWithComps cachedRefrigeratedCargoTransferServiceHost;
        private ShuttleAssemblyState cachedRefrigeratedCargoTransferServiceAssemblyState;
        private ShuttleRuntimeState cachedRefrigeratedCargoTransferServiceRuntimeState;
        private ShuttleProfile cachedRefrigeratedCargoTransferServiceProfile;
        private int cachedRefrigeratedCargoTransferServiceProfileRevision = -1;
        private ShuttleCargoPostDepositColdRouter cachedPostDepositColdRouter;
        private ThingWithComps cachedPostDepositColdRouterHost;
        private ShuttleAssemblyState cachedPostDepositColdRouterAssemblyState;
        private ShuttleRuntimeState cachedPostDepositColdRouterRuntimeState;
        private ShuttleProfile cachedPostDepositColdRouterProfile;
        private int cachedPostDepositColdRouterProfileRevision = -1;
        private bool refrigeratedCargoRegistryDirty = true;
        private int lastRefrigeratedCargoRegistryReconcileTick = int.MinValue;

        // Materializes layout defs into durable segment/module slot records.
        private readonly ShuttleAssemblyBootstrapper assemblyBootstrapper = new ShuttleAssemblyBootstrapper();

        // Owns legality checks and mutation of AssemblyState.
        private readonly ShuttleAssemblyMutationController assemblyMutationController;

        // Temporary cargo backend seam. CompTransporter stays hidden behind this until a native cargo system replaces it.
        private readonly IShuttleCargoBackend cargoBackend = new VanillaTransporterCargoBackend();

        // Internal shuttle power bus. It mutates only ShuttleRuntimeState and never touches RimWorld PowerNet.
        private readonly PowerSystem powerSystem = new PowerSystem();
        private readonly ShuttleModuleRemovalService moduleRemovalService =
            new ShuttleModuleRemovalService();
        private readonly ShuttleAssemblyConstructionSystem assemblyConstructionSystem =
            new ShuttleAssemblyConstructionSystem();
        private readonly ShuttleCargoUnloadSystem cargoUnloadSystem =
            new ShuttleCargoUnloadSystem();
        private readonly ShuttleStarterPresetAssemblyInstaller starterPresetAssemblyInstaller =
            new ShuttleStarterPresetAssemblyInstaller();
        private readonly ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator;
        private readonly ShuttlePrisonerRuntimeSystem prisonerRuntimeSystem =
            new ShuttlePrisonerRuntimeSystem();

        private readonly IShuttleFlightEnergyCalculator flightEnergyCalculator;
        private readonly ShuttleLaunchValidator launchValidator;
        private readonly ShuttleLaunchInputFactory launchInputFactory = new ShuttleLaunchInputFactory();
        private readonly ShuttleWorldTargetingService worldTargetingService;
        private readonly ElectricShuttleLaunchService electricLaunchService;
        private readonly ShuttleCommandDispatcher commandDispatcher;

        // External adapter for exposing runtime-calculated reactor surplus to RimWorld's power grid.
        private readonly PowerPlantBridge powerPlantBridge = new PowerPlantBridge();

        // Host comp configuration used only for the first runtime charge initialization.
        private float initialStoredEnergyPercent = 1f;

        public ShuttleController()
        {
            this.assemblyMutationController = new ShuttleAssemblyMutationController(this.assemblyBootstrapper);
            this.moduleRuntimeCoordinator = new ShuttleModuleRuntimeCoordinator(BuiltInShuttleModuleRuntimeRegistry.CreateDefault());
            this.flightEnergyCalculator = new DefaultShuttleFlightEnergyCalculator();
            this.launchValidator = new ShuttleLaunchValidator(this.cargoBackend, this.moduleRuntimeCoordinator);
            this.worldTargetingService = new ShuttleWorldTargetingService(this.flightEnergyCalculator, this.cargoBackend);
            this.electricLaunchService = new ElectricShuttleLaunchService(
                this.powerSystem,
                this.launchValidator,
                this.cargoBackend,
                new DefaultShuttleSkyfallerFactory(),
                this.moduleRuntimeCoordinator);
            this.commandDispatcher = new ShuttleCommandDispatcher(new IShuttleCommandHandler[]
            {
                new ShuttleAssemblyConstructionCommandHandler(),
                new ShuttleModuleRemovalCommandHandler(),
                new SetExternalRuntimeEnabledCommandHandler(),
                new ExternalShuttleCommandHandlerAdapter(),
                new ShuttleAssemblyCommandHandler(),
                new ShuttleCargoUnloadCommandHandler(),
                new ShuttleCargoCommandHandler(),
                new ShuttleRefrigeratedCargoCommandHandler(),
                new ShuttleHabitatCommandHandler(),
                new ShuttleMedicalBayCommandHandler(),
                new ShuttlePrisonCellCommandHandler(),
                new ShuttleMechChargerCommandHandler(),
                new ShuttleShieldCommandHandler(),
                new ShuttleWeaponCommandHandler(),
                new ShuttleAutoWorkTableCommandHandler(),
                new ShuttlePaintCommandHandler(),
                new ShuttleLaunchCommandHandler()
            });
        }


        internal Building_ModularShuttle ShuttleBuildingHost
        {
            get
            {
                return this.shuttleBuildingHost;
            }
        }

        internal ThingWithComps ShuttleHost
        {
            get
            {
                return this.shuttleHost;
            }
        }

        internal ShuttleAssemblyState AssemblyState
        {
            get
            {
                return this.assemblyState;
            }
        }

        internal ShuttleProfile CurrentProfile
        {
            get
            {
                return this.GetProfileForRead();
            }
        }

        internal bool IsProfileDirty
        {
            get
            {
                return this.profileDirty;
            }
        }

        internal int ProfileRevision
        {
            get
            {
                return this.profileRevision;
            }
        }

        internal ShuttleCargoRegionConfigState CargoRegionConfig
        {
            get
            {
                this.GetProfileForRead();
                if (this.assemblyState == null)
                {
                    return null;
                }

                return this.assemblyState.CargoRegionConfig;
            }
        }

        internal int ActiveCargoRegionCount
        {
            get
            {
                ShuttleProfile currentProfile = this.GetProfileForRead();
                if (currentProfile == null || currentProfile.Cargo == null)
                {
                    return 0;
                }

                return (int)currentProfile.Cargo.CargoRegionCount;
            }
        }

    }
}
