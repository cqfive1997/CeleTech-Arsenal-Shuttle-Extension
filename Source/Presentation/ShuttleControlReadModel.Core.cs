using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// Dialog-facing control projection built from profile sections and detached snapshots.
    /// UI code reads this model instead of walking live assembly or runtime state directly.
    /// </summary>
    public sealed class ShuttleControlReadModel
    {
        public const int DeveloperDiagnosticsMax = 500;
        public const int PerformanceDiagnosticsMax = 200;

        public string ShuttleLabel;
        public int ProfileRevision;
        public bool IsProfileDirty;
        public int SegmentSlotCount;
        public int InstalledSegmentCount;
        public int ModuleSlotCount;
        public int InstalledModuleCount;
        public float TotalMass;
        public float ExternalRuntimeMassKg;
        public float MassCapacity;
        public float RemainingMassCapacity;
        public float CargoMassCapacityKg;
        public bool HasCockpit;
        public bool StabilizesAdverseWeather;
        public bool ProvidesAutonomousLaunchControl;
        public float PowerGeneration;
        public float StoredEnergyWd;
        public float EnergyCapacityWd;
        public float ReactorGenerationWatts;
        public float InternalIdleDemandWatts;
        public float MaxBatteryChargeWatts;
        public float MaxBatteryDischargeWatts;
        public float GridExportWatts;
        public float BatteryChargeWatts;
        public float BatteryDischargeWatts;
        public bool HasPowerRuntimeSnapshot;
        public float CurrentReactorGenerationWatts;
        // Compatibility name: this is the last-tick total idle-plus-transient demand.
        public float ActiveInternalDemandWatts;
        // Runtime-only surcharge collected in addition to profile idle demand.
        public float TransientInternalDemandWatts;
        public float UnmetInternalDemandWatts;
        public bool InternalBusPowered;
        public bool HasCargoLogistics;
        public bool CargoLogisticsSupportsItemTransfer;
        public bool CargoLogisticsSupportsItemConsumption;
        public bool CargoLogisticsSupportsItemDeposit;
        public bool CargoLogisticsSupportsNutritionDistribution;
        public int CargoLogisticsMaxStacksMovedPerTick;
        public int HardRangeCapTiles;
        public int LaunchCooldownTicks;
        public int LaunchCooldownTotalTicks;
        public int LaunchCooldownRemainingTicks;
        public float LaunchReadyProgress = 1f;
        public bool HasLaunchCooldown;
        public float EnergyPerTileWd;
        public float EnergyPerKgTileWd;
        public uint CargoRegionCount;
        public ShuttleHabitatReadModel Habitat = new ShuttleHabitatReadModel();
        public ShuttleMedicalBayReadModel MedicalBay = new ShuttleMedicalBayReadModel();
        public ShuttleMechChargerReadModel MechCharger = new ShuttleMechChargerReadModel();
        public ShuttlePrisonCellReadModel PrisonCell = new ShuttlePrisonCellReadModel();
        internal ShuttleCargoSupplySnapshot CargoSupply;
        public ShuttleHullReadModel Hull = new ShuttleHullReadModel();
        public ShuttleAssemblyConstructionReadModel AssemblyConstruction = new ShuttleAssemblyConstructionReadModel();
        public ShuttlePaintSchemeSnapshot PaintScheme = ShuttlePaintSchemeSnapshot.Default;
        public ShuttleControlPageAvailabilityReadModel PageAvailability = new ShuttleControlPageAvailabilityReadModel();
        public List<ShuttleControlSegmentSlotModel> SegmentSlots = new List<ShuttleControlSegmentSlotModel>();
        public List<ShuttleControlIssueModel> Issues = new List<ShuttleControlIssueModel>();
        public List<ShuttleLaunchChecklistItemModel> LaunchChecklist =
            new List<ShuttleLaunchChecklistItemModel>();
        public List<ShuttleDeveloperDiagnosticModel> DeveloperDiagnostics =
            new List<ShuttleDeveloperDiagnosticModel>();

        public ShuttleControlSegmentSlotModel FindSegmentSlot(string slotID)
        {
            if (string.IsNullOrEmpty(slotID) || this.SegmentSlots == null)
            {
                return null;
            }

            for (int i = 0; i < this.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel slot = this.SegmentSlots[i];
                if (slot != null && slot.SlotID == slotID)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
