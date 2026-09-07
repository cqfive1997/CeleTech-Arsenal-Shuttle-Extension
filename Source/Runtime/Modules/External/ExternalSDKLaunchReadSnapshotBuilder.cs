using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchReadSnapshotBuilder
    {
        private readonly ExternalSDKLaunchIssueMapper issueMapper =
            new ExternalSDKLaunchIssueMapper();
        private readonly ExternalSDKLaunchPayloadSnapshotBuilder payloadBuilder =
            new ExternalSDKLaunchPayloadSnapshotBuilder();
        private readonly ExternalSDKLaunchCooldownSnapshotBuilder cooldownBuilder =
            new ExternalSDKLaunchCooldownSnapshotBuilder();
        private readonly ExternalSDKLaunchQuoteBuilder quoteBuilder =
            new ExternalSDKLaunchQuoteBuilder();
        private readonly ExternalSDKLaunchReadinessBuilder readinessBuilder =
            new ExternalSDKLaunchReadinessBuilder();
        private readonly ExternalSDKLaunchRuleEvaluator ruleEvaluator =
            new ExternalSDKLaunchRuleEvaluator();

        internal ShuttleExternalLaunchReadSnapshot Build(ShuttleController controller)
        {
            if (controller == null)
            {
                return Unavailable("shuttle controller is unavailable");
            }

            ShuttleProfile profile = controller.GetProfileForRead();
            if (profile == null)
            {
                return Unavailable("shuttle profile is unavailable");
            }

            ShuttleRuntimeState runtimeState = controller.GetLaunchRuntimeState();
            if (runtimeState == null)
            {
                return Unavailable("shuttle runtime state is unavailable");
            }

            ShuttleCargoSnapshot cargoSnapshot = controller.BuildLaunchCargoSnapshot();
            if (cargoSnapshot == null)
            {
                return Unavailable("launch cargo snapshot is unavailable");
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            List<ShuttleExternalLaunchIssueSnapshot> issues =
                this.issueMapper.Map(
                    controller.BuildAssemblyReadinessIssuesForExternalLaunchRead(
                        profile,
                        cargoSnapshot));
            ShuttleExternalLaunchPayloadSnapshot payload =
                this.payloadBuilder.Build(profile, cargoSnapshot);
            ShuttleExternalLaunchCostSnapshot cost =
                this.quoteBuilder.BuildCost(profile, runtimeState, cargoSnapshot);
            ShuttleExternalLaunchCooldownSnapshot cooldown =
                this.cooldownBuilder.Build(profile, runtimeState, ticksGame);
            ShuttleExternalLaunchHostStateKind hostStateKind =
                this.GetHostStateKind(controller);
            ShuttleExternalLaunchReadinessSnapshot baseReadiness =
                this.readinessBuilder.Build(
                    profile,
                    cargoSnapshot,
                    cost,
                    cooldown,
                    issues,
                    hostStateKind,
                    controller.ShuttleHost);
            string hostStableId = this.GetHostStableId(controller);
            ShuttleExternalLaunchReadSnapshot baseSnapshot =
                new ShuttleExternalLaunchReadSnapshot(
                    true,
                    null,
                    hostStableId,
                    profile.Revision,
                    ticksGame,
                    hostStateKind,
                    baseReadiness,
                    payload,
                    cost,
                    cooldown,
                    issues,
                    false,
                    ExternalSDKLaunchQuoteBuilder.DestinationQuotePolicy,
                    ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty());

            ExternalSDKLaunchRuleEvaluation ruleEvaluation =
                this.ruleEvaluator.Evaluate(baseSnapshot);
            List<ShuttleExternalLaunchIssueSnapshot> combinedIssues =
                new List<ShuttleExternalLaunchIssueSnapshot>(issues);
            if (ruleEvaluation != null && ruleEvaluation.Issues != null)
            {
                combinedIssues.AddRange(ruleEvaluation.Issues);
            }

            ShuttleExternalLaunchRuleDiagnosticsSnapshot ruleDiagnostics =
                ruleEvaluation != null
                    ? ruleEvaluation.Diagnostics
                    : ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty();
            ShuttleExternalLaunchReadinessSnapshot readiness =
                this.readinessBuilder.Build(
                    profile,
                    cargoSnapshot,
                    cost,
                    cooldown,
                    combinedIssues,
                    hostStateKind,
                    controller.ShuttleHost,
                    ruleDiagnostics);

            return new ShuttleExternalLaunchReadSnapshot(
                true,
                null,
                hostStableId,
                profile.Revision,
                ticksGame,
                hostStateKind,
                readiness,
                payload,
                cost,
                cooldown,
                combinedIssues,
                false,
                ExternalSDKLaunchQuoteBuilder.DestinationQuotePolicy,
                ruleDiagnostics);
        }

        internal static ShuttleExternalLaunchReadSnapshot Unavailable(string reason)
        {
            return new ShuttleExternalLaunchReadSnapshot(
                false,
                reason,
                0,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                ShuttleExternalLaunchHostStateKind.Unavailable,
                ExternalSDKLaunchReadinessBuilder.Unavailable(),
                ExternalSDKLaunchPayloadSnapshotBuilder.Unavailable(),
                ExternalSDKLaunchQuoteBuilder.UnavailableCost(),
                ExternalSDKLaunchCooldownSnapshotBuilder.Unavailable(),
                new List<ShuttleExternalLaunchIssueSnapshot>(),
                false,
                ExternalSDKLaunchQuoteBuilder.DestinationQuotePolicy);
        }

        internal ShuttleExternalLaunchReadSnapshot BuildUnavailableFromException(
            Exception exception)
        {
            return Unavailable(
                "launch read snapshot failed: " +
                (exception != null ? exception.GetType().Name : "unknown"));
        }

        private ShuttleExternalLaunchHostStateKind GetHostStateKind(
            ShuttleController controller)
        {
            if (controller == null || controller.ShuttleHost == null)
            {
                return ShuttleExternalLaunchHostStateKind.Unavailable;
            }

            if (controller.ShuttleHost.Destroyed)
            {
                return ShuttleExternalLaunchHostStateKind.Unavailable;
            }

            if (controller.ShuttleHost.Spawned && controller.ShuttleHost.Map != null)
            {
                return ShuttleExternalLaunchHostStateKind.SpawnedMap;
            }

            return ShuttleExternalLaunchHostStateKind.Unknown;
        }

        private string GetHostStableId(ShuttleController controller)
        {
            if (controller == null || controller.ShuttleHost == null)
            {
                return null;
            }

            try
            {
                int id = controller.ShuttleHost.thingIDNumber;
                return id > 0 ? id.ToString() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
