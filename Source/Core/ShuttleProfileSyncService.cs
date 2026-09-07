using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Applies profile-derived state to runtime and host seams only when dirty gates require it.
    /// The service is stateless; controller-owned revision guards stay non-persistent.
    /// </summary>
    internal sealed class ShuttleProfileSyncService
    {
        private readonly ShuttleHullIntegrityService hullIntegrityService =
            new ShuttleHullIntegrityService();

        internal int SyncRuntimeToProfileIfNeeded(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile currentProfile,
            ShuttleRuntimeState runtimeState,
            PowerSystem powerSystem,
            ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator,
            IShuttleCargoBackend cargoBackend,
            int lastRuntimeSyncProfileRevision,
            int profileRevisionFallback,
            float initialStoredEnergyPercent,
            int ticksGame)
        {
            if (!this.NeedsRuntimeSync(assemblyState, currentProfile, lastRuntimeSyncProfileRevision, profileRevisionFallback))
            {
                return lastRuntimeSyncProfileRevision;
            }

            powerSystem.ApplyProfile(currentProfile, runtimeState, initialStoredEnergyPercent);
            this.hullIntegrityService.ApplyProfile(currentProfile, runtimeState);
            moduleRuntimeCoordinator.Reconcile(
                host,
                assemblyState,
                currentProfile,
                runtimeState,
                powerSystem,
                ticksGame,
                this.BuildCargoResourceBroker(host, currentProfile, runtimeState, cargoBackend));

            if (assemblyState != null)
            {
                assemblyState.ClearDirty(ShuttleDirtyFlags.RuntimeSync);
            }

            return this.GetProfileRevision(currentProfile, profileRevisionFallback);
        }

        internal int SyncExternalHostIfNeeded(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile currentProfile,
            IShuttleCargoBackend cargoBackend,
            int lastExternalSyncProfileRevision,
            int profileRevisionFallback)
        {
            if (!this.NeedsExternalSync(assemblyState, currentProfile, lastExternalSyncProfileRevision, profileRevisionFallback))
            {
                return lastExternalSyncProfileRevision;
            }

            this.ReconcileCargoRegionConfig(assemblyState, currentProfile);
            cargoBackend.ApplyProfile(host, currentProfile);

            if (assemblyState != null)
            {
                assemblyState.ClearDirty(ShuttleDirtyFlags.ExternalSync);
            }

            return this.GetProfileRevision(currentProfile, profileRevisionFallback);
        }

        private bool NeedsRuntimeSync(
            ShuttleAssemblyState assemblyState,
            ShuttleProfile currentProfile,
            int lastRuntimeSyncProfileRevision,
            int profileRevisionFallback)
        {
            if (lastRuntimeSyncProfileRevision != this.GetProfileRevision(currentProfile, profileRevisionFallback))
            {
                return true;
            }

            return assemblyState != null && assemblyState.HasDirty(ShuttleDirtyFlags.RuntimeSync);
        }

        private bool NeedsExternalSync(
            ShuttleAssemblyState assemblyState,
            ShuttleProfile currentProfile,
            int lastExternalSyncProfileRevision,
            int profileRevisionFallback)
        {
            if (lastExternalSyncProfileRevision != this.GetProfileRevision(currentProfile, profileRevisionFallback))
            {
                return true;
            }

            return assemblyState != null && assemblyState.HasDirty(ShuttleDirtyFlags.ExternalSync);
        }

        private int GetProfileRevision(ShuttleProfile currentProfile, int profileRevisionFallback)
        {
            return currentProfile != null ? currentProfile.Revision : profileRevisionFallback;
        }

        private IShuttleCargoResourceBroker BuildCargoResourceBroker(
            ThingWithComps host,
            ShuttleProfile currentProfile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend)
        {
            return new ShuttleCargoResourceBroker(
                host,
                currentProfile,
                runtimeState,
                cargoBackend);
        }

        private void ReconcileCargoRegionConfig(
            ShuttleAssemblyState assemblyState,
            ShuttleProfile currentProfile)
        {
            if (assemblyState == null || assemblyState.CargoRegionConfig == null)
            {
                return;
            }

            int cargoRegionCount = currentProfile != null && currentProfile.Cargo != null
                ? (int)currentProfile.Cargo.CargoRegionCount
                : 0;
            assemblyState.CargoRegionConfig.EnsureRegionCount(cargoRegionCount);
        }
    }
}
