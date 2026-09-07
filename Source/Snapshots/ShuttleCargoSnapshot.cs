using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Snapshots
{
    /// <summary>
    /// Read-only cargo projection for UI and diagnostics.
    /// This is rebuilt from CompTransporter on demand and must never become persisted cargo truth.
    /// </summary>
    public sealed class ShuttleCargoSnapshot
    {
        public bool HasTransporter;
        public int CargoRegionCount;

        // Structural mass comes from shuttle profile/assembly projections; cargo mass values
        // come from the current vanilla transporter state.
        public float StructuralMass;
        public float LoadedMassKg;
        public float QueuedMassKg;
        public float TotalPlannedMassKg;
        public float ExternalRuntimeMassKg;
        public int ExternalRuntimeMassContributionCount;

        // Compatibility aliases for vanilla transporter mass UI.
        public float CargoMassUsage;
        public float MassUsage;
        public float MassCapacity;

        // Aggregate counts copied from loaded contents and queued transferables.
        public int StackCount;
        public int ThingCount;
        public int LoadedStackCount;
        public int LoadedThingCount;
        public int AssignedStackCount;
        public int AssignedThingCount;

        // Items that do not match any active cargo region filter.
        public int BlockedStackCount;

        public int RefrigeratedStackCount;
        public int RefrigeratedThingCount;
        public float RefrigeratedMassKg;
        public bool RefrigeratedMassSharesOverallCapacity;
        public float RefrigeratedMassCapacityKg;

        public int MedicalBayPatientCount;
        public float MedicalBayPatientMassKg;

        // Item rows summarize the real transporter contents at one point in time.
        // They are safe for display, but not for mutation or save/load ownership.
        public List<ShuttleCargoItemSnapshot> Items = new List<ShuttleCargoItemSnapshot>();

        public List<ShuttleRefrigeratedCargoModuleSnapshot> RefrigeratedCargoModules =
            new List<ShuttleRefrigeratedCargoModuleSnapshot>();
    }

    /// <summary>
    /// One display row copied from the current transporter contents.
    /// It may carry a transient Thing reference only for immediate icon drawing.
    /// </summary>
    public sealed class ShuttleCargoItemSnapshot
    {
        public string Label;
        public string DefName;

        // Coarse UI category used by the visual cargo-region filters.
        // It is not a gameplay rule or cargo assignment.
        public string Category;

        // -1 means no active cargo region accepted this row.
        public int CargoRegionIndex;

        // Vanilla transporter coordinates used by command handlers for controlled mutations.
        public int TransporterIndex;
        public int LoadedIndex;
        public int QueueIndex;
        public int ThingIDNumber;
        public int StackCount;
        public float Mass;
        public bool IsLoaded;
        public bool IsAssignedToLoad;
        public bool IsPawn;

        // Transient display source for Verse icon drawing. This snapshot is rebuilt on demand
        // and is never persisted, so this must not be treated as cargo ownership truth.
        public Thing DisplayThing;
    }

    public sealed class ShuttleRefrigeratedCargoModuleSnapshot
    {
        public string ModuleInstanceID;
        public string ModuleDefName;
        public string Label;
        public bool HasCustomLabel;
        public bool ModuleResolved;
        public bool IsEnabled;
        public bool CoolingActive;
        public string InactiveReason;
        public float TargetTemperatureC;
        public float StoredMassKg;
        public float CapacityKg;
        public int StackCount;
        public int ThingCount;
        public bool AutoTransferEnabled;
        public bool HasCustomAutoTransferFilter;
        public ThingFilter AutoTransferFilter;
        public List<ShuttleRefrigeratedCargoItemSnapshot> Items =
            new List<ShuttleRefrigeratedCargoItemSnapshot>();
    }

    public sealed class ShuttleRefrigeratedCargoItemSnapshot
    {
        public string ModuleInstanceID;
        public int ColdIndex;
        public int ThingIDNumber;
        public string DefName;
        public string Label;
        public int StackCount;
        public float Mass;
        public Thing DisplayThing;
    }
}
