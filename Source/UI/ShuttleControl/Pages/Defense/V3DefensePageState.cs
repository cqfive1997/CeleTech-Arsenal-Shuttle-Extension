using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Native V3 owner for Defense UI state.
    /// </summary>
    internal sealed class V3DefensePageState
    {
        internal Vector2 WeaponScroll;
        internal Vector2 MessageScroll;
        internal Vector2 SelectedWeaponControlScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal string SelectedWeaponId;
        internal float ShieldRangePreview = -1f;
        internal string ShieldRangePreviewModuleID;
        internal float SurfaceShieldRechargeSpeedPreview = -1f;
        internal string SurfaceShieldRechargeSpeedPreviewModuleID;
    }
}
