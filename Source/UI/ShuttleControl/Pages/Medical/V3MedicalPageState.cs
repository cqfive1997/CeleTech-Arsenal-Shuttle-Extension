using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPageState
    {
        internal Vector2 PatientScroll;
        internal Vector2 DetailScroll;
        internal Vector2 StatusScroll;
        internal Vector2 MessageScroll;
        internal Vector2 LayoutScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal int SelectedPatientThingID = -1;
        internal string SelectedPatientKey;
    }
}
