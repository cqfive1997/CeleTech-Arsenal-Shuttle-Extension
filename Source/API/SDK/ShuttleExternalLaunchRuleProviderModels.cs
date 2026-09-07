using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public interface IShuttleExternalLaunchRuleProvider
    {
        ShuttleExternalLaunchRuleProviderInfo GetProviderInfo();

        bool TryEvaluateLaunchRules(
            ShuttleExternalLaunchRuleContext context,
            out ShuttleExternalLaunchRuleResult result);
    }

    public enum ShuttleExternalLaunchRuleSeverity
    {
        Info,
        Warning,
        Blocker
    }

    public enum ShuttleExternalLaunchRuleEvaluationStatus
    {
        Unknown,
        Succeeded,
        Unavailable,
        InvalidResult,
        Exception,
        Skipped
    }

    public sealed class ShuttleExternalLaunchRuleProviderInfo
    {
        public ShuttleExternalLaunchRuleProviderInfo(
            string ownerPackageId,
            string localProviderKey,
            string displayName,
            string version,
            bool enabledByDefault,
            string description)
        {
            this.OwnerPackageId =
                ShuttleExternalLaunchRuleText.ClampRequiredKey(ownerPackageId);
            this.LocalProviderKey =
                ShuttleExternalLaunchRuleText.ClampRequiredKey(localProviderKey);
            this.ProviderKey = ShuttleExternalLaunchRuleText.BuildProviderKey(
                this.OwnerPackageId,
                this.LocalProviderKey);
            this.DisplayName =
                ShuttleExternalLaunchRuleText.ClampOptionalText(displayName);
            this.Version =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(version);
            this.EnabledByDefault = enabledByDefault;
            this.Description =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(description);
        }

        public string OwnerPackageId { get; private set; }
        public string LocalProviderKey { get; private set; }
        public string ProviderKey { get; private set; }
        public string DisplayName { get; private set; }
        public string Version { get; private set; }
        public bool EnabledByDefault { get; private set; }
        public string Description { get; private set; }
    }

    public sealed class ShuttleExternalLaunchRuleContext
    {
        public ShuttleExternalLaunchRuleContext(
            string sdkVersion,
            string hostStableId,
            int profileRevision,
            int ticksGame,
            ShuttleExternalLaunchHostStateKind hostStateKind,
            ShuttleExternalLaunchReadinessSnapshot readiness,
            ShuttleExternalLaunchPayloadSnapshot payload,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IEnumerable<ShuttleExternalLaunchIssueSnapshot> baseIssues,
            bool destinationQuoteSupported,
            string destinationQuotePolicy)
        {
            this.SdkVersion =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(sdkVersion);
            this.HostStableId =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(hostStableId);
            this.ProfileRevision = profileRevision;
            this.TicksGame = ticksGame;
            this.HostStateKind = hostStateKind;
            this.Readiness = readiness;
            this.Payload = payload;
            this.Cost = cost;
            this.Cooldown = cooldown;
            this.BaseIssues = ShuttleExternalSDKCollections.Copy(baseIssues);
            this.DestinationQuoteSupported = destinationQuoteSupported;
            this.DestinationQuotePolicy =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(destinationQuotePolicy);
        }

        public string SdkVersion { get; private set; }
        public string HostStableId { get; private set; }
        public int ProfileRevision { get; private set; }
        public int TicksGame { get; private set; }
        public ShuttleExternalLaunchHostStateKind HostStateKind { get; private set; }
        public ShuttleExternalLaunchReadinessSnapshot Readiness { get; private set; }
        public ShuttleExternalLaunchPayloadSnapshot Payload { get; private set; }
        public ShuttleExternalLaunchCostSnapshot Cost { get; private set; }
        public ShuttleExternalLaunchCooldownSnapshot Cooldown { get; private set; }
        public IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> BaseIssues { get; private set; }
        public bool DestinationQuoteSupported { get; private set; }
        public string DestinationQuotePolicy { get; private set; }
    }

    public sealed class ShuttleExternalLaunchRuleResult
    {
        public ShuttleExternalLaunchRuleResult(
            bool available,
            string unavailableReason,
            string providerKey,
            string ownerPackageId,
            string localProviderKey,
            ShuttleExternalLaunchRuleEvaluationStatus status,
            IEnumerable<ShuttleExternalLaunchRuleIssue> issues,
            IEnumerable<string> diagnostics)
        {
            this.Available = available;
            this.UnavailableReason =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(unavailableReason);
            this.ProviderKey =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(providerKey);
            this.OwnerPackageId =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(ownerPackageId);
            this.LocalProviderKey =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(localProviderKey);
            this.Status = status;
            this.Issues = ShuttleExternalSDKCollections.Copy(issues);
            this.Diagnostics = ShuttleExternalSDKCollections.CopyStrings(diagnostics);
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public string ProviderKey { get; private set; }
        public string OwnerPackageId { get; private set; }
        public string LocalProviderKey { get; private set; }
        public ShuttleExternalLaunchRuleEvaluationStatus Status { get; private set; }
        public IReadOnlyList<ShuttleExternalLaunchRuleIssue> Issues { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }
    }

    public sealed class ShuttleExternalLaunchRuleIssue
    {
        public ShuttleExternalLaunchRuleIssue(
            string ruleKey,
            ShuttleExternalLaunchRuleSeverity severity,
            bool blocksLaunch,
            string messageKey,
            string message,
            string sourceId,
            string referenceId,
            string diagnosticCode,
            string suggestedActionKey)
        {
            this.RuleKey =
                ShuttleExternalLaunchRuleText.ClampRequiredKey(ruleKey);
            this.Severity = severity;
            this.BlocksLaunch = blocksLaunch ||
                severity == ShuttleExternalLaunchRuleSeverity.Blocker;
            this.MessageKey =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(messageKey);
            this.Message =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(message);
            this.SourceId =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(sourceId);
            this.ReferenceId =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(referenceId);
            this.DiagnosticCode =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(diagnosticCode);
            this.SuggestedActionKey =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(suggestedActionKey);
            this.EnforcedInActualLaunch = false;
            this.AffectsSdkReadinessOnly = true;
        }

        public string RuleKey { get; private set; }
        public ShuttleExternalLaunchRuleSeverity Severity { get; private set; }
        public bool BlocksLaunch { get; private set; }
        public string MessageKey { get; private set; }
        public string Message { get; private set; }
        public string SourceId { get; private set; }
        public string ReferenceId { get; private set; }
        public string DiagnosticCode { get; private set; }
        public string SuggestedActionKey { get; private set; }
        public bool EnforcedInActualLaunch { get; private set; }
        public bool AffectsSdkReadinessOnly { get; private set; }
    }

    public sealed class ShuttleExternalLaunchRuleDiagnosticsSnapshot
    {
        public ShuttleExternalLaunchRuleDiagnosticsSnapshot(
            bool available,
            string unavailableReason,
            int registeredProviderCount,
            int evaluatedProviderCount,
            int skippedProviderCount,
            int providerExceptionCount,
            int invalidResultCount,
            int externalRuleIssueCount,
            int externalRuleBlockerCount,
            bool recordsTruncated,
            bool externalRulesEvaluated,
            bool externalRulesAffectSdkReadinessOnly,
            IEnumerable<ShuttleExternalLaunchRuleProviderDiagnosticSnapshot> providerRecords)
        {
            this.Available = available;
            this.UnavailableReason =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(unavailableReason);
            this.RegisteredProviderCount = registeredProviderCount;
            this.EvaluatedProviderCount = evaluatedProviderCount;
            this.SkippedProviderCount = skippedProviderCount;
            this.ProviderExceptionCount = providerExceptionCount;
            this.InvalidResultCount = invalidResultCount;
            this.ExternalRuleIssueCount = externalRuleIssueCount;
            this.ExternalRuleBlockerCount = externalRuleBlockerCount;
            this.RecordsTruncated = recordsTruncated;
            this.ExternalRulesEvaluated = externalRulesEvaluated;
            this.ExternalRulesAffectSdkReadinessOnly =
                externalRulesAffectSdkReadinessOnly;
            this.ProviderRecords =
                ShuttleExternalSDKCollections.Copy(providerRecords);
        }

        public static ShuttleExternalLaunchRuleDiagnosticsSnapshot Empty()
        {
            return new ShuttleExternalLaunchRuleDiagnosticsSnapshot(
                true,
                null,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                null);
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public int RegisteredProviderCount { get; private set; }
        public int EvaluatedProviderCount { get; private set; }
        public int SkippedProviderCount { get; private set; }
        public int ProviderExceptionCount { get; private set; }
        public int InvalidResultCount { get; private set; }
        public int ExternalRuleIssueCount { get; private set; }
        public int ExternalRuleBlockerCount { get; private set; }
        public bool RecordsTruncated { get; private set; }
        public bool ExternalRulesEvaluated { get; private set; }
        public bool ExternalRulesAffectSdkReadinessOnly { get; private set; }
        public IReadOnlyList<ShuttleExternalLaunchRuleProviderDiagnosticSnapshot> ProviderRecords
        {
            get;
            private set;
        }
    }

    public sealed class ShuttleExternalLaunchRuleProviderDiagnosticSnapshot
    {
        public ShuttleExternalLaunchRuleProviderDiagnosticSnapshot(
            string providerKey,
            string ownerPackageId,
            string localProviderKey,
            string displayName,
            ShuttleExternalLaunchRuleEvaluationStatus status,
            int issueCount,
            int blockerCount,
            string message)
        {
            this.ProviderKey =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(providerKey);
            this.OwnerPackageId =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(ownerPackageId);
            this.LocalProviderKey =
                ShuttleExternalLaunchRuleText.ClampOptionalKey(localProviderKey);
            this.DisplayName =
                ShuttleExternalLaunchRuleText.ClampOptionalText(displayName);
            this.Status = status;
            this.IssueCount = issueCount;
            this.BlockerCount = blockerCount;
            this.Message =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(message);
        }

        public string ProviderKey { get; private set; }
        public string OwnerPackageId { get; private set; }
        public string LocalProviderKey { get; private set; }
        public string DisplayName { get; private set; }
        public ShuttleExternalLaunchRuleEvaluationStatus Status { get; private set; }
        public int IssueCount { get; private set; }
        public int BlockerCount { get; private set; }
        public string Message { get; private set; }
    }

    internal static class ShuttleExternalLaunchRuleText
    {
        internal const int MaxKeyLength = 128;
        internal const int MaxTextLength = 256;
        internal const int MaxMessageLength = 512;

        internal static string ClampRequiredKey(string value)
        {
            return Clamp(value, MaxKeyLength, true);
        }

        internal static string ClampOptionalKey(string value)
        {
            return Clamp(value, MaxKeyLength, false);
        }

        internal static string ClampOptionalText(string value)
        {
            return Clamp(value, MaxTextLength, false);
        }

        internal static string ClampOptionalMessage(string value)
        {
            return Clamp(value, MaxMessageLength, false);
        }

        internal static string BuildProviderKey(string ownerPackageId, string localProviderKey)
        {
            if (string.IsNullOrEmpty(ownerPackageId) ||
                string.IsNullOrEmpty(localProviderKey))
            {
                return null;
            }

            return ClampOptionalKey(ownerPackageId + "/" + localProviderKey);
        }

        private static string Clamp(string value, int maxLength, bool required)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length <= maxLength)
            {
                return trimmed;
            }

            return trimmed.Substring(0, maxLength);
        }
    }
}
