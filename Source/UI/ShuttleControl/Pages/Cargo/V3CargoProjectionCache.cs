using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoProjectionCache
    {
        private readonly V3CargoPageModel cachedModel = new V3CargoPageModel();
        private V3CargoProjectionKey cachedKey;
        private bool hasCachedModel;
        private int projectionRevision;

        internal int ProjectionRevision
        {
            get { return this.projectionRevision; }
        }

        internal V3CargoPageModel GetOrBuild(
            V3CargoProjectionKey key,
            V3CargoPageInputs inputs,
            V3CargoPageModelBuilder builder)
        {
            key = key ?? V3CargoProjectionKey.Empty;
            if (this.hasCachedModel &&
                this.cachedKey != null &&
                this.cachedKey.Matches(key))
            {
                ShuttleUIProfiler.RecordCounter(
                    ShuttleUIProfileCounter.CargoProjectionCacheHit);
                this.RefreshSourceFields(inputs);
                return this.cachedModel;
            }

            ShuttleUIProfiler.RecordCounter(
                ShuttleUIProfileCounter.CargoProjectionCacheMiss);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoProjectionBuild))
            {
                if (builder != null)
                {
                    builder.Fill(this.cachedModel, inputs);
                }
                else
                {
                    this.RefreshSourceFields(inputs);
                    this.cachedModel.CargoPageModel = new V3CargoPageReadModel();
                }
            }

            this.cachedKey = key;
            this.hasCachedModel = true;
            this.AdvanceProjectionRevision();
            return this.cachedModel;
        }

        private void RefreshSourceFields(V3CargoPageInputs inputs)
        {
            this.cachedModel.ControlModel =
                inputs != null && inputs.ControlModel != null
                    ? inputs.ControlModel
                    : new ShuttleControlReadModel();
            this.cachedModel.CargoSnapshot =
                inputs != null && inputs.CargoSnapshot != null
                    ? inputs.CargoSnapshot
                    : new ShuttleCargoSnapshot();
            this.cachedModel.CargoSnapshotRevision =
                inputs != null ? inputs.CargoSnapshotRevision : 0;
        }

        private void AdvanceProjectionRevision()
        {
            this.projectionRevision =
                this.projectionRevision == int.MaxValue
                    ? 1
                    : this.projectionRevision + 1;
        }
    }
}
