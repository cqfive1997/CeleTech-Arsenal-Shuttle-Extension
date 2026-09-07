using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Flight
{
    public sealed class DefaultShuttleFlightEnergyCalculator : IShuttleFlightEnergyCalculator
    {
        private const float Epsilon = 0.0001f;

        public ShuttleFlightEnergyQuote Quote(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoSnapshot cargoSnapshot,
            int distanceTiles,
            float rangeDistanceFactor)
        {
            ShuttleFlightEnergyQuote quote = new ShuttleFlightEnergyQuote();
            quote.DistanceTiles = distanceTiles < 0 ? 0 : distanceTiles;

            if (profile == null)
            {
                quote.FailureReason = "CT_Shuttle_Launch_Failed_ProfileUnavailable".Translate().ToString();
                return quote;
            }

            if (runtimeState == null)
            {
                quote.FailureReason = "CT_Shuttle_Launch_Failed_RuntimeUnavailable".Translate().ToString();
                return quote;
            }

            runtimeState.EnsureInitialized();

            float rangeFactor = rangeDistanceFactor > 0f ? rangeDistanceFactor : 1f;
            // The quote uses gross launch mass. Structure is always present; loaded cargo is
            // already in the hold; queued cargo is included so targeting can reject unfinished
            // load plans with the same mass envelope the validator will see. Refrigerated cargo
            // and MedicalBay patients are already loaded payload even though they live outside
            // CompTransporter.
            quote.StructuralMassKg = this.GetStructuralMassKg(profile, cargoSnapshot);
            quote.LoadedCargoMassKg = cargoSnapshot != null
                ? cargoSnapshot.LoadedMassKg +
                    cargoSnapshot.RefrigeratedMassKg +
                    cargoSnapshot.MedicalBayPatientMassKg +
                    cargoSnapshot.ExternalRuntimeMassKg
                : 0f;
            quote.QueuedCargoMassKg = cargoSnapshot != null ? cargoSnapshot.QueuedMassKg : 0f;
            quote.TotalLaunchMassKg = quote.StructuralMassKg + quote.LoadedCargoMassKg + quote.QueuedCargoMassKg;
            quote.PayloadLiftEnergyWd = this.GetPayloadLiftEnergyWd(
                profile,
                quote.LoadedCargoMassKg + quote.QueuedCargoMassKg);

            // Reserve energy is not spendable launch energy. This keeps the calculator aligned
            // with the future return-flight floor without storing any runtime value in profile.
            quote.AvailableEnergyWd = this.GetAvailableEnergyWd(profile, runtimeState);
            quote.RequiredEnergyWd = this.GetRequiredEnergyWd(
                profile,
                quote.DistanceTiles,
                rangeFactor,
                quote.StructuralMassKg,
                quote.LoadedCargoMassKg + quote.QueuedCargoMassKg,
                quote.PayloadLiftEnergyWd);
            quote.MaxDistanceTilesAtCurrentLoad = this.GetMaxDistanceTilesAtCurrentLoad(
                profile,
                quote.AvailableEnergyWd,
                rangeFactor,
                quote.StructuralMassKg,
                quote.LoadedCargoMassKg + quote.QueuedCargoMassKg,
                quote.PayloadLiftEnergyWd);

            ShuttleCargoCapacityFailure capacityFailure =
                ShuttleRefrigeratedCargoCapacityPolicy.ValidatePlannedCargo(
                    profile,
                    cargoSnapshot);
            if (capacityFailure ==
                ShuttleCargoCapacityFailure.PositiveBaseCapacityRequired)
            {
                quote.FailureReason = "CT_Shuttle_Launch_Failed_PositiveCargoCapacityRequired".Translate().ToString();
                return quote;
            }

            if (capacityFailure == ShuttleCargoCapacityFailure.BaseCargoExceeded)
            {
                quote.FailureReason = "CT_Shuttle_Launch_Failed_CargoExceedsCapacity".Translate().ToString();
                return quote;
            }

            if (capacityFailure ==
                ShuttleCargoCapacityFailure.RefrigeratedCargoExceeded)
            {
                quote.FailureReason =
                    "CT_Shuttle_Launch_Failed_RefrigeratedCargoExceedsCapacity"
                        .Translate()
                        .ToString();
                return quote;
            }

            if (quote.DistanceTiles > quote.MaxDistanceTilesAtCurrentLoad)
            {
                quote.FailureReason = "CT_Shuttle_Launch_Failed_BeyondCurrentRange".Translate().ToString();
                return quote;
            }

            if (quote.RequiredEnergyWd > quote.AvailableEnergyWd + Epsilon)
            {
                quote.FailureReason = "CT_Shuttle_Launch_Failed_StoredChargeInsufficient".Translate().ToString();
                return quote;
            }

            quote.CanLaunch = true;
            return quote;
        }

        private float GetAvailableEnergyWd(ShuttleProfile profile, ShuttleRuntimeState runtimeState)
        {
            float storedEnergyWd = runtimeState.Power != null ? runtimeState.Power.StoredEnergyWd : 0f;
            float reserveEnergyWd = profile.Flight != null ? profile.Flight.ReserveEnergyWd : 0f;
            float available = storedEnergyWd - reserveEnergyWd;
            return available > 0f ? available : 0f;
        }

        private float GetRequiredEnergyWd(
            ShuttleProfile profile,
            int distanceTiles,
            float rangeDistanceFactor,
            float structuralMassKg,
            float payloadMassKg,
            float payloadLiftEnergyWd)
        {
            if (profile == null || profile.Flight == null)
            {
                return 0f;
            }

            // RequiredEnergy = base cost + payload lift cost + distance cost + mass-distance cost.
            // Keeping this formula here prevents UI/targeting code from reimplementing it.
            float effectiveMassDistanceEnergyPerKgTileWd =
                ShuttlePayloadCapacityPolicy
                    .ResolveEffectiveMassDistanceEnergyPerKgTileWd(profile);
            return profile.Flight.BaseLaunchEnergyWd +
                payloadLiftEnergyWd +
                (distanceTiles * rangeDistanceFactor * profile.Flight.EnergyPerTileWd) +
                (distanceTiles * rangeDistanceFactor *
                    ((structuralMassKg * profile.Flight.EnergyPerKgTileWd) +
                    (payloadMassKg * effectiveMassDistanceEnergyPerKgTileWd)));
        }

        private float GetPayloadLiftEnergyWd(ShuttleProfile profile, float payloadMassKg)
        {
            if (profile == null || profile.Flight == null || payloadMassKg <= 0f)
            {
                return 0f;
            }

            return ShuttlePayloadLiftEnergyPolicy.CalculatePayloadLiftEnergyWd(
                profile,
                payloadMassKg);
        }

        private int GetMaxDistanceTilesAtCurrentLoad(
            ShuttleProfile profile,
            float availableEnergyWd,
            float rangeDistanceFactor,
            float structuralMassKg,
            float payloadMassKg,
            float payloadLiftEnergyWd)
        {
            if (profile == null || profile.Flight == null)
            {
                return 0;
            }

            int hardCap = profile.Flight.HardRangeCapTiles >= 0
                ? profile.Flight.HardRangeCapTiles
                : int.MaxValue;
            float energyAfterBase = availableEnergyWd - profile.Flight.BaseLaunchEnergyWd - payloadLiftEnergyWd;
            if (energyAfterBase < 0f)
            {
                return 0;
            }

            // This is the same launch formula solved for distance at the current load. If the
            // cargo mass changes, the denominator changes and the max distance must be quoted again.
            float effectiveMassDistanceEnergyPerKgTileWd =
                ShuttlePayloadCapacityPolicy
                    .ResolveEffectiveMassDistanceEnergyPerKgTileWd(profile);
            float denominator = rangeDistanceFactor *
                (profile.Flight.EnergyPerTileWd +
                    (structuralMassKg * profile.Flight.EnergyPerKgTileWd) +
                    (payloadMassKg * effectiveMassDistanceEnergyPerKgTileWd));
            if (denominator <= Epsilon)
            {
                return hardCap;
            }

            int energyLimited = (int)(energyAfterBase / denominator);
            if (energyLimited < 0)
            {
                energyLimited = 0;
            }

            return energyLimited < hardCap ? energyLimited : hardCap;
        }

        private float GetStructuralMassKg(ShuttleProfile profile, ShuttleCargoSnapshot cargoSnapshot)
        {
            if (profile != null && profile.Mass != null)
            {
                return profile.Mass.TotalMass;
            }

            return cargoSnapshot != null ? cargoSnapshot.StructuralMass : 0f;
        }

    }
}
