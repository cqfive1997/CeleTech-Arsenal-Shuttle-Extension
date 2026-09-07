using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal enum V3SharedMessagePanelTab
    {
        CurrentIssues,
        LaunchDiagnostics,
        DeveloperDiagnostics
    }

    internal sealed class V3SharedMessagePanelState
    {
        internal V3SharedMessagePanelTab CurrentTab = V3SharedMessagePanelTab.CurrentIssues;
        internal bool Expanded;
        internal Vector2 LaunchChecklistScroll;
        internal Vector2 DeveloperDiagnosticsScroll;
    }
}
