using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    /// <summary>
    /// Native V3 owner for ExternalModules scroll and selection state.
    /// </summary>
    internal sealed class V3ExternalModulesPageState
    {
        internal Vector2 ModuleListScroll;
        internal Vector2 PanelScroll;
        internal Vector2 MessageScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal string SelectedModuleInstanceID;
        internal string SelectedRuntimeSystemKey;
    }
}
