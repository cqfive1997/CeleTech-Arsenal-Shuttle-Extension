using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal sealed class ShuttleSegmentProfileContributorRunner
    {
        private readonly ShuttleModuleProfileContributorRunner moduleRunner;
        private readonly ShuttleProfileIssueFactory issueFactory;

        internal ShuttleSegmentProfileContributorRunner(
            ShuttleModuleProfileContributorRunner moduleRunner,
            ShuttleProfileIssueFactory issueFactory)
        {
            this.moduleRunner = moduleRunner;
            this.issueFactory = issueFactory;
        }

        internal void TraverseAssembly(
            ShuttleAssemblyState assemblyState,
            ShuttleProfileBuildContext context)
        {
            foreach (ShuttleSegmentSlot segmentSlot in assemblyState.SegmentSlots)
            {
                // Shuttle-level layout is derived from materialized segment slots, whether occupied or empty.
                context.SegmentSlotCount++;

                if (segmentSlot == null)
                {
                    continue;
                }

                string installedSegmentDefName = null;
                string installedSegmentInstanceID = null;
                ShuttleSegment installedSegment = null;

                if (!string.IsNullOrEmpty(segmentSlot.InstalledSegmentInstanceID))
                {
                    installedSegment = assemblyState.GetSegment(segmentSlot.InstalledSegmentInstanceID);
                    if (installedSegment != null)
                    {
                        context.InstalledSegmentCount++;
                        installedSegmentInstanceID = installedSegment.SegmentInstanceID;
                        installedSegmentDefName = installedSegment.segmentDefName;

                        if (installedSegment.SegmentDef != null)
                        {
                            ShuttleProfileAccumulatorContributionSink contributionSink =
                                new ShuttleProfileAccumulatorContributionSink(
                                    context.Accumulator,
                                    new ShuttleProfileIssueSink(context.Issues),
                                    installedSegment.SegmentInstanceID,
                                    ProfileBuildIssueScope.Segment);
                            contributionSink.AddSegmentMass(installedSegment.SegmentDef.mass);
                            if (installedSegment.SegmentDef.SegmentType == ShuttleSegmentType.Cargo)
                            {
                                // Each cargo segment owns one visual cargo region even though the
                                // current real container remains a single CompTransporter.
                                contributionSink.IncrementCargoRegionCount();
                            }
                        }

                        if (installedSegment.SegmentDef != null &&
                            installedSegment.SegmentDef.SegmentType == ShuttleSegmentType.Unknown)
                        {
                            this.issueFactory.AddIssue(
                                context.Issues,
                                "segment-type-unknown",
                                "CT_Shuttle_Issue_SegmentTypeUnknown".Translate(
                                    installedSegment.SegmentInstanceID,
                                    installedSegment.segmentDefName).ToString(),
                                ProfileBuildIssueSeverity.Warning,
                                ProfileBuildIssueScope.Segment,
                                installedSegment.SegmentInstanceID);
                        }
                    }
                }

                context.SegmentEntries.Add(new SegmentLayoutEntry(
                    segmentSlot.SlotID,
                    segmentSlot.SlotTypeID,
                    installedSegmentInstanceID,
                    installedSegmentDefName,
                    segmentSlot.DefaultSegmentDefName,
                    segmentSlot.IsFixed,
                    segmentSlot.IsRequired,
                    segmentSlot.IsLocked));

                if (installedSegment == null)
                {
                    continue;
                }

                foreach (ShuttleModuleSlot moduleSlot in installedSegment.ModuleSlots)
                {
                    this.moduleRunner.ProcessModuleSlot(
                        assemblyState,
                        installedSegment,
                        moduleSlot,
                        context);
                }
            }
        }
    }
}
