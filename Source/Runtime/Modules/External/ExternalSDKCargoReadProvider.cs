using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoReadProvider : IShuttleExternalCargoReadProvider
    {
        private readonly ShuttleController controller;
        private readonly ExternalSDKCargoReadSnapshotBuilder snapshotBuilder =
            new ExternalSDKCargoReadSnapshotBuilder();
        private readonly ExternalSDKCargoQueryEvaluator queryEvaluator =
            new ExternalSDKCargoQueryEvaluator();

        internal ExternalSDKCargoReadProvider(ShuttleController controller)
        {
            this.controller = controller;
        }

        public bool TryGetCargoReadSnapshot(out ShuttleExternalCargoReadSnapshot snapshot)
        {
            snapshot = this.GetCargoReadSnapshotOrUnavailable();
            return snapshot != null && snapshot.Available;
        }

        public ShuttleExternalCargoReadSnapshot GetCargoReadSnapshotOrUnavailable()
        {
            try
            {
                return this.snapshotBuilder.Build(this.controller);
            }
            catch (Exception exception)
            {
                return ExternalSDKCargoReadSnapshotBuilder.Unavailable(
                    "cargo read snapshot failed: " + exception.GetType().Name);
            }
        }

        public bool TryQueryCargo(
            ShuttleExternalCargoQuery query,
            out ShuttleExternalCargoQueryResult result)
        {
            result = null;
            try
            {
                ShuttleExternalCargoReadSnapshot snapshot =
                    this.GetCargoReadSnapshotOrUnavailable();
                result = this.queryEvaluator.Evaluate(snapshot, query);
                return result != null && result.Available;
            }
            catch (Exception exception)
            {
                result = ExternalSDKCargoQueryEvaluator.Unavailable(
                    "cargo query failed: " + exception.GetType().Name);
                return false;
            }
        }
    }
}
