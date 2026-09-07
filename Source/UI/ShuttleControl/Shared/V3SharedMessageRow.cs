using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3SharedMessageRow
    {
        internal string Severity;
        internal string Category;
        internal string Message;
        internal string Tooltip;
        internal string ActionLabel;
        internal string ActionTooltip;
        internal bool ActionEnabled;
        internal ShuttleIssueActionKind ActionKind;
        internal ShuttleIssueNavigationTarget NavigationTarget;
        internal bool IsNormal;
    }
}
