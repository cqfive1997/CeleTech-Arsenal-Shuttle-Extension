using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchRuleEvaluationRecord
    {
        internal ExternalSDKLaunchRuleEvaluationRecord(
            ShuttleExternalLaunchRuleProviderInfo providerInfo,
            ShuttleExternalLaunchRuleEvaluationStatus status,
            IEnumerable<ShuttleExternalLaunchRuleIssue> issues,
            string message,
            bool recordsTruncated)
        {
            this.ProviderInfo = providerInfo;
            this.Status = status;
            this.Issues = new List<ShuttleExternalLaunchRuleIssue>();
            if (issues != null)
            {
                foreach (ShuttleExternalLaunchRuleIssue issue in issues)
                {
                    if (issue != null)
                    {
                        this.Issues.Add(issue);
                    }
                }
            }

            this.Message =
                ShuttleExternalLaunchRuleText.ClampOptionalMessage(message);
            this.RecordsTruncated = recordsTruncated;
        }

        internal ShuttleExternalLaunchRuleProviderInfo ProviderInfo { get; private set; }
        internal ShuttleExternalLaunchRuleEvaluationStatus Status { get; private set; }
        internal List<ShuttleExternalLaunchRuleIssue> Issues { get; private set; }
        internal string Message { get; private set; }
        internal bool RecordsTruncated { get; private set; }

        internal int IssueCount
        {
            get
            {
                return this.Issues != null ? this.Issues.Count : 0;
            }
        }

        internal int BlockerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; this.Issues != null && i < this.Issues.Count; i++)
                {
                    if (this.Issues[i] != null && this.Issues[i].BlocksLaunch)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        internal ShuttleExternalLaunchRuleProviderDiagnosticSnapshot
            ToDiagnosticSnapshot()
        {
            return new ShuttleExternalLaunchRuleProviderDiagnosticSnapshot(
                this.ProviderInfo != null ? this.ProviderInfo.ProviderKey : null,
                this.ProviderInfo != null ? this.ProviderInfo.OwnerPackageId : null,
                this.ProviderInfo != null ? this.ProviderInfo.LocalProviderKey : null,
                this.ProviderInfo != null ? this.ProviderInfo.DisplayName : null,
                this.Status,
                this.IssueCount,
                this.BlockerCount,
                this.Message);
        }
    }
}
