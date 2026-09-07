using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainPageModelBuilder
    {
        private static readonly ExternalModuleUIReadModel[] EmptyExternalModules =
            new ExternalModuleUIReadModel[0];

        internal void Fill(V3MainPageModel model, V3MainPageInputs inputs)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            model.CargoSnapshot = inputs != null && inputs.CargoSnapshot != null
                ? inputs.CargoSnapshot
                : new ShuttleCargoSnapshot();
            model.WeaponBayModel = inputs != null && inputs.WeaponBayModel != null
                ? inputs.WeaponBayModel
                : ShuttleWeaponBayReadModel.Empty;
            model.ExternalModuleModels =
                inputs != null && inputs.ExternalModuleModels != null
                    ? inputs.ExternalModuleModels
                    : EmptyExternalModules;
        }
    }
}
