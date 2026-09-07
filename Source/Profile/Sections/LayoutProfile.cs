using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class LayoutProfile
    {
        public LayoutProfile(
            IReadOnlyList<SegmentLayoutEntry> segments,
            IReadOnlyList<ModuleLayoutEntry> modules,
            int segmentSlotCount,
            int installedSegmentCount,
            int moduleSlotCount,
            int installedModuleCount)
        {
            this.Segments = segments;
            this.Modules = modules;
            this.SegmentSlotCount = segmentSlotCount;
            this.InstalledSegmentCount = installedSegmentCount;
            this.ModuleSlotCount = moduleSlotCount;
            this.InstalledModuleCount = installedModuleCount;
        }

        public IReadOnlyList<SegmentLayoutEntry> Segments { get; private set; }

        public IReadOnlyList<ModuleLayoutEntry> Modules { get; private set; }

        public int SegmentSlotCount { get; private set; }

        public int InstalledSegmentCount { get; private set; }

        public int ModuleSlotCount { get; private set; }

        public int InstalledModuleCount { get; private set; }
    }

    public sealed class SegmentLayoutEntry
    {
        public SegmentLayoutEntry(
            string slotID,
            string slotTypeID,
            string segmentInstanceID,
            string segmentDefName,
            string defaultSegmentDefName,
            bool isFixed,
            bool isRequired,
            bool isLocked)
        {
            this.SlotID = slotID;
            this.SlotTypeID = slotTypeID;
            this.SegmentInstanceID = segmentInstanceID;
            this.SegmentDefName = segmentDefName;
            this.DefaultSegmentDefName = defaultSegmentDefName;
            this.IsFixed = isFixed;
            this.IsRequired = isRequired;
            this.IsLocked = isLocked;
        }

        public string SlotID { get; private set; }
        public string SlotTypeID { get; private set; }
        public string SegmentInstanceID { get; private set; }
        public string SegmentDefName { get; private set; }
        public string DefaultSegmentDefName { get; private set; }
        public bool IsFixed { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsLocked { get; private set; }
    }

    public sealed class ModuleLayoutEntry
    {
        public ModuleLayoutEntry(
            string parentSegmentInstanceID,
            string slotID,
            string slotTypeID,
            string moduleInstanceID,
            string moduleDefName,
            bool isRequired,
            bool isLocked,
            bool isEnabled)
        {
            this.ParentSegmentInstanceID = parentSegmentInstanceID;
            this.SlotID = slotID;
            this.SlotTypeID = slotTypeID;
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleDefName = moduleDefName;
            this.IsRequired = isRequired;
            this.IsLocked = isLocked;
            this.IsEnabled = isEnabled;
        }

        public string ParentSegmentInstanceID { get; private set; }
        public string SlotID { get; private set; }
        public string SlotTypeID { get; private set; }
        public string ModuleInstanceID { get; private set; }
        public string ModuleDefName { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsEnabled { get; private set; }
    }
}
