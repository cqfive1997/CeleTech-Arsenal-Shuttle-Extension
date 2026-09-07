namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public interface IShuttleExternalSDKInfoProvider
    {
        ShuttleExternalSDKInfo GetSDKInfo();

        bool IsFeatureSupported(string featureKey);
    }

    public interface IShuttleExternalIntegrationHealthProvider
    {
        ShuttleExternalIntegrationHealthReport GetIntegrationHealthReport();
    }

    public interface IShuttleExternalReadSnapshotProvider
    {
        bool TryGetReadSnapshot(out ShuttleExternalReadSnapshot snapshot);

        ShuttleExternalReadSnapshot GetReadSnapshotOrUnavailable();
    }

    public interface IShuttleExternalCargoReadProvider
    {
        bool TryGetCargoReadSnapshot(out ShuttleExternalCargoReadSnapshot snapshot);

        ShuttleExternalCargoReadSnapshot GetCargoReadSnapshotOrUnavailable();

        bool TryQueryCargo(
            ShuttleExternalCargoQuery query,
            out ShuttleExternalCargoQueryResult result);
    }

    public interface IShuttleExternalCargoTransactionProvider
    {
        bool TryQuoteConsume(
            ShuttleExternalCargoConsumeRequest request,
            out ShuttleExternalCargoTransactionResult result);

        bool TryConsume(
            ShuttleExternalCargoConsumeRequest request,
            out ShuttleExternalCargoTransactionResult result);

        bool TryQuoteDeposit(
            ShuttleExternalCargoDepositRequest request,
            out ShuttleExternalCargoTransactionResult result);

        bool TryDeposit(
            ShuttleExternalCargoDepositRequest request,
            out ShuttleExternalCargoTransactionResult result);
    }

    public interface IShuttleExternalCargoTransactionDiagnosticsProvider
    {
        bool TryGetCargoTransactionDiagnostics(
            out ShuttleExternalCargoTransactionDiagnosticsSnapshot snapshot);

        ShuttleExternalCargoTransactionDiagnosticsSnapshot GetCargoTransactionDiagnosticsOrUnavailable();
    }

    public interface IShuttleExternalLaunchReadProvider
    {
        bool TryGetLaunchReadSnapshot(out ShuttleExternalLaunchReadSnapshot snapshot);

        ShuttleExternalLaunchReadSnapshot GetLaunchReadSnapshotOrUnavailable();

        bool TryQuoteLaunch(
            ShuttleExternalLaunchQuoteRequest request,
            out ShuttleExternalLaunchQuoteResult result);
    }

    public interface IShuttleExternalLaunchOccupantHandoffProvider
    {
        /// <summary>
        /// Checks whether a caller-owned holder pawn can be moved into launch payload now.
        /// Quote does not move the pawn and must not be treated as a later reservation.
        /// </summary>
        bool TryQuoteHandoff(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ShuttleExternalLaunchOccupantHandoffResult result);

        /// <summary>
        /// Moves a caller-owned holder pawn into launch payload after re-running validation.
        /// </summary>
        bool TryHandoff(
            ShuttleExternalLaunchOccupantHandoffRequest request,
            out ShuttleExternalLaunchOccupantHandoffResult result);
    }

    public interface IShuttleExternalSDKHost
    {
        bool TryGetExternalReadSnapshotProvider(out IShuttleExternalReadSnapshotProvider provider);

        bool TryGetExternalReadSnapshot(out ShuttleExternalReadSnapshot snapshot);
    }

    public interface IShuttleExternalSDKCargoHost
    {
        bool TryGetExternalCargoReadProvider(out IShuttleExternalCargoReadProvider provider);

        bool TryGetExternalCargoReadSnapshot(out ShuttleExternalCargoReadSnapshot snapshot);
    }

    public interface IShuttleExternalSDKCargoTransactionHost
    {
        bool TryGetExternalCargoTransactionProvider(
            out IShuttleExternalCargoTransactionProvider provider);
    }

    public interface IShuttleExternalSDKCargoTransactionDiagnosticsHost
    {
        bool TryGetExternalCargoTransactionDiagnosticsProvider(
            out IShuttleExternalCargoTransactionDiagnosticsProvider provider);
    }

    public interface IShuttleExternalSDKLaunchReadHost
    {
        bool TryGetExternalLaunchReadProvider(
            out IShuttleExternalLaunchReadProvider provider);
    }

    public interface IShuttleExternalSDKLaunchOccupantHandoffHost
    {
        bool TryGetExternalLaunchOccupantHandoffProvider(
            out IShuttleExternalLaunchOccupantHandoffProvider provider);
    }
}
