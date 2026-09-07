using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueReadModelSet
    {
        internal static void AddIssueIfUnique(
            List<ShuttleIssueReadModel> issues,
            ShuttleIssueReadModel issue)
        {
            if (issues == null || issue == null)
            {
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                ShuttleIssueReadModel existing = issues[i];
                if (existing != null && existing.StableId == issue.StableId)
                {
                    return;
                }
            }

            issues.Add(issue);
        }

        internal static bool ContainsStableId(
            List<ShuttleIssueReadModel> issues,
            string stableId)
        {
            if (issues == null || string.IsNullOrEmpty(stableId))
            {
                return false;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                ShuttleIssueReadModel issue = issues[i];
                if (issue != null && issue.StableId == stableId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
