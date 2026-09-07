namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleIssueNavigationTarget
    {
        internal ShuttleIssueNavigationTarget()
        {
            this.Kind = ShuttleIssueNavigationTargetKind.None;
            this.CargoBayKey = null;
            this.SegmentSlotID = null;
            this.ModuleSlotID = null;
            this.ModuleInstanceID = null;
            this.RuntimeSystemKey = null;
        }

        internal ShuttleIssueNavigationTargetKind Kind;
        internal string CargoBayKey;
        internal string SegmentSlotID;
        internal string ModuleSlotID;
        internal string ModuleInstanceID;
        internal string RuntimeSystemKey;
    }
}
