using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    public sealed class ShuttleControlSegmentSlotModel
    {
        public string SlotID;
        public string SlotTypeID;
        public bool IsRequired;
        public bool IsLocked;
        public bool IsFixed;
        public string DisplayLabel;
        public string ShortDisplayLabel;
        public string DisplayLabelKey;
        public string Description;
        public string DescriptionKey;
        public string DefaultSegmentDefName;
        public string DefaultSegmentLabel;
        public string DefaultSegmentDescription;
        public string InstalledSegmentInstanceID;
        public string InstalledSegmentDefName;
        public string InstalledSegmentLabel;
        public string InstalledSegmentDescription;
        public string SegmentTypeID;
        public bool IsRemovalInProgress;
        public float RemovalProgress01;
        public string RemovalStatusLabel;
        public string RemovalTooltip;
        public string RemovalLastFailureReason;
        public bool CanCancelRemoval;
        public bool CanAssignRemovalWorker;
        public List<ShuttleControlModuleSlotModel> ModuleSlots = new List<ShuttleControlModuleSlotModel>();
    }

    public sealed class ShuttleControlModuleSlotModel
    {
        public string SlotID;
        public string SlotTypeID;
        public bool IsRequired;
        public bool IsLocked;
        public string DisplayLabel;
        public string ShortDisplayLabel;
        public string DisplayLabelKey;
        public string Description;
        public string DescriptionKey;
        public string InstalledModuleInstanceID;
        public string InstalledModuleDefName;
        public string InstalledModuleLabel;
        public string InstalledModuleDescription;
        public string InstalledModuleTypeID;
        public bool InstalledModuleEnabled;
        public bool IsRemovalInProgress;
        public float RemovalProgress01;
        public string RemovalStatusLabel;
        public string RemovalTooltip;
        public string RemovalLastFailureReason;
        public bool CanCancelRemoval;
        public bool CanAssignRemovalWorker;
    }
}
