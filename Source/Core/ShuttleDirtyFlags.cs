using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    [Flags]
    public enum ShuttleDirtyFlags
    {
        None = 0,

        // The live host and non-persistent references must be rebound after load before
        // any derived or runtime-facing work is trusted again.
        PostLoadRebind = 1 << 0,

        // Assembly topology caches or slot-derived views are stale because segment or module
        // ownership changed. This flag is about install structure, not runtime values.
        AssemblyTopology = 1 << 1,

        // The derived static shuttle snapshot must be rebuilt from the current assembly state.
        Profile = 1 << 2,

        // Runtime state must be reconciled against the refreshed assembly/profile result.
        RuntimeSync = 1 << 3,

        // Host adapters, bridges, or vanilla-facing integration seams must be synchronized from
        // the refreshed derived state.
        ExternalSync = 1 << 4,

        // Read-models, UI summaries, and other projected views should be rebuilt on demand.
        ReadModel = 1 << 5,
    }
}
