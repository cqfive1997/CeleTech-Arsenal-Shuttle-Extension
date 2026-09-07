using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal sealed class ShuttleCargoRegionSettingsSnapshotBuilder
    {
        public ShuttleCargoRegionSettingsSnapshot Build(int regionIndex, ShuttleCargoRegionSettings settings)
        {
            ShuttleCargoRegionSettingsSnapshot snapshot = new ShuttleCargoRegionSettingsSnapshot();
            snapshot.RegionIndex = regionIndex;
            snapshot.Label = settings != null ? settings.Label : null;
            snapshot.HasCustomLabel = settings != null && settings.HasCustomLabel;
            snapshot.AllowHumans = true;
            snapshot.AllowAnimals = true;
            snapshot.AllowMechs = true;
            snapshot.ItemFilter = this.CopyFilter(settings != null ? settings.FilterForRead : null);
            return snapshot;
        }

        private ThingFilter CopyFilter(ThingFilter source)
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
