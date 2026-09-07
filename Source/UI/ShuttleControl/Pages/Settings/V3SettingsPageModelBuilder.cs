using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsPageModelBuilder
    {
        private readonly ShuttleSettingsReadModelBuilder settingsBuilder =
            new ShuttleSettingsReadModelBuilder();

        internal void Fill(V3SettingsPageModel model, V3SettingsPageInputs inputs)
        {
            if (model == null)
            {
                return;
            }

            model.ControlModel = inputs != null && inputs.ControlModel != null
                ? inputs.ControlModel
                : new ShuttleControlReadModel();
            model.WeaponBayModel = inputs != null && inputs.WeaponBayModel != null
                ? inputs.WeaponBayModel
                : ShuttleWeaponBayReadModel.Empty;
            model.SettingsModel = this.settingsBuilder.Build();
        }
    }
}
