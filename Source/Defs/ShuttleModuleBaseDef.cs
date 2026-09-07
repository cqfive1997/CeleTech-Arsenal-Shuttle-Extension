using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Base definition for all shuttle module types.
    /// This describes the static module category and where it is allowed to be installed.
    /// Runtime live values should not be stored here.
    /// </summary>
    public abstract class ShuttleModuleBaseDef : Def
    {
        public const int CorruptionGuardMaxModuleComplexityCost = 1000000000;

        // Raw string persisted through XML authoring and used for display/debug text.
        // Unsupported input is normalized to unknown so def validation can warn explicitly.
        public string moduleTypeID;

        // Every module contributes its own structural mass once installed on the shuttle.
        // This is install-time static data, not a runtime load value.
        public float mass;

        // Constant draw on the shuttle's internal bus. This is not RimWorld grid demand.
        public float idlePowerDrawWatts;

        // Segment-local install budget cost used only by assembly mutation rules.
        // It is not mass, power draw, market value, runtime load, or profile capacity.
        // Default 0 keeps existing defs compatible until a module opts into the gate.
        public int moduleComplexityCost;

        // Player-facing install/replace availability. Deprecated or compatibility-only defs
        // may remain loadable for old saves while being hidden from new V2 installs.
        public bool playerInstallable = true;

        // Segment category IDs this module is allowed to be installed into.
        // This is a broad compatibility rule checked before concrete slot-type compatibility.
        public List<string> installableSegmentTypes = new List<string>();

        // Concrete module slot types this module may occupy.
        // These use the module-type catalog, where optional is a slot-side wildcard.
        public List<string> installableModuleSlotTypes = new List<string>();

        // Research projects required before this module can be newly installed or used as a
        // replacement. Existing installed modules remain loadable for save compatibility.
        public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();

        // Construction-order costs. These are paid/staged before the module is installed;
        // final topology mutation still goes through the normal install/replace command path.
        public List<ThingDefCountClass> constructionCostList = new List<ThingDefCountClass>();
        public int constructionWorkTicks;

        private ShuttleModuleType moduleType = ShuttleModuleType.Unknown;
        private List<ShuttleSegmentType> installableSegmentTypeEnums = new List<ShuttleSegmentType>();
        private List<ShuttleModuleType> installableModuleSlotTypeEnums = new List<ShuttleModuleType>();

        public ShuttleModuleType ModuleType
        {
            get
            {
                if (this.moduleType == ShuttleModuleType.Unknown &&
                    !string.IsNullOrWhiteSpace(this.moduleTypeID))
                {
                    this.EnsureTypeCachesInitialized();
                }

                return this.moduleType;
            }
        }

        public IReadOnlyList<ShuttleSegmentType> InstallableSegmentTypeEnums
        {
            get
            {
                if (this.installableSegmentTypeEnums == null ||
                    (this.installableSegmentTypes != null &&
                     this.installableSegmentTypeEnums.Count != this.installableSegmentTypes.Count))
                {
                    this.EnsureTypeCachesInitialized();
                }

                return this.installableSegmentTypeEnums;
            }
        }

        public IReadOnlyList<ShuttleModuleType> InstallableModuleSlotTypeEnums
        {
            get
            {
                if (this.installableModuleSlotTypeEnums == null ||
                    (this.installableModuleSlotTypes != null &&
                     this.installableModuleSlotTypeEnums.Count != this.installableModuleSlotTypes.Count))
                {
                    this.EnsureTypeCachesInitialized();
                }

                return this.installableModuleSlotTypeEnums;
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

            if (string.IsNullOrEmpty(this.moduleTypeID))
            {
                yield return this.defName + " must define moduleTypeID.";
            }

            this.EnsureTypeCachesInitialized();

            if (this.moduleType == ShuttleModuleType.Unknown)
            {
                yield return this.defName + " defines unknown moduleTypeID " + this.moduleTypeID + ".";
            }
            for (int i = 0; i < this.installableSegmentTypes.Count; i++)
            {
                ShuttleSegmentType segmentType = this.installableSegmentTypeEnums[i];
                if (segmentType == ShuttleSegmentType.Unknown)
                {
                    yield return this.defName + " defines unknown installableSegmentTypes entry " + this.installableSegmentTypes[i] + ".";
                }
            }
            for (int i = 0; i < this.installableModuleSlotTypes.Count; i++)
            {
                ShuttleModuleType slotType = this.installableModuleSlotTypeEnums[i];
                if (slotType == ShuttleModuleType.Unknown)
                {
                    yield return this.defName + " defines unknown installableModuleSlotTypes entry " + this.installableModuleSlotTypes[i] + ".";
                }
            }

            if (!this.IsFiniteFloat(this.mass))
            {
                yield return this.defName + " has non-finite mass.";
            }
            else if (this.mass < 0f)
            {
                yield return this.defName + " has negative mass.";
            }

            if (!this.IsFiniteFloat(this.idlePowerDrawWatts))
            {
                yield return this.defName + " has non-finite idlePowerDrawWatts.";
            }
            else if (this.idlePowerDrawWatts < 0f)
            {
                yield return this.defName + " has negative idlePowerDrawWatts.";
            }

            if (this.moduleComplexityCost < 0)
            {
                yield return this.defName + " has negative moduleComplexityCost.";
            }
            else if (this.moduleComplexityCost > CorruptionGuardMaxModuleComplexityCost)
            {
                yield return this.defName + " moduleComplexityCost exceeds corruption guard max " +
                    CorruptionGuardMaxModuleComplexityCost + ".";
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
        }

        private List<ShuttleSegmentType> BuildSegmentTypeEnumList(List<string> rawTypeIDs)
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
                parsedTypes.Add(ShuttleModuleTypeCatalog.ParseSlotType(rawTypeIDs[i]));
            }

            return parsedTypes;
        }

        private void EnsureTypeCachesInitialized()
        {
            if (string.IsNullOrWhiteSpace(this.moduleTypeID))
            {
                this.moduleType = ShuttleModuleType.Unknown;
            }
            else
            {
                this.moduleTypeID = ShuttleModuleTypeCatalog.NormalizeModuleTypeID(this.moduleTypeID);
                this.moduleType = ShuttleModuleTypeCatalog.ParseModuleType(this.moduleTypeID);
            }

            if (this.installableSegmentTypes == null)
            {
                this.installableSegmentTypes = new List<string>();
            }

            ShuttleSegmentTypeCatalog.NormalizeTypeList(this.installableSegmentTypes);
            this.installableSegmentTypeEnums =
                this.BuildSegmentTypeEnumList(this.installableSegmentTypes);

            if (this.installableModuleSlotTypes == null)
            {
                this.installableModuleSlotTypes = new List<string>();
            }

            ShuttleModuleTypeCatalog.NormalizeTypeList(this.installableModuleSlotTypes);
            this.installableModuleSlotTypeEnums =
                this.BuildModuleTypeEnumList(this.installableModuleSlotTypes);
        }

        protected bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
} // CeleTech.ShuttleExtension.ModularShuttle.Defs
