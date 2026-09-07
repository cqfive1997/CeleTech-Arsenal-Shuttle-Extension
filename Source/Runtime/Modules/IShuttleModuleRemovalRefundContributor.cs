using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Internal, read-only contribution seam for runtime-owned items that must be returned
    /// before a module's runtime payload is deleted. Assembly removal/replacement remains
    /// responsible for item creation, placement, retry, and rollback-safe holding.
    /// </summary>
    internal interface IShuttleModuleRemovalRefundContributor
    {
        bool TryCollectRemovalRefunds(
            ShuttleModuleRuntimeContext context,
            ShuttleModuleRemovalRefundCollector collector,
            out string failureReason);
    }

    internal sealed class ShuttleModuleRemovalRefundCollector
    {
        private readonly List<ThingDefCountClass> refunds =
            new List<ThingDefCountClass>();

        internal void Add(ThingDef thingDef, int count)
        {
            if (thingDef == null || count <= 0)
            {
                return;
            }

            this.refunds.Add(new ThingDefCountClass(thingDef, count));
        }

        internal List<ThingDefCountClass> BuildList()
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            for (int i = 0; i < this.refunds.Count; i++)
            {
                ThingDefCountClass refund = this.refunds[i];
                if (refund == null || refund.thingDef == null || refund.count <= 0)
                {
                    continue;
                }

                result.Add(new ThingDefCountClass(refund.thingDef, refund.count));
            }

            return result;
        }
    }
}
