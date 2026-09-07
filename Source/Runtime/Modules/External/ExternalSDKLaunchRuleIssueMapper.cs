using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchRuleIssueMapper
    {
        internal ShuttleExternalLaunchIssueSnapshot Map(
            ShuttleExternalLaunchRuleProviderInfo providerInfo,
            ShuttleExternalLaunchRuleIssue issue)
        {
            if (providerInfo == null || issue == null)
            {
                return null;
            }

            string providerKey = providerInfo.ProviderKey;
            string ruleKey = string.IsNullOrEmpty(issue.RuleKey)
                ? "unknown-rule"
                : issue.RuleKey;
            string message = issue.Message;
            if (string.IsNullOrEmpty(message))
            {
                message = issue.DiagnosticCode;
            }

            return new ShuttleExternalLaunchIssueSnapshot(
                ShuttleExternalLaunchRuleText.ClampOptionalKey(
                    "external-rule/" + providerKey + "/" + ruleKey),
                this.MapSeverity(issue.Severity),
                issue.MessageKey,
                message,
                issue.BlocksLaunch,
                ShuttleExternalLaunchIssueSourceKind.ExternalLaunchRule,
                ShuttleExternalLaunchRuleText.ClampOptionalKey(
                    "sdk-launch-rule:" + providerKey),
                string.IsNullOrEmpty(issue.ReferenceId) ? ruleKey : issue.ReferenceId,
                issue.SuggestedActionKey,
                true,
                false,
                true);
        }

        private ShuttleExternalLaunchIssueSeverity MapSeverity(
            ShuttleExternalLaunchRuleSeverity severity)
        {
            if (severity == ShuttleExternalLaunchRuleSeverity.Blocker)
            {
                return ShuttleExternalLaunchIssueSeverity.Blocker;
            }

            if (severity == ShuttleExternalLaunchRuleSeverity.Warning)
            {
                return ShuttleExternalLaunchIssueSeverity.Warning;
            }

            return ShuttleExternalLaunchIssueSeverity.Info;
        }
    }
}
