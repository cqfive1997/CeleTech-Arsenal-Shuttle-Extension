using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    public sealed class ShuttleAssemblyIntegrityReport
    {
        public const string BlockingPlayerIssueCode = "assembly-topology-corrupted";

        public static readonly ShuttleAssemblyIntegrityReport Empty =
            new ShuttleAssemblyIntegrityReport(new List<ShuttleAssemblyIntegrityIssue>());

        private readonly List<ShuttleAssemblyIntegrityIssue> issues;
        private readonly ShuttleAssemblyIntegrityIssue firstBlockingIssue;
        private readonly bool hasBlockingIssues;

        public ShuttleAssemblyIntegrityReport(List<ShuttleAssemblyIntegrityIssue> issues)
        {
            this.issues = issues ?? new List<ShuttleAssemblyIntegrityIssue>();
            for (int i = 0; i < this.issues.Count; i++)
            {
                ShuttleAssemblyIntegrityIssue issue = this.issues[i];
                if (issue != null && issue.Blocking)
                {
                    this.hasBlockingIssues = true;
                    this.firstBlockingIssue = issue;
                    break;
                }
            }
        }

        public IReadOnlyList<ShuttleAssemblyIntegrityIssue> Issues
        {
            get
            {
                return this.issues;
            }
        }

        public bool HasBlockingIssues
        {
            get
            {
                return this.hasBlockingIssues;
            }
        }

        public ShuttleAssemblyIntegrityIssue FirstBlockingIssue
        {
            get
            {
                return this.firstBlockingIssue;
            }
        }

        public string BuildDeveloperSummary(int maxIssues)
        {
            if (this.issues == null || this.issues.Count == 0)
            {
                return "No assembly integrity issues.";
            }

            if (maxIssues < 1)
            {
                maxIssues = 1;
            }

            string summary = "Assembly integrity issues: " + this.issues.Count;
            int count = this.issues.Count < maxIssues ? this.issues.Count : maxIssues;
            for (int i = 0; i < count; i++)
            {
                ShuttleAssemblyIntegrityIssue issue = this.issues[i];
                if (issue == null)
                {
                    continue;
                }

                summary += "\n- " + (issue.Code ?? "unknown") +
                    " [" + (issue.ReferenceID ?? "-") + "]: " +
                    (issue.DeveloperDetail ?? "-");
            }

            if (this.issues.Count > count)
            {
                summary += "\n- ... " + (this.issues.Count - count) + " more";
            }

            return summary;
        }

        public int BuildStableHash()
        {
            unchecked
            {
                int hash = 31;
                if (this.issues != null)
                {
                    for (int i = 0; i < this.issues.Count; i++)
                    {
                        ShuttleAssemblyIntegrityIssue issue = this.issues[i];
                        if (issue == null)
                        {
                            continue;
                        }

                        hash = hash * 397 ^ SafeHash(issue.Code);
                        hash = hash * 397 ^ SafeHash(issue.ReferenceID);
                        hash = hash * 397 ^ SafeHash(issue.DeveloperDetail);
                    }
                }

                return hash;
            }
        }

        private static int SafeHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }
    }
}
