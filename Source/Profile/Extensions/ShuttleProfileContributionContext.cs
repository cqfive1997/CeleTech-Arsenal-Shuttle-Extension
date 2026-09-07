using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.API.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions
{
    /// <summary>
    /// Read-only profile contribution input. Contributors must treat these references as
    /// immutable and report values only through Contributions or Issues.
    /// AssemblyState is intentionally absent to avoid exposing mutation-capable APIs.
    /// </summary>
    public sealed class ShuttleProfileContributionContext
    {
        internal ShuttleProfileContributionContext(
            ShuttleModule module,
            ShuttleSegment parentSegment,
            IShuttleProfileContributionSink contributions,
            IShuttleProfileIssueSink issues)
            : this(
                module,
                module != null ? module.ModuleDef : null,
                parentSegment,
                parentSegment != null ? parentSegment.SegmentDef : null,
                contributions,
                issues,
                null)
        {
        }

        internal ShuttleProfileContributionContext(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef,
            ShuttleSegment parentSegment,
            ShuttleSegmentBaseDef parentSegmentDef,
            IShuttleProfileContributionSink contributions,
            IShuttleProfileIssueSink issues)
            : this(
                module,
                moduleDef,
                parentSegment,
                parentSegmentDef,
                contributions,
                issues,
                null)
        {
        }

        internal ShuttleProfileContributionContext(
            ShuttleModule module,
            ShuttleModuleBaseDef moduleDef,
            ShuttleSegment parentSegment,
            ShuttleSegmentBaseDef parentSegmentDef,
            IShuttleProfileContributionSink contributions,
            IShuttleProfileIssueSink issues,
            IShuttleExternalProfileExpressionSink externalExpressions)
        {
            this.Module = module;
            this.ModuleDef = moduleDef;
            this.ParentSegment = parentSegment;
            this.ParentSegmentDef = parentSegmentDef;
            this.ModuleView = ShuttleModuleContributionView.From(module, moduleDef);
            this.ParentSegmentView = ShuttleSegmentContributionView.From(parentSegment, parentSegmentDef);
            this.Contributions = contributions ?? NullShuttleProfileContributionSink.Instance;
            this.Issues = issues ?? NullShuttleProfileIssueSink.Instance;
            this.ExternalExpressions =
                externalExpressions ?? NullShuttleExternalProfileExpressionSink.Instance;
        }

        internal ShuttleModule Module { get; private set; }

        internal ShuttleModuleBaseDef ModuleDef { get; private set; }

        internal ShuttleSegment ParentSegment { get; private set; }

        internal ShuttleSegmentBaseDef ParentSegmentDef { get; private set; }

        public ShuttleModuleContributionView ModuleView { get; private set; }

        public ShuttleSegmentContributionView ParentSegmentView { get; private set; }

        public IShuttleProfileContributionSink Contributions { get; private set; }

        public IShuttleExternalProfileExpressionSink ExternalExpressions { get; private set; }

        public IShuttleProfileIssueSink Issues { get; private set; }
    }
}
