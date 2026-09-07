using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal enum V3PrisonCellOperationMode
    {
        Carry,
        Feed,
        Tend,
        Eject
    }

    internal sealed class V3PrisonCellPageState
    {
        internal Vector2 HeldScroll;
        internal Vector2 CandidateScroll;
        internal Vector2 CarrierScroll;
        internal Vector2 FeederScroll;
        internal Vector2 DoctorScroll;
        internal Vector2 MessageScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal int SelectedCandidateThingID = -1;
        internal int SelectedCarrierThingID = -1;
        internal int SelectedFeederThingID = -1;
        internal int SelectedDoctorThingID = -1;
        internal int SelectedHeldPrisonerThingID = -1;
        internal V3PrisonCellOperationMode SelectedOperationMode = V3PrisonCellOperationMode.Carry;
    }
}
