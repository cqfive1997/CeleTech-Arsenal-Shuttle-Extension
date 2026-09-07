using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        private static class ShuttleControllerCargoCache
        {
            internal static IShuttleCargoResourceBroker BuildCargoResourceBroker(
                ShuttleController owner,
                ShuttleProfile currentProfile)
            {
                int profileRevision = currentProfile != null ? currentProfile.Revision : owner.profileRevision;
                if (owner.cachedCargoResourceBroker != null &&
                    object.ReferenceEquals(owner.cachedCargoResourceBrokerHost, owner.shuttleHost) &&
                    object.ReferenceEquals(owner.cachedCargoResourceBrokerAssemblyState, owner.assemblyState) &&
                    object.ReferenceEquals(owner.cachedCargoResourceBrokerRuntimeState, owner.runtimeState) &&
                    object.ReferenceEquals(owner.cachedCargoResourceBrokerProfile, currentProfile) &&
                    owner.cachedCargoResourceBrokerProfileRevision == profileRevision)
                {
                    return owner.cachedCargoResourceBroker;
                }

                // The broker is profile-bound but reads mutable cargo/runtime state on demand.
                // Profile dirtying and host/load rebinding invalidate this cached service.
                owner.cachedCargoResourceBroker = new ShuttleCargoResourceBroker(
                    owner.shuttleHost,
                    currentProfile,
                    owner.runtimeState,
                    owner.cargoBackend);
                owner.cachedCargoResourceBrokerHost = owner.shuttleHost;
                owner.cachedCargoResourceBrokerAssemblyState = owner.assemblyState;
                owner.cachedCargoResourceBrokerRuntimeState = owner.runtimeState;
                owner.cachedCargoResourceBrokerProfile = currentProfile;
                owner.cachedCargoResourceBrokerProfileRevision = profileRevision;
                return owner.cachedCargoResourceBroker;
            }

            internal static void InvalidateCargoInventorySnapshotCaches(ShuttleController owner)
            {
                // Unified cargo inventory mutation invalidation point:
                // load-cargo read model, external mass, broker inventory,
                // cold transfer service, and auto work table inventory.
                owner.InvalidateLoadCargoReadModelCache();
                owner.InvalidateExternalRuntimeMassContributionCache();

                if (owner.cachedCargoResourceBroker != null)
                {
                    owner.cachedCargoResourceBroker.InvalidateInventorySnapshot();
                }

                owner.cachedRefrigeratedCargoTransferService = null;
                owner.cachedRefrigeratedCargoTransferServiceHost = null;
                owner.cachedRefrigeratedCargoTransferServiceAssemblyState = null;
                owner.cachedRefrigeratedCargoTransferServiceRuntimeState = null;
                owner.cachedRefrigeratedCargoTransferServiceProfile = null;
                owner.cachedRefrigeratedCargoTransferServiceProfileRevision = -1;
                ClearPostDepositColdRouter(owner);
                owner.InvalidateAutoWorkTableInventorySnapshotCache();
            }

            internal static void InvalidateProfileDependentRuntimeCaches(ShuttleController owner)
            {
                owner.InvalidateLoadCargoReadModelCache();
                owner.InvalidateExternalRuntimeMassContributionCache();

                owner.cachedCargoResourceBroker = null;
                owner.cachedCargoResourceBrokerHost = null;
                owner.cachedCargoResourceBrokerAssemblyState = null;
                owner.cachedCargoResourceBrokerRuntimeState = null;
                owner.cachedCargoResourceBrokerProfile = null;
                owner.cachedCargoResourceBrokerProfileRevision = -1;
                owner.cachedRefrigeratedCargoTransferService = null;
                owner.cachedRefrigeratedCargoTransferServiceHost = null;
                owner.cachedRefrigeratedCargoTransferServiceAssemblyState = null;
                owner.cachedRefrigeratedCargoTransferServiceRuntimeState = null;
                owner.cachedRefrigeratedCargoTransferServiceProfile = null;
                owner.cachedRefrigeratedCargoTransferServiceProfileRevision = -1;
                ClearPostDepositColdRouter(owner);
                owner.InvalidateAutoWorkTableInventorySnapshotCache();
            }

            internal static ShuttleRuntimeMassContributionSnapshot BuildExternalRuntimeMassContributionSnapshot(
                ShuttleController owner,
                ShuttleProfile currentProfile,
                int ticksGame)
            {
                // Per-tick cache. Module runtime tick must invalidate this
                // through InvalidateExternalRuntimeMassContributionCache.
                // Do not stretch it across ticks unless all mass providers
                // expose revision sources.
                int profileRevision = currentProfile != null ? currentProfile.Revision : owner.profileRevision;
                if (owner.cachedExternalMassContributionSnapshot != null &&
                    object.ReferenceEquals(owner.cachedExternalMassContributionHost, owner.shuttleHost) &&
                    object.ReferenceEquals(owner.cachedExternalMassContributionAssemblyState, owner.assemblyState) &&
                    object.ReferenceEquals(owner.cachedExternalMassContributionRuntimeState, owner.runtimeState) &&
                    owner.cachedExternalMassContributionProfileRevision == profileRevision &&
                    owner.cachedExternalMassContributionTick == ticksGame)
                {
                    return owner.cachedExternalMassContributionSnapshot;
                }

                owner.EnsureRuntimeState();
                ShuttleRuntimeMassContributionSnapshot snapshot =
                    owner.moduleRuntimeCoordinator != null
                        ? owner.moduleRuntimeCoordinator.CollectMassContributions(
                            owner.shuttleHost,
                            owner.assemblyState,
                            currentProfile,
                            owner.runtimeState,
                            ticksGame)
                        : null;
                if (snapshot == null)
                {
                    snapshot = new ShuttleRuntimeMassContributionSnapshot();
                }

                owner.cachedExternalMassContributionSnapshot = snapshot;
                owner.cachedExternalMassContributionHost = owner.shuttleHost;
                owner.cachedExternalMassContributionAssemblyState = owner.assemblyState;
                owner.cachedExternalMassContributionRuntimeState = owner.runtimeState;
                owner.cachedExternalMassContributionProfileRevision = profileRevision;
                owner.cachedExternalMassContributionTick = ticksGame;
                return snapshot;
            }

            internal static ShuttleRuntimeMassContributionSnapshot BuildExternalRuntimeMassContributionSnapshotForRead(
                ShuttleController owner)
            {
                return owner.BuildExternalRuntimeMassContributionSnapshot(
                    owner.GetProfileForRead(),
                    owner.GetTicksGameSafe());
            }

            internal static void InvalidateExternalRuntimeMassContributionCache(ShuttleController owner)
            {
                owner.cachedExternalMassContributionSnapshot = null;
                owner.cachedExternalMassContributionHost = null;
                owner.cachedExternalMassContributionAssemblyState = null;
                owner.cachedExternalMassContributionRuntimeState = null;
                owner.cachedExternalMassContributionProfileRevision = -1;
                owner.cachedExternalMassContributionTick = int.MinValue;
            }

            internal static IShuttleCargoColdTransferService BuildRefrigeratedCargoTransferService(
                ShuttleController owner,
                ShuttleProfile currentProfile)
            {
                int profileRevision = currentProfile != null ? currentProfile.Revision : owner.profileRevision;
                if (owner.cachedRefrigeratedCargoTransferService != null &&
                    object.ReferenceEquals(owner.cachedRefrigeratedCargoTransferServiceHost, owner.shuttleHost) &&
                    object.ReferenceEquals(owner.cachedRefrigeratedCargoTransferServiceAssemblyState, owner.assemblyState) &&
                    object.ReferenceEquals(owner.cachedRefrigeratedCargoTransferServiceRuntimeState, owner.runtimeState) &&
                    object.ReferenceEquals(owner.cachedRefrigeratedCargoTransferServiceProfile, currentProfile) &&
                    owner.cachedRefrigeratedCargoTransferServiceProfileRevision == profileRevision)
                {
                    return owner.cachedRefrigeratedCargoTransferService;
                }

                // Cold transfer service caches only same-tick candidate scans internally.
                // Reuse the service until profile/host/runtime identity changes.
                owner.cachedRefrigeratedCargoTransferService = new ShuttleCargoColdTransferService(
                    owner.shuttleHost,
                    owner.assemblyState,
                    owner.runtimeState,
                    currentProfile,
                    owner.cargoBackend);
                owner.cachedRefrigeratedCargoTransferServiceHost = owner.shuttleHost;
                owner.cachedRefrigeratedCargoTransferServiceAssemblyState = owner.assemblyState;
                owner.cachedRefrigeratedCargoTransferServiceRuntimeState = owner.runtimeState;
                owner.cachedRefrigeratedCargoTransferServiceProfile = currentProfile;
                owner.cachedRefrigeratedCargoTransferServiceProfileRevision = profileRevision;
                return owner.cachedRefrigeratedCargoTransferService;
            }

            internal static IShuttleCargoPostDepositRouter BuildPostDepositColdRouter(
                ShuttleController owner,
                ShuttleProfile currentProfile)
            {
                int profileRevision = currentProfile != null
                    ? currentProfile.Revision
                    : owner.profileRevision;
                if (owner.cachedPostDepositColdRouter != null &&
                    object.ReferenceEquals(
                        owner.cachedPostDepositColdRouterHost,
                        owner.shuttleHost) &&
                    object.ReferenceEquals(
                        owner.cachedPostDepositColdRouterAssemblyState,
                        owner.assemblyState) &&
                    object.ReferenceEquals(
                        owner.cachedPostDepositColdRouterRuntimeState,
                        owner.runtimeState) &&
                    object.ReferenceEquals(
                        owner.cachedPostDepositColdRouterProfile,
                        currentProfile) &&
                    owner.cachedPostDepositColdRouterProfileRevision == profileRevision)
                {
                    return owner.cachedPostDepositColdRouter;
                }

                IShuttleCargoColdTransferService coldTransferService =
                    BuildRefrigeratedCargoTransferService(owner, currentProfile);
                owner.cachedPostDepositColdRouter =
                    new ShuttleCargoPostDepositColdRouter(
                        owner.shuttleHost,
                        owner.assemblyState,
                        owner.runtimeState,
                        currentProfile,
                        owner.cargoBackend,
                        coldTransferService);
                owner.cachedPostDepositColdRouterHost = owner.shuttleHost;
                owner.cachedPostDepositColdRouterAssemblyState = owner.assemblyState;
                owner.cachedPostDepositColdRouterRuntimeState = owner.runtimeState;
                owner.cachedPostDepositColdRouterProfile = currentProfile;
                owner.cachedPostDepositColdRouterProfileRevision = profileRevision;
                return owner.cachedPostDepositColdRouter;
            }

            private static void ClearPostDepositColdRouter(ShuttleController owner)
            {
                owner.cachedPostDepositColdRouter = null;
                owner.cachedPostDepositColdRouterHost = null;
                owner.cachedPostDepositColdRouterAssemblyState = null;
                owner.cachedPostDepositColdRouterRuntimeState = null;
                owner.cachedPostDepositColdRouterProfile = null;
                owner.cachedPostDepositColdRouterProfileRevision = -1;
            }

            internal static ShuttleRefrigeratedCargoAutoTransferConfig ResolveRefrigeratedCargoAutoTransferConfig(
                ShuttleController owner,
                string moduleInstanceID)
            {
                if (string.IsNullOrEmpty(moduleInstanceID) || owner.assemblyState == null)
                {
                    return null;
                }

                ShuttleModule module = owner.assemblyState.GetModule(moduleInstanceID);
                ShuttleRefrigeratedCargoModuleDef moduleDef =
                    module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
                if (moduleDef == null)
                {
                    return null;
                }

                ShuttleRefrigeratedCargoConfigState configState = owner.assemblyState.RefrigeratedCargoConfig;
                return configState != null
                    ? configState.BuildEffectiveAutoTransferConfig(moduleInstanceID, moduleDef)
                    : ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(moduleInstanceID, moduleDef);
            }

            internal static void MarkRefrigeratedCargoRegistryDirty(ShuttleController owner)
            {
                // Installed cold modules and profile-derived runtime state determine registry records.
                // Assembly/profile dirty paths mark this flag; Tick performs immediate dirty reconcile
                // plus a low-frequency fallback for external edge cases such as load/rebind ordering.
                owner.refrigeratedCargoRegistryDirty = true;
            }

            internal static void ReconcileRefrigeratedCargoRegistryIfNeeded(
                ShuttleController owner,
                int ticksGame)
            {
                bool fallbackDue =
                    ticksGame >= 0 &&
                    (owner.lastRefrigeratedCargoRegistryReconcileTick == int.MinValue ||
                        ticksGame - owner.lastRefrigeratedCargoRegistryReconcileTick >=
                            RefrigeratedCargoRegistryFallbackIntervalTicks);
                if (!owner.refrigeratedCargoRegistryDirty && !fallbackDue)
                {
                    return;
                }

                ReconcileRefrigeratedCargoRegistry(owner, ticksGame);
            }

            private static void ReconcileRefrigeratedCargoRegistry(
                ShuttleController owner,
                int ticksGame)
            {
                CompShuttleRefrigeratedCargoRegistry registry = owner.shuttleHost != null
                    ? owner.shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                    : null;
                if (registry == null)
                {
                    owner.refrigeratedCargoRegistryDirty = false;
                    owner.lastRefrigeratedCargoRegistryReconcileTick = ticksGame;
                    return;
                }

                registry.Reconcile(owner.assemblyState, owner.runtimeState);
                owner.refrigeratedCargoRegistryDirty = false;
                owner.lastRefrigeratedCargoRegistryReconcileTick = ticksGame;
            }
        }
    }
}
