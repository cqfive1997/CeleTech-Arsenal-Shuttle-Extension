using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleSegmentReadinessRules
    {
        private readonly ShuttleModuleReadinessRules moduleRules;

        internal ShuttleSegmentReadinessRules(ShuttleModuleReadinessRules moduleRules)
        {
            this.moduleRules = moduleRules;
        }

        internal void ValidateSegmentSlots(ShuttleReadinessRuleContext context)
        {
            if (context == null || context.AssemblyState == null)
            {
                return;
            }

            if (context.AssemblyState.SegmentSlots == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "segment-slots-missing",
                    "CT_Shuttle_Issue_SegmentSlotsMissing".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    null);
                return;
            }

            for (int i = 0; i < context.AssemblyState.SegmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = context.AssemblyState.SegmentSlots[i];
                if (slot == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "segment-slot-null",
                        "CT_Shuttle_Issue_NullSegmentSlot".Translate().ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Assembly,
                        null);
                    continue;
                }

                this.ValidateDefaultSegmentDef(context, slot);

                if (string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
                {
                    if (slot.IsRequired)
                    {
                        context.IssueFactory.AddIssue(
                            context.Issues,
                            "required-segment-slot-empty",
                            "CT_Shuttle_Issue_RequiredSegmentSlotEmpty".Translate(slot.SlotID).ToString(),
                            ProfileBuildIssueSeverity.Error,
                            ProfileBuildIssueScope.Segment,
                            slot.SlotID);
                    }

                    continue;
                }

                ShuttleSegment installedSegment = context.FindSegmentByID(slot.InstalledSegmentInstanceID);
                if (installedSegment == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "segment-slot-reference-missing",
                        "CT_Shuttle_Issue_SegmentSlotReferenceMissing".Translate(slot.SlotID, slot.InstalledSegmentInstanceID).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        slot.SlotID);
                    continue;
                }

                if (installedSegment.SegmentDef != null &&
                    !context.CanInstallSegmentIntoSlot(slot, installedSegment.SegmentDef))
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "installed-segment-incompatible",
                        "CT_Shuttle_Issue_InstalledSegmentIncompatible".Translate(slot.SlotID, installedSegment.SegmentInstanceID).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        slot.SlotID);
                }
            }
        }

        internal void ValidateInstalledSegments(ShuttleReadinessRuleContext context)
        {
            if (context == null || context.AssemblyState == null)
            {
                return;
            }

            if (context.AssemblyState.Segments == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "installed-segments-list-missing",
                    "CT_Shuttle_Issue_InstalledSegmentsListMissing".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Segment,
                    null);
                return;
            }

            for (int i = 0; i < context.AssemblyState.Segments.Count; i++)
            {
                ShuttleSegment segment = context.AssemblyState.Segments[i];
                if (segment == null)
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "installed-segment-null",
                        "CT_Shuttle_Issue_NullInstalledSegment".Translate().ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        null);
                    continue;
                }

                string segmentReference = !string.IsNullOrEmpty(segment.SegmentInstanceID)
                    ? segment.SegmentInstanceID
                    : "unknown-segment";

                if (segment.SegmentDef == null)
                {
                    string segmentDefReference = !string.IsNullOrEmpty(segment.segmentDefName)
                        ? segment.segmentDefName
                        : "unknown segment def";

                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "installed-segment-def-missing",
                        "CT_Shuttle_Issue_InstalledSegmentDefMissing".Translate(segmentReference, segmentDefReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        segment.SegmentInstanceID);
                }

                if (!string.IsNullOrEmpty(segment.SegmentInstanceID) &&
                    !context.HasReference(context.Topology.SegmentSlotReferences, segment.SegmentInstanceID))
                {
                    context.IssueFactory.AddIssue(
                        context.Issues,
                        "orphan-segment",
                        "CT_Shuttle_Issue_OrphanSegment".Translate(segmentReference).ToString(),
                        ProfileBuildIssueSeverity.Error,
                        ProfileBuildIssueScope.Segment,
                        segment.SegmentInstanceID);
                }

                this.moduleRules.ValidateModuleSlots(context, segment);
            }
        }

        private void ValidateDefaultSegmentDef(
            ShuttleReadinessRuleContext context,
            ShuttleSegmentSlot slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.DefaultSegmentDefName))
            {
                return;
            }

            ShuttleSegmentBaseDef defaultSegmentDef = DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(slot.DefaultSegmentDefName);
            if (defaultSegmentDef == null)
            {
                context.IssueFactory.AddIssue(
                    context.Issues,
                    "default-segment-def-missing",
                    "CT_Shuttle_Issue_DefaultSegmentDefMissing".Translate(slot.SlotID, slot.DefaultSegmentDefName).ToString(),
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Segment,
                    slot.SlotID);
            }
        }
    }
}
