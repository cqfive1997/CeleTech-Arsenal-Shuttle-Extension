using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchReadProvider : IShuttleExternalLaunchReadProvider
    {
        private readonly ShuttleController controller;
        private readonly ExternalSDKLaunchReadSnapshotBuilder snapshotBuilder =
            new ExternalSDKLaunchReadSnapshotBuilder();
        private readonly ExternalSDKLaunchQuoteBuilder quoteBuilder =
            new ExternalSDKLaunchQuoteBuilder();

        internal ExternalSDKLaunchReadProvider(ShuttleController controller)
        {
            this.controller = controller;
        }

        public bool TryGetLaunchReadSnapshot(out ShuttleExternalLaunchReadSnapshot snapshot)
        {
            snapshot = this.GetLaunchReadSnapshotOrUnavailable();
            return snapshot != null && snapshot.Available;
        }

        public ShuttleExternalLaunchReadSnapshot GetLaunchReadSnapshotOrUnavailable()
        {
            try
            {
                return this.snapshotBuilder.Build(this.controller);
            }
            catch (Exception exception)
            {
                return this.snapshotBuilder.BuildUnavailableFromException(exception);
            }
        }

        public bool TryQuoteLaunch(
            ShuttleExternalLaunchQuoteRequest request,
            out ShuttleExternalLaunchQuoteResult result)
        {
            result = null;
            try
            {
                ShuttleExternalLaunchReadSnapshot snapshot =
                    this.GetLaunchReadSnapshotOrUnavailable();
                result = this.quoteBuilder.BuildResult(request, snapshot);
                return result != null && result.Available;
            }
            catch (Exception exception)
            {
                result = ExternalSDKLaunchQuoteBuilder.UnavailableResult(
                    "launch quote failed: " + exception.GetType().Name);
                return false;
            }
        }
    }
}
