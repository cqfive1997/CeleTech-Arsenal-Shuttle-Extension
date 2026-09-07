using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Snapshots
{
    /// <summary>
    /// Detached cargo-region settings projection for UI read models.
    /// It carries copied values only and must not be persisted or treated as cargo truth.
    /// </summary>
    public class ShuttleCargoRegionSnapshot
    {
        public int RegionIndex;
        public string Label;
        public bool HasCustomLabel;
        public bool AllowHumans = true;
        public bool AllowAnimals = true;
        public bool AllowMechs = true;

        // Detached filter copy for immediate UI editing. The durable filter remains in
        // ShuttleCargoRegionConfigState and is updated only through commands.
        public ThingFilter ItemFilter = ThingFilter.CreateOnlyEverStorableThingFilter();
    }

    public sealed class ShuttleCargoRegionSettingsSnapshot : ShuttleCargoRegionSnapshot
    {
    }
}
