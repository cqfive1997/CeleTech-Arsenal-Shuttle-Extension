using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleIssueReadModel
    {
        internal string StableId;
        internal ShuttleIssueSeverity Severity;
        internal ShuttleIssueCategory Category;
        internal string Title;
        internal string Detail;
        internal List<ShuttleIssueEvidenceLine> Evidence =
            new List<ShuttleIssueEvidenceLine>();
        internal ShuttleIssueActionKind ActionKind;
        internal string ActionLabel;
        internal ShuttleIssueNavigationTarget NavigationTarget =
            new ShuttleIssueNavigationTarget();
        internal int SortPriority;
    }
}
