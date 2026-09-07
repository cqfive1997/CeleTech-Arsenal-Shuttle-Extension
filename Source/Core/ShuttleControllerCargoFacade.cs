using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        // Phase 4 low-risk cargo facade. It temporarily keeps cargo routing,
        // snapshot assembly, and simple cargo query wrappers together without
        // holding durable state. Future splits should move snapshot assembly
        // toward CargoSnapshotBuilder instead of growing this facade.
        private static class ShuttleControllerCargoFacade
        {
            internal static void NotifyCargoHauledToTransporter(
                ShuttleController owner,
                CompTransporter transporter,
                Thing thing,
                int count)
            {
                if (transporter == null || thing == null || count <= 0)
                {
                    return;
                }

                owner.EnsureRuntimeState();
                ShuttlePendingLoadDestinationState pendingDestinations =
                    owner.runtimeState.PendingLoadDestinations;
                ShuttlePendingLoadDestinationRecord destinationRecord;
                if (pendingDestinations == null ||
                    !pendingDestinations.TryFindRefrigeratedDestination(thing, out destinationRecord) ||
                    destinationRecord == null)
                {
                    owner.InvalidateCargoInventorySnapshotCaches();
                    return;
                }

                ShuttleProfile currentProfile = owner.GetProfileForRead();
                IShuttleCargoColdTransferService coldTransferService =
                    owner.BuildRefrigeratedCargoTransferService(currentProfile);
                int movedCount;
                string failureReason;
                if (coldTransferService != null &&
                    coldTransferService.TryRouteLoadedCargoToCold(
                        destinationRecord.RefrigeratedModuleInstanceID,
                        transporter,
                        thing,
                        count,
                        "load-routing",
                        out movedCount,
                        out failureReason) &&
                    movedCount > 0)
                {
                    pendingDestinations.Consume(destinationRecord, movedCount);
                    owner.InvalidateCargoInventorySnapshotCaches();
                    return;
                }

                // The direct delivery identity can become unusable after a modded stack merge,
                // or an earlier ordinary stack can still be waiting from a transient failure.
                // Reconcile once from live ordinary cargo through the existing transaction
                // service. This is event-driven and does not restore the old Tick scan.
                if (coldTransferService != null)
                {
                    int recoveredStackCount;
                    int recoveredThingCount;
                    string recoveryFailureReason;
                    ShuttleRefrigeratedCargoEventReconciler.TryReconcile(
                        owner.assemblyState,
                        destinationRecord.RefrigeratedModuleInstanceID,
                        coldTransferService,
                        "load-routing-recovery",
                        out recoveredStackCount,
                        out recoveredThingCount,
                        out recoveryFailureReason);
                }

                // The delivery has now received both its exact route attempt and its one-shot
                // live-holder reconciliation. Retire this intent so stale records cannot match
                // unrelated future stacks.
                pendingDestinations.Consume(destinationRecord, count);
                owner.InvalidateCargoInventorySnapshotCaches();
            }

            internal static void ClearPendingLoadDestinations(ShuttleController owner)
            {
                owner.EnsureRuntimeState();
                if (owner.runtimeState.PendingLoadDestinations != null)
                {
                    owner.runtimeState.PendingLoadDestinations.Clear();
                }
            }

            internal static ShuttleCargoSnapshot BuildCargoSnapshot(ShuttleController owner)
            {
                ShuttleProfile currentProfile = owner.GetProfileForRead();
                ShuttleCargoRegionConfigState cargoRegionConfig = owner.assemblyState != null
                    ? owner.assemblyState.CargoRegionConfig
                    : null;

                ShuttleCargoSnapshot snapshot =
                    owner.cargoBackend.BuildSnapshot(owner.shuttleHost, currentProfile, cargoRegionConfig);
                AppendMedicalBayPatientPayloadSnapshot(owner, snapshot);
                AppendExternalRuntimeMassSnapshot(owner, snapshot, currentProfile);
                AppendRefrigeratedCargoSnapshot(owner, snapshot, currentProfile);
                return snapshot;
            }

            internal static ShuttleCargoUnloadProgressSnapshot BuildCargoUnloadProgressSnapshot(
                ShuttleController owner)
            {
                ShuttleCargoUnloadProgressSnapshot snapshot =
                    new ShuttleCargoUnloadProgressSnapshot();
                if (owner == null)
                {
                    return snapshot;
                }

                owner.EnsureRuntimeState();
                Runtime.CargoUnloading.ShuttleCargoUnloadState state =
                    owner.runtimeState != null ? owner.runtimeState.CargoUnload : null;
                if (state == null)
                {
                    return snapshot;
                }

                snapshot.Active = state.IsActive;
                snapshot.PendingStackCount = state.PendingStackCount;
                snapshot.PendingThingCount = state.PendingThingCount;
                snapshot.TotalStackCount = state.TotalStackCount;
                snapshot.TotalThingCount = state.TotalThingCount;
                snapshot.CompletedStackCount = state.CompletedStackCount;
                snapshot.CompletedThingCount = state.CompletedThingCount;
                snapshot.SkippedStackCount = state.SkippedStackCount;
                snapshot.SkippedThingCount = state.SkippedThingCount;
                snapshot.StartedTick = state.StartedTick;
                snapshot.LastCompletedTick = state.LastCompletedTick;
                snapshot.LastFailureReason = state.LastFailureReason;
                snapshot.Revision = state.Revision;
                return snapshot;
            }

            internal static bool HasQueuedLoadsForLoadCargo(ShuttleController owner)
            {
                owner.EnsureRuntimeState();
                return owner.cargoBackend.HasQueuedLoads(owner.shuttleHost) ||
                    (owner.runtimeState.PassengerBoardingIntents != null &&
                        owner.runtimeState.PassengerBoardingIntents.HasAny);
            }

            internal static float GetLoadCargoAvailableMass(ShuttleController owner)
            {
                ShuttleProfile currentProfile = owner.GetProfileForRead();
                owner.EnsureRuntimeState();
                float regularAvailableMass = owner.cargoBackend.GetAvailableMass(owner.shuttleHost);
                ShuttleRuntimeMassContributionSnapshot externalMass =
                    owner.BuildExternalRuntimeMassContributionSnapshot(
                        currentProfile,
                        owner.GetTicksGameSafe());
                if (externalMass != null && externalMass.TotalMassKg > 0f)
                {
                    regularAvailableMass -= externalMass.TotalMassKg;
                    if (regularAvailableMass < 0f)
                    {
                        regularAvailableMass = 0f;
                    }
                }

                ShuttleCargoLoadAdmissionResolver admissionResolver =
                    new ShuttleCargoLoadAdmissionResolver(
                        owner.shuttleHost,
                        currentProfile,
                        owner.assemblyState,
                        owner.runtimeState);
                if (admissionResolver.RefrigeratedMassSharesOverallCapacity)
                {
                    regularAvailableMass -=
                        admissionResolver.RefrigeratedAutoTransferUsedMassKg;
                    return regularAvailableMass > 0f ? regularAvailableMass : 0f;
                }

                return regularAvailableMass +
                    admissionResolver.RefrigeratedAutoTransferAvailableMassKg;
            }

            internal static int GetLoadCargoSelectedCount(
                ShuttleController owner,
                List<TransferableOneWay> passengerTransferables,
                List<TransferableOneWay> cargoTransferables)
            {
                return owner.cargoBackend.GetSelectedCount(passengerTransferables, cargoTransferables);
            }

            internal static float GetLoadCargoSelectedMassKg(
                ShuttleController owner,
                List<TransferableOneWay> passengerTransferables,
                List<TransferableOneWay> cargoTransferables)
            {
                return owner.cargoBackend.GetSelectedMassKg(passengerTransferables, cargoTransferables);
            }

            private static void AppendExternalRuntimeMassSnapshot(
                ShuttleController owner,
                ShuttleCargoSnapshot snapshot,
                ShuttleProfile currentProfile)
            {
                if (snapshot == null)
                {
                    return;
                }

                ShuttleRuntimeMassContributionSnapshot externalMass =
                    owner.BuildExternalRuntimeMassContributionSnapshot(
                        currentProfile,
                        owner.GetTicksGameSafe());
                if (externalMass == null || externalMass.TotalMassKg <= 0f)
                {
                    return;
                }

                snapshot.ExternalRuntimeMassKg = externalMass.TotalMassKg;
                snapshot.ExternalRuntimeMassContributionCount =
                    externalMass.Records != null ? externalMass.Records.Count : 0;
                RecalculateCargoSnapshotMass(snapshot);
            }

            private static void AppendRefrigeratedCargoSnapshot(
                ShuttleController owner,
                ShuttleCargoSnapshot snapshot,
                ShuttleProfile currentProfile)
            {
                if (snapshot == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttleRefrigeratedCargoRegistry registry =
                    owner.shuttleHost.TryGetComp<CompShuttleRefrigeratedCargoRegistry>();
                if (registry == null)
                {
                    snapshot.RefrigeratedMassSharesOverallCapacity =
                        ShuttleRefrigeratedCargoCapacityPolicy.SharesOverallCapacity;
                    return;
                }

                owner.ReconcileRefrigeratedCargoRegistryIfNeeded(owner.GetTicksGameSafe());
                float refrigeratedCapacityKg =
                    ShuttleRefrigeratedCargoCapacityPolicy
                        .GetRefrigeratedPoolCapacityKg(currentProfile);
                bool hasEnabledRefrigeratedCapacity = false;
                IReadOnlyList<RefrigeratedCargoRecord> records = registry.Records;
                for (int i = 0; i < records.Count; i++)
                {
                    RefrigeratedCargoRecord record = records[i];
                    if (record == null)
                    {
                        continue;
                    }

                    ShuttleRefrigeratedCargoModuleSnapshot moduleSnapshot =
                        BuildRefrigeratedCargoModuleSnapshot(
                            owner,
                            record,
                            refrigeratedCapacityKg);
                    snapshot.RefrigeratedCargoModules.Add(moduleSnapshot);
                    snapshot.RefrigeratedStackCount += moduleSnapshot.StackCount;
                    snapshot.RefrigeratedThingCount += moduleSnapshot.ThingCount;
                    snapshot.RefrigeratedMassKg += moduleSnapshot.StoredMassKg;
                    hasEnabledRefrigeratedCapacity |= moduleSnapshot.IsEnabled;
                }

                snapshot.RefrigeratedMassSharesOverallCapacity =
                    ShuttleRefrigeratedCargoCapacityPolicy.SharesOverallCapacity;
                snapshot.RefrigeratedMassCapacityKg =
                    hasEnabledRefrigeratedCapacity || snapshot.RefrigeratedMassKg > 0f
                        ? refrigeratedCapacityKg
                        : 0f;
                snapshot.MassCapacity =
                    ShuttleRefrigeratedCargoCapacityPolicy.GetEffectiveTotalCapacityKg(
                        currentProfile,
                        hasEnabledRefrigeratedCapacity || snapshot.RefrigeratedMassKg > 0f);
                RecalculateCargoSnapshotMass(snapshot);
            }

            private static void AppendMedicalBayPatientPayloadSnapshot(
                ShuttleController owner,
                ShuttleCargoSnapshot snapshot)
            {
                if (snapshot == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttleMedicalBayOccupancy medicalBay =
                    owner.shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
                if (medicalBay == null || !medicalBay.HasPatients)
                {
                    RecalculateCargoSnapshotMass(snapshot);
                    return;
                }

                List<Pawn> patients = medicalBay.HeldPatients;
                for (int i = 0; i < patients.Count; i++)
                {
                    Pawn patient = patients[i];
                    if (patient == null || patient.Destroyed)
                    {
                        continue;
                    }

                    snapshot.MedicalBayPatientCount++;
                    snapshot.MedicalBayPatientMassKg += CargoDisplayUtility.GetThingMass(patient, 1);
                }

                RecalculateCargoSnapshotMass(snapshot);
            }

            private static void RecalculateCargoSnapshotMass(ShuttleCargoSnapshot snapshot)
            {
                if (snapshot == null)
                {
                    return;
                }

                snapshot.CargoMassUsage = snapshot.LoadedMassKg +
                    snapshot.QueuedMassKg +
                    snapshot.RefrigeratedMassKg +
                    snapshot.MedicalBayPatientMassKg +
                    snapshot.ExternalRuntimeMassKg;
                snapshot.TotalPlannedMassKg = snapshot.CargoMassUsage;
                snapshot.MassUsage = snapshot.StructuralMass + snapshot.CargoMassUsage;
            }

            private static ShuttleRefrigeratedCargoModuleSnapshot BuildRefrigeratedCargoModuleSnapshot(
                ShuttleController owner,
                RefrigeratedCargoRecord record,
                float refrigeratedCapacityKg)
            {
                ShuttleRefrigeratedCargoModuleSnapshot snapshot =
                    new ShuttleRefrigeratedCargoModuleSnapshot();
                if (record == null)
                {
                    return snapshot;
                }

                ShuttleModule module = owner.assemblyState != null
                    ? owner.assemblyState.GetModule(record.ModuleInstanceID)
                    : null;
                ShuttleRefrigeratedCargoModuleDef moduleDef =
                    module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
                if (moduleDef == null && !string.IsNullOrEmpty(record.ModuleDefName))
                {
                    moduleDef = DefDatabase<ShuttleRefrigeratedCargoModuleDef>
                        .GetNamedSilentFail(record.ModuleDefName);
                }

                snapshot.ModuleInstanceID = record.ModuleInstanceID;
                snapshot.ModuleDefName = record.ModuleDefName;
                string configuredLabel = ResolveRefrigeratedCargoLabel(owner, record.ModuleInstanceID);
                snapshot.HasCustomLabel = !string.IsNullOrEmpty(configuredLabel);
                snapshot.Label = !string.IsNullOrEmpty(configuredLabel)
                    ? configuredLabel
                    : moduleDef != null
                        ? moduleDef.LabelCap.ToString()
                        : (!string.IsNullOrEmpty(record.ModuleDefName) ? record.ModuleDefName : record.ModuleInstanceID);
                snapshot.ModuleResolved = module != null;
                bool moduleEnabled = module != null && module.IsEnabled;
                snapshot.IsEnabled = moduleEnabled;
                snapshot.CoolingActive = moduleEnabled && record.CoolingActive;
                snapshot.InactiveReason = record.InactiveReason;
                snapshot.TargetTemperatureC = record.TargetTemperatureC;
                snapshot.StoredMassKg = record.StoredMassKg;
                snapshot.CapacityKg = moduleEnabled ? refrigeratedCapacityKg : 0f;
                snapshot.StackCount = record.StackCount;
                ShuttleRefrigeratedCargoAutoTransferConfig autoTransferConfig =
                    moduleEnabled ? owner.ResolveRefrigeratedCargoAutoTransferConfig(record.ModuleInstanceID) : null;
                if (autoTransferConfig == null)
                {
                    autoTransferConfig = moduleEnabled
                        ? ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(
                            record.ModuleInstanceID,
                            moduleDef)
                        : null;
                }

                snapshot.AutoTransferEnabled =
                    moduleEnabled && autoTransferConfig != null && autoTransferConfig.AutoTransferEnabled;
                snapshot.HasCustomAutoTransferFilter =
                    autoTransferConfig != null && autoTransferConfig.HasCustomAutoTransferFilter;
                snapshot.AutoTransferFilter =
                    autoTransferConfig != null ? autoTransferConfig.AutoTransferFilter : null;

                ThingOwner<Thing> contents = record.Contents;
                if (contents != null)
                {
                    for (int coldIndex = 0; coldIndex < contents.Count; coldIndex++)
                    {
                        Thing thing = contents[coldIndex];
                        if (thing == null)
                        {
                            continue;
                        }

                        ShuttleRefrigeratedCargoItemSnapshot itemSnapshot =
                            new ShuttleRefrigeratedCargoItemSnapshot();
                        itemSnapshot.ModuleInstanceID = record.ModuleInstanceID;
                        itemSnapshot.ColdIndex = coldIndex;
                        itemSnapshot.ThingIDNumber = thing.thingIDNumber;
                        itemSnapshot.DefName = thing.def != null ? thing.def.defName : null;
                        itemSnapshot.Label = CargoDisplayUtility.GetDisplayLabel(thing);
                        itemSnapshot.StackCount = thing.stackCount;
                        itemSnapshot.Mass = CargoDisplayUtility.GetThingMass(thing, thing.stackCount);
                        itemSnapshot.DisplayThing = thing;
                        snapshot.Items.Add(itemSnapshot);
                        snapshot.ThingCount += itemSnapshot.StackCount;
                    }
                }

                return snapshot;
            }

            private static string ResolveRefrigeratedCargoLabel(
                ShuttleController owner,
                string moduleInstanceID)
            {
                if (string.IsNullOrEmpty(moduleInstanceID) ||
                    owner.assemblyState == null ||
                    owner.assemblyState.RefrigeratedCargoConfig == null)
                {
                    return null;
                }

                ShuttleRefrigeratedCargoModuleConfig config;
                return owner.assemblyState.RefrigeratedCargoConfig.TryGetModuleConfig(moduleInstanceID, out config) &&
                    config != null &&
                    !string.IsNullOrEmpty(config.Label)
                        ? config.Label
                        : null;
            }
        }
    }
}
