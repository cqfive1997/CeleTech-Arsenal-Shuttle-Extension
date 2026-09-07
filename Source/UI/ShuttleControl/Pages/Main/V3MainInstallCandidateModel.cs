namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal enum V3MainInstallCandidateStatus
    {
        Available,
        Locked,
        Incompatible,
        Constructing
    }

    internal sealed class V3MainInstallCandidateModel
    {
        internal string Id;
        internal string DisplayLabel;
        internal string DisplayDescription;
        internal string StatusTranslateKey;
        internal string DisabledReason;
        internal string ConstructionCostSummary;
        internal bool IsUnlocked;
        internal bool IsCompatible;
        internal bool CanInstall;
        internal V3MainInstallCandidateStatus Status;
    }
}
