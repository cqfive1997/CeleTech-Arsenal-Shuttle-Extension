using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Orchestrates static assembly readiness rules shared by launch gating and UI diagnostics.
    /// Rules read durable assembly truth only; repair and materialization stay in explicit
    /// initialization/mutation paths.
    /// </summary>
    internal sealed class ShuttleAssemblyReadinessValidator
    {
        private readonly ShuttleReadinessIssueFactory issueFactory = new ShuttleReadinessIssueFactory();
        private readonly ShuttleAssemblyTopologyIndexBuilder topologyIndexBuilder =
            new ShuttleAssemblyTopologyIndexBuilder();
        private readonly ShuttleTopologyReadinessRules topologyRules =
            new ShuttleTopologyReadinessRules();
        private readonly ShuttleModuleReadinessRules moduleRules =
            new ShuttleModuleReadinessRules();
        private readonly ShuttleSegmentReadinessRules segmentRules;

        internal ShuttleAssemblyReadinessValidator()
        {
            this.segmentRules = new ShuttleSegmentReadinessRules(this.moduleRules);
        }

        public List<ProfileBuildIssue> Validate(ShuttleAssemblyState assemblyState)
        {
            List<ProfileBuildIssue> issues = new List<ProfileBuildIssue>();

            if (assemblyState == null)
            {
                this.issueFactory.AddIssue(
                    issues,
                    "assembly-state-missing",
                    "CT_Shuttle_Issue_AssemblyStateMissing".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    null);
                return issues;
            }

            // Keep rule invocation order identical to the original validator so issue lists remain stable.
            ShuttleAssemblyTopologyIndex topology =
                this.topologyIndexBuilder.Build(assemblyState, issues, this.issueFactory);
            ShuttleReadinessRuleContext context = new ShuttleReadinessRuleContext(
                assemblyState,
                topology,
                issues,
                this.issueFactory);

            this.topologyRules.ValidateReferenceCardinality(context);
            this.segmentRules.ValidateSegmentSlots(context);
            this.segmentRules.ValidateInstalledSegments(context);
            this.moduleRules.ValidateInstalledModules(context);
            return issues;
        }
    }
}
