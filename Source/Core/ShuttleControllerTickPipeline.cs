using System.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        private static class ShuttleControllerTickPipeline
        {
            internal static void Tick(ShuttleController owner)
            {
                // Behavior-sensitive order. Keep these phases aligned:
                // 1. EnsureRuntimeState
                // 2. GetProfileForRead
                // 3. prisoner runtime tick
                // 4. SyncRuntimeToProfileIfNeeded
                // 5. SyncExternalHostIfNeeded
                // 6. power demand collection
                // 7. power system tick
                // 8. module removal tick
                // 9. assembly construction tick
                // 10. medical procedure tick
                // 11. refrigerated cargo registry reconcile
                // 12. cargo broker / refrigerated transfer service build
                // 13. paced global cargo unload tick
                // 14. passenger boarding intent reconcile
                // 15. module runtime coordinator tick
                // 16. post module runtime cache invalidation
                // 17. power plant output apply
                // 18. runtimeState.NotifyTick
                // 19. profiler MaybeLog
                bool profileTick = owner.tickProfiler.IsTickProfilingRequested;
                long tickStart = StartTickProfileSection(profileTick);
                long sectionStart = StartTickProfileSection(profileTick);
                owner.EnsureRuntimeState();
                ShuttleProfile currentProfile = owner.GetProfileForRead();
                int ticksGame = ShuttleTickUtility.TicksGameOrZero();
                profileTick = owner.tickProfiler.PrepareTickProfiling(
                    owner.profileRevision);
                RecordTickProfileSection(
                    owner,
                    profileTick,
                    "runtime/profile bootstrap",
                    sectionStart);
                sectionStart = StartTickProfileSection(profileTick);

                owner.prisonerRuntimeSystem.Tick(
                    owner.shuttleHost,
                    currentProfile,
                    ticksGame);
                RecordTickProfileSection(owner, profileTick, "prisoner runtime tick", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                SyncRuntimeToProfileIfNeeded(owner, currentProfile, ticksGame);
                RecordTickProfileSection(owner, profileTick, "profile/runtime sync", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                SyncExternalHostIfNeeded(owner, currentProfile);
                RecordTickProfileSection(owner, profileTick, "external host/runtime sync", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                ShuttlePowerHostStatus hostStatus = owner.powerPlantBridge.BuildHostStatus(owner.shuttleHost);
                owner.moduleRuntimeCoordinator.CollectPowerDemand(
                    owner.shuttleHost,
                    owner.assemblyState,
                    currentProfile,
                    owner.runtimeState,
                    owner.powerSystem,
                    ticksGame,
                    profileTick ? owner.tickProfiler : null);
                if (profileTick)
                {
                    owner.tickProfiler.RecordPowerDemandCollection(sectionStart);
                }

                RecordTickProfileSection(owner, profileTick, "power demand collection", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                owner.powerSystem.Tick(currentProfile, owner.runtimeState, hostStatus);
                RecordTickProfileSection(owner, profileTick, "power tick", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                ShuttleModuleRemovalState removalState =
                    owner.runtimeState.ModuleRemoval;
                if (removalState != null && removalState.HasActiveRemoval)
                {
                    owner.moduleRemovalService.Tick(owner.BuildCommandContext());
                }
                RecordTickProfileSection(owner, profileTick, "module removal tick", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                ShuttleAssemblyConstructionState constructionState =
                    owner.runtimeState.AssemblyConstruction;
                if (constructionState != null && constructionState.HasActiveOrder)
                {
                    owner.assemblyConstructionSystem.Tick(owner.BuildCommandContext());
                }
                RecordTickProfileSection(owner, profileTick, "assembly construction tick", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                SharedMedicalBayProcedureRuntimeSystem.Tick(
                    owner.shuttleHost,
                    owner.runtimeState,
                    ticksGame);
                RecordTickProfileSection(owner, profileTick, "medical procedure tick", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                owner.ReconcileRefrigeratedCargoRegistryIfNeeded(ticksGame);
                RecordTickProfileSection(owner, profileTick, "refrigerated cargo registry reconcile", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                IShuttleCargoResourceBroker cargoResourceBroker = owner.BuildCargoResourceBroker(currentProfile);
                IShuttleCargoColdTransferService coldTransferService =
                    owner.BuildRefrigeratedCargoTransferService(currentProfile);
                IShuttleCargoPostDepositRouter postDepositCargoRouter =
                    owner.BuildPostDepositColdRouter(currentProfile);
                RecordTickProfileSection(owner, profileTick, "cargo broker/cold transfer service", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                if (owner.cargoUnloadSystem.Tick(
                    owner.shuttleHost,
                    owner.runtimeState,
                    owner.cargoBackend,
                    coldTransferService,
                    ticksGame))
                {
                    owner.InvalidateCargoInventorySnapshotCaches();
                }

                RecordTickProfileSection(owner, profileTick, "global cargo unload tick", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                if (ShuttlePassengerBoardingIntentSystem.Tick(
                    owner.shuttleHost,
                    owner.runtimeState,
                    owner.cargoBackend,
                    ticksGame))
                {
                    owner.InvalidateLoadCargoReadModelCache();
                }

                RecordTickProfileSection(
                    owner,
                    profileTick,
                    "passenger boarding intent reconcile",
                    sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                long moduleRuntimeTickStart = sectionStart;
                long moduleRuntimeDispatcherTickStart = StartTickProfileSection(profileTick);
                owner.moduleRuntimeCoordinator.Tick(
                    owner.shuttleHost,
                    owner.assemblyState,
                    currentProfile,
                    owner.runtimeState,
                    owner.powerSystem,
                    ticksGame,
                    cargoResourceBroker,
                    coldTransferService,
                    owner.ResolveRefrigeratedCargoAutoTransferConfig,
                    postDepositCargoRouter,
                    profileTick ? owner.tickProfiler : null);
                RecordTickProfileSection(
                    owner,
                    profileTick,
                    "module runtime dispatcher tick",
                    moduleRuntimeDispatcherTickStart);
                long postModuleRuntimeCacheInvalidationStart = StartTickProfileSection(profileTick);
                owner.InvalidateLoadCargoReadModelCache();
                owner.InvalidateExternalRuntimeMassContributionCache();
                RecordTickProfileSection(
                    owner,
                    profileTick,
                    "post module runtime cache invalidation",
                    postModuleRuntimeCacheInvalidationStart);
                RecordTickProfileSection(owner, profileTick, "module runtime tick", moduleRuntimeTickStart);
                sectionStart = StartTickProfileSection(profileTick);
                owner.powerPlantBridge.ApplyRuntimeOutput(owner.shuttleHost, owner.runtimeState);
                RecordTickProfileSection(owner, profileTick, "power plant output apply", sectionStart);
                sectionStart = StartTickProfileSection(profileTick);
                owner.runtimeState.NotifyTick();
                RecordTickProfileSection(owner, profileTick, "runtimeState.NotifyTick", sectionStart);
                RecordTickProfileSection(owner, profileTick, "total controller tick", tickStart);
                if (profileTick)
                {
                    owner.tickProfiler.CompleteDashboardTick(ticksGame);
                    int installedModuleCount = owner.assemblyState != null &&
                        owner.assemblyState.Modules != null
                            ? owner.assemblyState.Modules.Count
                            : 0;
                    owner.tickProfiler.MaybeLog(ticksGame, owner.profileRevision, installedModuleCount);
                }
            }

            internal static ShuttleProfile ReconcileProfileToHost(ShuttleController owner)
            {
                owner.EnsureRuntimeState();
                ShuttleProfile currentProfile = owner.GetProfileForRead();
                int ticksGame = ShuttleTickUtility.TicksGameOrZero();
                SyncRuntimeToProfileIfNeeded(owner, currentProfile, ticksGame);
                SyncExternalHostIfNeeded(owner, currentProfile);
                owner.ReconcileRefrigeratedCargoRegistryIfNeeded(ticksGame);
                return currentProfile;
            }

            private static void SyncRuntimeToProfileIfNeeded(
                ShuttleController owner,
                ShuttleProfile currentProfile,
                int ticksGame)
            {
                owner.EnsureRuntimeState();
                int currentRuntimeDispatchFingerprint = owner.moduleRuntimeCoordinator != null
                    ? owner.moduleRuntimeCoordinator.DispatchFingerprint
                    : 0;

                if (owner.assemblyState != null &&
                    owner.lastRuntimeSyncDispatchFingerprint != currentRuntimeDispatchFingerprint)
                {
                    owner.assemblyState.MarkDirty(ShuttleDirtyFlags.RuntimeSync);
                }

                owner.lastRuntimeSyncProfileRevision = SharedProfileSyncService.SyncRuntimeToProfileIfNeeded(
                    owner.shuttleHost,
                    owner.assemblyState,
                    currentProfile,
                    owner.runtimeState,
                    owner.powerSystem,
                    owner.moduleRuntimeCoordinator,
                    owner.cargoBackend,
                    owner.lastRuntimeSyncProfileRevision,
                    owner.profileRevision,
                    owner.initialStoredEnergyPercent,
                    ticksGame);
                if (owner.lastRuntimeSyncDispatchFingerprint != currentRuntimeDispatchFingerprint)
                {
                    ExternalRuntimeDiagnostics.LogBindingsAndStatesIfDevMode(
                        owner.assemblyState,
                        owner.runtimeState,
                        currentRuntimeDispatchFingerprint);
                }

                ExternalRuntimeDiagnostics.WarnMissingRegisteredBindingsIfDevMode(owner.assemblyState);
                owner.lastRuntimeSyncDispatchFingerprint = currentRuntimeDispatchFingerprint;
            }

            private static void SyncExternalHostIfNeeded(
                ShuttleController owner,
                ShuttleProfile currentProfile)
            {
                owner.lastExternalSyncProfileRevision = SharedProfileSyncService.SyncExternalHostIfNeeded(
                    owner.shuttleHost,
                    owner.assemblyState,
                    currentProfile,
                    owner.cargoBackend,
                    owner.lastExternalSyncProfileRevision,
                    owner.profileRevision);
            }

            private static long StartTickProfileSection(bool enabled)
            {
                return enabled ? Stopwatch.GetTimestamp() : 0L;
            }

            private static void RecordTickProfileSection(
                ShuttleController owner,
                bool enabled,
                string sectionName,
                long startTimestamp)
            {
                if (enabled)
                {
                    owner.tickProfiler.Record(sectionName, startTimestamp);
                }
            }
        }
    }
}
