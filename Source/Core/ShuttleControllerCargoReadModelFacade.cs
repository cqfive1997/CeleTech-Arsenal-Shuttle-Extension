using System.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        private static class ShuttleControllerCargoReadModelFacade
        {
            internal static ShuttleLoadCargoReadModel BuildLoadCargoReadModel(
                ShuttleController owner,
                bool forceRefresh)
            {
                ShuttleProfile currentProfile = owner.GetProfileForRead();
                int ticksGame = owner.GetTicksGameSafe();
                int currentRevision = currentProfile != null ? currentProfile.Revision : owner.profileRevision;
                ShuttleRuntimeMassContributionSnapshot externalMassSnapshot =
                    owner.BuildExternalRuntimeMassContributionSnapshot(currentProfile, ticksGame);
                int externalMassRevision = externalMassSnapshot != null ? externalMassSnapshot.Revision : 0;
                owner.loadCargoReadModelRequestCount++;

                if (!forceRefresh &&
                    CanUseCachedLoadCargoReadModel(
                        owner,
                        ticksGame,
                        currentRevision,
                        externalMassRevision))
                {
                    return owner.cachedLoadCargoReadModel;
                }

                Stopwatch stopwatch = CeleTechShuttleMod.ShouldLogDevTimeCosts ? Stopwatch.StartNew() : null;
                ShuttleLoadCargoReadModel readModel = owner.cargoBackend.BuildLoadCargoReadModel(
                    owner.shuttleHost,
                    currentProfile,
                    owner.assemblyState,
                    owner.runtimeState);
                ApplyExternalRuntimeMassToLoadCargoReadModel(readModel, externalMassSnapshot);
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                }

                owner.cachedLoadCargoReadModel = readModel;
                owner.cachedLoadCargoReadModelHost = owner.shuttleHost;
                owner.cachedLoadCargoReadModelAssemblyState = owner.assemblyState;
                owner.cachedLoadCargoReadModelRuntimeState = owner.runtimeState;
                owner.cachedLoadCargoReadModelTick = ticksGame;
                owner.cachedLoadCargoReadModelProfileRevision = currentRevision;
                owner.cachedLoadCargoReadModelExternalMassRevision = externalMassRevision;
                owner.loadCargoReadModelBuildCount++;
                LogLoadCargoReadModelDevStats(
                    owner,
                    ticksGame,
                    stopwatch != null ? stopwatch.ElapsedMilliseconds : 0L,
                    readModel,
                    currentRevision);
                return readModel;
            }

            internal static void InvalidateLoadCargoReadModelCache(ShuttleController owner)
            {
                owner.cachedLoadCargoReadModel = null;
                owner.cachedLoadCargoReadModelHost = null;
                owner.cachedLoadCargoReadModelAssemblyState = null;
                owner.cachedLoadCargoReadModelRuntimeState = null;
                owner.cachedLoadCargoReadModelTick = int.MinValue;
                owner.cachedLoadCargoReadModelProfileRevision = -1;
                owner.cachedLoadCargoReadModelExternalMassRevision = 0;
            }

            private static bool CanUseCachedLoadCargoReadModel(
                ShuttleController owner,
                int ticksGame,
                int profileRevision,
                int externalMassRevision)
            {
                if (owner.cachedLoadCargoReadModel == null ||
                    !object.ReferenceEquals(owner.cachedLoadCargoReadModelHost, owner.shuttleHost) ||
                    !object.ReferenceEquals(owner.cachedLoadCargoReadModelAssemblyState, owner.assemblyState) ||
                    !object.ReferenceEquals(owner.cachedLoadCargoReadModelRuntimeState, owner.runtimeState) ||
                    owner.cachedLoadCargoReadModelProfileRevision != profileRevision ||
                    owner.cachedLoadCargoReadModelExternalMassRevision != externalMassRevision ||
                    owner.cachedLoadCargoReadModelTick == int.MinValue)
                {
                    return false;
                }

                return ticksGame < 0 ||
                    ticksGame == owner.cachedLoadCargoReadModelTick ||
                    ticksGame - owner.cachedLoadCargoReadModelTick < LoadCargoReadModelCacheRefreshTicks;
            }

            private static void ApplyExternalRuntimeMassToLoadCargoReadModel(
                ShuttleLoadCargoReadModel readModel,
                ShuttleRuntimeMassContributionSnapshot externalMassSnapshot)
            {
                if (readModel == null || externalMassSnapshot == null || externalMassSnapshot.TotalMassKg <= 0f)
                {
                    return;
                }

                readModel.ExistingMassUsageKg += externalMassSnapshot.TotalMassKg;
                readModel.RegularAvailableMassKg = readModel.MassCapacityKg - readModel.ExistingMassUsageKg;
                if (readModel.RefrigeratedMassSharesOverallCapacity)
                {
                    readModel.RegularAvailableMassKg -=
                        readModel.RefrigeratedAutoTransferUsedMassKg;
                }

                if (readModel.RegularAvailableMassKg < 0f)
                {
                    readModel.RegularAvailableMassKg = 0f;
                }

                if (readModel.RefrigeratedMassSharesOverallCapacity)
                {
                    readModel.RefrigeratedAutoTransferAvailableMassKg =
                        readModel.RegularAvailableMassKg;
                    readModel.AvailableMassKg = readModel.RegularAvailableMassKg;
                }
                else
                {
                    readModel.AvailableMassKg = readModel.RegularAvailableMassKg +
                        readModel.RefrigeratedAutoTransferAvailableMassKg;
                }
            }

            private static void LogLoadCargoReadModelDevStats(
                ShuttleController owner,
                int ticksGame,
                long elapsedMs,
                ShuttleLoadCargoReadModel readModel,
                int profileRevision)
            {
                if (!CeleTechShuttleMod.ShouldLogDevTimeCosts ||
                    elapsedMs < LoadCargoReadModelSlowBuildLogThresholdMs)
                {
                    owner.loadCargoReadModelRequestCount = 0;
                    owner.loadCargoReadModelBuildCount = 0;
                    return;
                }

                if (ticksGame >= 0 &&
                    owner.loadCargoReadModelLastDevLogTick != int.MinValue &&
                    ticksGame - owner.loadCargoReadModelLastDevLogTick < LoadCargoReadModelCacheRefreshTicks)
                {
                    owner.loadCargoReadModelRequestCount = 0;
                    owner.loadCargoReadModelBuildCount = 0;
                    return;
                }

                int passengerRows = readModel != null && readModel.PassengerTransferables != null
                    ? readModel.PassengerTransferables.Count
                    : 0;
                int cargoRows = readModel != null && readModel.CargoTransferables != null
                    ? readModel.CargoTransferables.Count
                    : 0;
                Log.Message(
                    "[CeleTech Shuttle] Load cargo read model rebuilt in " + elapsedMs +
                    " ms; rows passenger=" + passengerRows +
                    ", cargo=" + cargoRows +
                    ", profileRevision=" + profileRevision +
                    ", requests=" + owner.loadCargoReadModelRequestCount +
                    ", builds=" + owner.loadCargoReadModelBuildCount + ".");
                owner.loadCargoReadModelRequestCount = 0;
                owner.loadCargoReadModelBuildCount = 0;
                owner.loadCargoReadModelLastDevLogTick = ticksGame;
            }
        }
    }
}
