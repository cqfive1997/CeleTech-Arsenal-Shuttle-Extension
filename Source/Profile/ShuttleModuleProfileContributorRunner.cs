using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal sealed class ShuttleModuleProfileContributorRunner
    {
        private readonly ShuttleProfileIssueFactory issueFactory;

        internal ShuttleModuleProfileContributorRunner(ShuttleProfileIssueFactory issueFactory)
        {
            this.issueFactory = issueFactory;
        }

        internal void ProcessModuleSlot(
            ShuttleAssemblyState assemblyState,
            ShuttleSegment installedSegment,
            ShuttleModuleSlot moduleSlot,
            ShuttleProfileBuildContext context)
        {
            // Segment-level layout is derived only from module slots exposed by installed segments.
            context.ModuleSlotCount++;

            if (moduleSlot == null)
            {
                return;
            }

            string installedModuleDefName = null;
            string installedModuleInstanceID = null;
            bool installedModuleEnabled = false;

            if (!string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID))
            {
                ShuttleModule module = assemblyState.GetModule(moduleSlot.InstalledModuleInstanceID);
                if (module != null)
                {
                    context.InstalledModuleCount++;
                    installedModuleInstanceID = module.ModuleInstanceID;
                    installedModuleDefName = module.moduleDefName;
                    installedModuleEnabled = module.IsEnabled;

                    if (module.ModuleDef != null &&
                        module.ModuleDef.ModuleType == ShuttleModuleType.Unknown)
                    {
                        this.issueFactory.AddIssue(
                            context.Issues,
                            "module-type-unknown",
                            "CT_Shuttle_Issue_ModuleTypeUnknown".Translate(
                                module.ModuleInstanceID,
                                module.moduleDefName).ToString(),
                            ProfileBuildIssueSeverity.Warning,
                            ProfileBuildIssueScope.Module,
                            module.ModuleInstanceID);
                    }

                    this.ApplyModuleContribution(context.Accumulator, installedSegment, module, context);
                }
            }

            context.ModuleEntries.Add(new ModuleLayoutEntry(
                installedSegment.SegmentInstanceID,
                moduleSlot.SlotID,
                moduleSlot.SlotTypeID,
                installedModuleInstanceID,
                installedModuleDefName,
                moduleSlot.IsRequired,
                moduleSlot.IsLocked,
                installedModuleEnabled));
        }

        private void ApplyModuleContribution(
            ShuttleProfileAccumulator accumulator,
            ShuttleSegment parentSegment,
            ShuttleModule module,
            ShuttleProfileBuildContext context)
        {
            if (accumulator == null || module == null || module.ModuleDef == null)
            {
                return;
            }

            IShuttleProfileIssueSink issueSink = new ShuttleProfileIssueSink(context.Issues);
            ShuttleProfileAccumulatorContributionSink contributionSink =
                new ShuttleProfileAccumulatorContributionSink(
                    accumulator,
                    issueSink,
                    module.ModuleInstanceID);
            ShuttleExternalProfileExpressionSink externalExpressionSink =
                new ShuttleExternalProfileExpressionSink(
                    context.ExternalExpressions,
                    issueSink);

            // Structural mass always applies once the module is installed, regardless of whether
            // a more specific contribution handler exists.
            contributionSink.AddModuleMassKg(module.ModuleDef.mass);

            if (!module.IsEnabled)
            {
                this.issueFactory.AddIssue(
                    context.Issues,
                    "module-disabled",
                    "CT_Shuttle_Issue_ModuleDisabled".Translate(module.ModuleInstanceID).ToString(),
                    ProfileBuildIssueSeverity.Info,
                    ProfileBuildIssueScope.Module,
                    module.ModuleInstanceID);
                return;
            }

            if (module.ModuleDef.idlePowerDrawWatts > 0f)
            {
                contributionSink.AddInternalIdleDemandWatts(module.ModuleDef.idlePowerDrawWatts);
            }
            else if (!this.IsFiniteFloat(module.ModuleDef.idlePowerDrawWatts) ||
                     module.ModuleDef.idlePowerDrawWatts < 0f)
            {
                contributionSink.AddInternalIdleDemandWatts(module.ModuleDef.idlePowerDrawWatts);
            }

            this.ApplyBuiltInCommandCapability(contributionSink, module);

            List<ShuttleProfileContributorResolution> contributors =
                ShuttleProfileContributorResolver.ResolveContributorsOrFallback(
                    module.ModuleDef,
                    issueSink,
                    module.ModuleInstanceID);

            ShuttleProfileContributionContext contributionContext = new ShuttleProfileContributionContext(
                module,
                module.ModuleDef,
                parentSegment,
                parentSegment != null ? parentSegment.SegmentDef : null,
                contributionSink,
                issueSink,
                externalExpressionSink);

            for (int i = 0; i < contributors.Count; i++)
            {
                ShuttleProfileContributorResolution contributor = contributors[i];
                try
                {
                    externalExpressionSink.SetCurrentSource(
                        contributor.ContributorKey,
                        this.IsBuiltInContributor(contributor),
                        module);
                    contributor.Contributor.Contribute(contributionContext);
                }
                catch (Exception exception)
                {
                    ShuttleProfileContributorResolver.LogContributorException(
                        contributor.ContributorKey,
                        module.moduleDefName,
                        exception);

                    this.issueFactory.AddIssue(
                        context.Issues,
                        "profile-contributor-exception",
                        "CT_Shuttle_Issue_ProfileContributorException".Translate(
                            contributor.ContributorKey,
                            module.ModuleInstanceID,
                            module.moduleDefName,
                            exception.GetType().Name,
                            exception.Message).ToString(),
                        ProfileBuildIssueSeverity.Warning,
                        ProfileBuildIssueScope.Module,
                        module.ModuleInstanceID);
                }
                finally
                {
                    externalExpressionSink.ClearCurrentSource();
                }
            }
        }

        private void ApplyBuiltInCommandCapability(
            ShuttleProfileAccumulatorContributionSink contributionSink,
            ShuttleModule module)
        {
            if (contributionSink == null || module == null)
            {
                return;
            }

            ShuttleCockpitModuleDef cockpitDef = module.ModuleDef as ShuttleCockpitModuleDef;
            if (cockpitDef != null && cockpitDef.providesCockpit)
            {
                contributionSink.MarkHasCockpit();
            }

            ShuttleLandingStabilizerModuleDef stabilizerDef = module.ModuleDef as ShuttleLandingStabilizerModuleDef;
            if (stabilizerDef != null && stabilizerDef.EffectiveStabilizesAdverseWeather)
            {
                contributionSink.MarkStabilizesAdverseWeather();
            }

            ShuttleNavigationComputerModuleDef navigationDef = module.ModuleDef as ShuttleNavigationComputerModuleDef;
            if (navigationDef != null && navigationDef.providesAutonomousLaunchControl)
            {
                contributionSink.MarkProvidesAutonomousLaunchControl();
            }

            if (navigationDef != null && navigationDef.providesSecureSignalLink)
            {
                contributionSink.MarkProvidesSecureSignalLink();
            }
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private bool IsBuiltInContributor(ShuttleProfileContributorResolution contributor)
        {
            return contributor != null &&
                !contributor.IsExplicitKey &&
                !string.IsNullOrEmpty(contributor.ContributorKey) &&
                contributor.ContributorKey.StartsWith("built-in/", StringComparison.Ordinal);
        }
    }
}
