using UnityEngine;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    /// <summary>
    /// V3-owned Main shell state for Main page selection, scroll, and focus.
    /// </summary>
    internal sealed class V3MainPageState
    {
        internal Vector2 SegmentScroll;
        internal Vector2 SelectedModuleScroll;
        internal Vector2 MessageScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal string SelectedSegmentSlotID;
        internal string SelectedModuleSlotID;
        internal string OpenMainActionMenuKey;
        internal string FocusedSegmentSlotID;
        internal string FocusedModuleSlotID;
        internal int AssemblyFocusStartTick;
        internal int AssemblyFocusUntilTick;
        internal string SelectedExternalModuleInstanceID;
        internal string SelectedExternalRuntimeSystemKey;
    }
}
