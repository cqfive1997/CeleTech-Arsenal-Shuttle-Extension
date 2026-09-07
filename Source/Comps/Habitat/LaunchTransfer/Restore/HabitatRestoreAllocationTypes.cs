using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    // Transient local-restore allocation only. Not Scribed.
    internal sealed class DiningFoodRestoreAllocation
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Thing Food;
        internal ThingOwner Source;
        internal bool WasSplit;
    }

    // Transient local-restore allocation only. Not Scribed.
    internal sealed class PawnRestoreAllocation
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Pawn Pawn;
    }
}
