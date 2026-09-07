using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal static class V3CrewIssueUtility
    {
        internal const int SevereHealthPriority = 10;
        internal const int AssignmentPriority = 30;
        internal const int LowNeedPriority = 50;

        internal static void AddIssue(
            List<V3CrewIssueModel> issues,
            string label,
            string summary,
            V3CrewIssueSeverity severity,
            int priority)
        {
            if (issues == null || string.IsNullOrEmpty(label) || HasIssue(issues, label))
            {
                return;
            }

            V3CrewIssueModel issue = new V3CrewIssueModel();
            issue.Label = label;
            issue.Summary = string.IsNullOrEmpty(summary)
                ? ShuttleUIText.Tr("CT_ShuttleCrew_Issue_NoAdditionalExplanation")
                : summary;
            issue.Severity = severity;
            issue.Priority = priority;
            issues.Add(issue);
        }

        internal static int Compare(V3CrewIssueModel left, V3CrewIssueModel right)
        {
            int leftPriority = left != null ? left.Priority : int.MaxValue;
            int rightPriority = right != null ? right.Priority : int.MaxValue;
            int result = leftPriority.CompareTo(rightPriority);
            if (result != 0)
            {
                return result;
            }

            string leftLabel = left != null && !string.IsNullOrEmpty(left.Label)
                ? left.Label
                : string.Empty;
            string rightLabel = right != null && !string.IsNullOrEmpty(right.Label)
                ? right.Label
                : string.Empty;
            return string.CompareOrdinal(leftLabel, rightLabel);
        }

        internal static string BuildIssueTooltip(V3CrewCardModel card)
        {
            if (card == null || card.Issues == null || card.Issues.Count == 0)
            {
                return ShuttleUIText.Tr("CT_ShuttleCrew_NoMajorIssues");
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < card.Issues.Count; i++)
            {
                V3CrewIssueModel issue = card.Issues[i];
                if (issue == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(issue.Label);
                if (!string.IsNullOrEmpty(issue.Summary))
                {
                    builder.Append(": ");
                    builder.Append(issue.Summary);
                }
            }

            return builder.ToString();
        }

        private static bool HasIssue(List<V3CrewIssueModel> issues, string label)
        {
            if (issues == null || string.IsNullOrEmpty(label))
            {
                return false;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                V3CrewIssueModel issue = issues[i];
                if (issue != null &&
                    string.Equals(issue.Label, label, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
