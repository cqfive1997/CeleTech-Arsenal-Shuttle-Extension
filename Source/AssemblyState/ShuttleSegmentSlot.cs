using Verse;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// One concrete segment slot on the shuttle body.
    /// It only describes slot identity, slot order, and which installed segment currently
    /// occupies that position.
    /// </summary>
    public sealed class ShuttleSegmentSlot : IExposable
    {
        private string slotID;
        private string slotTypeID;
        private ShuttleSegmentType slotType = ShuttleSegmentType.Optional;
        private int slotIndex;
        private bool isRequired;
        private bool isLocked;
        private bool isFixed;
        private string defaultSegmentDefName;
        private string labelKey;
        private string shortLabelKey;
        private string descriptionKey;
        private string installedSegmentInstanceID;

        public ShuttleSegmentSlot()
        {
        }

        public ShuttleSegmentSlot(
            string slotID,
            string slotTypeID,
            int slotIndex,
            bool isRequired,
            bool isLocked,
            bool isFixed,
            string defaultSegmentDefName,
            string labelKey = null,
            string shortLabelKey = null,
            string descriptionKey = null)
        {
            this.slotID = slotID;
            this.slotIndex = slotIndex;
            this.isRequired = isRequired;
            this.isLocked = isLocked;
            this.isFixed = isFixed;
            this.defaultSegmentDefName = defaultSegmentDefName;
            this.labelKey = labelKey;
            this.shortLabelKey = shortLabelKey;
            this.descriptionKey = descriptionKey;
            this.SetSlotTypeID(slotTypeID);
        }

        public string SlotID
        {
            get
            {
                return this.slotID;
            }
        }

        public string SlotTypeID
        {
            get
            {
                return this.slotTypeID;
            }
        }

        public ShuttleSegmentType SlotType
        {
            get
            {
                return this.slotType;
            }
        }

        public int SlotIndex
        {
            get
            {
                return this.slotIndex;
            }
        }

        public bool IsRequired
        {
            get
            {
                return this.isRequired;
            }
        }

        public bool IsLocked
        {
            get
            {
                return this.isLocked;
            }
        }

        public bool IsFixed
        {
            get
            {
                return this.isFixed;
            }
        }

        public string DefaultSegmentDefName
        {
            get
            {
                return this.defaultSegmentDefName;
            }
        }

        public string LabelKey
        {
            get
            {
                return this.labelKey;
            }
        }

        public string ShortLabelKey
        {
            get
            {
                return this.shortLabelKey;
            }
        }

        public string DescriptionKey
        {
            get
            {
                return this.descriptionKey;
            }
        }

        public string InstalledSegmentInstanceID
        {
            get
            {
                return this.installedSegmentInstanceID;
            }
        }

        public bool IsOccupied
        {
            get
            {
                return !string.IsNullOrEmpty(this.installedSegmentInstanceID);
            }
        }

        internal void SetInstalledSegmentInstanceID(string value)
        {
            this.installedSegmentInstanceID = value;
        }

        internal bool UpdateDisplayMetadataIfMissing(
            string labelKey,
            string shortLabelKey,
            string descriptionKey)
        {
            bool changed = false;
            if (string.IsNullOrEmpty(this.labelKey) && !string.IsNullOrEmpty(labelKey))
            {
                this.labelKey = labelKey;
                changed = true;
            }

            if (string.IsNullOrEmpty(this.shortLabelKey) && !string.IsNullOrEmpty(shortLabelKey))
            {
                this.shortLabelKey = shortLabelKey;
                changed = true;
            }

            if (string.IsNullOrEmpty(this.descriptionKey) && !string.IsNullOrEmpty(descriptionKey))
            {
                this.descriptionKey = descriptionKey;
                changed = true;
            }

            return changed;
        }

        private void SetSlotTypeID(string value)
        {
            this.slotTypeID = ShuttleSegmentTypeCatalog.NormalizeSegmentSlotTypeID(value);
            this.slotType = ShuttleSegmentTypeCatalog.ParseSlotType(this.slotTypeID);
        }

        public void EnsureInitialized(int slotIndex)
        {
            if (this.slotIndex < 0)
            {
                this.slotIndex = slotIndex;
            }

            if (string.IsNullOrEmpty(this.slotID))
            {
                // Compatibility fallback only.
                // New runtime slot IDs should be generated by ShuttleAssemblyBootstrapper during
                // layout materialization and then persisted through save/load.
                this.slotID = "segment.slot." + this.slotIndex;
            }

            this.SetSlotTypeID(this.slotTypeID);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.slotID, "slotID");
            Scribe_Values.Look(ref this.slotTypeID, "slotTypeID");
            Scribe_Values.Look(ref this.slotIndex, "slotIndex", 0);
            Scribe_Values.Look(ref this.isRequired, "isRequired", false);
            Scribe_Values.Look(ref this.isLocked, "isLocked", false);
            Scribe_Values.Look(ref this.isFixed, "isFixed", false);
            Scribe_Values.Look(ref this.defaultSegmentDefName, "defaultSegmentDefName");
            Scribe_Values.Look(ref this.labelKey, "labelKey");
            Scribe_Values.Look(ref this.shortLabelKey, "shortLabelKey");
            Scribe_Values.Look(ref this.descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref this.installedSegmentInstanceID, "installedSegmentInstanceID");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized(this.slotIndex);
            }
        }
    }
}
