using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchReadinessBuilder
    {
        private const float Epsilon = 0.0001f;

        internal ShuttleExternalLaunchReadinessSnapshot Build(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues,
            ShuttleExternalLaunchHostStateKind hostStateKind,
            Verse.ThingWithComps host)
        {
            return this.Build(
                profile,
                cargoSnapshot,
                cost,
                cooldown,
                issues,
                hostStateKind,
                host,
                null);
        }

        internal ShuttleExternalLaunchReadinessSnapshot Build(
            ShuttleProfile profile,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues,
            ShuttleExternalLaunchHostStateKind hostStateKind,
            Verse.ThingWithComps host,
            ShuttleExternalLaunchRuleDiagnosticsSnapshot ruleDiagnostics)
        {
            int blockers = this.CountBlockers(issues);
            int warnings = this.CountWarnings(issues);
            bool hasTransporterBackend =
                cargoSnapshot != null && cargoSnapshot.HasTransporter;
            bool hasLaunchController =
                cargoSnapshot != null &&
                ShuttleLaunchCrewRequirementUtility.HasLaunchController(cargoSnapshot, host, profile);
            bool cargoLoadingComplete =
                cargoSnapshot != null &&
                cargoSnapshot.QueuedMassKg <= Epsilon &&
                cargoSnapshot.AssignedThingCount == 0 &&
                !this.HasIssue(issues, "launch-cargo-loading-incomplete");
            bool storedEnergyReadyForBaseCost =
                cost != null &&
                cost.StoredEnergyWd + Epsilon >= cost.BaseLaunchEnergyWd;
            bool cooldownActive = cooldown != null && cooldown.CooldownActive;
            bool environmentBlocked = this.HasSource(
                issues,
                ShuttleExternalLaunchIssueSourceKind.Environment);
            bool holderTransferBlocked = this.HasSource(
                issues,
                ShuttleExternalLaunchIssueSourceKind.HolderTransfer);
            bool refrigeratedCargoBlocked = this.HasSource(
                issues,
                ShuttleExternalLaunchIssueSourceKind.RefrigeratedCargo);
            bool moduleRuntimeBlocked = this.HasSource(
                issues,
                ShuttleExternalLaunchIssueSourceKind.ModuleRuntime);
            bool hasProfile = profile != null;
            bool canOpenLaunchFlow =
                hostStateKind == ShuttleExternalLaunchHostStateKind.SpawnedMap &&
                blockers == 0;
            bool canLaunchNow =
                hasProfile &&
                canOpenLaunchFlow &&
                hasTransporterBackend &&
                hasLaunchController &&
                cargoLoadingComplete &&
                storedEnergyReadyForBaseCost &&
                !cooldownActive &&
                cost != null &&
                cost.CanPayRequiredEnergy &&
                !environmentBlocked &&
                !holderTransferBlocked &&
                !refrigeratedCargoBlocked &&
                !moduleRuntimeBlocked;

            ShuttleExternalLaunchRuleDiagnosticsSnapshot diagnostics =
                ruleDiagnostics ?? ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty();

            return new ShuttleExternalLaunchReadinessSnapshot(
                canLaunchNow,
                canOpenLaunchFlow,
                blockers,
                warnings,
                issues != null ? issues.Count : 0,
                hasLaunchController,
                hasTransporterBackend,
                cargoLoadingComplete,
                storedEnergyReadyForBaseCost,
                cooldownActive,
                environmentBlocked,
                holderTransferBlocked,
                refrigeratedCargoBlocked,
                moduleRuntimeBlocked,
                diagnostics.ExternalRulesEvaluated,
                diagnostics.ExternalRuleIssueCount,
                diagnostics.ExternalRuleBlockerCount,
                diagnostics.ExternalRulesAffectSdkReadinessOnly);
        }

        internal static ShuttleExternalLaunchReadinessSnapshot Unavailable()
        {
            return new ShuttleExternalLaunchReadinessSnapshot(
                false,
                false,
                0,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);
        }

        private int CountBlockers(IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues)
        {
            int count = 0;
            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                ShuttleExternalLaunchIssueSnapshot issue = issues[i];
                if (issue != null && issue.BlocksLaunch)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountWarnings(IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues)
        {
            int count = 0;
            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                ShuttleExternalLaunchIssueSnapshot issue = issues[i];
                if (issue != null &&
                    issue.Severity == ShuttleExternalLaunchIssueSeverity.Warning)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasIssue(
            IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues,
            string issueKey)
        {
            if (string.IsNullOrEmpty(issueKey))
            {
                return false;
            }

            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                ShuttleExternalLaunchIssueSnapshot issue = issues[i];
                if (issue != null && issue.IssueKey == issueKey)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSource(
            IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues,
            ShuttleExternalLaunchIssueSourceKind sourceKind)
        {
            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                ShuttleExternalLaunchIssueSnapshot issue = issues[i];
                if (issue != null && issue.SourceKind == sourceKind && issue.BlocksLaunch)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
