using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueReadModelFactory
    {
        internal static ShuttleIssueReadModel CreateSimpleIssue(
            string stableId,
            ShuttleIssueSeverity severity,
            ShuttleIssueCategory category,
            string titleKey,
            string detailKey,
            ShuttleIssueActionKind actionKind,
            int sortPriority)
        {
            ShuttleIssueReadModel issue = new ShuttleIssueReadModel();
            issue.StableId = stableId;
            issue.Severity = severity;
            issue.Category = category;
            issue.Title = titleKey.Translate().ToString();
            issue.Detail = detailKey.Translate().ToString();
            issue.ActionKind = actionKind;
            issue.ActionLabel = ShuttleIssueActionResolver.ResolveActionLabel(actionKind);
            issue.NavigationTarget = new ShuttleIssueNavigationTarget();
            issue.SortPriority = sortPriority;
            issue.Evidence = new List<ShuttleIssueEvidenceLine>();
            return issue;
        }
    }
}
