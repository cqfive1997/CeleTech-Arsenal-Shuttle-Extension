using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Describes why the cached shuttle profile must be rebuilt.
    /// These reasons are diagnostic metadata for the controller, not a replacement for dirty flags.
    /// </summary>
    [Flags]
    public enum ProfileDirtyReason
    {
        None = 0,
        InitialCreation = 1 << 0,
        LoadCompleted = 1 << 1,
        AssemblyChanged = 1 << 2,
        AssemblyTopologyChanged = 1 << 3,
        ControllerBound = 1 << 4,
        CombatTuningChanged = 1 << 5,
        OtherSettingsChanged = 1 << 6
    }
}
