using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs
{
    internal sealed class ShuttlePageInputBuildContext
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleCargoSnapshot CargoSnapshot;
        internal readonly IReadOnlyList<ShuttleCargoRegionReadModel> CargoRegionSettings;
        internal readonly ShuttleWeaponBayReadModel WeaponBayModel;
        internal readonly ShuttleAutoWorkTableReadModel WorkTableModel;
        internal readonly IReadOnlyList<ExternalModuleUIReadModel> ExternalModuleModels;
        internal readonly ShuttlePawnPresenceSnapshot PawnPresenceSnapshot;
        internal readonly ShuttlePawnDynamicStatusSnapshot PawnDynamicStatusSnapshot;
        internal readonly int CargoSnapshotRevision;

        internal ShuttlePageInputBuildContext(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            IReadOnlyList<ShuttleCargoRegionReadModel> cargoRegionSettings,
            ShuttleWeaponBayReadModel weaponBayModel,
            ShuttleAutoWorkTableReadModel workTableModel,
            IReadOnlyList<ExternalModuleUIReadModel> externalModuleModels,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            ShuttlePawnDynamicStatusSnapshot pawnDynamicStatusSnapshot,
            int cargoSnapshotRevision)
        {
            this.ControlModel = controlModel;
            this.CargoSnapshot = cargoSnapshot;
            this.CargoRegionSettings = cargoRegionSettings;
            this.WeaponBayModel = weaponBayModel;
            this.WorkTableModel = workTableModel;
            this.ExternalModuleModels = externalModuleModels;
            this.PawnPresenceSnapshot = pawnPresenceSnapshot;
            this.PawnDynamicStatusSnapshot = pawnDynamicStatusSnapshot;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
        }
    }
}
