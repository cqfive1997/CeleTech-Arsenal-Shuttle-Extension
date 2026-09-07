using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    /// <summary>
    /// Native V3 owner for Settings UI scroll state.
    /// </summary>
    internal sealed class V3SettingsPageState
    {
        internal Vector2 MessageScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal Vector2 ConfigLogScroll;
        internal Vector2 UpdateScroll;
        internal Vector2 GuideScroll;
    }
}
