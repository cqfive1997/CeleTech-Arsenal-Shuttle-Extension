using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Reads the vanilla transporter manifest demand that shares the shuttle host's
    /// IHaulEnroute destination identity. It never mutates the manifest or holder.
    /// </summary>
    internal static class ShuttleTransporterEnrouteCapacityReader
    {
        internal static int GetPendingCountFor(ThingWithComps shuttleHost, ThingDef thingDef)
        {
            if (shuttleHost == null || thingDef == null)
            {
                return 0;
            }

            CompTransporter transporter = shuttleHost.TryGetComp<CompTransporter>();
            List<TransferableOneWay> leftToLoad = transporter != null
                ? transporter.leftToLoad
                : null;
            if (leftToLoad == null || leftToLoad.Count == 0)
            {
                return 0;
            }

            long pendingCount = 0L;
            for (int i = 0; i < leftToLoad.Count; i++)
            {
                TransferableOneWay transferable = leftToLoad[i];
                if (transferable == null ||
                    transferable.CountToTransfer <= 0 ||
                    !ContainsThingDef(transferable, thingDef))
                {
                    continue;
                }

                pendingCount += transferable.CountToTransfer;
                if (pendingCount >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)pendingCount;
        }

        private static bool ContainsThingDef(TransferableOneWay transferable, ThingDef thingDef)
        {
            if (transferable == null || transferable.things == null)
            {
                return false;
            }

            for (int i = 0; i < transferable.things.Count; i++)
            {
                Thing thing = transferable.things[i];
                if (thing != null && thing.def == thingDef)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
