using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleModuleReadinessRules
    {
        internal void ValidateModuleSlots(ShuttleReadinessRuleContext context, ShuttleSegment segment)
        {
            if (context == null || segment == null)
            {
                return;
            }

            string segmentReference = !string.IsNullOrEmpty(segment.SegmentInstanceID)
                ? segment.SegmentInstanceID
                : "unknown-segment";

            ShuttleSegmentBaseDef segmentDef = segment.SegmentDef;
            if (segmentDef == null || segmentDef.moduleSlots == null)
            {
                // With a missing segment def, only validate persisted slot references. Current
                // def topology cannot be used safely until the def resolves again.
                this.ValidateMaterializedModuleSlotReferences(context, segment);
                return;
            }

            HashSet<ShuttleModuleSlot> consumedSlots = new HashSet<ShuttleModuleSlot>();
            for (int i = 0; i < segmentDef.moduleSlots.Count; i++)
            {
                ShuttleModuleSlotDef slotDef = segmentDef.moduleSlots[i];
                string expectedSlotID = context.BuildExpectedModuleSlotID(segment.SegmentInstanceID, i);
                string slotReference = segmentReference + "/" + expectedSlotID;

                if (slotDef == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "module-slot-def-null",
                        "CT_Shuttle_Issue_NullModuleSlotDef".Translate(segmentReference, i).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        segment.SegmentInstanceID + "::" + expectedSlotID);
                    continue;
                }

                // Match by stable slot ID first, then by legacy index. This lets old saves report
                // useful drift diagnostics after a segment def changes slot ordering.
                ShuttleModuleSlot slot = context.FindMaterializedModuleSlot(segment, expectedSlotID, i, consumedSlots);
                if (slot == null)
                {
                    if (slotDef.isRequired)
                    {
                        context.IssueFactory.AddIssue(
                            context.Issues,
                            "required-module-slot-missing",
                            "CT_Shuttle_Issue_RequiredModuleSlotMissing".Translate(slotReference, segmentDef.defName).ToString(),
                            ProfileBuildIssueSeverity.Error,
                            ProfileBuildIssueScope.Module,
                            segment.SegmentInstanceID + "::" + expectedSlotID);
                    }
                    else
                    {
                        context.IssueFactory.AddIssue(
                            context.Issues,
                            "optional-module-slot-missing",
                            "CT_Shuttle_Issue_OptionalModuleSlotMissing".Translate(slotReference, segmentDef.defName).ToString(),
                            ProfileBuildIssueSeverity.Warning,
                            ProfileBuildIssueScope.Module,
                            segment.SegmentInstanceID + "::" + expectedSlotID);
                    }

                    continue;
                }

                consumedSlots.Add(slot);
                this.ValidateModuleSlotMatchesDef(context, segment, segmentDef, slot, slotDef, expectedSlotID, i, slotReference);
                this.ValidateModuleSlotOccupancy(
                    context,
                    segment,
                    slot,
                    slotDef,
                    slotDef.isRequired,
                    slotReference);
            }

            this.ValidateExtraMaterializedModuleSlotReferences(context, segment, segmentDef, consumedSlots);
        }

        internal void ValidateInstalledModules(ShuttleReadinessRuleContext context)
        {
            if (context == null || context.AssemblyState == null)
            {
                return;
            }

            if (context.AssemblyState.Modules == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "installed-modules-list-missing",
                    "CT_Shuttle_Issue_InstalledModulesListMissing".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    null);
                return;
            }

            for (int i = 0; i < context.AssemblyState.Modules.Count; i++)
            {
                ShuttleModule module = context.AssemblyState.Modules[i];
                if (module == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "installed-module-null",
                        "CT_Shuttle_Issue_NullInstalledModule".Translate().ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        null);
                    continue;
                }

                string moduleReference = !string.IsNullOrEmpty(module.ModuleInstanceID)
                    ? module.ModuleInstanceID
                    : "unknown-module";

                if (module.ModuleDef == null)
                {
                    string moduleDefReference = !string.IsNullOrEmpty(module.moduleDefName)
                        ? module.moduleDefName
                        : "unknown module def";

                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "installed-module-def-missing",
                        "CT_Shuttle_Issue_InstalledModuleDefMissing".Translate(moduleReference, moduleDefReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        module.ModuleInstanceID);
                }

                if (!string.IsNullOrEmpty(module.ModuleInstanceID) &&
                    !context.HasReference(context.Topology.ModuleSlotReferences, module.ModuleInstanceID))
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "orphan-module",
                        "CT_Shuttle_Issue_OrphanModule".Translate(moduleReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        module.ModuleInstanceID);
                }

                this.ValidateModuleParentReference(context, module);
            }
        }

        private void ValidateModuleSlotOccupancy(
            ShuttleReadinessRuleContext context,
            ShuttleSegment segment,
            ShuttleModuleSlot slot,
            ShuttleModuleSlotDef currentSlotDef,
            bool isRequiredByDef,
            string slotReference)
        {
            if (slot == null)
            {
                return;
            }

            if (segment != null &&
                !string.IsNullOrEmpty(slot.ParentSegmentInstanceID) &&
                slot.ParentSegmentInstanceID != segment.SegmentInstanceID)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-slot-parent-mismatch",
                    "CT_Shuttle_Issue_ModuleSlotParentMismatch".Translate(
                        slotReference,
                        slot.ParentSegmentInstanceID,
                        segment.SegmentInstanceID).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    segment.SegmentInstanceID + "::" + slot.SlotID);
            }

            if (string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                if (isRequiredByDef)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "required-module-slot-empty",
                        "CT_Shuttle_Issue_RequiredModuleSlotEmpty".Translate(slotReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        segment.SegmentInstanceID + "::" + slot.SlotID);
                }

                return;
            }

            ShuttleModule module = context.FindModuleByID(slot.InstalledModuleInstanceID);
            if (module == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-slot-reference-missing",
                    "CT_Shuttle_Issue_ModuleSlotReferenceMissing".Translate(slotReference, slot.InstalledModuleInstanceID).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    segment.SegmentInstanceID + "::" + slot.SlotID);
                return;
            }

            if (!module.IsInstalledIn(segment.SegmentInstanceID, slot.SlotID))
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-parent-mismatch",
                    "CT_Shuttle_Issue_ModuleParentMismatch".Translate(
                        slotReference,
                        module.ModuleInstanceID,
                        module.ParentSegmentInstanceID,
                        module.ParentSlotID).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
            }

            if (module.ModuleDef != null && !context.CanInstallModuleIntoSlot(segment, slot, currentSlotDef, module.ModuleDef))
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "installed-module-incompatible",
                    "CT_Shuttle_Issue_InstalledModuleIncompatible".Translate(
                        slotReference,
                        module.ModuleInstanceID,
                        module.moduleDefName).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
            }
        }

        private void ValidateMaterializedModuleSlotReferences(
            ShuttleReadinessRuleContext context,
            ShuttleSegment segment)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return;
            }

            string segmentReference = !string.IsNullOrEmpty(segment.SegmentInstanceID)
                ? segment.SegmentInstanceID
                : "unknown-segment";

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "module-slot-null",
                        "CT_Shuttle_Issue_NullModuleSlot".Translate(segmentReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        segment.SegmentInstanceID);
                    continue;
                }

                string slotReference = segmentReference + "/" + slot.SlotID;
                this.ValidateModuleSlotOccupancy(context, segment, slot, null, false, slotReference);
            }
        }

        private void ValidateExtraMaterializedModuleSlotReferences(
            ShuttleReadinessRuleContext context,
            ShuttleSegment segment,
            ShuttleSegmentBaseDef segmentDef,
            HashSet<ShuttleModuleSlot> consumedSlots)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return;
            }

            string segmentReference = !string.IsNullOrEmpty(segment.SegmentInstanceID)
                ? segment.SegmentInstanceID
                : "unknown-segment";

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "module-slot-null",
                        "CT_Shuttle_Issue_NullModuleSlot".Translate(segmentReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Module,
                        segment.SegmentInstanceID);
                    continue;
                }

                if (consumedSlots != null && consumedSlots.Contains(slot))
                {
                    continue;
                }

                string slotReference = segmentReference + "/" + slot.SlotID;
                // Extra materialized slots are kept visible as diagnostics instead of being
                // repaired here. Occupied obsolete slots can still carry modules/runtime state.
                context.IssueFactory.AddIssue(
                    context.Issues,
                    !string.IsNullOrEmpty(slot.InstalledModuleInstanceID)
                        ? "obsolete-module-slot-occupied"
                        : "obsolete-module-slot",
                    "CT_Shuttle_Issue_ObsoleteModuleSlot".Translate(slotReference, segmentDef.defName).ToString(),
                    !string.IsNullOrEmpty(slot.InstalledModuleInstanceID)
                        ? ProfileBuildIssueSeverity.Error
                        : ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Module,
                    segment.SegmentInstanceID + "::" + slot.SlotID);
                this.ValidateModuleSlotOccupancy(context, segment, slot, null, false, slotReference);
            }
        }

        private void ValidateModuleSlotMatchesDef(
            ShuttleReadinessRuleContext context,
            ShuttleSegment segment,
            ShuttleSegmentBaseDef segmentDef,
            ShuttleModuleSlot slot,
            ShuttleModuleSlotDef slotDef,
            string expectedSlotID,
            int expectedSlotIndex,
            string slotReference)
        {
            if (segment == null || segmentDef == null || slot == null || slotDef == null)
            {
                return;
            }

            // These are compatibility drift checks, not repair rules. Empty drifted slots are
            // warnings, while installed modules are validated separately against current defs.
            if (slot.SlotID != expectedSlotID)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-slot-id-drift",
                    "CT_Shuttle_Issue_ModuleSlotIDDrift".Translate(slotReference, expectedSlotID, expectedSlotIndex).ToString(),
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Module,
                    segment.SegmentInstanceID + "::" + slot.SlotID);
            }

            if (slot.SlotIndex != expectedSlotIndex)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-slot-index-drift",
                    "CT_Shuttle_Issue_ModuleSlotIndexDrift".Translate(slotReference, slot.SlotIndex, expectedSlotIndex).ToString(),
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Module,
                    segment.SegmentInstanceID + "::" + slot.SlotID);
            }

            ShuttleModuleType expectedSlotType = ShuttleModuleTypeCatalog.ParseSlotType(slotDef.slotTypeID);
            if (!ShuttleModuleTypeCatalog.AreCompatible(slot.SlotType, expectedSlotType))
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-slot-type-drift",
                    "CT_Shuttle_Issue_ModuleSlotTypeDrift".Translate(
                        slotReference,
                        slot.SlotTypeID,
                        ShuttleModuleTypeCatalog.ToTypeID(expectedSlotType)).ToString(),
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Module,
                    segment.SegmentInstanceID + "::" + slot.SlotID);
            }
        }

        private void ValidateModuleParentReference(
            ShuttleReadinessRuleContext context,
            ShuttleModule module)
        {
            if (context == null || context.AssemblyState == null || module == null)
            {
                return;
            }

            string moduleReference = !string.IsNullOrEmpty(module.ModuleInstanceID)
                ? module.ModuleInstanceID
                : "unknown-module";

            if (string.IsNullOrEmpty(module.ParentSegmentInstanceID) ||
                string.IsNullOrEmpty(module.ParentSlotID))
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-parent-reference-missing",
                    "CT_Shuttle_Issue_ModuleParentReferenceMissing".Translate(moduleReference).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
                return;
            }

            ShuttleSegment parentSegment = context.FindSegmentByID(module.ParentSegmentInstanceID);
            if (parentSegment == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-parent-segment-missing",
                    "CT_Shuttle_Issue_ModuleParentSegmentMissing".Translate(moduleReference, module.ParentSegmentInstanceID).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
                return;
            }

            ShuttleModuleSlot parentSlot = parentSegment.GetModuleSlotByID(module.ParentSlotID);
            if (parentSlot == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-parent-slot-missing",
                    "CT_Shuttle_Issue_ModuleParentSlotMissing".Translate(
                        moduleReference,
                        module.ParentSegmentInstanceID,
                        module.ParentSlotID).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
                return;
            }

            if (parentSlot.InstalledModuleInstanceID != module.ModuleInstanceID)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "module-parent-slot-not-referencing-module",
                    "CT_Shuttle_Issue_ModuleParentSlotNotReferencing".Translate(
                        moduleReference,
                        module.ParentSegmentInstanceID,
                        module.ParentSlotID,
                        !string.IsNullOrEmpty(parentSlot.InstalledModuleInstanceID)
                            ? parentSlot.InstalledModuleInstanceID
                            : "CT_Shuttle_Empty".Translate().ToString()).ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
            }
        }
    }
}
