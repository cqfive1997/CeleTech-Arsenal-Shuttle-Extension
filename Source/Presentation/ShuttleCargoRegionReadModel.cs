using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// Copied cargo-region settings for UI display/editing.
    /// The item filter is copied again so windows cannot mutate snapshot or durable state.
    /// </summary>
    public sealed class ShuttleCargoRegionReadModel
    {
        public int RegionIndex;
        public string Label;
        public bool HasCustomLabel;
        public bool AllowHumans;
        public bool AllowAnimals;
        public bool AllowMechs;
        public ThingFilter ItemFilter;

        public ShuttleCargoRegionReadModel()
        {
            this.ItemFilter = ThingFilter.CreateOnlyEverStorableThingFilter();
        }

        public ShuttleCargoRegionReadModel(ShuttleCargoRegionSnapshot snapshot)
        {
            int regionIndex = snapshot != null ? snapshot.RegionIndex : -1;
            this.RegionIndex = regionIndex;
            this.Label = snapshot != null ? snapshot.Label : null;
            this.HasCustomLabel = snapshot != null && snapshot.HasCustomLabel;
            this.AllowHumans = snapshot == null || snapshot.AllowHumans;
            this.AllowAnimals = snapshot == null || snapshot.AllowAnimals;
            this.AllowMechs = snapshot == null || snapshot.AllowMechs;
            this.ItemFilter = CopyFilter(snapshot != null ? snapshot.ItemFilter : null);
        }

        public static ShuttleCargoRegionReadModel FromSnapshot(ShuttleCargoRegionSnapshot snapshot)
        {
            return new ShuttleCargoRegionReadModel(snapshot);
        }

        private static ThingFilter CopyFilter(ThingFilter source)
        {
            ThingFilter copy = ThingFilter.CreateOnlyEverStorableThingFilter();
            if (source != null)
            {
                copy.CopyAllowancesFrom(source);
            }

            return copy;
        }
    }
}
