using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    /// <summary>
    /// Orchestrates static profile derivation from assembly truth only.
    /// Traversal, contribution dispatch, issue creation, and final section construction are
    /// split into focused helpers so the builder remains a thin pipeline owner.
    /// </summary>
    public sealed class ShuttleProfileBuilder
    {
        private readonly ShuttleProfileIssueFactory issueFactory = new ShuttleProfileIssueFactory();
        private readonly ShuttleModuleProfileContributorRunner moduleRunner;
        private readonly ShuttleSegmentProfileContributorRunner segmentRunner;
        private readonly ShuttleProfileFinalizer finalizer;

        public ShuttleProfileBuilder()
        {
            this.moduleRunner = new ShuttleModuleProfileContributorRunner(this.issueFactory);
            this.segmentRunner = new ShuttleSegmentProfileContributorRunner(this.moduleRunner, this.issueFactory);
            this.finalizer = new ShuttleProfileFinalizer(this.issueFactory);
        }

        public ShuttleProfile Build(ShuttleAssemblyState assemblyState, int revision, ProfileDirtyReason sourceReasons)
        {
            // Build fresh typed sections from current assembly truth.
            // This stage intentionally ignores runtime state and external integrations.
            assemblyState.EnsureInitialized();

            ShuttleProfileBuildContext context = new ShuttleProfileBuildContext(
                ShuttleProfileTuningResolver.ResolveDefault(),
                assemblyState.PrisonCellSupplyConfig);

            // Issue and contribution order matches the historical builder:
            // readiness first, then segment/module traversal, then final cockpit requirement.
            this.issueFactory.AddReadinessIssues(context.Issues, assemblyState);
            this.segmentRunner.TraverseAssembly(assemblyState, context);
            return this.finalizer.BuildFinalProfile(context, revision, sourceReasons);
        }
    }
}
