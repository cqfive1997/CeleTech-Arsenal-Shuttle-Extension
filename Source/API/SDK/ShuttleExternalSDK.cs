using System;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public static class ShuttleExternalSDK
    {
        private static readonly Version APIVersionValue = new Version(1, 8, 0);

        public static Version APIVersion
        {
            get
            {
                return APIVersionValue;
            }
        }

        public static IShuttleExternalSDKInfoProvider InfoProvider
        {
            get
            {
                return ExternalSDKInfoProvider.Instance;
            }
        }

        public static IShuttleExternalIntegrationHealthProvider IntegrationHealthProvider
        {
            get
            {
                return ExternalSDKHealthProvider.Instance;
            }
        }

        public static ShuttleExternalSDKInfo GetSDKInfo()
        {
            return InfoProvider.GetSDKInfo();
        }

        public static bool IsFeatureSupported(string featureKey)
        {
            return InfoProvider.IsFeatureSupported(featureKey);
        }

        public static ShuttleExternalIntegrationHealthReport GetIntegrationHealthReport()
        {
            return IntegrationHealthProvider.GetIntegrationHealthReport();
        }

        public static bool TryGetReadSnapshotProvider(
            IShuttleExternalSDKHost host,
            out IShuttleExternalReadSnapshotProvider provider)
        {
            provider = null;
            return host != null &&
                host.TryGetExternalReadSnapshotProvider(out provider) &&
                provider != null;
        }

        public static bool TryGetReadSnapshot(
            IShuttleExternalSDKHost host,
            out ShuttleExternalReadSnapshot snapshot)
        {
            snapshot = null;
            IShuttleExternalReadSnapshotProvider provider;
            return TryGetReadSnapshotProvider(host, out provider) &&
                provider.TryGetReadSnapshot(out snapshot);
        }

        public static bool TryGetCargoReadProvider(
            IShuttleExternalSDKHost host,
            out IShuttleExternalCargoReadProvider provider)
        {
            provider = null;
            IShuttleExternalSDKCargoHost cargoHost = host as IShuttleExternalSDKCargoHost;
            return cargoHost != null &&
                cargoHost.TryGetExternalCargoReadProvider(out provider) &&
                provider != null;
        }

        public static bool TryGetCargoReadSnapshot(
            IShuttleExternalSDKHost host,
            out ShuttleExternalCargoReadSnapshot snapshot)
        {
            snapshot = null;
            IShuttleExternalCargoReadProvider provider;
            return TryGetCargoReadProvider(host, out provider) &&
                provider.TryGetCargoReadSnapshot(out snapshot);
        }

        public static bool TryQueryCargo(
            IShuttleExternalSDKHost host,
            ShuttleExternalCargoQuery query,
            out ShuttleExternalCargoQueryResult result)
        {
            result = null;
            IShuttleExternalCargoReadProvider provider;
            return TryGetCargoReadProvider(host, out provider) &&
                provider.TryQueryCargo(query, out result);
        }

        public static bool TryGetCargoTransactionProvider(
            IShuttleExternalSDKHost host,
            out IShuttleExternalCargoTransactionProvider provider)
        {
            provider = null;
            IShuttleExternalSDKCargoTransactionHost transactionHost =
                host as IShuttleExternalSDKCargoTransactionHost;
            return transactionHost != null &&
                transactionHost.TryGetExternalCargoTransactionProvider(out provider) &&
                provider != null;
        }

        public static bool TryQuoteCargoConsume(
            IShuttleExternalSDKHost host,
            ShuttleExternalCargoConsumeRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            result = null;
            IShuttleExternalCargoTransactionProvider provider;
            return TryGetCargoTransactionProvider(host, out provider) &&
                provider.TryQuoteConsume(request, out result);
        }

        public static bool TryConsumeCargo(
            IShuttleExternalSDKHost host,
            ShuttleExternalCargoConsumeRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            result = null;
            IShuttleExternalCargoTransactionProvider provider;
            return TryGetCargoTransactionProvider(host, out provider) &&
                provider.TryConsume(request, out result);
        }

        public static bool TryQuoteCargoDeposit(
            IShuttleExternalSDKHost host,
            ShuttleExternalCargoDepositRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            result = null;
            IShuttleExternalCargoTransactionProvider provider;
            return TryGetCargoTransactionProvider(host, out provider) &&
                provider.TryQuoteDeposit(request, out result);
        }

        public static bool TryDepositCargo(
            IShuttleExternalSDKHost host,
            ShuttleExternalCargoDepositRequest request,
            out ShuttleExternalCargoTransactionResult result)
        {
            result = null;
            IShuttleExternalCargoTransactionProvider provider;
            return TryGetCargoTransactionProvider(host, out provider) &&
                provider.TryDeposit(request, out result);
        }

        public static bool TryGetCargoTransactionDiagnosticsProvider(
            IShuttleExternalSDKHost host,
            out IShuttleExternalCargoTransactionDiagnosticsProvider provider)
        {
            provider = null;
            IShuttleExternalSDKCargoTransactionDiagnosticsHost diagnosticsHost =
                host as IShuttleExternalSDKCargoTransactionDiagnosticsHost;
            return diagnosticsHost != null &&
                diagnosticsHost.TryGetExternalCargoTransactionDiagnosticsProvider(out provider) &&
                provider != null;
        }

        public static bool TryGetCargoTransactionDiagnostics(
            IShuttleExternalSDKHost host,
            out ShuttleExternalCargoTransactionDiagnosticsSnapshot snapshot)
        {
            snapshot = null;
            IShuttleExternalCargoTransactionDiagnosticsProvider provider;
            return TryGetCargoTransactionDiagnosticsProvider(host, out provider) &&
                provider.TryGetCargoTransactionDiagnostics(out snapshot);
        }

        public static bool TryGetLaunchReadProvider(
            IShuttleExternalSDKHost host,
            out IShuttleExternalLaunchReadProvider provider)
        {
            provider = null;
            IShuttleExternalSDKLaunchReadHost launchHost =
                host as IShuttleExternalSDKLaunchReadHost;
            return launchHost != null &&
                launchHost.TryGetExternalLaunchReadProvider(out provider) &&
                provider != null;
        }

        public static bool TryGetLaunchReadSnapshot(
            IShuttleExternalSDKHost host,
            out ShuttleExternalLaunchReadSnapshot snapshot)
        {
            snapshot = null;
            IShuttleExternalLaunchReadProvider provider;
            return TryGetLaunchReadProvider(host, out provider) &&
                provider.TryGetLaunchReadSnapshot(out snapshot);
        }

        public static bool TryQuoteLaunch(
            IShuttleExternalSDKHost host,
            ShuttleExternalLaunchQuoteRequest request,
            out ShuttleExternalLaunchQuoteResult result)
        {
            result = null;
            IShuttleExternalLaunchReadProvider provider;
            return TryGetLaunchReadProvider(host, out provider) &&
                provider.TryQuoteLaunch(request, out result);
        }

        public static bool TryGetLaunchOccupantHandoffProvider(
            IShuttleExternalSDKHost host,
            out IShuttleExternalLaunchOccupantHandoffProvider provider)
        {
            provider = null;
            IShuttleExternalSDKLaunchOccupantHandoffHost handoffHost =
                host as IShuttleExternalSDKLaunchOccupantHandoffHost;
            return handoffHost != null &&
                handoffHost.TryGetExternalLaunchOccupantHandoffProvider(out provider) &&
                provider != null;
        }

        public static bool TryQuoteLaunchOccupantHandoff(
            IShuttleExternalSDKHost host,
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            result = null;
            IShuttleExternalLaunchOccupantHandoffProvider provider;
            return TryGetLaunchOccupantHandoffProvider(host, out provider) &&
                provider.TryQuoteHandoff(request, out result);
        }

        public static bool TryHandoffLaunchOccupant(
            IShuttleExternalSDKHost host,
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ShuttleExternalLaunchOccupantHandoffResult result)
        {
            result = null;
            IShuttleExternalLaunchOccupantHandoffProvider provider;
            return TryGetLaunchOccupantHandoffProvider(host, out provider) &&
                provider.TryHandoff(request, out result);
        }
    }
}
