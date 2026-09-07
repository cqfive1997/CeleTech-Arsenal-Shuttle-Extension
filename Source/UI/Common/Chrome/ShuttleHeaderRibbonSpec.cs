using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleHeaderRibbonSpec
    {
        internal readonly ShuttleHeaderProgressSpec Progress;
        internal readonly IList<ShuttleHeaderMetricSpec> Metrics;

        internal ShuttleHeaderRibbonSpec(
            ShuttleHeaderProgressSpec progress,
            IList<ShuttleHeaderMetricSpec> metrics)
        {
            this.Progress = progress;
            this.Metrics = metrics;
        }
    }
}
