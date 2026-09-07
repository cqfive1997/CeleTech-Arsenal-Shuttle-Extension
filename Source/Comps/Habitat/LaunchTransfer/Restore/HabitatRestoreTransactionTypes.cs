using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    // Transient restore transaction carrier only. Not Scribed.
    internal sealed class HabitatLivingRestoreTransaction
    {
        internal readonly List<HabitatLivingMovedThing> MovedThings =
            new List<HabitatLivingMovedThing>();

        internal readonly List<HabitatDiningRestorePlanEntry> PreparedSplitFoods =
            new List<HabitatDiningRestorePlanEntry>();

        internal readonly List<HabitatMixedDiningRestorePlanEntry> PreparedMixedSplitFoods =
            new List<HabitatMixedDiningRestorePlanEntry>();
    }

    // Transient moved-thing rollback carrier only. Not Scribed.
    internal sealed class HabitatLivingMovedThing
    {
        internal Thing Thing;
        internal ThingOwner OriginalOwner;
        internal ShuttleHolderLaunchManifestEntry Entry;
    }
}
