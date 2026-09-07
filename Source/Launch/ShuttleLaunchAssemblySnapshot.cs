using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    /// <summary>
    /// Detached launch-time copy of assembly identity data. It is rebuilt on demand and is
    /// never persisted; live AssemblyState remains the source of truth for this phase.
    /// </summary>
    public sealed class ShuttleLaunchAssemblySnapshot
    {
        private readonly List<ShuttleLaunchSegmentRecord> segments;
        private readonly List<ShuttleLaunchModuleRecord> modules;

        public ShuttleLaunchAssemblySnapshot(
            IReadOnlyList<ShuttleLaunchSegmentRecord> segments,
            IReadOnlyList<ShuttleLaunchModuleRecord> modules)
        {
            this.segments = CopySegments(segments);
            this.modules = CopyModules(modules);
        }

        public IReadOnlyList<ShuttleLaunchSegmentRecord> Segments
        {
            get
            {
                return this.segments;
            }
        }

        public IReadOnlyList<ShuttleLaunchModuleRecord> Modules
        {
            get
            {
                return this.modules;
            }
        }

        public int SegmentCount
        {
            get
            {
                return this.segments.Count;
            }
        }

        public int ModuleCount
        {
            get
            {
                return this.modules.Count;
            }
        }

        public ShuttleLaunchSegmentRecord GetSegment(string segmentInstanceID)
        {
            if (string.IsNullOrEmpty(segmentInstanceID))
            {
                return null;
            }

            for (int i = 0; i < this.segments.Count; i++)
            {
                ShuttleLaunchSegmentRecord segment = this.segments[i];
                if (segment != null && segment.SegmentInstanceID == segmentInstanceID)
                {
                    return segment;
                }
            }

            return null;
        }

        internal static ShuttleLaunchAssemblySnapshot FromAssemblyState(ShuttleAssemblyState assemblyState)
        {
            if (assemblyState == null)
            {
                return null;
            }

            List<ShuttleLaunchSegmentRecord> segments = new List<ShuttleLaunchSegmentRecord>();
            List<ShuttleLaunchModuleRecord> modules = new List<ShuttleLaunchModuleRecord>();
            assemblyState.EnsureInitialized();

            for (int i = 0; i < assemblyState.Segments.Count; i++)
            {
                ShuttleSegment segment = assemblyState.Segments[i];
                if (segment == null)
                {
                    continue;
                }

                int moduleSlotCount = segment.ModuleSlots != null ? segment.ModuleSlots.Count : 0;
                segments.Add(new ShuttleLaunchSegmentRecord(
                    segment.SegmentInstanceID,
                    segment.segmentDefName,
                    segment.SegmentDef,
                    segment.SegmentIndex,
                    segment.IsFixed,
                    moduleSlotCount));
            }

            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                if (module == null)
                {
                    continue;
                }

                ShuttleSegment parentSegment = !string.IsNullOrEmpty(module.ParentSegmentInstanceID)
                    ? assemblyState.GetSegment(module.ParentSegmentInstanceID)
                    : null;
                ShuttleModuleSlot parentSlot = parentSegment != null && !string.IsNullOrEmpty(module.ParentSlotID)
                    ? parentSegment.GetModuleSlotByID(module.ParentSlotID)
                    : null;

                modules.Add(new ShuttleLaunchModuleRecord(
                    module.ModuleInstanceID,
                    module.moduleDefName,
                    module.ModuleDef,
                    module.ParentSegmentInstanceID,
                    parentSegment != null ? parentSegment.segmentDefName : null,
                    parentSegment != null ? parentSegment.SegmentDef : null,
                    parentSegment != null ? parentSegment.SegmentIndex : -1,
                    module.ParentSlotID,
                    parentSlot != null ? parentSlot.SlotTypeID : null,
                    parentSlot != null ? parentSlot.SlotIndex : -1,
                    parentSlot != null && parentSlot.IsRequired,
                    parentSlot != null && parentSlot.IsLocked,
                    true,
                    module.IsEnabled));
            }

            return new ShuttleLaunchAssemblySnapshot(segments, modules);
        }

        private static List<ShuttleLaunchSegmentRecord> CopySegments(
            IReadOnlyList<ShuttleLaunchSegmentRecord> source)
        {
            List<ShuttleLaunchSegmentRecord> copy = new List<ShuttleLaunchSegmentRecord>();
            if (source == null)
            {
                return copy;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    copy.Add(source[i]);
                }
            }

            return copy;
        }

        private static List<ShuttleLaunchModuleRecord> CopyModules(
            IReadOnlyList<ShuttleLaunchModuleRecord> source)
        {
            List<ShuttleLaunchModuleRecord> copy = new List<ShuttleLaunchModuleRecord>();
            if (source == null)
            {
                return copy;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    copy.Add(source[i]);
                }
            }

            return copy;
        }
    }

    public sealed class ShuttleLaunchSegmentRecord
    {
        public ShuttleLaunchSegmentRecord(
            string segmentInstanceID,
            string segmentDefName,
            ShuttleSegmentBaseDef segmentDef,
            int segmentIndex,
            bool isFixed,
            int moduleSlotCount)
        {
            this.SegmentInstanceID = segmentInstanceID;
            this.SegmentDefName = segmentDefName;
            this.SegmentDef = segmentDef;
            this.SegmentIndex = segmentIndex;
            this.IsFixed = isFixed;
            this.ModuleSlotCount = moduleSlotCount;
        }

        public string SegmentInstanceID { get; private set; }
        public string SegmentDefName { get; private set; }
        public ShuttleSegmentBaseDef SegmentDef { get; private set; }
        public int SegmentIndex { get; private set; }
        public bool IsFixed { get; private set; }
        public int ModuleSlotCount { get; private set; }
    }

    public sealed class ShuttleLaunchModuleRecord
    {
        public ShuttleLaunchModuleRecord(
            string moduleInstanceID,
            string moduleDefName,
            ShuttleModuleBaseDef moduleDef,
            string parentSegmentInstanceID,
            string parentSegmentDefName,
            ShuttleSegmentBaseDef parentSegmentDef,
            int parentSegmentIndex,
            string parentSlotID,
            string parentSlotTypeID,
            int parentSlotIndex,
            bool parentSlotRequired,
            bool parentSlotLocked,
            bool isInstalled,
            bool isEnabled)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleDefName = moduleDefName;
            this.ModuleDef = moduleDef;
            this.ParentSegmentInstanceID = parentSegmentInstanceID;
            this.ParentSegmentDefName = parentSegmentDefName;
            this.ParentSegmentDef = parentSegmentDef;
            this.ParentSegmentIndex = parentSegmentIndex;
            this.ParentSlotID = parentSlotID;
            this.ParentSlotTypeID = parentSlotTypeID;
            this.ParentSlotIndex = parentSlotIndex;
            this.ParentSlotRequired = parentSlotRequired;
            this.ParentSlotLocked = parentSlotLocked;
            this.IsInstalled = isInstalled;
            this.IsEnabled = isEnabled;
        }

        public string ModuleInstanceID { get; private set; }
        public string ModuleDefName { get; private set; }
        public ShuttleModuleBaseDef ModuleDef { get; private set; }
        public string ParentSegmentInstanceID { get; private set; }
        public string ParentSegmentDefName { get; private set; }
        public ShuttleSegmentBaseDef ParentSegmentDef { get; private set; }
        public int ParentSegmentIndex { get; private set; }
        public string ParentSlotID { get; private set; }
        public string ParentSlotTypeID { get; private set; }
        public int ParentSlotIndex { get; private set; }
        public bool ParentSlotRequired { get; private set; }
        public bool ParentSlotLocked { get; private set; }
        public bool IsInstalled { get; private set; }
        public bool IsEnabled { get; private set; }
    }
}
