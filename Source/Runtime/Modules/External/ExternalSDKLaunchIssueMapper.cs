using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchIssueMapper
    {
        internal List<ShuttleExternalLaunchIssueSnapshot> Map(
            IReadOnlyList<ProfileBuildIssue> issues)
        {
            List<ShuttleExternalLaunchIssueSnapshot> result =
                new List<ShuttleExternalLaunchIssueSnapshot>();
            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                ShuttleExternalLaunchIssueSnapshot mapped = this.Map(issues[i]);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }

        private ShuttleExternalLaunchIssueSnapshot Map(ProfileBuildIssue issue)
        {
            if (issue == null)
            {
                return null;
            }

            ShuttleExternalLaunchIssueSeverity severity =
                this.MapSeverity(issue.Severity);
            bool blocksLaunch =
                severity == ShuttleExternalLaunchIssueSeverity.Blocker ||
                severity == ShuttleExternalLaunchIssueSeverity.Unavailable;

            return new ShuttleExternalLaunchIssueSnapshot(
                issue.Code,
                severity,
                null,
                issue.Message,
                blocksLaunch,
                this.MapSourceKind(issue),
                issue.Scope.ToString(),
                issue.ReferenceID,
                null);
        }

        private ShuttleExternalLaunchIssueSeverity MapSeverity(
            ProfileBuildIssueSeverity severity)
        {
            if (severity == ProfileBuildIssueSeverity.Error)
            {
                return ShuttleExternalLaunchIssueSeverity.Blocker;
            }

            if (severity == ProfileBuildIssueSeverity.Warning)
            {
                return ShuttleExternalLaunchIssueSeverity.Warning;
            }

            return ShuttleExternalLaunchIssueSeverity.Info;
        }

        private ShuttleExternalLaunchIssueSourceKind MapSourceKind(ProfileBuildIssue issue)
        {
            string code = issue != null ? issue.Code : null;
            if (this.IsPowerIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.Power;
            }

            if (this.IsCargoIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.Cargo;
            }

            if (this.IsCrewIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.Crew;
            }

            if (this.IsEnvironmentIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.Environment;
            }

            if (this.IsHolderTransferIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.HolderTransfer;
            }

            if (this.IsRefrigeratedIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.RefrigeratedCargo;
            }

            if (this.IsRuntimeIssue(code))
            {
                return ShuttleExternalLaunchIssueSourceKind.ModuleRuntime;
            }

            if (issue != null && issue.Scope == ProfileBuildIssueScope.Profile)
            {
                return ShuttleExternalLaunchIssueSourceKind.Profile;
            }

            if (issue != null && issue.Scope == ProfileBuildIssueScope.Assembly)
            {
                return ShuttleExternalLaunchIssueSourceKind.Assembly;
            }

            return ShuttleExternalLaunchIssueSourceKind.Unknown;
        }

        private bool IsPowerIssue(string code)
        {
            return code == "launch-stored-energy-insufficient" ||
                code == "launch-cooldown" ||
                code == "launch-range-unavailable";
        }

        private bool IsCargoIssue(string code)
        {
            return code == "launch-cargo-backend-unavailable" ||
                code == "launch-cargo-loading-incomplete" ||
                code == "launch-cargo-capacity-unavailable" ||
                code == "launch-cargo-mass-exceeds-capacity" ||
                code == "launch-under-roof";
        }

        private bool IsCrewIssue(string code)
        {
            return code == "cockpit-colonist-required";
        }

        private bool IsEnvironmentIssue(string code)
        {
            return code == "launch-environment-blocked";
        }

        private bool IsHolderTransferIssue(string code)
        {
            return code == "holder-transfer-manifest-active" ||
                code == "habitat-occupied" ||
                code == "habitat-joy-occupied" ||
                code == "habitat-transfer-invalid-state" ||
                code == "medical-bay-occupied" ||
                code == "mech-charger-occupied" ||
                code == "prison-cell-occupied" ||
                code == "active-medical-procedure";
        }

        private bool IsRefrigeratedIssue(string code)
        {
            return code == "refrigerated-cargo-launch-transfer-unavailable";
        }

        private bool IsRuntimeIssue(string code)
        {
            return code == "launch-runtime-preflight";
        }
    }
}
