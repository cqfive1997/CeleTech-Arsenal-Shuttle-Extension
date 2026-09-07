using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionDiagnosticsProvider :
        IShuttleExternalCargoTransactionDiagnosticsProvider
    {
        private static readonly ExternalSDKCargoTransactionDiagnosticsProvider instance =
            new ExternalSDKCargoTransactionDiagnosticsProvider();

        private ExternalSDKCargoTransactionDiagnosticsProvider()
        {
        }

        internal static ExternalSDKCargoTransactionDiagnosticsProvider Instance
        {
            get
            {
                return instance;
            }
        }

        public bool TryGetCargoTransactionDiagnostics(
            out ShuttleExternalCargoTransactionDiagnosticsSnapshot snapshot)
        {
            return ExternalSDKCargoTransactionDiagnosticsRecorder.Instance.TryGetSnapshot(
                out snapshot);
        }

        public ShuttleExternalCargoTransactionDiagnosticsSnapshot GetCargoTransactionDiagnosticsOrUnavailable()
        {
            ShuttleExternalCargoTransactionDiagnosticsSnapshot snapshot;
            if (this.TryGetCargoTransactionDiagnostics(out snapshot) && snapshot != null)
            {
                return snapshot;
            }

            return ExternalSDKCargoTransactionDiagnosticsRecorder.Instance.BuildUnavailableSnapshotForRead(
                "cargo transaction diagnostics unavailable");
        }
    }
}
