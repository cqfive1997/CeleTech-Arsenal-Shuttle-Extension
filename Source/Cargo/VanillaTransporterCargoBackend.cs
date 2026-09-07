using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Temporary cargo backend backed by RimWorld's CompTransporter.
    /// This stays as the public cargo boundary while small internal helpers own the vanilla details.
    /// </summary>
    public sealed class VanillaTransporterCargoBackend :
        IShuttleCargoBackend,
        IShuttlePassengerBoardingBackend
    {
        private readonly VanillaTransporterAdapter transporterAdapter;
        private readonly VanillaPassengerBoardingQueueAdapter passengerBoardingAdapter;
        private readonly VanillaPassengerInternalTransferAdapter passengerInternalTransferAdapter;
        private readonly CargoTransferFilter transferFilter;
        private readonly CargoSnapshotBuilder snapshotBuilder;
        private readonly LoadCargoReadModelBuilder loadCargoReadModelBuilder;
        private readonly LaunchCargoTransaction launchCargoTransaction;

        public VanillaTransporterCargoBackend()
        {
            this.transporterAdapter = new VanillaTransporterAdapter();
            this.passengerBoardingAdapter =
                new VanillaPassengerBoardingQueueAdapter(this.transporterAdapter);
            this.passengerInternalTransferAdapter =
                new VanillaPassengerInternalTransferAdapter(this.transporterAdapter);
            this.transferFilter = new CargoTransferFilter(this.transporterAdapter);
            this.snapshotBuilder = new CargoSnapshotBuilder(this.transporterAdapter);
            this.loadCargoReadModelBuilder = new LoadCargoReadModelBuilder(this.transporterAdapter, this.transferFilter);
            this.launchCargoTransaction = new LaunchCargoTransaction(this.transporterAdapter);
        }

        public void ApplyProfile(ThingWithComps host, ShuttleProfile profile)
        {
            this.transporterAdapter.ApplyProfile(host, profile);
        }

        public ShuttleCargoSnapshot BuildSnapshot(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig)
        {
            return this.snapshotBuilder.Build(host, profile, cargoRegionConfig);
        }

        public ShuttleLoadCargoReadModel BuildLoadCargoReadModel(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            return this.loadCargoReadModelBuilder.Build(host, profile, assemblyState, runtimeState);
        }

        public List<TransferableOneWay> BuildPassengerTransferables(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig)
        {
            return this.transferFilter.BuildPassengerTransferables(host, profile, cargoRegionConfig);
        }

        public List<TransferableOneWay> BuildCargoTransferables(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig)
        {
            return this.transferFilter.BuildCargoTransferables(host, profile, cargoRegionConfig, null);
        }

        public bool HasQueuedLoads(ThingWithComps host)
        {
            return this.transporterAdapter.HasPendingLoadQueue(host);
        }

        public bool ContainsPassenger(ThingWithComps host, Pawn pawn)
        {
            return this.passengerBoardingAdapter.ContainsPassenger(host, pawn);
        }

        public bool EnsurePassengerQueued(
            ThingWithComps host,
            Pawn pawn,
            out bool queuedNow,
            out string failureReason)
        {
            return this.passengerBoardingAdapter.EnsurePassengerQueued(
                host,
                pawn,
                out queuedNow,
                out failureReason);
        }

        bool IShuttlePassengerBoardingBackend.TryCancelPendingPassenger(
            ThingWithComps host,
            Pawn pawn,
            out bool canceled,
            out string failureReason)
        {
            return this.passengerBoardingAdapter.TryCancelPendingPassenger(
                host,
                pawn,
                out canceled,
                out failureReason);
        }

        ShuttlePassengerInternalTransferResult IShuttlePassengerBoardingBackend.TryTransferHeldPassengerToCockpit(
            ThingWithComps host,
            Pawn pawn,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            return this.passengerInternalTransferAdapter.TryTransfer(
                host,
                pawn,
                source,
                out failureReason);
        }

        public float GetAvailableMass(ThingWithComps host)
        {
            return this.transporterAdapter.GetAvailableMass(host);
        }

        public int GetSelectedCount(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            return this.transferFilter.SelectedCount(passengerTransferables) +
                this.transferFilter.SelectedCount(cargoTransferables);
        }

        public float GetSelectedMassKg(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            return this.transferFilter.SelectedMass(passengerTransferables) +
                this.transferFilter.SelectedMass(cargoTransferables);
        }

        public bool BeginLoading(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            bool cleanExistingQueue,
            float reservedExternalMassKg,
            out string failureReason)
        {
            ShuttleCargoLoadAdmissionResolver admissionResolver =
                new ShuttleCargoLoadAdmissionResolver(host, profile, assemblyState, runtimeState);
            List<ShuttlePendingLoadDestinationRecord> destinationPlan =
                this.BuildPendingLoadDestinationPlan(cargoTransferables, admissionResolver);
            return this.transporterAdapter.BeginLoading(
                host,
                passengerTransferables,
                cargoTransferables,
                cleanExistingQueue,
                this.transferFilter,
                admissionResolver,
                reservedExternalMassKg,
                out failureReason) &&
                this.CommitPendingLoadDestinationPlan(
                    runtimeState,
                    cleanExistingQueue,
                    destinationPlan);
        }

        private List<ShuttlePendingLoadDestinationRecord> BuildPendingLoadDestinationPlan(
            List<TransferableOneWay> cargoTransferables,
            ShuttleCargoLoadAdmissionResolver admissionResolver)
        {
            List<ShuttlePendingLoadDestinationRecord> records =
                new List<ShuttlePendingLoadDestinationRecord>();
            if (cargoTransferables == null || admissionResolver == null)
            {
                return records;
            }

            Dictionary<string, float> reservedColdMassByModule = new Dictionary<string, float>();
            for (int i = 0; i < cargoTransferables.Count; i++)
            {
                TransferableOneWay transferable = cargoTransferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0 || transferable.things == null)
                {
                    continue;
                }

                int remaining = transferable.CountToTransfer;
                for (int j = 0; j < transferable.things.Count && remaining > 0; j++)
                {
                    Thing thing = transferable.things[j];
                    if (thing == null || thing.stackCount <= 0)
                    {
                        continue;
                    }

                    int count = remaining < thing.stackCount ? remaining : thing.stackCount;
                    string moduleInstanceID;
                    string moduleLabel;
                    float availableMassKg;
                    if (admissionResolver.TryResolvePreferredRefrigeratedDestination(
                        thing,
                        count,
                        reservedColdMassByModule,
                        out moduleInstanceID,
                        out moduleLabel,
                        out availableMassKg))
                    {
                        records.Add(new ShuttlePendingLoadDestinationRecord(
                            thing,
                            count,
                            moduleInstanceID));

                        float mass = CargoDisplayUtility.GetThingMass(thing, count);
                        float reservedMass;
                        reservedColdMassByModule.TryGetValue(moduleInstanceID, out reservedMass);
                        reservedColdMassByModule[moduleInstanceID] = reservedMass + mass;
                    }

                    remaining -= count;
                }
            }

            return records;
        }

        private bool CommitPendingLoadDestinationPlan(
            ShuttleRuntimeState runtimeState,
            bool cleanExistingQueue,
            List<ShuttlePendingLoadDestinationRecord> destinationPlan)
        {
            if (runtimeState == null)
            {
                return true;
            }

            ShuttlePendingLoadDestinationState pendingDestinations = runtimeState.PendingLoadDestinations;
            if (pendingDestinations == null)
            {
                return true;
            }

            if (cleanExistingQueue)
            {
                pendingDestinations.Clear();
            }

            pendingDestinations.AddRange(destinationPlan);
            return true;
        }

        public bool ClearQueuedLoad(ThingWithComps host, out string failureReason)
        {
            bool cleared = this.transporterAdapter.ClearQueuedLoad(host, out failureReason);
            if (cleared)
            {
                CompModularShuttleCore core = host != null ? host.TryGetComp<CompModularShuttleCore>() : null;
                if (core != null)
                {
                    core.ClearPendingLoadDestinations();
                }
            }

            return cleared;
        }

        public bool CancelQueuedLoadEntry(
            ThingWithComps host,
            ShuttleRuntimeState runtimeState,
            int transporterIndex,
            int queueIndex,
            int count,
            out string failureReason)
        {
            TransferableOneWay canceledTransferable;
            int canceledCount;
            bool canceled = this.transporterAdapter.CancelQueuedLoadEntry(
                host,
                transporterIndex,
                queueIndex,
                count,
                out canceledTransferable,
                out canceledCount,
                out failureReason);
            if (canceled &&
                runtimeState != null &&
                runtimeState.PendingLoadDestinations != null)
            {
                runtimeState.PendingLoadDestinations.RemoveForTransferable(canceledTransferable, canceledCount);
            }

            return canceled;
        }

        public bool UnloadLoadedCargoEntry(
            ThingWithComps host,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            out string failureReason)
        {
            return this.transporterAdapter.UnloadLoadedCargoEntry(
                host,
                transporterIndex,
                loadedIndex,
                thingIDNumber,
                expectedDefName,
                count,
                out failureReason);
        }

        public bool UnloadLoadedCargoEntry(
            ThingWithComps host,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            out int unloadedCount,
            out string failureReason)
        {
            return this.transporterAdapter.UnloadLoadedCargoEntry(
                host,
                transporterIndex,
                loadedIndex,
                thingIDNumber,
                expectedDefName,
                count,
                out unloadedCount,
                out failureReason);
        }

        public bool UnloadLoadedCargoBay(
            ThingWithComps host,
            IReadOnlyList<ShuttleLoadedCargoUnloadTarget> targets,
            out int unloadedStackCount,
            out int unloadedThingCount,
            out string failureReason)
        {
            return this.transporterAdapter.UnloadLoadedCargoBay(
                host,
                targets,
                out unloadedStackCount,
                out unloadedThingCount,
                out failureReason);
        }

        public List<CompTransporter> ResolveTransportersForLaunch(ThingWithComps host)
        {
            return this.transporterAdapter.ResolveTransportersForLaunch(host);
        }

        public List<IThingHolder> ResolveTransporterHoldersForLaunch(ThingWithComps host)
        {
            return this.transporterAdapter.ResolveTransporterHoldersForLaunch(host);
        }

        public bool HasPendingLoadQueue(ThingWithComps host)
        {
            return this.transporterAdapter.HasPendingLoadQueue(host);
        }

        public bool TryGetLoadedMassKg(ShuttleCargoSnapshot cargoSnapshot, out float loadedMassKg)
        {
            loadedMassKg = 0f;
            if (cargoSnapshot == null)
            {
                return false;
            }

            loadedMassKg = cargoSnapshot.LoadedMassKg +
                cargoSnapshot.RefrigeratedMassKg +
                cargoSnapshot.MedicalBayPatientMassKg +
                cargoSnapshot.ExternalRuntimeMassKg;
            return true;
        }

        public bool TryGetQueuedMassKg(ShuttleCargoSnapshot cargoSnapshot, out float queuedMassKg)
        {
            queuedMassKg = 0f;
            if (cargoSnapshot == null)
            {
                return false;
            }

            queuedMassKg = cargoSnapshot.QueuedMassKg;
            return true;
        }

        public bool AnyLaunchTransporterPositionRoofed(ThingWithComps host, Map map)
        {
            return this.transporterAdapter.AnyLaunchTransporterPositionRoofed(host, map);
        }

        public int GetLaunchGroupID(ThingWithComps host)
        {
            return this.transporterAdapter.GetLaunchGroupID(host);
        }

        public void TryRemoveLaunchLords(ThingWithComps host, Map map)
        {
            this.transporterAdapter.TryRemoveLaunchLords(host, map);
        }

        public bool PlanLaunchCargoHandoffs(
            ThingWithComps host,
            Map map,
            ThingDef activeTransporterDef,
            out List<ShuttleLaunchCargoHandoffPlan> plans,
            out string failureReason)
        {
            return this.launchCargoTransaction.PlanLaunchCargoHandoffs(
                host,
                map,
                activeTransporterDef,
                out plans,
                out failureReason);
        }

        public bool CommitLaunchCargoHandoffs(
            List<ShuttleLaunchCargoHandoffPlan> plans,
            Map rollbackMap,
            out List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            return this.launchCargoTransaction.CommitLaunchCargoHandoffs(
                plans,
                rollbackMap,
                out handoffs,
                out failureReason);
        }

        public LaunchCargoRollbackResult RollbackLaunchCargoHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, Map map)
        {
            return this.launchCargoTransaction.RollbackLaunchCargoHandoffs(handoffs, map);
        }

        public LaunchCargoFinalizeResult FinalizeLaunchCargoHandoffs(List<ShuttleLaunchCargoHandoff> handoffs, Map map)
        {
            return this.launchCargoTransaction.FinalizeLaunchCargoHandoffs(handoffs, map);
        }
    }
}
