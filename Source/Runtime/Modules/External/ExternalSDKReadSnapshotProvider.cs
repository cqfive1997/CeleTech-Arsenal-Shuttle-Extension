using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKReadSnapshotProvider : IShuttleExternalReadSnapshotProvider
    {
        private readonly ShuttleController controller;

        internal ExternalSDKReadSnapshotProvider(ShuttleController controller)
        {
            this.controller = controller;
        }

        public bool TryGetReadSnapshot(out ShuttleExternalReadSnapshot snapshot)
        {
            snapshot = this.GetReadSnapshotOrUnavailable();
            return snapshot != null && snapshot.Available;
        }

        public ShuttleExternalReadSnapshot GetReadSnapshotOrUnavailable()
        {
            try
            {
                return ExternalSDKReadSnapshotBuilder.Build(this.controller);
            }
            catch (Exception exception)
            {
                return ExternalSDKReadSnapshotBuilder.Unavailable(
                    "External read snapshot failed: " + exception.GetType().Name + ": " +
                    exception.Message);
            }
        }
    }
}
