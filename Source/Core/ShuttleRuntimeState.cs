using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Durable live shuttle state. Static topology stays in AssemblyState, and derived capacity
    /// stays in ShuttleProfile; this object owns values that must survive save/load.
    /// </summary>
    public sealed class ShuttleRuntimeState : IExposable
    {
        private const int CurrentSaveVersion = 8;

        private int saveVersion = CurrentSaveVersion;
        private int tickCounter;
        private bool initializationComplete;
        private PowerRuntimeState power = new PowerRuntimeState();
        private LaunchRuntimeState launch = new LaunchRuntimeState();
        private ShuttleHullRuntimeState hull = new ShuttleHullRuntimeState();
        private ShuttleModuleRuntimeStateBucket modules = new ShuttleModuleRuntimeStateBucket();
        private ShuttleModuleRemovalState moduleRemoval = new ShuttleModuleRemovalState();
        private ShuttleAssemblyConstructionState assemblyConstruction = new ShuttleAssemblyConstructionState();
        private MedicalBayProcedureRuntimeState medicalProcedures = new MedicalBayProcedureRuntimeState();
        private ShuttlePendingLoadDestinationState pendingLoadDestinations =
            new ShuttlePendingLoadDestinationState();
        private ShuttlePassengerBoardingIntentState passengerBoardingIntents =
            new ShuttlePassengerBoardingIntentState();
        private ShuttleCargoUnloadState cargoUnload = new ShuttleCargoUnloadState();

        public int TickCounter
        {
            get
            {
                return this.tickCounter;
            }
        }

        public PowerRuntimeState Power
        {
            get
            {
                this.EnsureInitialized();
                return this.power;
            }
        }

        public LaunchRuntimeState Launch
        {
            get
            {
                this.EnsureInitialized();
                return this.launch;
            }
        }

        public ShuttleHullRuntimeState Hull
        {
            get
            {
                this.EnsureInitialized();
                return this.hull;
            }
        }

        internal ShuttleModuleRuntimeStateBucket Modules
        {
            get
            {
                this.EnsureInitialized();
                return this.modules;
            }
        }

        public ShuttleModuleRemovalState ModuleRemoval
        {
            get
            {
                this.EnsureInitialized();
                return this.moduleRemoval;
            }
        }

        public ShuttleAssemblyConstructionState AssemblyConstruction
        {
            get
            {
                this.EnsureInitialized();
                return this.assemblyConstruction;
            }
        }

        internal MedicalBayProcedureRuntimeState MedicalProcedures
        {
            get
            {
                this.EnsureInitialized();
                return this.medicalProcedures;
            }
        }

        internal ShuttlePendingLoadDestinationState PendingLoadDestinations
        {
            get
            {
                this.EnsureInitialized();
                return this.pendingLoadDestinations;
            }
        }

        internal ShuttlePassengerBoardingIntentState PassengerBoardingIntents
        {
            get
            {
                this.EnsureInitialized();
                return this.passengerBoardingIntents;
            }
        }

        public ShuttleCargoUnloadState CargoUnload
        {
            get
            {
                this.EnsureInitialized();
                return this.cargoUnload;
            }
        }

        public void EnsureInitialized()
        {
            if (this.initializationComplete)
            {
                return;
            }

            if (this.power == null)
            {
                this.power = new PowerRuntimeState();
            }

            if (this.launch == null)
            {
                this.launch = new LaunchRuntimeState();
            }

            if (this.hull == null)
            {
                this.hull = new ShuttleHullRuntimeState();
            }

            if (this.modules == null)
            {
                this.modules = new ShuttleModuleRuntimeStateBucket();
            }

            if (this.moduleRemoval == null)
            {
                this.moduleRemoval = new ShuttleModuleRemovalState();
            }

            if (this.assemblyConstruction == null)
            {
                this.assemblyConstruction = new ShuttleAssemblyConstructionState();
            }

            if (this.medicalProcedures == null)
            {
                this.medicalProcedures = new MedicalBayProcedureRuntimeState();
            }

            if (this.pendingLoadDestinations == null)
            {
                this.pendingLoadDestinations = new ShuttlePendingLoadDestinationState();
            }

            if (this.passengerBoardingIntents == null)
            {
                this.passengerBoardingIntents =
                    new ShuttlePassengerBoardingIntentState();
            }

            if (this.cargoUnload == null)
            {
                this.cargoUnload = new ShuttleCargoUnloadState();
            }

            this.modules.EnsureInitialized();
            this.moduleRemoval.EnsureInitialized();
            this.assemblyConstruction.EnsureInitialized();
            this.medicalProcedures.EnsureInitialized();
            this.pendingLoadDestinations.EnsureInitialized();
            this.passengerBoardingIntents.EnsureInitialized();
            this.cargoUnload.EnsureInitialized();
            this.initializationComplete = true;
        }

        public void NotifyTick()
        {
            this.tickCounter++;
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.initializationComplete = false;
            }

            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.tickCounter, "tickCounter", 0);
            Scribe_Deep.Look(ref this.power, "power");
            Scribe_Deep.Look(ref this.launch, "launch");
            Scribe_Deep.Look(ref this.hull, "hull");
            Scribe_Deep.Look(ref this.modules, "modules");
            Scribe_Deep.Look(ref this.moduleRemoval, "moduleRemoval");
            Scribe_Deep.Look(ref this.assemblyConstruction, "assemblyConstruction");
            Scribe_Deep.Look(ref this.medicalProcedures, "medicalProcedures");
            Scribe_Deep.Look(ref this.pendingLoadDestinations, "pendingLoadDestinations");
            Scribe_Deep.Look(
                ref this.passengerBoardingIntents,
                "passengerBoardingIntents");
            Scribe_Deep.Look(ref this.cargoUnload, "cargoUnload");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                this.initializationComplete = false;
                this.EnsureInitialized();
                this.power.SanitizePostLoad();
                this.modules.NormalizeAfterLoad();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ShuttleRuntimeState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded runtime state intact.");
            }

            // Version 0 predates persisted schema metadata. It intentionally receives no
            // destructive migration: keep stored energy, cooldown ticks, launch ticks, and
            // diagnostics as loaded. EnsureInitialized already repairs only missing buckets.
            if (loadedVersion < 1)
            {
                return;
            }

            // Version 1 predates module runtime buckets. Missing module buckets are repaired to
            // an empty durable collection without changing existing power or launch truth.
            if (loadedVersion < 2)
            {
                return;
            }

            // Version 2 predates whole-shuttle hull runtime state. Missing hull state is repaired
            // by EnsureInitialized and then reconciled from the current profile sync pass.
            if (loadedVersion < 3)
            {
                return;
            }

            // Version 3 predates Medical Bay procedure runtime state. Missing procedure state is
            // repaired to an empty bucket; active doctor holders are owned by the host comp.
            if (loadedVersion < 4)
            {
                return;
            }

            // Version 4 predates module removal runtime state. Missing removal state is repaired
            // to an empty bucket; installed topology remains in AssemblyState.
            if (loadedVersion < 5)
            {
                return;
            }

            // Version 5 predates pending load destination plans. Missing plans are repaired to
            // an empty list; vanilla transporter queues continue to fall back to normal cargo.
            if (loadedVersion < 6)
            {
                return;
            }

            // Version 6 predates the durable global cargo unload intent. Missing state is
            // repaired to an inactive empty plan; cargo remains owned by its existing holders.
            if (loadedVersion < 7)
            {
                return;
            }

            // Version 7 predates durable boarding intent for pawns selected while held by
            // shuttle modules. Missing state is repaired to an empty intent list.
            if (loadedVersion < 8)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Durable electric-drive power bucket. PowerSystem owns tick math; this owns save/load truth.
    /// </summary>
    public sealed class PowerRuntimeState : IExposable
    {
        private float storedEnergyWd;
        private bool hasInitializedCharge;
        private float lastGridExportWatts;
        private bool internalBusPowered;
        private float lastReactorGenerationWatts;
        private float lastInternalDemandWatts;
        private float lastBatteryChargeWatts;
        private float lastBatteryDischargeWatts;
        private float lastUnmetDemandWatts;
        private float lastAppliedEnergyCapacityWd;
        private float transientInternalDemandWatts;
        private float lastTransientInternalDemandWatts;

        public float StoredEnergyWd
        {
            get
            {
                return this.storedEnergyWd;
            }
            set
            {
                this.storedEnergyWd = value;
            }
        }

        public bool HasInitializedCharge
        {
            get
            {
                return this.hasInitializedCharge;
            }
            set
            {
                this.hasInitializedCharge = value;
            }
        }

        public float LastGridExportWatts
        {
            get
            {
                return this.lastGridExportWatts;
            }
            set
            {
                this.lastGridExportWatts = value;
            }
        }

        public bool InternalBusPowered
        {
            get
            {
                return this.internalBusPowered;
            }
            set
            {
                this.internalBusPowered = value;
            }
        }

        public float LastReactorGenerationWatts
        {
            get
            {
                return this.lastReactorGenerationWatts;
            }
            set
            {
                this.lastReactorGenerationWatts = value;
            }
        }

        public float LastInternalDemandWatts
        {
            get
            {
                return this.lastInternalDemandWatts;
            }
            set
            {
                this.lastInternalDemandWatts = value;
            }
        }

        public float LastBatteryChargeWatts
        {
            get
            {
                return this.lastBatteryChargeWatts;
            }
            set
            {
                this.lastBatteryChargeWatts = value;
            }
        }

        public float LastBatteryDischargeWatts
        {
            get
            {
                return this.lastBatteryDischargeWatts;
            }
            set
            {
                this.lastBatteryDischargeWatts = value;
            }
        }

        public float LastUnmetDemandWatts
        {
            get
            {
                return this.lastUnmetDemandWatts;
            }
            set
            {
                this.lastUnmetDemandWatts = value;
            }
        }

        internal float TransientInternalDemandWatts
        {
            get
            {
                return this.transientInternalDemandWatts;
            }
            set
            {
                this.transientInternalDemandWatts = this.SanitizeNonNegativeFinite(value);
            }
        }

        public float LastTransientInternalDemandWatts
        {
            get
            {
                return this.lastTransientInternalDemandWatts;
            }
        }

        public float LastAppliedEnergyCapacityWd
        {
            get
            {
                return this.lastAppliedEnergyCapacityWd;
            }
            set
            {
                this.lastAppliedEnergyCapacityWd = value;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.storedEnergyWd, "storedEnergyWd", 0f);
            Scribe_Values.Look(ref this.hasInitializedCharge, "hasInitializedCharge", false);
            Scribe_Values.Look(ref this.lastGridExportWatts, "lastGridExportWatts", 0f);
            Scribe_Values.Look(ref this.internalBusPowered, "internalBusPowered", false);
            Scribe_Values.Look(ref this.lastReactorGenerationWatts, "lastReactorGenerationWatts", 0f);
            Scribe_Values.Look(ref this.lastInternalDemandWatts, "lastInternalDemandWatts", 0f);
            Scribe_Values.Look(ref this.lastBatteryChargeWatts, "lastBatteryChargeWatts", 0f);
            Scribe_Values.Look(ref this.lastBatteryDischargeWatts, "lastBatteryDischargeWatts", 0f);
            Scribe_Values.Look(ref this.lastUnmetDemandWatts, "lastUnmetDemandWatts", 0f);
            Scribe_Values.Look(ref this.lastAppliedEnergyCapacityWd, "lastAppliedEnergyCapacityWd", 0f);
        }

        internal float ConsumeTransientInternalDemandWatts()
        {
            float result = this.SanitizeNonNegativeFinite(this.transientInternalDemandWatts);
            this.transientInternalDemandWatts = 0f;
            this.lastTransientInternalDemandWatts = result;
            return result;
        }

        internal void SanitizePostLoad()
        {
            this.storedEnergyWd = this.SanitizeNonNegativeFinite(this.storedEnergyWd);
            this.lastGridExportWatts = this.SanitizeNonNegativeFinite(this.lastGridExportWatts);
            this.lastReactorGenerationWatts = this.SanitizeNonNegativeFinite(this.lastReactorGenerationWatts);
            this.lastInternalDemandWatts = this.SanitizeNonNegativeFinite(this.lastInternalDemandWatts);
            this.lastBatteryChargeWatts = this.SanitizeNonNegativeFinite(this.lastBatteryChargeWatts);
            this.lastBatteryDischargeWatts = this.SanitizeNonNegativeFinite(this.lastBatteryDischargeWatts);
            this.lastUnmetDemandWatts = this.SanitizeNonNegativeFinite(this.lastUnmetDemandWatts);
            this.lastAppliedEnergyCapacityWd = this.SanitizeNonNegativeFinite(this.lastAppliedEnergyCapacityWd);
            this.transientInternalDemandWatts = 0f;
            this.lastTransientInternalDemandWatts = this.SanitizeNonNegativeFinite(this.lastTransientInternalDemandWatts);
        }

        private float SanitizeNonNegativeFinite(float value)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Durable launch timing bucket. Electric launch behavior remains unchanged until M4.
    /// </summary>
    public sealed class LaunchRuntimeState : IExposable
    {
        private int lastLaunchTick = -1;
        private int cooldownEndTick = -1;
        private int lastArrivalTick = -1;

        public int LastLaunchTick
        {
            get
            {
                return this.lastLaunchTick;
            }
            set
            {
                this.lastLaunchTick = value;
            }
        }

        public int CooldownEndTick
        {
            get
            {
                return this.cooldownEndTick;
            }
            set
            {
                this.cooldownEndTick = value;
            }
        }

        public int LastArrivalTick
        {
            get
            {
                return this.lastArrivalTick;
            }
            set
            {
                this.lastArrivalTick = value;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.lastLaunchTick, "lastLaunchTick", -1);
            Scribe_Values.Look(ref this.cooldownEndTick, "cooldownEndTick", -1);
            Scribe_Values.Look(ref this.lastArrivalTick, "lastArrivalTick", -1);
        }
    }
}
