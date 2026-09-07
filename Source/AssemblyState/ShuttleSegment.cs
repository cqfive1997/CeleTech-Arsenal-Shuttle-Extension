using System.Collections.Generic;
using Verse;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// One shuttle segment instance installed into the current assembly topology.
    /// It stores install-time identity, the referenced segment def, the position on the shuttle,
    /// and the module slots exposed by this segment instance. Runtime live values should not
    /// live here.
    /// </summary>
    public sealed class ShuttleSegment : IExposable
    {
        // Stable instance ID for this installed segment.
        private string segmentInstanceID;

        // Static segment definition referenced by this segment instance.
        private ShuttleSegmentBaseDef segmentDef;

        // Persisted fallback for missing/renamed defs. Keep this even if segmentDef fails to load.
        private string segmentDefNameForSave;

        // Position index of this segment on the shuttle body.
        private int segmentIndex;

        // Whether this segment is fixed and cannot be removed.
        private bool isFixed;

        // Per-instance installation configuration overrides.
        private Dictionary<string, string> instanceConfig = new Dictionary<string, string>();

        // Concrete module slots exposed by this segment instance.
        // The installed module objects themselves live in ShuttleAssemblyState.
        private List<ShuttleModuleSlot> moduleSlots = new List<ShuttleModuleSlot>();

        public ShuttleSegment()
        {
        }

        public ShuttleSegment(
            string segmentInstanceID,
            ShuttleSegmentBaseDef segmentDef,
            int segmentIndex)
        {
            this.segmentInstanceID = segmentInstanceID;
            this.segmentDef = segmentDef;
            this.segmentDefNameForSave = segmentDef != null ? segmentDef.defName : null;
            this.segmentIndex = segmentIndex;
        }

        public string SegmentInstanceID
        {
            get
            {
                return this.segmentInstanceID;
            }
        }

        public ShuttleSegmentBaseDef SegmentDef
        {
            get
            {
                return this.segmentDef;
            }
        }

        public int SegmentIndex
        {
            get
            {
                return this.segmentIndex;
            }
        }

        public bool IsFixed
        {
            get
            {
                return this.isFixed;
            }
        }

        public IReadOnlyList<ShuttleModuleSlot> ModuleSlots
        {
            get
            {
                return this.moduleSlots;
            }
        }

        public string segmentDefName
        {
            get
            {
                return this.segmentDef != null ? this.segmentDef.defName : this.segmentDefNameForSave;
            }
        }

        internal void AssignSegmentInstanceID(string value)
        {
            this.segmentInstanceID = value;
        }

        internal void SetFixed(bool value)
        {
            this.isFixed = value;
        }

        internal void ClearModuleSlots()
        {
            this.EnsureInitialized();
            this.moduleSlots.Clear();
        }

        internal void AddModuleSlot(ShuttleModuleSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            this.EnsureInitialized();
            this.moduleSlots.Add(slot);
        }

        public TSegmentDef GetSegmentDef<TSegmentDef>() where TSegmentDef : ShuttleSegmentBaseDef
        {
            return this.segmentDef as TSegmentDef;
        }

        public ShuttleModuleSlot GetModuleSlotByID(string slotID)
        {
            if (this.moduleSlots == null)
            {
                return null;
            }

            for (int i = 0; i < this.moduleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = this.moduleSlots[i];
                if (slot != null && slot.SlotID == slotID)
                {
                    return slot;
                }
            }

            return null;
        }

        public int GetModuleCount()
        {
            if (this.moduleSlots == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < this.moduleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = this.moduleSlots[i];
                if (slot != null && !string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                {
                    count++;
                }
            }

            return count;
        }

        public bool CanInstallMoreModules()
        {
            int slotCount = this.moduleSlots != null ? this.moduleSlots.Count : 0;
            return this.GetModuleCount() < slotCount;
        }

        public void EnsureInitialized()
        {
            if (this.instanceConfig == null)
            {
                this.instanceConfig = new Dictionary<string, string>();
            }

            if (this.moduleSlots == null)
            {
                this.moduleSlots = new List<ShuttleModuleSlot>();
            }

            for (int i = 0; i < this.moduleSlots.Count; i++)
            {
                if (this.moduleSlots[i] != null)
                {
                    this.moduleSlots[i].EnsureInitialized(this.segmentInstanceID, i);
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.segmentInstanceID, "segmentInstanceID");

            if (Scribe.mode == LoadSaveMode.Saving && this.segmentDef != null)
            {
                this.segmentDefNameForSave = this.segmentDef.defName;
            }

            Scribe_Values.Look(ref this.segmentDefNameForSave, "segmentDefName", null);

            string savedSegmentDefName = this.segmentDef != null
                ? this.segmentDef.defName
                : this.segmentDefNameForSave;
            Scribe_Values.Look(ref savedSegmentDefName, "segmentDef", null);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.segmentDef = !string.IsNullOrEmpty(savedSegmentDefName)
                    ? DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(savedSegmentDefName)
                    : null;

                if (!string.IsNullOrEmpty(savedSegmentDefName))
                {
                    this.segmentDefNameForSave = savedSegmentDefName;
                }
            }

            Scribe_Values.Look(ref this.segmentIndex, "segmentIndex", 0);
            Scribe_Values.Look(ref this.isFixed, "isFixed", false);

            Scribe_Collections.Look(
                ref this.instanceConfig,
                "instanceConfig",
                LookMode.Value,
                LookMode.Value);

            Scribe_Collections.Look(
                ref this.moduleSlots,
                "moduleSlots",
                LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.segmentDef != null)
                {
                    this.segmentDefNameForSave = this.segmentDef.defName;
                }

                this.EnsureInitialized();
            }
        }

        public override string ToString()
        {
            string defName = !string.IsNullOrEmpty(this.segmentDefName) ? this.segmentDefName : "null";
            int moduleCount = this.GetModuleCount();
            return this.segmentInstanceID + " (" + defName + ") @ index "
                + this.segmentIndex + ", modules=" + moduleCount;
        }
    }
}  // CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
