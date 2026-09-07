using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Snapshots
{
    /// <summary>
    /// Detached assembly projection for UI/read-model consumers.
    /// It must contain copied primitive data only, never live AssemblyState objects.
    /// </summary>
    public sealed class ShuttleAssemblyReadSnapshot
    {
        // Copied shuttle-level segment slots in display order.
        public List<ShuttleAssemblySegmentSlotSnapshot> SegmentSlots = new List<ShuttleAssemblySegmentSlotSnapshot>();
    }

    /// <summary>
    /// One copied segment-slot row. Labels are resolved by the core-side builder so UI does not
    /// need to traverse live assembly state.
    /// </summary>
    public sealed class ShuttleAssemblySegmentSlotSnapshot
    {
        public string SlotID;
        public string SlotTypeID;
        public bool IsRequired;
        public bool IsLocked;
        public bool IsFixed;
        public string SlotLabelKey;
        public string SlotShortLabelKey;
        public string SlotDescriptionKey;
        public string SlotDisplayLabel;
        public string SlotShortDisplayLabel;
        public string SlotDescription;
        public string DefaultSegmentDefName;
        public string DefaultSegmentLabel;
        public string DefaultSegmentDescription;
        public string InstalledSegmentInstanceID;
        public string InstalledSegmentDefName;
        public string InstalledSegmentLabel;
        public string InstalledSegmentDescription;
        public string InstalledSegmentTypeID;

        // Copied module slots exposed by the installed segment, if any.
        public List<ShuttleAssemblyModuleSlotSnapshot> ModuleSlots = new List<ShuttleAssemblyModuleSlotSnapshot>();
    }

    /// <summary>
    /// One copied module-slot row. This never owns the installed module; it only names it.
    /// </summary>
    public sealed class ShuttleAssemblyModuleSlotSnapshot
    {
        public string SlotID;
        public string SlotTypeID;
        public bool IsRequired;
        public bool IsLocked;
        public string SlotLabelKey;
        public string SlotShortLabelKey;
        public string SlotDescriptionKey;
        public string SlotDisplayLabel;
        public string SlotShortDisplayLabel;
        public string SlotDescription;
        public string InstalledModuleInstanceID;
        public string InstalledModuleDefName;
        public string InstalledModuleLabel;
        public string InstalledModuleDescription;
        public string InstalledModuleTypeID;
        public bool InstalledModuleEnabled;
    }
}
