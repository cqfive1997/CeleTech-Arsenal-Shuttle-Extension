using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchQuoteBuilder
    {
        internal const string DestinationQuotePolicy =
            "Destination-specific launch quote is not supported in SDK 1.7.0. " +
            "This quote is destination-independent and reports current capability only.";

        private const float Epsilon = 0.0001f;
        private readonly IShuttleFlightEnergyCalculator flightEnergyCalculator =
            new DefaultShuttleFlightEnergyCalculator();

        internal ShuttleExternalLaunchCostSnapshot BuildCost(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            ShuttleFlightEnergyQuote quote = this.flightEnergyCalculator.Quote(
                profile,
                runtimeState,
                cargoSnapshot,
                0,
                1f);

            if (quote == null)
            {
                return UnavailableCost();
            }

            return new ShuttleExternalLaunchCostSnapshot(
                profile != null && profile.Flight != null
                    ? profile.Flight.BaseLaunchEnergyWd
                    : 0f,
                profile != null && profile.Flight != null
                    ? profile.Flight.EnergyPerTileWd
                    : 0f,
                profile != null && profile.Flight != null
                    ? ShuttlePayloadCapacityPolicy
                        .ResolveEffectiveMassDistanceEnergyPerKgTileWd(profile)
                    : 0f,
                ShuttlePayloadLiftEnergyPolicy.ResolveNominalEnergyPerKgWd(profile),
                quote.PayloadLiftEnergyWd,
                profile != null && profile.Flight != null
                    ? profile.Flight.ReserveEnergyWd
                    : 0f,
                runtimeState != null && runtimeState.Power != null
                    ? runtimeState.Power.StoredEnergyWd
                    : 0f,
                quote.AvailableEnergyWd,
                quote.RequiredEnergyWd,
                quote.MaxDistanceTilesAtCurrentLoad,
                profile != null && profile.Flight != null
                    ? profile.Flight.HardRangeCapTiles
                    : 0,
                1f,
                false,
                quote.RequiredEnergyWd <= quote.AvailableEnergyWd + Epsilon);
        }

        internal ShuttleExternalLaunchQuoteResult BuildResult(
            ShuttleExternalLaunchQuoteRequest request,
            ShuttleExternalLaunchReadSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return UnavailableResult("launch read snapshot is unavailable");
            }

            bool destinationRequested =
                request != null && request.IncludeDestinationQuote;
            if (destinationRequested)
            {
                return new ShuttleExternalLaunchQuoteResult(
                    false,
                    "destination-specific launch quote is deferred",
                    false,
                    false,
                    DestinationQuotePolicy,
                    snapshot.Readiness,
                    snapshot.Payload,
                    snapshot.Cost,
                    snapshot.Cooldown,
                    snapshot.Issues,
                    snapshot.RuleDiagnostics);
            }

            return new ShuttleExternalLaunchQuoteResult(
                snapshot.Available,
                snapshot.UnavailableReason,
                false,
                false,
                DestinationQuotePolicy,
                snapshot.Readiness,
                snapshot.Payload,
                snapshot.Cost,
                snapshot.Cooldown,
                snapshot.Issues,
                snapshot.RuleDiagnostics);
        }

        internal static ShuttleExternalLaunchCostSnapshot UnavailableCost()
        {
            return new ShuttleExternalLaunchCostSnapshot(
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0,
                0,
                1f,
                false,
                false);
        }

        internal static ShuttleExternalLaunchQuoteResult UnavailableResult(string reason)
        {
            return new ShuttleExternalLaunchQuoteResult(
                false,
                reason,
                false,
                false,
                DestinationQuotePolicy,
                ExternalSDKLaunchReadinessBuilder.Unavailable(),
                ExternalSDKLaunchPayloadSnapshotBuilder.Unavailable(),
                UnavailableCost(),
                ExternalSDKLaunchCooldownSnapshotBuilder.Unavailable(),
                new List<ShuttleExternalLaunchIssueSnapshot>());
        }
    }
}
