using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal interface IShuttleCargoColdTransferService
    {
        bool TryRouteLoadedCargoToCold(
            string moduleInstanceID,
            CompTransporter sourceTransporter,
            Thing deliveredThing,
            int count,
            string reason,
            out int movedCount,
            out string failureReason);

        bool TryTransferLoadedCargoToCold(
            string moduleInstanceID,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            string reason,
            out int movedCount,
            out string failureReason);

        bool TryAutoTransferLoadedCargoToCold(
            string moduleInstanceID,
            int maxStacks,
            ThingFilter effectiveAutoTransferFilter,
            bool hasCustomAutoTransferFilter,
            string reason,
            out int movedStackCount,
            out int movedThingCount,
            out string failureReason);

        bool TryTransferColdCargoToLoadedCargo(
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            string reason,
            out int movedCount,
            out string failureReason);

        bool TryUnloadColdCargoBay(
            string moduleInstanceID,
            IReadOnlyList<ShuttleColdCargoUnloadTarget> targets,
            string reason,
            out int unloadedStackCount,
            out int unloadedThingCount,
            out string failureReason);

        bool TryUnloadColdCargoEntry(
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            string reason,
            out int unloadedCount,
            out string failureReason);
    }

    internal sealed class ShuttleColdCargoUnloadTarget
    {
        internal ShuttleColdCargoUnloadTarget(
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count)
        {
            this.ColdIndex = coldIndex;
            this.ThingIDNumber = thingIDNumber;
            this.ExpectedDefName = expectedDefName;
            this.Count = count;
        }

        internal int ColdIndex { get; private set; }
        internal int ThingIDNumber { get; private set; }
        internal string ExpectedDefName { get; private set; }
        internal int Count { get; private set; }
    }
}
