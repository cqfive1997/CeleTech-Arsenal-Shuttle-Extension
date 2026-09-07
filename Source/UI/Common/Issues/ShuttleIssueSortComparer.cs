namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueSortComparer
    {
        internal static int Compare(ShuttleIssueReadModel left, ShuttleIssueReadModel right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int severityCompare = GetSeverityPriority(left.Severity)
                .CompareTo(GetSeverityPriority(right.Severity));
            if (severityCompare != 0)
            {
                return severityCompare;
            }

            int priorityCompare = left.SortPriority.CompareTo(right.SortPriority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            int categoryCompare = left.Category.CompareTo(right.Category);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            return string.CompareOrdinal(left.StableId, right.StableId);
        }

        private static int GetSeverityPriority(ShuttleIssueSeverity severity)
        {
            if (severity == ShuttleIssueSeverity.Blocking)
            {
                return 0;
            }

            if (severity == ShuttleIssueSeverity.Error)
            {
                return 1;
            }

            if (severity == ShuttleIssueSeverity.Warning)
            {
                return 2;
            }

            return 3;
        }
    }
}
