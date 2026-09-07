using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainPageInputs
    {
        internal readonly ShuttleControlReadModel ControlModel;
        internal readonly ShuttleCargoSnapshot CargoSnapshot;
        internal readonly ShuttleWeaponBayReadModel WeaponBayModel;
        internal readonly IReadOnlyList<ExternalModuleUIReadModel> ExternalModuleModels;

        internal V3MainPageInputs(
            ShuttleControlReadModel controlModel,
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleWeaponBayReadModel weaponBayModel,
            IReadOnlyList<ExternalModuleUIReadModel> externalModuleModels)
        {
            this.ControlModel = controlModel ?? new ShuttleControlReadModel();
            this.CargoSnapshot = cargoSnapshot ?? new ShuttleCargoSnapshot();
            this.WeaponBayModel = weaponBayModel ?? ShuttleWeaponBayReadModel.Empty;
            this.ExternalModuleModels = externalModuleModels;
        }
    }
}
