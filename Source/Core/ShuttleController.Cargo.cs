using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Food;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal void NotifyCargoHauledToTransporter(CompTransporter transporter, Thing thing, int count)
        {
            ShuttleControllerCargoFacade.NotifyCargoHauledToTransporter(
                this,
                transporter,
                thing,
                count);
        }

        internal void ClearPendingLoadDestinations()
        {
            ShuttleControllerCargoFacade.ClearPendingLoadDestinations(this);
        }

        internal ShuttleCargoSnapshot BuildCargoSnapshot()
        {
            return ShuttleControllerCargoFacade.BuildCargoSnapshot(this);
        }

        internal ShuttleCargoUnloadProgressSnapshot BuildCargoUnloadProgressSnapshot()
        {
            return ShuttleControllerCargoFacade.BuildCargoUnloadProgressSnapshot(this);
        }

        internal bool HasQueuedLoadsForLoadCargo()
        {
            return ShuttleControllerCargoFacade.HasQueuedLoadsForLoadCargo(this);
        }

        internal float GetLoadCargoAvailableMass()
        {
            return ShuttleControllerCargoFacade.GetLoadCargoAvailableMass(this);
        }

        internal int GetLoadCargoSelectedCount(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            return ShuttleControllerCargoFacade.GetLoadCargoSelectedCount(
                this,
                passengerTransferables,
                cargoTransferables);
        }

        internal float GetLoadCargoSelectedMassKg(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            return ShuttleControllerCargoFacade.GetLoadCargoSelectedMassKg(
                this,
                passengerTransferables,
                cargoTransferables);
        }

        private IShuttleCargoResourceBroker BuildCargoResourceBroker(ShuttleProfile currentProfile)
        {
            return ShuttleControllerCargoCache.BuildCargoResourceBroker(this, currentProfile);
        }

        internal IShuttleCargoResourceBroker GetCargoResourceBrokerForInternalTransactions()
        {
            this.EnsureRuntimeState();
            return this.BuildCargoResourceBroker(this.GetProfileForRead());
        }

        internal ShuttleCargoSupplySnapshot BuildCargoSupplySnapshotForRead()
        {
            IShuttleCargoResourceBroker cargoBroker =
                this.GetCargoResourceBrokerForInternalTransactions();
            IShuttleCargoSupplyReadPort supplyReadPort =
                cargoBroker as IShuttleCargoSupplyReadPort;
            return supplyReadPort != null ? supplyReadPort.GetSupplySnapshot() : null;
        }

        internal void InvalidateCargoInventorySnapshotCaches()
        {
            ShuttleControllerCargoCache.InvalidateCargoInventorySnapshotCaches(this);
        }

        private void InvalidateProfileDependentRuntimeCaches()
        {
            ShuttleControllerCargoCache.InvalidateProfileDependentRuntimeCaches(this);
        }

        private ShuttleRuntimeMassContributionSnapshot BuildExternalRuntimeMassContributionSnapshot(
            ShuttleProfile currentProfile,
            int ticksGame)
        {
            return ShuttleControllerCargoCache.BuildExternalRuntimeMassContributionSnapshot(
                this,
                currentProfile,
                ticksGame);
        }

        internal ShuttleRuntimeMassContributionSnapshot BuildExternalRuntimeMassContributionSnapshotForRead()
        {
            return ShuttleControllerCargoCache.BuildExternalRuntimeMassContributionSnapshotForRead(this);
        }

        private void InvalidateExternalRuntimeMassContributionCache()
        {
            ShuttleControllerCargoCache.InvalidateExternalRuntimeMassContributionCache(this);
        }

        private IShuttleCargoColdTransferService BuildRefrigeratedCargoTransferService(ShuttleProfile currentProfile)
        {
            return ShuttleControllerCargoCache.BuildRefrigeratedCargoTransferService(this, currentProfile);
        }

        private IShuttleCargoPostDepositRouter BuildPostDepositColdRouter(
            ShuttleProfile currentProfile)
        {
            return ShuttleControllerCargoCache.BuildPostDepositColdRouter(
                this,
                currentProfile);
        }

        private ShuttleRefrigeratedCargoAutoTransferConfig ResolveRefrigeratedCargoAutoTransferConfig(
            string moduleInstanceID)
        {
            return ShuttleControllerCargoCache.ResolveRefrigeratedCargoAutoTransferConfig(
                this,
                moduleInstanceID);
        }

        private void MarkRefrigeratedCargoRegistryDirty()
        {
            ShuttleControllerCargoCache.MarkRefrigeratedCargoRegistryDirty(this);
        }

        private void ReconcileRefrigeratedCargoRegistryIfNeeded(int ticksGame)
        {
            ShuttleControllerCargoCache.ReconcileRefrigeratedCargoRegistryIfNeeded(this, ticksGame);
        }

        internal bool IsPassengerScheduledForBoarding(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return false;
            }

            if (this.runtimeState != null &&
                this.runtimeState.PassengerBoardingIntents != null &&
                this.runtimeState.PassengerBoardingIntents.ContainsThingID(
                    pawn.thingIDNumber))
            {
                return true;
            }

            IShuttlePassengerBoardingBackend boardingBackend =
                this.cargoBackend as IShuttlePassengerBoardingBackend;
            return boardingBackend != null &&
                boardingBackend.ContainsPassenger(this.shuttleHost, pawn);
        }

        internal bool TryPreparePassengerForMedicalAdmission(
            Pawn pawn,
            out string failureReason)
        {
            failureReason = null;
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                failureReason =
                    "CT_Shuttle_Cargo_PassengerUnavailable".Translate().ToString();
                return false;
            }

            this.EnsureRuntimeState();
            ShuttlePassengerBoardingIntentState intentState =
                this.runtimeState.PassengerBoardingIntents;
            bool hasDurableIntent =
                intentState != null &&
                intentState.ContainsThingID(pawn.thingIDNumber);
            IShuttlePassengerBoardingBackend boardingBackend =
                this.cargoBackend as IShuttlePassengerBoardingBackend;
            bool isInBoardingBackend =
                boardingBackend != null &&
                boardingBackend.ContainsPassenger(this.shuttleHost, pawn);
            if (!hasDurableIntent && !isInBoardingBackend)
            {
                return true;
            }

            if (isInBoardingBackend)
            {
                bool canceled;
                if (!boardingBackend.TryCancelPendingPassenger(
                        this.shuttleHost,
                        pawn,
                        out canceled,
                        out failureReason))
                {
                    return false;
                }

                if (!canceled ||
                    boardingBackend.ContainsPassenger(this.shuttleHost, pawn))
                {
                    failureReason =
                        "CT_Shuttle_Cargo_QueuedEntryUnavailable".Translate().ToString();
                    return false;
                }

                this.InvalidateCargoInventorySnapshotCaches();
            }

            if (intentState == null || !intentState.Ensure(pawn))
            {
                failureReason =
                    "CT_Shuttle_Cargo_PassengerUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal ShuttlePassengerInternalTransferResult TryTransferCompletedPassengerToCockpit(
            Pawn pawn,
            ThingOwner<Thing> source,
            out string failureReason)
        {
            failureReason = null;
            this.EnsureRuntimeState();
            if (this.shuttleHost == null ||
                pawn == null ||
                source == null ||
                this.runtimeState.PassengerBoardingIntents == null ||
                !this.runtimeState.PassengerBoardingIntents.ContainsThingID(pawn.thingIDNumber))
            {
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            IShuttlePassengerBoardingBackend boardingBackend =
                this.cargoBackend as IShuttlePassengerBoardingBackend;
            if (boardingBackend == null)
            {
                failureReason = "Passenger boarding backend is unavailable.";
                return ShuttlePassengerInternalTransferResult.FallbackToMap;
            }

            ShuttlePassengerInternalTransferResult result =
                boardingBackend.TryTransferHeldPassengerToCockpit(
                    this.shuttleHost,
                    pawn,
                    source,
                    out failureReason);
            if (result == ShuttlePassengerInternalTransferResult.Transferred)
            {
                this.InvalidateCargoInventorySnapshotCaches();
            }

            return result;
        }

        internal IShuttleHabitatFoodSource BuildHabitatFoodSource()
        {
            this.EnsureRuntimeState();
            ShuttleProfile currentProfile = this.GetProfileForRead();
            IShuttleCargoResourceBroker cargoBroker =
                this.BuildCargoResourceBroker(currentProfile);
            ShuttleCargoFoodWithdrawalSource foodSource =
                new ShuttleCargoFoodWithdrawalSource(
                    this.cargoBackend,
                    cargoBroker,
                    currentProfile,
                    this.runtimeState,
                    this.assemblyState);
            return new ShuttleHabitatFoodSource(foodSource);
        }

        internal ShuttlePrisonerCargoFoodSource BuildPrisonerCargoFoodSource()
        {
            this.EnsureRuntimeState();
            ShuttleProfile currentProfile = this.GetProfileForRead();
            IShuttleCargoResourceBroker cargoBroker =
                this.BuildCargoResourceBroker(currentProfile);
            return new ShuttlePrisonerCargoFoodSource(
                currentProfile,
                new ShuttleCargoFoodWithdrawalSource(
                    this.cargoBackend,
                    cargoBroker,
                    currentProfile,
                    this.runtimeState,
                    this.assemblyState));
        }
    }
}
