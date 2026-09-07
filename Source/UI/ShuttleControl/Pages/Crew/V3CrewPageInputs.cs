using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleCargoSnapshot CargoSnapshot;
        internal readonly ShuttlePawnPresenceSnapshot PawnPresenceSnapshot;
        internal readonly ShuttlePawnDynamicStatusSnapshot PawnDynamicStatusSnapshot;

        internal V3CrewPageInputs(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.CargoSnapshot = cargoSnapshot ?? new ShuttleCargoSnapshot();
            this.PawnPresenceSnapshot =
                pawnPresenceSnapshot ?? ShuttlePawnPresenceSnapshot.Empty;
            this.PawnDynamicStatusSnapshot =
                pawnDynamicStatusSnapshot ?? ShuttlePawnDynamicStatusSnapshot.Empty;
        }
    }
}
