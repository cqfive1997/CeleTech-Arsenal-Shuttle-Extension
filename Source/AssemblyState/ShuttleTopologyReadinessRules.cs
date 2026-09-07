using CeleTech.ShuttleExtension.ModularShuttle.Profile;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    internal sealed class ShuttleTopologyReadinessRules
    {
        internal void ValidateReferenceCardinality(ShuttleReadinessRuleContext context)
        {
            if (context == null || context.Topology == null)
            {
                return;
            }

            // One installed instance must have one owning slot. Multiple owners make profile
            // aggregation and later remove/replace commands ambiguous, so these fail closed.
            context.IssueFactory.ValidateDuplicateIDReferences(
                context.Topology.SegmentSlotReferences,
                "segment-referenced-by-multiple-slots",
                "CT_Shuttle_Issue_SegmentReferencedByMultipleSlots",
                null,
                ProfileBuildIssueScope.Segment,
                context.Issues);

            context.IssueFactory.ValidateDuplicateIDReferences(
                context.Topology.ModuleSlotReferences,
                "module-referenced-by-multiple-slots",
                "CT_Shuttle_Issue_ModuleReferencedByMultipleSlots",
                null,
                ProfileBuildIssueScope.Module,
                context.Issues);
        }
    }
}
