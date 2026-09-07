using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchRuleResultSanitizer
    {
        internal const int MaxIssuesPerProvider = 8;

        internal ExternalSDKLaunchRuleEvaluationRecord SanitizeResult(
            ShuttleExternalLaunchRuleProviderRegistration registration,
            bool providerReturned,
            ShuttleExternalLaunchRuleResult result)
        {
            if (registration == null || registration.Info == null)
            {
                return null;
            }

            if (!providerReturned)
            {
                string message = result != null
                    ? result.UnavailableReason
                    : "provider returned unavailable";
                return this.BuildDiagnosticRecord(
                    registration,
                    ShuttleExternalLaunchRuleEvaluationStatus.Unavailable,
                    "provider-unavailable",
                    "External launch rule provider is unavailable.",
                    message,
                    false);
            }

            if (result == null)
            {
                return this.BuildDiagnosticRecord(
                    registration,
                    ShuttleExternalLaunchRuleEvaluationStatus.InvalidResult,
                    "provider-invalid-result",
                    "External launch rule provider returned no result.",
                    null,
                    false);
            }

            if (!result.Available)
            {
                List<ShuttleExternalLaunchRuleIssue> issues =
                    new List<ShuttleExternalLaunchRuleIssue>();
                issues.Add(this.BuildDiagnosticIssue(
                    registration,
                    "provider-unavailable",
                    "External launch rule provider is unavailable.",
                    result.UnavailableReason,
                    false));
                bool truncated;
                this.CopyIssues(result.Issues, issues, out truncated);
                return new ExternalSDKLaunchRuleEvaluationRecord(
                    registration.Info,
                    ShuttleExternalLaunchRuleEvaluationStatus.Unavailable,
                    issues,
                    result.UnavailableReason,
                    truncated);
            }

            ShuttleExternalLaunchRuleEvaluationStatus status = result.Status;
            if (status == ShuttleExternalLaunchRuleEvaluationStatus.Unknown)
            {
                status = ShuttleExternalLaunchRuleEvaluationStatus.Succeeded;
            }

            if (status == ShuttleExternalLaunchRuleEvaluationStatus.InvalidResult)
            {
                return this.BuildDiagnosticRecord(
                    registration,
                    ShuttleExternalLaunchRuleEvaluationStatus.InvalidResult,
                    "provider-invalid-result",
                    "External launch rule provider reported an invalid result.",
                    FirstDiagnostic(result),
                    false);
            }

            if (status == ShuttleExternalLaunchRuleEvaluationStatus.Exception)
            {
                return this.BuildDiagnosticRecord(
                    registration,
                    ShuttleExternalLaunchRuleEvaluationStatus.Exception,
                    "provider-exception",
                    "External launch rule provider reported an exception.",
                    FirstDiagnostic(result),
                    false);
            }

            if (status == ShuttleExternalLaunchRuleEvaluationStatus.Skipped)
            {
                return new ExternalSDKLaunchRuleEvaluationRecord(
                    registration.Info,
                    ShuttleExternalLaunchRuleEvaluationStatus.Skipped,
                    null,
                    FirstDiagnostic(result),
                    false);
            }

            List<ShuttleExternalLaunchRuleIssue> sanitizedIssues =
                new List<ShuttleExternalLaunchRuleIssue>();
            bool recordsTruncated;
            int rejected = this.CopyIssues(result.Issues, sanitizedIssues, out recordsTruncated);
            if (rejected > 0 && sanitizedIssues.Count < MaxIssuesPerProvider)
            {
                sanitizedIssues.Add(this.BuildDiagnosticIssue(
                    registration,
                    "provider-invalid-issue-row",
                    "External launch rule provider returned an invalid issue row.",
                    rejected.ToString(),
                    false));
            }

            return new ExternalSDKLaunchRuleEvaluationRecord(
                registration.Info,
                status,
                sanitizedIssues,
                FirstDiagnostic(result),
                recordsTruncated);
        }

        internal ExternalSDKLaunchRuleEvaluationRecord Skipped(
            ShuttleExternalLaunchRuleProviderRegistration registration,
            string message)
        {
            if (registration == null || registration.Info == null)
            {
                return null;
            }

            return new ExternalSDKLaunchRuleEvaluationRecord(
                registration.Info,
                ShuttleExternalLaunchRuleEvaluationStatus.Skipped,
                null,
                message,
                false);
        }

        internal ExternalSDKLaunchRuleEvaluationRecord FromException(
            ShuttleExternalLaunchRuleProviderRegistration registration,
            Exception exception)
        {
            if (registration == null || registration.Info == null)
            {
                return null;
            }

            string message = "provider exception: " +
                (exception != null ? exception.GetType().Name : "unknown");
            return this.BuildDiagnosticRecord(
                registration,
                ShuttleExternalLaunchRuleEvaluationStatus.Exception,
                "provider-exception",
                "External launch rule provider failed during SDK evaluation.",
                message,
                false);
        }

        private ExternalSDKLaunchRuleEvaluationRecord BuildDiagnosticRecord(
            ShuttleExternalLaunchRuleProviderRegistration registration,
            ShuttleExternalLaunchRuleEvaluationStatus status,
            string ruleKey,
            string fallbackMessage,
            string detail,
            bool blocksLaunch)
        {
            List<ShuttleExternalLaunchRuleIssue> issues =
                new List<ShuttleExternalLaunchRuleIssue>();
            issues.Add(this.BuildDiagnosticIssue(
                registration,
                ruleKey,
                fallbackMessage,
                detail,
                blocksLaunch));

            return new ExternalSDKLaunchRuleEvaluationRecord(
                registration.Info,
                status,
                issues,
                detail,
                false);
        }

        private ShuttleExternalLaunchRuleIssue BuildDiagnosticIssue(
            ShuttleExternalLaunchRuleProviderRegistration registration,
            string ruleKey,
            string fallbackMessage,
            string detail,
            bool blocksLaunch)
        {
            string message = fallbackMessage;
            if (!string.IsNullOrEmpty(detail))
            {
                message = fallbackMessage + " " +
                    ShuttleExternalLaunchRuleText.ClampOptionalMessage(detail);
            }

            return new ShuttleExternalLaunchRuleIssue(
                ruleKey,
                ShuttleExternalLaunchRuleSeverity.Warning,
                blocksLaunch,
                null,
                message,
                registration != null ? registration.ProviderKey : null,
                registration != null ? registration.ProviderKey : null,
                ruleKey,
                null);
        }

        private int CopyIssues(
            IReadOnlyList<ShuttleExternalLaunchRuleIssue> source,
            List<ShuttleExternalLaunchRuleIssue> target,
            out bool recordsTruncated)
        {
            recordsTruncated = false;
            int rejected = 0;
            for (int i = 0; source != null && i < source.Count; i++)
            {
                if (target.Count >= MaxIssuesPerProvider)
                {
                    recordsTruncated = true;
                    break;
                }

                ShuttleExternalLaunchRuleIssue issue = source[i];
                if (issue == null || string.IsNullOrEmpty(issue.RuleKey))
                {
                    rejected++;
                    continue;
                }

                target.Add(new ShuttleExternalLaunchRuleIssue(
                    issue.RuleKey,
                    this.NormalizeSeverity(issue.Severity),
                    issue.BlocksLaunch,
                    issue.MessageKey,
                    issue.Message,
                    issue.SourceId,
                    issue.ReferenceId,
                    issue.DiagnosticCode,
                    issue.SuggestedActionKey));
            }

            return rejected;
        }

        private ShuttleExternalLaunchRuleSeverity NormalizeSeverity(
            ShuttleExternalLaunchRuleSeverity severity)
        {
            if (severity == ShuttleExternalLaunchRuleSeverity.Blocker)
            {
                return ShuttleExternalLaunchRuleSeverity.Blocker;
            }

            if (severity == ShuttleExternalLaunchRuleSeverity.Warning)
            {
                return ShuttleExternalLaunchRuleSeverity.Warning;
            }

            return ShuttleExternalLaunchRuleSeverity.Info;
        }

        private static string FirstDiagnostic(ShuttleExternalLaunchRuleResult result)
        {
            if (result == null || result.Diagnostics == null)
            {
                return null;
            }

            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (!string.IsNullOrEmpty(result.Diagnostics[i]))
                {
                    return result.Diagnostics[i];
                }
            }

            return result.UnavailableReason;
        }
    }
}
