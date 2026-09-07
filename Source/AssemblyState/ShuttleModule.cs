using System.Collections.Generic;
using Verse;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// One shuttle module instance installed into the current assembly topology.
    /// It stores install-time identity, the referenced module def, the parent segment/slot,
    /// and instance-level configuration overrides. Runtime live values should not live here.
    /// </summary>
    public sealed class ShuttleModule : IExposable
    {
        internal const string HullStuffDefNameConfigKey = "hullStuffDefName";

        // Stable instance ID used to distinguish multiple installed copies of the same module type.
        private string moduleInstanceID;

        // Static module definition referenced by this module instance.
        private ShuttleModuleBaseDef moduleDef;

        // Persisted fallback for missing/renamed defs. Keep this even if moduleDef fails to load.
        private string moduleDefNameForSave;

        // Stable parent segment instance ID that owns this installation.
        private string parentSegmentInstanceID;

        // Concrete slot ID within the parent segment.
        private string parentSlotID;

        // Install-level enable flag only.
        private bool isEnabled = true;

        // Per-instance installation configuration overrides reserved for future module-specific
        // static settings. Runtime live values must go into RuntimeState, not this dictionary.
        private Dictionary<string, string> instanceConfig = new Dictionary<string, string>();

        public ShuttleModule()
        {
        }

        public ShuttleModule(
            string moduleInstanceID,
            ShuttleModuleBaseDef moduleDef,
            string parentSegmentInstanceID,
            string parentSlotID)
        {
            this.moduleInstanceID = moduleInstanceID;
            this.moduleDef = moduleDef;
            this.moduleDefNameForSave = moduleDef != null ? moduleDef.defName : null;
            this.parentSegmentInstanceID = parentSegmentInstanceID;
            this.parentSlotID = parentSlotID;
        }

        public string ModuleInstanceID
        {
            get
            {
                return this.moduleInstanceID;
            }
        }

        public ShuttleModuleBaseDef ModuleDef
        {
            get
            {
                return this.moduleDef;
            }
        }

        public string ParentSegmentInstanceID
        {
            get
            {
                return this.parentSegmentInstanceID;
            }
        }

        public string ParentSlotID
        {
            get
            {
                return this.parentSlotID;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return this.isEnabled;
            }
        }

        public string moduleDefName
        {
            get
            {
                return this.moduleDef != null ? this.moduleDef.defName : this.moduleDefNameForSave;
            }
        }

        public string SelectedStuffDefName
        {
            get
            {
                string value;
                return this.TryGetInstanceConfig(HullStuffDefNameConfigKey, out value)
                    ? value
                    : null;
            }
        }

        internal void AssignModuleInstanceID(string value)
        {
            this.moduleInstanceID = value;
        }

        internal void SetEnabled(bool value)
        {
            this.isEnabled = value;
        }

        internal void SetSelectedStuffDefName(string stuffDefName)
        {
            string sanitized = string.IsNullOrWhiteSpace(stuffDefName)
                ? null
                : stuffDefName.Trim();
            this.SetInstanceConfig(HullStuffDefNameConfigKey, sanitized);
        }

        internal Dictionary<string, string> CopyInstanceConfig()
        {
            this.EnsureInitialized();
            return new Dictionary<string, string>(this.instanceConfig);
        }

        public TModuleDef GetModuleDef<TModuleDef>() where TModuleDef : ShuttleModuleBaseDef
        {
            return this.moduleDef as TModuleDef;
        }

        public bool IsInstalledIn(string segmentInstanceID, string slotID)
        {
            return this.parentSegmentInstanceID == segmentInstanceID
                && this.parentSlotID == slotID;
        }

        public void EnsureInitialized()
        {
            if (this.instanceConfig == null)
            {
                this.instanceConfig = new Dictionary<string, string>();
            }
        }

        public bool TryGetInstanceConfig(string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            this.EnsureInitialized();
            return this.instanceConfig.TryGetValue(key, out value);
        }

        public void SetInstanceConfig(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            this.EnsureInitialized();
            if (value == null)
            {
                this.instanceConfig.Remove(key);
                return;
            }

            this.instanceConfig[key] = value;
        }

        public void RemoveInstanceConfig(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            this.EnsureInitialized();
            this.instanceConfig.Remove(key);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");

            if (Scribe.mode == LoadSaveMode.Saving && this.moduleDef != null)
            {
                this.moduleDefNameForSave = this.moduleDef.defName;
            }

            Scribe_Values.Look(ref this.moduleDefNameForSave, "moduleDefName", null);

            string savedModuleDefName = this.moduleDef != null
                ? this.moduleDef.defName
                : this.moduleDefNameForSave;
            Scribe_Values.Look(ref savedModuleDefName, "moduleDef", null);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.moduleDef = !string.IsNullOrEmpty(savedModuleDefName)
                    ? DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(savedModuleDefName)
                    : null;

                if (!string.IsNullOrEmpty(savedModuleDefName))
                {
                    this.moduleDefNameForSave = savedModuleDefName;
                }
            }

            Scribe_Values.Look(ref this.parentSegmentInstanceID, "parentSegmentInstanceID");
            Scribe_Values.Look(ref this.parentSlotID, "parentSlotID");
            Scribe_Values.Look(ref this.isEnabled, "isEnabled", true);
            Scribe_Collections.Look(
                ref this.instanceConfig,
                "instanceConfig",
                LookMode.Value,
                LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.moduleDef != null)
                {
                    this.moduleDefNameForSave = this.moduleDef.defName;
                }

                this.EnsureInitialized();
            }
        }

        public override string ToString()
        {
            string defName = !string.IsNullOrEmpty(this.moduleDefName) ? this.moduleDefName : "null";
            return this.moduleInstanceID + " (" + defName + ") @ "
                + this.parentSegmentInstanceID + "/" + this.parentSlotID;
        }
    }
}  // CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
