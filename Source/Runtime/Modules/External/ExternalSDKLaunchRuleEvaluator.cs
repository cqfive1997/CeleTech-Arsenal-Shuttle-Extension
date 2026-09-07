using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchRuleEvaluator
    {
        private const int MaxProvidersEvaluated = 64;
        private const int MaxTotalIssues = 128;
        private const int MaxProviderRecords = 64;

        private readonly ExternalSDKLaunchRuleContextBuilder contextBuilder =
            new ExternalSDKLaunchRuleContextBuilder();
        private readonly ExternalSDKLaunchRuleResultSanitizer sanitizer =
            new ExternalSDKLaunchRuleResultSanitizer();
        private readonly ExternalSDKLaunchRuleIssueMapper issueMapper =
            new ExternalSDKLaunchRuleIssueMapper();

        internal ExternalSDKLaunchRuleEvaluation Evaluate(
            ShuttleExternalLaunchReadSnapshot baseSnapshot)
        {
            if (baseSnapshot == null)
            {
                return ExternalSDKLaunchRuleEvaluation.Empty();
            }

            List<ShuttleExternalLaunchRuleProviderRegistration> registrations =
                ShuttleExternalLaunchRuleProviderRegistry.GetRegistrationsSnapshotForEvaluation();
            ShuttleExternalLaunchRuleContext context =
                this.contextBuilder.Build(baseSnapshot);
            if (context == null)
            {
                return this.BuildUnavailableDiagnostics(registrations.Count);
            }

            List<ShuttleExternalLaunchIssueSnapshot> mappedIssues =
                new List<ShuttleExternalLaunchIssueSnapshot>();
            List<ShuttleExternalLaunchRuleProviderDiagnosticSnapshot> records =
                new List<ShuttleExternalLaunchRuleProviderDiagnosticSnapshot>();
            int evaluated = 0;
            int skipped = 0;
            int exceptions = 0;
            int invalid = 0;
            bool recordsTruncated = false;

            for (int i = 0; i < registrations.Count; i++)
            {
                if (i >= MaxProvidersEvaluated)
                {
                    skipped += registrations.Count - i;
                    recordsTruncated = true;
                    break;
                }

                ShuttleExternalLaunchRuleProviderRegistration registration =
                    registrations[i];
                ExternalSDKLaunchRuleEvaluationRecord record =
                    this.EvaluateRegistration(registration, context);
                if (record == null)
                {
                    skipped++;
                    continue;
                }

                if (record.Status == ShuttleExternalLaunchRuleEvaluationStatus.Skipped)
                {
                    skipped++;
                }
                else
                {
                    evaluated++;
                }

                if (record.Status == ShuttleExternalLaunchRuleEvaluationStatus.Exception)
                {
                    exceptions++;
                }

                if (record.Status == ShuttleExternalLaunchRuleEvaluationStatus.InvalidResult)
                {
                    invalid++;
                }

                if (records.Count < MaxProviderRecords)
                {
                    records.Add(record.ToDiagnosticSnapshot());
                }
                else
                {
                    recordsTruncated = true;
                }

                if (record.RecordsTruncated)
                {
                    recordsTruncated = true;
                }

                this.AppendMappedIssues(
                    record,
                    mappedIssues,
                    ref recordsTruncated);
            }

            int externalBlockers = CountBlockers(mappedIssues);
            ShuttleExternalLaunchRuleDiagnosticsSnapshot diagnostics =
                new ShuttleExternalLaunchRuleDiagnosticsSnapshot(
                    true,
                    null,
                    registrations.Count,
                    evaluated,
                    skipped,
                    exceptions,
                    invalid,
                    mappedIssues.Count,
                    externalBlockers,
                    recordsTruncated,
                    evaluated > 0,
                    evaluated > 0,
                    records);

            return new ExternalSDKLaunchRuleEvaluation(mappedIssues, diagnostics);
        }

        private ExternalSDKLaunchRuleEvaluationRecord EvaluateRegistration(
            ShuttleExternalLaunchRuleProviderRegistration registration,
            ShuttleExternalLaunchRuleContext context)
        {
            if (registration == null ||
                registration.Provider == null ||
                registration.Info == null)
            {
                return null;
            }

            ShuttleExternalLaunchRuleResult result;
            try
            {
                bool returned =
                    registration.Provider.TryEvaluateLaunchRules(context, out result);
                return this.sanitizer.SanitizeResult(registration, returned, result);
            }
            catch (Exception exception)
            {
                return this.sanitizer.FromException(registration, exception);
            }
        }

        private void AppendMappedIssues(
            ExternalSDKLaunchRuleEvaluationRecord record,
            List<ShuttleExternalLaunchIssueSnapshot> target,
            ref bool recordsTruncated)
        {
            for (int i = 0; record != null && record.Issues != null && i < record.Issues.Count; i++)
            {
                if (target.Count >= MaxTotalIssues)
                {
                    recordsTruncated = true;
                    return;
                }

                ShuttleExternalLaunchIssueSnapshot mapped =
                    this.issueMapper.Map(record.ProviderInfo, record.Issues[i]);
                if (mapped != null)
                {
                    target.Add(mapped);
                }
            }
        }

        private ExternalSDKLaunchRuleEvaluation BuildUnavailableDiagnostics(
            int registeredProviderCount)
        {
            ShuttleExternalLaunchRuleDiagnosticsSnapshot diagnostics =
                new ShuttleExternalLaunchRuleDiagnosticsSnapshot(
                    false,
                    "launch rule context is unavailable",
                    registeredProviderCount,
                    0,
                    registeredProviderCount,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    null);

            return new ExternalSDKLaunchRuleEvaluation(
                new List<ShuttleExternalLaunchIssueSnapshot>(),
                diagnostics);
        }

        private static int CountBlockers(
            IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> issues)
        {
            int count = 0;
            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                if (issues[i] != null && issues[i].BlocksLaunch)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
