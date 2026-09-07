using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal enum ShuttleSettingsHubCommitMode
    {
        Immediate,
        Explicit
    }

    /// <summary>
    /// Embeddable category content hosted by the unified settings window.
    /// The host owns navigation and footer chrome; each section keeps its own
    /// setting or command boundary.
    /// </summary>
    internal interface IShuttleSettingsHubSection
    {
        ShuttleSettingsHubCommitMode CommitMode { get; }

        bool CanApply { get; }

        bool HasPendingChanges { get; }

        string ApplyDisabledReason { get; }

        void Draw(Rect rect);

        void Reset();

        bool Apply();

        void OnHubClosed();
    }
}
