using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// ShuttleAssemblyState is the single source of truth for shuttle assembly topology.
    /// It only answers what is installed, where it is installed, which slots exist, and which
    /// stable instance IDs are assigned. It must not own runtime cargo, launch flow, world
    /// travel, UI behavior, or ticking logic.
    /// </summary>
    public sealed class ShuttleAssemblyState : IExposable
    {
        private const int CurrentSaveVersion = 3;
        private static readonly ShuttleAssemblyIntegrityValidator IntegrityValidator =
            new ShuttleAssemblyIntegrityValidator();

        private int saveVersion = CurrentSaveVersion;

        /// <summary>
        /// Concrete shuttle-level segment slots.
        /// Each slot identifies one install position on the shuttle body.
        /// </summary>
        private List<ShuttleSegmentSlot> segmentSlots = new List<ShuttleSegmentSlot>();

        /// <summary>
        /// Segment instances currently mounted on the shuttle.
        /// </summary>
        private List<ShuttleSegment> segments = new List<ShuttleSegment>();

        /// <summary>
        /// Module instances currently mounted into segment-owned module slots.
        /// </summary>
        private List<ShuttleModule> modules = new List<ShuttleModule>();

        /// <summary>
        /// Durable player configuration for cargo region filters.
        /// The actual loaded things are not stored here.
        /// </summary>
        private ShuttleCargoRegionConfigState cargoRegionConfig = new ShuttleCargoRegionConfigState();

        /// <summary>
        /// Durable player configuration for refrigerated cargo automatic transfer.
        /// The cold holders and loaded cargo items are not stored here.
        /// </summary>
        private ShuttleRefrigeratedCargoConfigState refrigeratedCargoConfig =
            new ShuttleRefrigeratedCargoConfigState();

        /// <summary>
        /// Durable player policy for supplying held prisoners from this shuttle's cargo.
        /// Loaded food and automatic-feeding cooldowns are not stored here.
        /// </summary>
        private ShuttlePrisonCellSupplyConfigState prisonCellSupplyConfig =
            new ShuttlePrisonCellSupplyConfigState();

        /// <summary>
        /// Durable player configuration for this shuttle's visual paint scheme.
        /// Map-body rendering reads this through a visual bridge; the assembly state only owns
        /// the saved appearance choice.
        /// </summary>
        private ShuttlePaintScheme paintScheme = new ShuttlePaintScheme();

        /// <summary>
        /// One-shot construction provenance for the starter package. This is captured when
        /// the host is first created so later save-level mode changes cannot retrofit it.
        /// </summary>
        private bool starterPresetDecisionCaptured;
        private bool starterPresetInstallationPending;

        /// <summary>
        /// Monotonic ID allocator for installed segment instances.
        /// </summary>
        private int nextSegmentInstanceID = 1;

        /// <summary>
        /// Monotonic ID allocator for installed module instances.
        /// </summary>
        private int nextModuleInstanceID = 1;

        private bool segmentInstanceAllocatorExhausted;
        private bool moduleInstanceAllocatorExhausted;

        /// <summary>
        /// Assembly-layer dirty flags. These describe which downstream layers need refresh after
        /// assembly topology or static configuration changes.
        /// </summary>
        private ShuttleDirtyFlags dirtyFlags = ShuttleDirtyFlags.Profile;

        private int moduleDispatchRevision;

        /// <summary>
        /// Map the slot with name, it provides a fast search method.
        /// </summary>
        private Dictionary<string, ShuttleSegmentSlot> segmentSlotsByID;
        private Dictionary<string, ShuttleSegment> segmentsByID;
        private Dictionary<string, ShuttleModule> modulesByID;
        private Dictionary<string, ShuttleModuleSlot> moduleSlotsByKey;

        // Non-persistent cache lifecycle flags. Save data stores only assembly truth; these are
        // rebuilt after load or after topology mutations.
        private bool initialized;
        private bool indexesBuilt;
        private bool indexesDirty = true;
        private ShuttleAssemblyIntegrityReport lastIntegrityReport =
            ShuttleAssemblyIntegrityReport.Empty;

        public IReadOnlyList<ShuttleSegmentSlot> SegmentSlots
        {
            get
            {
                return this.segmentSlots;
            }
        }

        public IReadOnlyList<ShuttleSegment> Segments
        {
            get
            {
                return this.segments;
            }
        }

        public IReadOnlyList<ShuttleModule> Modules
        {
            get
            {
                return this.modules;
            }
        }

        internal int ModuleDispatchRevision
        {
            get
            {
                return this.moduleDispatchRevision;
            }
        }

        internal ShuttleCargoRegionConfigState CargoRegionConfig
        {
            get
            {
                this.EnsureInitialized();
                return this.cargoRegionConfig;
            }
        }

        internal ShuttleRefrigeratedCargoConfigState RefrigeratedCargoConfig
        {
            get
            {
                this.EnsureInitialized();
                return this.refrigeratedCargoConfig;
            }
        }

        internal ShuttlePrisonCellSupplyConfigState PrisonCellSupplyConfig
        {
            get
            {
                this.EnsureInitialized();
                return this.prisonCellSupplyConfig;
            }
        }

        internal ShuttlePaintScheme PaintScheme
        {
            get
            {
                this.EnsureInitialized();
                return this.paintScheme;
            }
        }

        internal bool StarterPresetInstallationPending
        {
            get
            {
                return this.starterPresetDecisionCaptured &&
                    this.starterPresetInstallationPending;
            }
        }

        internal void CaptureStarterPresetDecisionAtCreation(bool installBasicPackage)
        {
            if (this.starterPresetDecisionCaptured)
            {
                return;
            }

            this.starterPresetDecisionCaptured = true;
            this.starterPresetInstallationPending = installBasicPackage;
        }

        internal void ScheduleStarterPresetForInitialScenarioSelection()
        {
            this.starterPresetDecisionCaptured = true;
            this.starterPresetInstallationPending = true;
        }

        internal void CompleteStarterPresetInstallation()
        {
            this.starterPresetDecisionCaptured = true;
            this.starterPresetInstallationPending = false;
        }

        public ShuttleDirtyFlags DirtyFlags
        {
            get
            {
                return this.dirtyFlags;
            }
        }

        public IEnumerable<ShuttleModuleSlot> ModuleSlots
        {
            get
            {
                // Flatten segment-owned module slots into a single read-only enumeration.
                // This is a projection over assembly truth, not a second source of truth.
                if (this.segments == null)
                {
                    yield break;
                }

                for (int i = 0; i < this.segments.Count; i++)
                {
                    ShuttleSegment segment = this.segments[i];
                    if (segment == null || segment.ModuleSlots == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < segment.ModuleSlots.Count; j++)
                    {
                        ShuttleModuleSlot slot = segment.ModuleSlots[j];
                        if (slot != null)
                        {
                            yield return slot;
                        }
                    }
                }
            }
        }

        public bool IsProfileDirty
        {
            get
            {
                return (this.dirtyFlags & ShuttleDirtyFlags.Profile) != 0;
            }
        }

        public bool HasBlockingIntegrityIssues
        {
            get
            {
                this.EnsureIntegrityReportFresh();
                return this.lastIntegrityReport != null &&
                    this.lastIntegrityReport.HasBlockingIssues;
            }
        }

        public ShuttleAssemblyIntegrityReport LastIntegrityReport
        {
            get
            {
                this.EnsureIntegrityReportFresh();
                return this.lastIntegrityReport ?? ShuttleAssemblyIntegrityReport.Empty;
            }
        }

        public bool TryGetBlockingIntegrityFailureReason(out string reason)
        {
            reason = null;
            this.EnsureIntegrityReportFresh();
            if (this.lastIntegrityReport == null ||
                !this.lastIntegrityReport.HasBlockingIssues)
            {
                return false;
            }

            reason = "CT_Shuttle_Command_AssemblyTopologyCorrupted".Translate().ToString();
            return true;
        }

        public bool CanAllocateSegmentInstanceID
        {
            get
            {
                this.EnsureInitialized();
                return ShuttleAssemblyInstanceIDAllocator.CanAllocate(
                    this.nextSegmentInstanceID,
                    this.segmentInstanceAllocatorExhausted);
            }
        }

        public bool CanAllocateModuleInstanceID
        {
            get
            {
                this.EnsureInitialized();
                return ShuttleAssemblyInstanceIDAllocator.CanAllocate(
                    this.nextModuleInstanceID,
                    this.moduleInstanceAllocatorExhausted);
            }
        }

        /// <summary>
        /// It restores structural containers before use.
        /// </summary>
        public void EnsureInitialized()
        {
            // This method only repairs structural containers and lookup readiness.
            // It must not create or rewrite durable topology IDs.
            if (this.initialized)
            {
                return;
            }

            bool moduleDispatchChanged = false;

            if (this.segmentSlots == null)
            {
                this.segmentSlots = new List<ShuttleSegmentSlot>();
            }

            if (this.segments == null)
            {
                this.segments = new List<ShuttleSegment>();
            }

            if (this.modules == null)
            {
                this.modules = new List<ShuttleModule>();
                moduleDispatchChanged = true;
            }

            if (this.cargoRegionConfig == null)
            {
                this.cargoRegionConfig = new ShuttleCargoRegionConfigState();
            }

            if (this.refrigeratedCargoConfig == null)
            {
                this.refrigeratedCargoConfig = new ShuttleRefrigeratedCargoConfigState();
            }

            if (this.prisonCellSupplyConfig == null)
            {
                this.prisonCellSupplyConfig = new ShuttlePrisonCellSupplyConfigState();
            }

            if (this.paintScheme == null)
            {
                this.paintScheme = new ShuttlePaintScheme();
            }

            this.refrigeratedCargoConfig.EnsureInitialized();
            this.prisonCellSupplyConfig.EnsureInitialized();
            this.paintScheme.EnsureInitialized();

            for (int i = 0; i < this.segmentSlots.Count; i++)
            {
                if (this.segmentSlots[i] != null)
                {
                    this.segmentSlots[i].EnsureInitialized(i);
                }
            }

            for (int i = 0; i < this.segments.Count; i++)
            {
                ShuttleSegment segment = this.segments[i];
                if (segment == null)
                {
                    continue;
                }

                segment.EnsureInitialized();
            }

            for (int i = 0; i < this.modules.Count; i++)
            {
                ShuttleModule module = this.modules[i];
                if (module == null)
                {
                    continue;
                }

                module.EnsureInitialized();
            }

            this.NormalizeInstanceIDAllocators();
            this.initialized = true;
            this.MarkIndexesDirty();
            this.RefreshIntegrityReport();
            if (moduleDispatchChanged)
            {
                this.NotifyModuleDispatchTopologyChanged();
            }
        }

        public void RebuildIndexes()
        {
            this.EnsureInitialized();

            // Rebuild fast lookup maps from persisted assembly truth.
            // These dictionaries are non-persistent caches and may always be regenerated.
            this.segmentSlotsByID = new Dictionary<string, ShuttleSegmentSlot>();
            this.segmentsByID = new Dictionary<string, ShuttleSegment>();
            this.modulesByID = new Dictionary<string, ShuttleModule>();
            this.moduleSlotsByKey = new Dictionary<string, ShuttleModuleSlot>();

            for (int i = 0; i < this.segmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = this.segmentSlots[i];
                if (slot == null || string.IsNullOrEmpty(slot.SlotID))
                {
                    continue;
                }

                if (this.segmentSlotsByID.ContainsKey(slot.SlotID))
                {
                    this.WarnDuplicateIndexKeyOnce("segment slot id", slot.SlotID);
                    continue;
                }

                this.segmentSlotsByID.Add(slot.SlotID, slot);
            }

            for (int i = 0; i < this.segments.Count; i++)
            {
                ShuttleSegment segment = this.segments[i];
                if (segment == null || string.IsNullOrEmpty(segment.SegmentInstanceID))
                {
                    continue;
                }

                if (this.segmentsByID.ContainsKey(segment.SegmentInstanceID))
                {
                    this.WarnDuplicateIndexKeyOnce("segment instance id", segment.SegmentInstanceID);
                    continue;
                }

                this.segmentsByID.Add(segment.SegmentInstanceID, segment);

                if (segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleModuleSlot slot = segment.ModuleSlots[j];
                    if (slot == null || string.IsNullOrEmpty(slot.SlotID))
                    {
                        continue;
                    }

                    string moduleSlotKey = this.GetModuleSlotKey(segment.SegmentInstanceID, slot.SlotID);
                    if (this.moduleSlotsByKey.ContainsKey(moduleSlotKey))
                    {
                        this.WarnDuplicateIndexKeyOnce("module slot key", moduleSlotKey);
                        continue;
                    }

                    this.moduleSlotsByKey.Add(moduleSlotKey, slot);
                }
            }

            for (int i = 0; i < this.modules.Count; i++)
            {
                ShuttleModule module = this.modules[i];
                if (module == null || string.IsNullOrEmpty(module.ModuleInstanceID))
                {
                    continue;
                }

                if (this.modulesByID.ContainsKey(module.ModuleInstanceID))
                {
                    this.WarnDuplicateIndexKeyOnce("module instance id", module.ModuleInstanceID);
                    continue;
                }

                this.modulesByID.Add(module.ModuleInstanceID, module);
            }

            this.indexesBuilt = true;
            this.indexesDirty = false;
            this.RefreshIntegrityReport();
        }

        public ShuttleSegmentSlot GetSegmentSlot(string slotID)
        {
            this.EnsureIndexesReady();

            ShuttleSegmentSlot slot;
            if (string.IsNullOrEmpty(slotID) || !this.segmentSlotsByID.TryGetValue(slotID, out slot))
            {
                return null;
            }

            return slot;
        }

        public ShuttleSegment GetSegment(string segmentInstanceID)
        {
            this.EnsureIndexesReady();

            ShuttleSegment segment;
            if (string.IsNullOrEmpty(segmentInstanceID) || !this.segmentsByID.TryGetValue(segmentInstanceID, out segment))
            {
                return null;
            }

            return segment;
        }

        public ShuttleModuleSlot GetModuleSlot(string segmentInstanceID, string slotID)
        {
            this.EnsureIndexesReady();

            ShuttleModuleSlot slot;
            string key = this.GetModuleSlotKey(segmentInstanceID, slotID);
            if (string.IsNullOrEmpty(segmentInstanceID) ||
                string.IsNullOrEmpty(slotID) ||
                !this.moduleSlotsByKey.TryGetValue(key, out slot))
            {
                return null;
            }

            return slot;
        }

        public ShuttleModule GetModule(string moduleInstanceID)
        {
            this.EnsureIndexesReady();

            ShuttleModule module;
            if (string.IsNullOrEmpty(moduleInstanceID) || !this.modulesByID.TryGetValue(moduleInstanceID, out module))
            {
                return null;
            }

            return module;
        }

        internal bool AddSegmentSlot(ShuttleSegmentSlot slot)
        {
            if (slot == null)
            {
                return false;
            }

            this.EnsureInitialized();
            this.segmentSlots.Add(slot);
            this.MarkIndexesDirty();
            return true;
        }

        internal bool AddSegment(ShuttleSegment segment)
        {
            if (segment == null)
            {
                return false;
            }

            this.EnsureInitialized();
            this.segments.Add(segment);
            this.MarkIndexesDirty();
            return true;
        }

        internal bool RemoveSegment(ShuttleSegment segment)
        {
            if (segment == null)
            {
                return false;
            }

            this.EnsureInitialized();
            bool removed = this.segments.Remove(segment);
            if (removed)
            {
                this.MarkIndexesDirty();
            }

            return removed;
        }

        internal bool AddModule(ShuttleModule module)
        {
            if (module == null)
            {
                return false;
            }

            this.EnsureInitialized();
            this.modules.Add(module);
            this.MarkIndexesDirty();
            this.NotifyModuleDispatchTopologyChanged();
            return true;
        }

        internal bool RemoveModule(ShuttleModule module)
        {
            if (module == null)
            {
                return false;
            }

            this.EnsureInitialized();
            bool removed = this.modules.Remove(module);
            if (removed)
            {
                this.MarkIndexesDirty();
                this.NotifyModuleDispatchTopologyChanged();
            }

            return removed;
        }

        internal void NotifyModuleDispatchTopologyChanged()
        {
            unchecked
            {
                this.moduleDispatchRevision++;
            }
        }

        public bool TryAllocateNextSegmentInstanceID(out string allocated)
        {
            allocated = null;
            this.EnsureInitialized();
            if (this.HasBlockingIntegrityIssues)
            {
                return false;
            }

            bool success = ShuttleAssemblyInstanceIDAllocator.TryAllocateSegment(
                this.segments,
                ref this.nextSegmentInstanceID,
                ref this.segmentInstanceAllocatorExhausted,
                out allocated);
            this.WarnSegmentAllocatorIfExhausted();
            return success;
        }

        public bool TryAllocateNextModuleInstanceID(out string allocated)
        {
            allocated = null;
            this.EnsureInitialized();
            if (this.HasBlockingIntegrityIssues)
            {
                return false;
            }

            bool success = ShuttleAssemblyInstanceIDAllocator.TryAllocateModule(
                this.modules,
                ref this.nextModuleInstanceID,
                ref this.moduleInstanceAllocatorExhausted,
                out allocated);
            this.WarnModuleAllocatorIfExhausted();
            return success;
        }

        public string AllocateNextSegmentInstanceID()
        {
            string allocated;
            return this.TryAllocateNextSegmentInstanceID(out allocated) ? allocated : null;
        }

        public string AllocateNextModuleInstanceID()
        {
            string allocated;
            return this.TryAllocateNextModuleInstanceID(out allocated) ? allocated : null;
        }

        public void MarkDirty(ShuttleDirtyFlags flags)
        {
            // Assembly state never performs downstream refresh work itself.
            // It only records which later layers must be refreshed.
            this.dirtyFlags |= flags;
        }

        public void ClearDirty(ShuttleDirtyFlags flags)
        {
            this.dirtyFlags &= ~flags;
        }

        public bool HasDirty(ShuttleDirtyFlags flags)
        {
            return (this.dirtyFlags & flags) != 0;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Collections.Look(ref this.segmentSlots, "segmentSlots", LookMode.Deep);
            Scribe_Collections.Look(ref this.segments, "segments", LookMode.Deep);
            Scribe_Collections.Look(ref this.modules, "modules", LookMode.Deep);
            Scribe_Deep.Look(ref this.cargoRegionConfig, "cargoRegionConfig");
            Scribe_Deep.Look(ref this.refrigeratedCargoConfig, "refrigeratedCargoConfig");
            Scribe_Deep.Look(ref this.prisonCellSupplyConfig, "prisonCellSupplyConfig");
            Scribe_Deep.Look(ref this.paintScheme, "paintScheme");
            Scribe_Values.Look(
                ref this.starterPresetDecisionCaptured,
                "starterPresetDecisionCaptured",
                false);
            Scribe_Values.Look(
                ref this.starterPresetInstallationPending,
                "starterPresetInstallationPending",
                false);
            Scribe_Values.Look(ref this.nextSegmentInstanceID, "nextSegmentInstanceID", 1);
            Scribe_Values.Look(ref this.nextModuleInstanceID, "nextModuleInstanceID", 1);
            Scribe_Values.Look(ref this.dirtyFlags, "dirtyFlags", ShuttleDirtyFlags.Profile);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                // Post-load work only restores structural integrity and indices. Higher layers are
                // still responsible for def validation and profile rebuild.
                this.initialized = false;
                this.EnsureInitialized();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
                this.MarkIndexesDirty();
                this.MarkDirty(ShuttleDirtyFlags.Profile);
                this.RefreshIntegrityReport();
            }
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ShuttleAssemblyState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded assembly topology and def names intact.");
            }

            if (loadedVersion < 3)
            {
                // Existing shuttles predate per-host construction provenance. Treat their
                // current topology as final so later mode changes cannot retrofit an old
                // empty airframe when it respawns after flight.
                this.starterPresetDecisionCaptured = true;
                this.starterPresetInstallationPending = false;
            }

            // Version 0 predates persisted schema metadata. It intentionally receives no
            // destructive migration: keep slot, segment, module, cargo-region, and defName data
            // intact so profile validation can report missing defs without data loss.
            if (loadedVersion < 1)
            {
                return;
            }

            // Version 2 adds only an optional static policy object. Missing data uses the
            // compatibility defaults established by EnsureInitialized; cargo holders are not
            // migrated or rewritten.
            if (loadedVersion < 2)
            {
                return;
            }

            // Version 3 adds only the one-shot starter-preset construction provenance above.
        }

        private void EnsureIndexesReady()
        {
            this.EnsureInitialized();
            if (!this.indexesBuilt ||
                this.indexesDirty ||
                this.segmentSlotsByID == null ||
                this.segmentsByID == null ||
                this.modulesByID == null ||
                this.moduleSlotsByKey == null)
            {
                this.RebuildIndexes();
            }
        }

        private void MarkIndexesDirty()
        {
            this.indexesDirty = true;
        }

        private void EnsureIntegrityReportFresh()
        {
            this.EnsureInitialized();
            if (!this.indexesBuilt ||
                this.indexesDirty ||
                this.segmentSlotsByID == null ||
                this.segmentsByID == null ||
                this.modulesByID == null ||
                this.moduleSlotsByKey == null)
            {
                this.RebuildIndexes();
                return;
            }

            if (this.lastIntegrityReport == null)
            {
                this.RefreshIntegrityReport();
            }
        }

        private string GetModuleSlotKey(string segmentInstanceID, string slotID)
        {
            return segmentInstanceID + "::" + slotID;
        }

        private void WarnDuplicateIndexKeyOnce(string keyKind, string key)
        {
            Log.WarningOnce(
                "[CeleTech Shuttle] Duplicate " + keyKind +
                " '" + key + "' while rebuilding ShuttleAssemblyState indexes. " +
                "Keeping the first object and skipping later duplicates. Context: segmentSlots=" +
                (this.segmentSlots != null ? this.segmentSlots.Count.ToString() : "null") +
                ", segments=" +
                (this.segments != null ? this.segments.Count.ToString() : "null") +
                ", modules=" +
                (this.modules != null ? this.modules.Count.ToString() : "null") + ".",
                this.MakeDuplicateIndexWarningHash(keyKind, key));
        }

        private void RefreshIntegrityReport()
        {
            this.lastIntegrityReport = IntegrityValidator.Validate(this) ??
                ShuttleAssemblyIntegrityReport.Empty;
            if (this.lastIntegrityReport.HasBlockingIssues)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] Blocking ShuttleAssemblyState integrity issue detected. " +
                    this.lastIntegrityReport.BuildDeveloperSummary(8),
                    this.lastIntegrityReport.BuildStableHash());
            }
        }

        private int MakeDuplicateIndexWarningHash(string keyKind, string key)
        {
            unchecked
            {
                int hash = 29;
                hash = (hash * 31) + (keyKind != null ? keyKind.GetHashCode() : 0);
                hash = (hash * 31) + (key != null ? key.GetHashCode() : 0);
                return hash;
            }
        }

        private void NormalizeInstanceIDAllocators()
        {
            this.segmentInstanceAllocatorExhausted =
                ShuttleAssemblyInstanceIDAllocator.NormalizeSegmentAllocator(
                    this.segments,
                    ref this.nextSegmentInstanceID);
            this.moduleInstanceAllocatorExhausted =
                ShuttleAssemblyInstanceIDAllocator.NormalizeModuleAllocator(
                    this.modules,
                    ref this.nextModuleInstanceID);
            this.WarnSegmentAllocatorIfExhausted();
            this.WarnModuleAllocatorIfExhausted();
        }

        private void WarnSegmentAllocatorIfExhausted()
        {
            if (this.segmentInstanceAllocatorExhausted)
            {
                this.WarnAllocatorExhaustedOnce("segment", this.nextSegmentInstanceID);
            }
        }

        private void WarnModuleAllocatorIfExhausted()
        {
            if (this.moduleInstanceAllocatorExhausted)
            {
                this.WarnAllocatorExhaustedOnce("module", this.nextModuleInstanceID);
            }
        }

        private void WarnAllocatorExhaustedOnce(string allocatorKind, int nextID)
        {
            Log.WarningOnce(
                "[CeleTech Shuttle] ShuttleAssemblyState " + allocatorKind +
                " instance ID allocator is exhausted or too close to int.MaxValue. " +
                "Future " + allocatorKind + " installs will be rejected. nextID=" + nextID + ".",
                this.MakeAllocatorWarningHash(allocatorKind));
        }

        private int MakeAllocatorWarningHash(string allocatorKind)
        {
            unchecked
            {
                int hash = 217;
                hash = (hash * 31) + (allocatorKind != null ? allocatorKind.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
