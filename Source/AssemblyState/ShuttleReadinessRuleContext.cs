using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleReadinessRuleContext
    {
        internal ShuttleReadinessRuleContext(
            ShuttleAssemblyState assemblyState,
            ShuttleAssemblyTopologyIndex topology,
            List<ProfileBuildIssue> issues,
            ShuttleReadinessIssueFactory issueFactory)
        {
            this.AssemblyState = assemblyState;
            this.Topology = topology;
            this.Issues = issues;
            this.IssueFactory = issueFactory;
        }

        internal ShuttleAssemblyState AssemblyState { get; private set; }
        internal ShuttleAssemblyTopologyIndex Topology { get; private set; }
        internal List<ProfileBuildIssue> Issues { get; private set; }
        internal ShuttleReadinessIssueFactory IssueFactory { get; private set; }

        internal ShuttleSegment FindSegmentByID(string segmentInstanceID)
        {
            if (this.AssemblyState == null ||
                this.AssemblyState.Segments == null ||
                string.IsNullOrEmpty(segmentInstanceID))
            {
                return null;
            }

            for (int i = 0; i < this.AssemblyState.Segments.Count; i++)
            {
                ShuttleSegment segment = this.AssemblyState.Segments[i];
                if (segment != null && segment.SegmentInstanceID == segmentInstanceID)
                {
                    return segment;
                }
            }

            return null;
        }

        internal ShuttleModule FindModuleByID(string moduleInstanceID)
        {
            if (this.AssemblyState == null ||
                this.AssemblyState.Modules == null ||
                string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            for (int i = 0; i < this.AssemblyState.Modules.Count; i++)
            {
                ShuttleModule module = this.AssemblyState.Modules[i];
                if (module != null && module.ModuleInstanceID == moduleInstanceID)
                {
                    return module;
                }
            }

            return null;
        }

        internal bool HasReference(Dictionary<string, List<string>> references, string id)
        {
            if (references == null || string.IsNullOrEmpty(id))
            {
                return false;
            }

            List<string> existingReferences;
            return references.TryGetValue(id, out existingReferences) &&
                existingReferences != null &&
                existingReferences.Count > 0;
        }

        internal bool CanInstallSegmentIntoSlot(ShuttleSegmentSlot slot, ShuttleSegmentBaseDef segmentDef)
        {
            if (slot == null || segmentDef == null)
            {
                return false;
            }

            // Mirrors install-time compatibility while remaining read-only for old save validation.
            if (segmentDef.SegmentType == ShuttleSegmentType.Unknown)
            {
                return false;
            }

            return ShuttleSegmentInstallCompatibility.CanInstallIntoSlot(
                slot.SlotType,
                slot.IsRequired,
                slot.IsFixed,
                slot.IsLocked,
                slot.IsOccupied,
                segmentDef,
                false);
        }

        internal bool CanInstallModuleIntoSlot(
            ShuttleSegment segment,
            ShuttleModuleSlot slot,
            ShuttleModuleSlotDef currentSlotDef,
            ShuttleModuleBaseDef moduleDef)
        {
            if (segment == null || slot == null || moduleDef == null)
            {
                return false;
            }

            ShuttleSegmentBaseDef segmentDef = segment.SegmentDef;
            ShuttleModuleType effectiveSlotType = currentSlotDef != null
                ? ShuttleModuleTypeCatalog.ParseSlotType(currentSlotDef.slotTypeID)
                : slot.SlotType;
            bool effectiveSlotRequired = currentSlotDef != null
                ? currentSlotDef.isRequired
                : slot.IsRequired;
            bool optionalWildcardSlot =
                !effectiveSlotRequired && effectiveSlotType == ShuttleModuleType.Optional;

            if (!ModuleSupportsSegment(segmentDef, moduleDef, optionalWildcardSlot))
            {
                return false;
            }

            if (segmentDef != null &&
                segmentDef.installableModuleTypes != null &&
                segmentDef.installableModuleTypes.Count > 0)
            {
                if (moduleDef.ModuleType == ShuttleModuleType.Unknown)
                {
                    return false;
                }

                if (!SegmentAllowsModuleType(segmentDef, moduleDef))
                {
                    return false;
                }
            }

            if (optionalWildcardSlot)
            {
                return true;
            }

            if (moduleDef.installableModuleSlotTypes == null || moduleDef.installableModuleSlotTypes.Count == 0)
            {
                return true;
            }

            if (moduleDef.ModuleType == ShuttleModuleType.Unknown)
            {
                return false;
            }

            return ShuttleModuleTypeCatalog.MatchesAnyInstallableType(
                effectiveSlotType,
                moduleDef.InstallableModuleSlotTypeEnums);
        }

        private static bool ModuleSupportsSegment(
            ShuttleSegmentBaseDef segmentDef,
            ShuttleModuleBaseDef moduleDef,
            bool requireExplicitSegmentType)
        {
            if (moduleDef == null)
            {
                return false;
            }

            if (moduleDef.installableSegmentTypes == null ||
                moduleDef.installableSegmentTypes.Count == 0)
            {
                return !requireExplicitSegmentType;
            }

            return segmentDef != null &&
                ShuttleSegmentTypeCatalog.MatchesAnyInstallableType(
                    segmentDef.SegmentType,
                    moduleDef.InstallableSegmentTypeEnums);
        }

        private static bool SegmentAllowsModuleType(
            ShuttleSegmentBaseDef segmentDef,
            ShuttleModuleBaseDef moduleDef)
        {
            if (segmentDef == null || moduleDef == null)
            {
                return false;
            }

            IReadOnlyList<ShuttleModuleType> installableTypes = segmentDef.InstallableModuleTypeEnums;
            if (installableTypes == null || installableTypes.Count == 0)
            {
                return true;
            }

            ShuttleModuleType moduleType = moduleDef.ModuleType;
            if (moduleType == ShuttleModuleType.Unknown)
            {
                return false;
            }

            for (int i = 0; i < installableTypes.Count; i++)
            {
                ShuttleModuleType allowedType = installableTypes[i];
                if (allowedType == ShuttleModuleType.Optional || allowedType == moduleType)
                {
                    return true;
                }
            }

            return false;
        }

        internal ShuttleModuleSlot FindMaterializedModuleSlot(
            ShuttleSegment segment,
            string expectedSlotID,
            int slotIndex,
            HashSet<ShuttleModuleSlot> consumedSlots)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return null;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot != null &&
                    slot.SlotID == expectedSlotID &&
                    !this.IsConsumedSlot(slot, consumedSlots))
                {
                    return slot;
                }
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot != null &&
                    slot.SlotIndex == slotIndex &&
                    !this.IsConsumedSlot(slot, consumedSlots))
                {
                    return slot;
                }
            }

            return null;
        }

        internal string BuildExpectedModuleSlotID(string segmentInstanceID, int slotIndex)
        {
            return segmentInstanceID + ".slot." + slotIndex;
        }

        private bool IsConsumedSlot(ShuttleModuleSlot slot, HashSet<ShuttleModuleSlot> consumedSlots)
        {
            if (slot == null || consumedSlots == null)
            {
                return false;
            }

            return consumedSlots.Contains(slot);
        }
    }
}
