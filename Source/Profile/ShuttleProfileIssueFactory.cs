using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal sealed class ShuttleProfileIssueFactory
    {
        private readonly ShuttleAssemblyReadinessValidator readinessValidator =
            new ShuttleAssemblyReadinessValidator();

        internal void AddReadinessIssues(
            List<ProfileBuildIssue> issues,
            ShuttleAssemblyState assemblyState)
        {
            List<ProfileBuildIssue> readinessIssues = this.readinessValidator.Validate(assemblyState);
            if (readinessIssues == null)
            {
                return;
            }

            for (int i = 0; i < readinessIssues.Count; i++)
            {
                if (readinessIssues[i] != null)
                {
                    issues.Add(readinessIssues[i]);
                }
            }
        }

        internal void AddIssue(
            List<ProfileBuildIssue> issues,
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID)
        {
            if (issues == null)
            {
                return;
            }

            issues.Add(new ProfileBuildIssue(code, message, severity, scope, referenceID));
        }

        internal void AddCockpitRequirementIssue(List<ProfileBuildIssue> issues, bool hasCockpit)
        {
            if (hasCockpit || issues == null)
            {
                return;
            }

            issues.Insert(
                0,
                new ProfileBuildIssue(
                    "cockpit-required",
                    "CT_Shuttle_Issue_CockpitRequired".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Module,
                    null));
        }
    }
}
