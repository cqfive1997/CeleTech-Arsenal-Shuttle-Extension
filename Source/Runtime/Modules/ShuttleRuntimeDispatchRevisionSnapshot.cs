using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal struct ShuttleRuntimeDispatchRevisionSnapshot
    {
        private readonly int profileRevision;
        private readonly int registryFingerprint;
        private readonly int moduleDispatchRevision;
        private readonly int runtimeStateDispatchRevision;

        private ShuttleRuntimeDispatchRevisionSnapshot(
            int profileRevision,
            int registryFingerprint,
            int moduleDispatchRevision,
            int runtimeStateDispatchRevision)
        {
            this.profileRevision = profileRevision;
            this.registryFingerprint = registryFingerprint;
            this.moduleDispatchRevision = moduleDispatchRevision;
            this.runtimeStateDispatchRevision = runtimeStateDispatchRevision;
        }

        internal static ShuttleRuntimeDispatchRevisionSnapshot Capture(
            ShuttleProfile profile,
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            return new ShuttleRuntimeDispatchRevisionSnapshot(
                profile != null ? profile.Revision : -1,
                registry != null ? registry.DispatchFingerprint : 0,
                assemblyState != null ? assemblyState.ModuleDispatchRevision : -1,
                runtimeState != null && runtimeState.Modules != null
                    ? runtimeState.Modules.DispatchRevision
                    : -1);
        }

        internal bool Matches(
            ShuttleProfile profile,
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            ShuttleRuntimeDispatchRevisionSnapshot current = Capture(
                profile,
                registry,
                assemblyState,
                runtimeState);
            return this.profileRevision == current.profileRevision &&
                this.registryFingerprint == current.registryFingerprint &&
                this.moduleDispatchRevision == current.moduleDispatchRevision &&
                this.runtimeStateDispatchRevision == current.runtimeStateDispatchRevision;
        }
    }
}
