using Verse;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// One concrete module slot exposed by an installed segment instance.
    /// It only answers which slot this is, which segment owns it, and which installed module
    /// currently occupies it.
    /// </summary>
    public sealed class ShuttleModuleSlot : IExposable
    {
        private string slotID;
        private string slotTypeID;
        private ShuttleModuleType slotType = ShuttleModuleType.Optional;
        private string parentSegmentInstanceID;
        private int slotIndex;
        private bool isRequired;
        private bool isLocked;
        private string labelKey;
        private string shortLabelKey;
        private string descriptionKey;
        private string installedModuleInstanceID;

        public ShuttleModuleSlot()
        {
        }

        public ShuttleModuleSlot(
            string slotID,
            string slotTypeID,
            string parentSegmentInstanceID,
            int slotIndex,
            bool isRequired,
            bool isLocked,
            string labelKey = null,
            string shortLabelKey = null,
            string descriptionKey = null)
        {
            this.slotID = slotID;
            this.parentSegmentInstanceID = parentSegmentInstanceID;
            this.slotIndex = slotIndex;
            this.isRequired = isRequired;
            this.isLocked = isLocked;
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

        public ShuttleModuleType SlotType
        {
            get
            {
                return this.slotType;
            }
        }

        public string ParentSegmentInstanceID
        {
            get
            {
                return this.parentSegmentInstanceID;
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

        public string InstalledModuleInstanceID
        {
            get
            {
                return this.installedModuleInstanceID;
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

        public bool IsOccupied
        {
            get
            {
                return !string.IsNullOrEmpty(this.installedModuleInstanceID);
            }
        }

        internal void SetInstalledModuleInstanceID(string value)
        {
            this.installedModuleInstanceID = value;
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
            this.slotTypeID = ShuttleModuleTypeCatalog.NormalizeModuleSlotTypeID(value);
            this.slotType = ShuttleModuleTypeCatalog.ParseSlotType(this.slotTypeID);
        }

        public void EnsureInitialized(string parentSegmentInstanceID, int slotIndex)
        {
            if (string.IsNullOrEmpty(this.parentSegmentInstanceID))
            {
                this.parentSegmentInstanceID = parentSegmentInstanceID;
            }

            if (this.slotIndex < 0)
            {
                this.slotIndex = slotIndex;
            }

            if (string.IsNullOrEmpty(this.slotID))
            {
                // Compatibility fallback only.
                // New runtime slot IDs should be generated when a concrete segment materializes
                // its module topology and then persisted through save/load.
                this.slotID = this.parentSegmentInstanceID + ".slot." + this.slotIndex;
            }

            this.SetSlotTypeID(this.slotTypeID);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.slotID, "slotID");
            Scribe_Values.Look(ref this.slotTypeID, "slotTypeID");
            Scribe_Values.Look(ref this.parentSegmentInstanceID, "parentSegmentInstanceID");
            Scribe_Values.Look(ref this.slotIndex, "slotIndex", 0);
            Scribe_Values.Look(ref this.isRequired, "isRequired", false);
            Scribe_Values.Look(ref this.isLocked, "isLocked", false);
            Scribe_Values.Look(ref this.labelKey, "labelKey");
            Scribe_Values.Look(ref this.shortLabelKey, "shortLabelKey");
            Scribe_Values.Look(ref this.descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref this.installedModuleInstanceID, "installedModuleInstanceID");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized(this.parentSegmentInstanceID, this.slotIndex);
            }
        }
    }
}
