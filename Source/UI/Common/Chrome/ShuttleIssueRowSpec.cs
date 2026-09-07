namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome
{
    internal struct ShuttleIssueRowSpec
    {
        internal readonly ShuttleIssueSeverity Severity;
        internal readonly string BadgeLabel;
        internal readonly string CategoryLabel;
        internal readonly string Message;
        internal readonly string Tooltip;
        internal readonly string ActionLabel;
        internal readonly string ActionTooltip;
        internal readonly bool ActionEnabled;

        internal ShuttleIssueRowSpec(
            ShuttleIssueSeverity severity,
            string badgeLabel,
            string message,
            string tooltip)
            : this(
                severity,
                badgeLabel,
                null,
                message,
                tooltip,
                null,
                null,
                false)
        {
        }

        internal ShuttleIssueRowSpec(
            ShuttleIssueSeverity severity,
            string badgeLabel,
            string categoryLabel,
            string message,
            string tooltip,
            string actionLabel,
            string actionTooltip,
            bool actionEnabled)
        {
            this.Severity = severity;
            this.BadgeLabel = badgeLabel;
            this.CategoryLabel = categoryLabel;
            this.Message = message;
            this.Tooltip = tooltip;
            this.ActionLabel = actionLabel;
            this.ActionTooltip = actionTooltip;
            this.ActionEnabled = actionEnabled;
        }
    }
}
