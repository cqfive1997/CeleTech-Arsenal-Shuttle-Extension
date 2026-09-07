using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Base definition for all shuttle segment types.
    /// A segment def describes static install-time facts only: segment category, allowed shuttle
    /// slot types, allowed module categories, and the module slot topology exposed once the
    /// segment is installed. Runtime values must not be stored here.
    /// </summary>
    public abstract class ShuttleSegmentBaseDef : Def
    {
        // Raw string persisted through XML authoring and used for display/debug text.
        // Unsupported input is normalized to unknown so def validation can warn explicitly.
        public string segmentTypeID;

        // Whether this segment should be treated as non-removable by default rules.
        public bool isNonRemovable;

        // Player-facing construction availability. Existing installed segments remain loadable
        // for save compatibility even if a def is hidden from new construction orders.
        public bool playerInstallable = true;

        // Every segment contributes its own structural mass once installed on the shuttle.
        // This is install-time static data, not a runtime load value.
        public float mass;

        // Segment-local cap for the sum of installed modules' moduleComplexityCost values.
        // -1 disables the gate for this segment. This is not a whole-ship budget and should
        // not be mirrored into ShuttleProfile or RuntimeState.
        public int maxModuleComplexity = -1;

        // Shuttle-level slot types this segment may occupy. Unsupported values normalize to optional.
        public List<string> installableSegmentSlotTypes = new List<string>();

        // Module category IDs allowed inside this segment.
        // This is a segment-level compatibility filter independent from concrete module slot types.
        public List<string> installableModuleTypes = new List<string>();

        // Concrete module slot topology exposed once this segment is installed.
        // These slots are static definitions and will be materialized into runtime assembly truth
        // only when a real segment instance is installed.
        public List<ShuttleModuleSlotDef> moduleSlots = new List<ShuttleModuleSlotDef>();

        // Research projects required before this segment can be newly installed. Existing
        // installed segments remain loadable for save compatibility.
        public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();

        // Construction-order costs. These are paid/staged before the segment is installed;
        // the eventual topology mutation still goes through the normal install command path.
        public List<ThingDefCountClass> constructionCostList = new List<ThingDefCountClass>();
        public int constructionWorkTicks;

        private ShuttleSegmentType segmentType = ShuttleSegmentType.Unknown;
        private List<ShuttleSegmentType> installableSegmentSlotTypeEnums = new List<ShuttleSegmentType>();
        private List<ShuttleModuleType> installableModuleTypeEnums = new List<ShuttleModuleType>();

        public ShuttleSegmentType SegmentType
        {
            get
            {
                if (this.segmentType == ShuttleSegmentType.Unknown &&
                    !string.IsNullOrWhiteSpace(this.segmentTypeID))
                {
                    this.EnsureTypeCachesInitialized();
                }

                return this.segmentType;
            }
        }

        public IReadOnlyList<ShuttleSegmentType> InstallableSegmentSlotTypeEnums
        {
            get
            {
                if (this.installableSegmentSlotTypeEnums == null ||
                    (this.installableSegmentSlotTypes != null &&
                     this.installableSegmentSlotTypeEnums.Count != this.installableSegmentSlotTypes.Count))
                {
                    this.EnsureTypeCachesInitialized();
                }

                return this.installableSegmentSlotTypeEnums;
            }
        }

        public IReadOnlyList<ShuttleModuleType> InstallableModuleTypeEnums
        {
            get
            {
                if (this.installableModuleTypeEnums == null ||
                    (this.installableModuleTypes != null &&
                     this.installableModuleTypeEnums.Count != this.installableModuleTypes.Count))
                {
                    this.installableModuleTypeEnums = this.BuildModuleTypeEnumList(this.installableModuleTypes);
                }

                return this.installableModuleTypeEnums;
            }
        }

        // Convenience count for callers that only need the total number of exposed module slots.
        public int moduleSlotCount
        {
            get
            {
                return this.moduleSlots != null ? this.moduleSlots.Count : 0;
            }
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            this.EnsureTypeCachesInitialized();
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
            {
                yield return error;
            }

            if (string.IsNullOrEmpty(this.segmentTypeID))
            {
                yield return this.defName + " must define segmentTypeID.";
            }

            this.EnsureTypeCachesInitialized();

            if (this.segmentType == ShuttleSegmentType.Unknown)
            {
                yield return this.defName + " defines unknown segmentTypeID " + this.segmentTypeID + ".";
            }
            for (int i = 0; i < this.installableSegmentSlotTypes.Count; i++)
            {
                ShuttleSegmentType slotType = this.installableSegmentSlotTypeEnums[i];
                if (slotType == ShuttleSegmentType.Unknown)
                {
                    yield return this.defName + " defines unknown installableSegmentSlotTypes entry " + this.installableSegmentSlotTypes[i] + ".";
                }
            }

            for (int i = 0; i < this.installableModuleTypes.Count; i++)
            {
                ShuttleModuleType moduleType = this.installableModuleTypeEnums[i];
                if (moduleType == ShuttleModuleType.Unknown)
                {
                    yield return this.defName + " defines unknown installableModuleTypes entry " + this.installableModuleTypes[i] + ".";
                }

                this.installableModuleTypes[i] = ShuttleModuleTypeCatalog.ToTypeID(moduleType);
            }

            if (this.moduleSlots == null)
            {
                this.moduleSlots = new List<ShuttleModuleSlotDef>();
            }

            if (!this.IsFiniteFloat(this.mass))
            {
                yield return this.defName + " has non-finite mass.";
            }
            else if (this.mass < 0f)
            {
                yield return this.defName + " has negative mass.";
            }

            if (this.maxModuleComplexity < -1)
            {
                yield return this.defName + " has maxModuleComplexity less than -1.";
            }
            else if (this.maxModuleComplexity > ShuttleModuleBaseDef.CorruptionGuardMaxModuleComplexityCost)
            {
                yield return this.defName + " maxModuleComplexity exceeds corruption guard max " +
                    ShuttleModuleBaseDef.CorruptionGuardMaxModuleComplexityCost + ".";
            }

            if (this.researchPrerequisites == null)
            {
                this.researchPrerequisites = new List<ResearchProjectDef>();
            }

            for (int i = 0; i < this.researchPrerequisites.Count; i++)
            {
                if (this.researchPrerequisites[i] == null)
                {
                    yield return this.defName + " contains a null researchPrerequisites entry at index " + i + ".";
                }
            }

            if (this.constructionCostList == null)
            {
                this.constructionCostList = new List<ThingDefCountClass>();
            }

            for (int i = 0; i < this.constructionCostList.Count; i++)
            {
                ThingDefCountClass cost = this.constructionCostList[i];
                if (cost == null)
                {
                    yield return this.defName + " contains a null constructionCostList entry at index " + i + ".";
                    continue;
                }

                if (cost.thingDef == null)
                {
                    yield return this.defName + " constructionCostList entry at index " + i + " has null thingDef.";
                }

                if (cost.count <= 0)
                {
                    yield return this.defName + " constructionCostList entry at index " + i + " has count <= 0.";
                }
            }

            if (this.constructionWorkTicks < 0)
            {
                yield return this.defName + " has negative constructionWorkTicks.";
            }

            if (this.playerInstallable && this.constructionWorkTicks <= 0)
            {
                Log.Warning("[CeleTech Shuttle] " + this.defName +
                    " is player-installable but has constructionWorkTicks <= 0. A fallback construction work value will be used.");
            }

            if (this.playerInstallable && this.constructionCostList.Count == 0)
            {
                Log.Warning("[CeleTech Shuttle] " + this.defName +
                    " is player-installable but has an empty constructionCostList. A fallback construction cost will be used.");
            }

            // Validate concrete module slot topology once in the def layer so the bootstrapper can
            // materialize slots directly from trusted static data. Runtime slot IDs are generated
            // later from the owning segment instance plus slot order, so defs do not own them.
            for (int i = 0; i < this.moduleSlots.Count; i++)
            {
                ShuttleModuleSlotDef slotDef = this.moduleSlots[i];
                if (slotDef == null)
                {
                    yield return this.defName + " contains a null module slot definition.";
                    continue;
                }

                slotDef.EnsureInitialized(i);

                if (string.IsNullOrEmpty(slotDef.slotTypeID))
                {
                    yield return this.defName + " module slot at index " + i + " must define slotTypeID.";
                }

                if (slotDef.SlotType == ShuttleModuleType.Unknown)
                {
                    yield return this.defName + " module slot at index " + i + " defines unknown slotTypeID.";
                }
            }
        }

        private List<ShuttleSegmentType> BuildAssemblyTypeEnumList(List<string> rawTypeIDs)
        {
            List<ShuttleSegmentType> parsedTypes = new List<ShuttleSegmentType>();
            if (rawTypeIDs == null)
            {
                return parsedTypes;
            }

            for (int i = 0; i < rawTypeIDs.Count; i++)
            {
                parsedTypes.Add(ShuttleSegmentTypeCatalog.ParseSlotType(rawTypeIDs[i]));
            }

            return parsedTypes;
        }

        private List<ShuttleModuleType> BuildModuleTypeEnumList(List<string> rawTypeIDs)
        {
            List<ShuttleModuleType> parsedTypes = new List<ShuttleModuleType>();
            if (rawTypeIDs == null)
            {
                return parsedTypes;
            }

            for (int i = 0; i < rawTypeIDs.Count; i++)
            {
                parsedTypes.Add(ShuttleModuleTypeCatalog.ParseModuleType(rawTypeIDs[i]));
            }

            return parsedTypes;
        }

        private void EnsureTypeCachesInitialized()
        {
            if (string.IsNullOrWhiteSpace(this.segmentTypeID))
            {
                this.segmentType = ShuttleSegmentType.Unknown;
            }
            else
            {
                this.segmentTypeID = ShuttleSegmentTypeCatalog.NormalizeSegmentTypeID(this.segmentTypeID);
                this.segmentType = ShuttleSegmentTypeCatalog.ParseSegmentType(this.segmentTypeID);
            }

            if (this.installableSegmentSlotTypes == null)
            {
                this.installableSegmentSlotTypes = new List<string>();
            }

            ShuttleSegmentTypeCatalog.NormalizeTypeList(this.installableSegmentSlotTypes);
            this.installableSegmentSlotTypeEnums =
                this.BuildAssemblyTypeEnumList(this.installableSegmentSlotTypes);

            if (this.installableModuleTypes == null)
            {
                this.installableModuleTypes = new List<string>();
            }

            this.installableModuleTypeEnums = this.BuildModuleTypeEnumList(this.installableModuleTypes);
            for (int i = 0; i < this.installableModuleTypes.Count; i++)
            {
                this.installableModuleTypes[i] =
                    ShuttleModuleTypeCatalog.ToTypeID(this.installableModuleTypeEnums[i]);
            }
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
} // CeleTech.ShuttleExtension.ModularShuttle.Defs
