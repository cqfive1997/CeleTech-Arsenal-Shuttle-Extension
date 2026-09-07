using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoVisibleStackCache
    {
        private int cachedProjectionRevision = int.MinValue;
        private V3CargoCategory cachedCategory = V3CargoCategory.None;
        private List<V3CargoVisibleStackModel> cachedVisibleStacks;

        internal List<V3CargoVisibleStackModel> GetOrBuild(
            int projectionRevision,
            V3CargoCategory selectedCategory,
            V3CargoPageReadModel model,
            V3CargoSelection selection)
        {
            if (this.cachedVisibleStacks != null &&
                this.cachedProjectionRevision == projectionRevision &&
                this.cachedCategory == selectedCategory)
            {
                ShuttleUIProfiler.RecordCounter(
                    ShuttleUIProfileCounter.CargoVisibleStackCacheHit);
                return this.cachedVisibleStacks;
            }

            ShuttleUIProfiler.RecordCounter(
                ShuttleUIProfileCounter.CargoVisibleStackCacheMiss);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.CargoVisibleStackBuild))
            {
                this.cachedVisibleStacks = selection != null
                    ? selection.BuildVisibleStacks(model, selectedCategory)
                    : new List<V3CargoVisibleStackModel>();
            }

            this.cachedProjectionRevision = projectionRevision;
            this.cachedCategory = selectedCategory;
            return this.cachedVisibleStacks;
        }
    }
}
