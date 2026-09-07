namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal enum ShuttleIssueSeverity
    {
        Info,
        Warning,
        Error,
        Blocking
    }

    internal enum ShuttleIssueCategory
    {
        General,
        Cargo,
        Loading,
        Refrigeration,
        Assembly,
        Launch,
        Crew,
        Power,
        Defense,
        Medical,
        PrisonCell
    }

    internal enum ShuttleIssueActionKind
    {
        None,
        OpenCargo,
        OpenLoading,
        OpenAssembly,
        OpenLaunch,
        OpenDefense,
        OpenMedical,
        OpenPrisonCell,
        OpenProcessing,
        OpenExternalModule
    }

    internal enum ShuttleIssueNavigationTargetKind
    {
        None,
        CargoBay,
        SegmentSlot,
        ModuleSlot,
        ExternalRuntime
    }
}
