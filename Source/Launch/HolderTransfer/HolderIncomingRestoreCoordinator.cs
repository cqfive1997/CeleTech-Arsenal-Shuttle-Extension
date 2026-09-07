using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    // Phase 6 incoming restore seam only. Specific restore implementation stays
    // in the service core for now; Habitat incoming restore transaction should be
    // the last piece migrated because it owns the most failure/recovery detail.
    internal static class HolderIncomingRestoreCoordinator
    {
        internal static bool TryRestoreBeforeIncomingImpact(
            ThingWithComps shuttleHost,
            ThingOwner incomingSkyfallerContainer,
            Map map,
            IntVec3 fallbackCell,
            out string failureReason,
            out ShuttleHolderIncomingRestoreFailureStatus failureStatus)
        {
            return ShuttleHolderLaunchTransferService.TryRestoreBeforeIncomingImpactCore(
                shuttleHost,
                incomingSkyfallerContainer,
                map,
                fallbackCell,
                out failureReason,
                out failureStatus);
        }
    }
}
