using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainPageModel
    {
        internal ShuttleControlReadModel ControlModel;
        internal ShuttleCargoSnapshot CargoSnapshot;
        internal ShuttleWeaponBayReadModel WeaponBayModel;
        internal IReadOnlyList<ExternalModuleUIReadModel> ExternalModuleModels;
    }
}
