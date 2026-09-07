using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPageState
    {
        internal Vector2 HumanCrewScroll;
        internal Vector2 MechCrewScroll;
        internal Vector2 AnimalCrewScroll;
        internal Vector2 EntityCrewScroll;
        internal Vector2 MessageScroll;
        internal Vector2 DetailScroll;
        internal readonly V3SharedMessagePanelState MessagePanelState =
            new V3SharedMessagePanelState();
        internal int SelectedCrewMemberThingID = -1;
        internal string SelectedCrewMemberKey;
    }
}
