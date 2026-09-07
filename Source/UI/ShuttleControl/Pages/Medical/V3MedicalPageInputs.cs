using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttlePawnPresenceSnapshot PawnPresenceSnapshot;
        internal readonly ShuttlePawnDynamicStatusSnapshot PawnDynamicStatusSnapshot;

        internal V3MedicalPageInputs(
            ShuttleControlReadModel controlModel,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.PawnPresenceSnapshot =
                pawnPresenceSnapshot ?? ShuttlePawnPresenceSnapshot.Empty;
            this.PawnDynamicStatusSnapshot =
                pawnDynamicStatusSnapshot ?? ShuttlePawnDynamicStatusSnapshot.Empty;
        }
    }
}
