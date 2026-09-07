using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public sealed class ShuttleExternalReadSnapshot
    {
        public ShuttleExternalReadSnapshot(
            bool available,
            string unavailableReason,
            int revision,
            int ticksGame,
            ShuttleExternalHostSnapshot host,
            ShuttleExternalAssemblySnapshot assembly,
            ShuttleExternalProfileSnapshot profile,
            ShuttleExternalRuntimeSummarySnapshot runtime,
            ShuttleExternalPowerSnapshot power,
            ShuttleExternalCargoSummarySnapshot cargo,
            ShuttleExternalLaunchSummarySnapshot launch,
            ShuttleExternalIntegrationHealthReport integrationHealth)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.Revision = revision;
            this.TicksGame = ticksGame;
            this.Host = host;
            this.Assembly = assembly;
            this.Profile = profile;
            this.Runtime = runtime;
            this.Power = power;
            this.Cargo = cargo;
            this.Launch = launch;
            this.IntegrationHealth = integrationHealth;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public int Revision { get; private set; }
        public int TicksGame { get; private set; }
        public ShuttleExternalHostSnapshot Host { get; private set; }
        public ShuttleExternalAssemblySnapshot Assembly { get; private set; }
        public ShuttleExternalProfileSnapshot Profile { get; private set; }
        public ShuttleExternalRuntimeSummarySnapshot Runtime { get; private set; }
        public ShuttleExternalPowerSnapshot Power { get; private set; }
        public ShuttleExternalCargoSummarySnapshot Cargo { get; private set; }
        public ShuttleExternalLaunchSummarySnapshot Launch { get; private set; }
        public ShuttleExternalIntegrationHealthReport IntegrationHealth { get; private set; }
    }

    public sealed class ShuttleExternalCellSnapshot
    {
        public ShuttleExternalCellSnapshot(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
    }

    public sealed class ShuttleExternalHostSnapshot
    {
        public ShuttleExternalHostSnapshot(
            int hostIdNumber,
            string hostStableId,
            string hostDefName,
            string hostLabel,
            string factionDefName,
            bool spawned,
            int locationUniqueId,
            string tile,
            int positionX,
            int positionY,
            int positionZ,
            int rotationAsInt,
            IEnumerable<ShuttleExternalCellSnapshot> occupiedCells,
            IEnumerable<ShuttleExternalCellSnapshot> adjacentExternalCells)
        {
            this.HostIdNumber = hostIdNumber;
            this.HostStableId = hostStableId;
            this.HostDefName = hostDefName;
            this.HostLabel = hostLabel;
            this.FactionDefName = factionDefName;
            this.Spawned = spawned;
            this.LocationUniqueId = locationUniqueId;
            this.Tile = tile;
            this.PositionX = positionX;
            this.PositionY = positionY;
            this.PositionZ = positionZ;
            this.RotationAsInt = rotationAsInt;
            this.OccupiedCells = ShuttleExternalSDKCollections.Copy(occupiedCells);
            this.AdjacentExternalCells = ShuttleExternalSDKCollections.Copy(adjacentExternalCells);
        }

        public int HostIdNumber { get; private set; }
        public string HostStableId { get; private set; }
        public string HostDefName { get; private set; }
        public string HostLabel { get; private set; }
        public string FactionDefName { get; private set; }
        public bool Spawned { get; private set; }
        public int LocationUniqueId { get; private set; }
        public string Tile { get; private set; }
        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public int PositionZ { get; private set; }
        public int RotationAsInt { get; private set; }
        public IReadOnlyList<ShuttleExternalCellSnapshot> OccupiedCells { get; private set; }
        public IReadOnlyList<ShuttleExternalCellSnapshot> AdjacentExternalCells { get; private set; }
    }

    public sealed class ShuttleExternalAssemblySnapshot
    {
        public ShuttleExternalAssemblySnapshot(
            bool available,
            string unavailableReason,
            string hostStableId,
            int segmentCount,
            int moduleCount,
            int installedModuleCount,
            int enabledModuleCount,
            int disabledModuleCount,
            int externallyExtensibleModuleCount,
            bool assemblyConstructionActive,
            bool moduleRemovalActive,
            IEnumerable<ShuttleExternalModuleSummarySnapshot> moduleRows)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.HostStableId = hostStableId;
            this.SegmentCount = segmentCount;
            this.ModuleCount = moduleCount;
            this.InstalledModuleCount = installedModuleCount;
            this.EnabledModuleCount = enabledModuleCount;
            this.DisabledModuleCount = disabledModuleCount;
            this.ExternallyExtensibleModuleCount = externallyExtensibleModuleCount;
            this.AssemblyConstructionActive = assemblyConstructionActive;
            this.ModuleRemovalActive = moduleRemovalActive;
            this.ModuleRows = ShuttleExternalSDKCollections.Copy(moduleRows);
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public string HostStableId { get; private set; }
        public int SegmentCount { get; private set; }
        public int ModuleCount { get; private set; }
        public int InstalledModuleCount { get; private set; }
        public int EnabledModuleCount { get; private set; }
        public int DisabledModuleCount { get; private set; }
        public int ExternallyExtensibleModuleCount { get; private set; }
        public bool AssemblyConstructionActive { get; private set; }
        public bool ModuleRemovalActive { get; private set; }
        public IReadOnlyList<ShuttleExternalModuleSummarySnapshot> ModuleRows { get; private set; }
    }

    public sealed class ShuttleExternalModuleSummarySnapshot
    {
        public ShuttleExternalModuleSummarySnapshot(
            string moduleInstanceId,
            string moduleDefName,
            string moduleLabel,
            string segmentInstanceId,
            string slotId,
            string slotTypeId,
            bool enabled,
            bool installed,
            bool constructed,
            bool underConstruction,
            bool removalPending,
            bool externallyExtensible,
            IEnumerable<string> externalRuntimeKeys)
        {
            this.ModuleInstanceId = moduleInstanceId;
            this.ModuleDefName = moduleDefName;
            this.ModuleLabel = moduleLabel;
            this.SegmentInstanceId = segmentInstanceId;
            this.SlotId = slotId;
            this.SlotTypeId = slotTypeId;
            this.Enabled = enabled;
            this.Installed = installed;
            this.Constructed = constructed;
            this.UnderConstruction = underConstruction;
            this.RemovalPending = removalPending;
            this.ExternallyExtensible = externallyExtensible;
            this.ExternalRuntimeKeys = ShuttleExternalSDKCollections.CopyStrings(externalRuntimeKeys);
        }

        public string ModuleInstanceId { get; private set; }
        public string ModuleDefName { get; private set; }
        public string ModuleLabel { get; private set; }
        public string SegmentInstanceId { get; private set; }
        public string SlotId { get; private set; }
        public string SlotTypeId { get; private set; }
        public bool Enabled { get; private set; }
        public bool Installed { get; private set; }
        public bool Constructed { get; private set; }
        public bool UnderConstruction { get; private set; }
        public bool RemovalPending { get; private set; }
        public bool ExternallyExtensible { get; private set; }
        public IReadOnlyList<string> ExternalRuntimeKeys { get; private set; }
    }

    public sealed class ShuttleExternalProfileSnapshot
    {
        public ShuttleExternalProfileSnapshot(
            bool available,
            string unavailableReason,
            int revision,
            float structuralMassKg,
            float grossMassCapacityKg,
            float cargoMassCapacityKg,
            int hardRangeCapTiles,
            float energyStorageCapacityWd,
            float reactorGenerationWatts,
            float internalIdleDemandWatts,
            int crewCapacity,
            int cargoRegionCount,
            bool hasMedicalBay,
            int medicalPatientSlots,
            bool hasPrisonCell,
            int prisonerSlots,
            bool hasMechCharger,
            int mechChargeSlots,
            bool hasDefenseSystems,
            int defenseSystemCount,
            int issueCount,
            int warningCount,
            int errorCount,
            ShuttleExternalProfileExtensionSnapshot externalProfileExtensions)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.Revision = revision;
            this.StructuralMassKg = structuralMassKg;
            this.GrossMassCapacityKg = grossMassCapacityKg;
            this.CargoMassCapacityKg = cargoMassCapacityKg;
            this.HardRangeCapTiles = hardRangeCapTiles;
            this.EnergyStorageCapacityWd = energyStorageCapacityWd;
            this.ReactorGenerationWatts = reactorGenerationWatts;
            this.InternalIdleDemandWatts = internalIdleDemandWatts;
            this.CrewCapacity = crewCapacity;
            this.CargoRegionCount = cargoRegionCount;
            this.HasMedicalBay = hasMedicalBay;
            this.MedicalPatientSlots = medicalPatientSlots;
            this.HasPrisonCell = hasPrisonCell;
            this.PrisonerSlots = prisonerSlots;
            this.HasMechCharger = hasMechCharger;
            this.MechChargeSlots = mechChargeSlots;
            this.HasDefenseSystems = hasDefenseSystems;
            this.DefenseSystemCount = defenseSystemCount;
            this.IssueCount = issueCount;
            this.WarningCount = warningCount;
            this.ErrorCount = errorCount;
            this.ExternalProfileExtensions =
                externalProfileExtensions ?? ShuttleExternalProfileExtensionSnapshot.Empty();
            this.ExternalCapabilityCount =
                this.ExternalProfileExtensions.ExternalCapabilityCount;
            this.ExternalMetricCount =
                this.ExternalProfileExtensions.ExternalMetricCount;
            this.ExternalRequirementCount =
                this.ExternalProfileExtensions.ExternalRequirementCount;
            this.ExternalRequirementWarningCount =
                this.ExternalProfileExtensions.ExternalRequirementWarningCount;
            this.ExternalRequirementErrorCount =
                this.ExternalProfileExtensions.ExternalRequirementErrorCount;
            this.ExternalRequirementBlockerCount =
                this.ExternalProfileExtensions.ExternalRequirementBlockerCount;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public int Revision { get; private set; }
        public float StructuralMassKg { get; private set; }
        public float GrossMassCapacityKg { get; private set; }
        public float CargoMassCapacityKg { get; private set; }
        public int HardRangeCapTiles { get; private set; }
        public float EnergyStorageCapacityWd { get; private set; }
        public float ReactorGenerationWatts { get; private set; }
        public float InternalIdleDemandWatts { get; private set; }
        public int CrewCapacity { get; private set; }
        public int CargoRegionCount { get; private set; }
        public bool HasMedicalBay { get; private set; }
        public int MedicalPatientSlots { get; private set; }
        public bool HasPrisonCell { get; private set; }
        public int PrisonerSlots { get; private set; }
        public bool HasMechCharger { get; private set; }
        public int MechChargeSlots { get; private set; }
        public bool HasDefenseSystems { get; private set; }
        public int DefenseSystemCount { get; private set; }
        public int IssueCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }
        public ShuttleExternalProfileExtensionSnapshot ExternalProfileExtensions { get; private set; }
        public int ExternalCapabilityCount { get; private set; }
        public int ExternalMetricCount { get; private set; }
        public int ExternalRequirementCount { get; private set; }
        public int ExternalRequirementWarningCount { get; private set; }
        public int ExternalRequirementErrorCount { get; private set; }
        public int ExternalRequirementBlockerCount { get; private set; }
    }

    public sealed class ShuttleExternalRuntimeSummarySnapshot
    {
        public ShuttleExternalRuntimeSummarySnapshot(
            int externalRuntimeCount,
            int enabledRuntimeCount,
            int failedRuntimeCount,
            int migrationFailedCount,
            IEnumerable<ShuttleExternalRuntimeHealthSnapshot> runtimeRows)
        {
            this.ExternalRuntimeCount = externalRuntimeCount;
            this.EnabledRuntimeCount = enabledRuntimeCount;
            this.FailedRuntimeCount = failedRuntimeCount;
            this.MigrationFailedCount = migrationFailedCount;
            this.RuntimeRows = ShuttleExternalSDKCollections.Copy(runtimeRows);
        }

        public int ExternalRuntimeCount { get; private set; }
        public int EnabledRuntimeCount { get; private set; }
        public int FailedRuntimeCount { get; private set; }
        public int MigrationFailedCount { get; private set; }
        public IReadOnlyList<ShuttleExternalRuntimeHealthSnapshot> RuntimeRows { get; private set; }
    }

    public sealed class ShuttleExternalPowerSnapshot
    {
        public ShuttleExternalPowerSnapshot(
            bool available,
            string unavailableReason,
            bool busPowered,
            float storedEnergyWd,
            float storageCapacityWd,
            float reactorGenerationWatts,
            float internalDemandWatts,
            float gridExportWatts,
            float unmetDemandWatts,
            bool shortage)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.BusPowered = busPowered;
            this.StoredEnergyWd = storedEnergyWd;
            this.StorageCapacityWd = storageCapacityWd;
            this.ReactorGenerationWatts = reactorGenerationWatts;
            this.InternalDemandWatts = internalDemandWatts;
            this.GridExportWatts = gridExportWatts;
            this.UnmetDemandWatts = unmetDemandWatts;
            this.Shortage = shortage;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public bool BusPowered { get; private set; }
        public float StoredEnergyWd { get; private set; }
        public float StorageCapacityWd { get; private set; }
        public float ReactorGenerationWatts { get; private set; }
        public float InternalDemandWatts { get; private set; }
        public float GridExportWatts { get; private set; }
        public float UnmetDemandWatts { get; private set; }
        public bool Shortage { get; private set; }
    }

    public sealed class ShuttleExternalCargoSummarySnapshot
    {
        public ShuttleExternalCargoSummarySnapshot(
            bool available,
            string unavailableReason,
            float totalCargoMassKg,
            float cargoMassCapacityKg,
            float usedMassKg,
            float freeMassKg,
            int loadedStackCount,
            int loadedUnitCount,
            int assignedStackCount,
            int assignedUnitCount,
            int blockedStackCount,
            int refrigeratedStackCount,
            int refrigeratedUnitCount,
            float refrigeratedMassKg)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.TotalCargoMassKg = totalCargoMassKg;
            this.CargoMassCapacityKg = cargoMassCapacityKg;
            this.UsedMassKg = usedMassKg;
            this.FreeMassKg = freeMassKg;
            this.LoadedStackCount = loadedStackCount;
            this.LoadedUnitCount = loadedUnitCount;
            this.AssignedStackCount = assignedStackCount;
            this.AssignedUnitCount = assignedUnitCount;
            this.BlockedStackCount = blockedStackCount;
            this.RefrigeratedStackCount = refrigeratedStackCount;
            this.RefrigeratedUnitCount = refrigeratedUnitCount;
            this.RefrigeratedMassKg = refrigeratedMassKg;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public float TotalCargoMassKg { get; private set; }
        public float CargoMassCapacityKg { get; private set; }
        public float UsedMassKg { get; private set; }
        public float FreeMassKg { get; private set; }
        public int LoadedStackCount { get; private set; }
        public int LoadedUnitCount { get; private set; }
        public int AssignedStackCount { get; private set; }
        public int AssignedUnitCount { get; private set; }
        public int BlockedStackCount { get; private set; }
        public int RefrigeratedStackCount { get; private set; }
        public int RefrigeratedUnitCount { get; private set; }
        public float RefrigeratedMassKg { get; private set; }
    }

    public sealed class ShuttleExternalLaunchSummarySnapshot
    {
        private readonly int cooldownEndTick;

        public ShuttleExternalLaunchSummarySnapshot(
            bool available,
            string unavailableReason,
            bool ready,
            int issueCount,
            int blockerCount,
            int warningCount,
            bool cooldownActive,
            int cooldownEndTick,
            int cooldownRemainingTicks,
            bool storedEnergyReady,
            float storedEnergyWd,
            float baseLaunchEnergyWd,
            int hardRangeCapTiles)
        {
            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.Ready = ready;
            this.IssueCount = issueCount;
            this.BlockerCount = blockerCount;
            this.WarningCount = warningCount;
            this.CooldownActive = cooldownActive;
            this.cooldownEndTick = cooldownEndTick;
            this.CooldownRemainingTicks = cooldownRemainingTicks;
            this.StoredEnergyReady = storedEnergyReady;
            this.StoredEnergyWd = storedEnergyWd;
            this.BaseLaunchEnergyWd = baseLaunchEnergyWd;
            this.HardRangeCapTiles = hardRangeCapTiles;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public bool Ready { get; private set; }
        public int IssueCount { get; private set; }
        public int BlockerCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool CooldownActive { get; private set; }
        public int CooldownEndTick
        {
            get
            {
                return this.cooldownEndTick;
            }
        }

        public int CooldownRemainingTicks { get; private set; }
        public bool StoredEnergyReady { get; private set; }
        public float StoredEnergyWd { get; private set; }
        public float BaseLaunchEnergyWd { get; private set; }
        public int HardRangeCapTiles { get; private set; }
    }
}
